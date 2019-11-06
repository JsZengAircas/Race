﻿using LineDraw.External;
using LineDraw.Interfaces;
using LineDraw.Models;
using Microsoft.Practices.Prism.Commands;
using Microsoft.Practices.Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Point = LineDraw.Models.Point;
using Size = LineDraw.Models.Size;

namespace LineDraw.Canvas
{
    /// <summary>
    /// This class implements the viewmodel for <see cref="CanvasView"/>.
    /// </summary>
    public class CanvasViewModel : BindableBase
    {
        private readonly ILineService lineService;
        private ICommand selectPointCommand;
        private ICommand clearLinesCommand;
        private ICommand applicationCloseCommand;

        private Point endPoint;
        private Point startPoint;
        private string errorMessage;
        private string timeMessage;
        private string state = CanvasState.ReadyState;
        private PathAlgorithm pathAlgorithm;
        private NotifyTaskCompletion<LineQueryResult> addLineTask;
        private CancellationTokenSource cancelToken = new CancellationTokenSource();

        #region Properties
        /// <summary>
        /// Lines to draw on the canvas.
        /// </summary>
        public ObservableCollection<Point[]> Lines { get; set; }

        /// <summary>
        /// The height of the clickable area.
        /// </summary>
        public int CanvasHeight { get; set; }

        /// <summary>
        /// The width of the clickable area.
        /// </summary>
        public int CanvasWidth { get; set; }

        /// <summary>
        /// Start point for user selected line
        /// </summary>
        public Point StartPoint
        {
            get { return this.startPoint; }
            set { this.SetProperty(ref this.startPoint, value); }
        }

        /// <summary>
        /// End point for user selected line
        /// </summary>
        public Point EndPoint
        {
            get { return this.endPoint; }
            set { SetProperty(ref this.endPoint, value); }
        }

        /// <summary>
        /// Pathfinding algorithm to use when computing lines.
        /// </summary>
        public PathAlgorithm PathAlgorithm
        {
            get { return this.pathAlgorithm; }
            set { SetProperty(ref this.pathAlgorithm, value); }
        }

        /// <summary>
        /// Command object for selecting line points.
        /// </summary>
        public ICommand SelectPointCommand
        {
            get
            {
                if (selectPointCommand == null)
                    selectPointCommand = new RelayCommand(param => SelectPoint((MouseEventArgs)param),
                        _ => (this.AddLineTask != null && this.AddLineTask.IsCompleted) || this.AddLineTask == null);
                return selectPointCommand;
            }
        }

        /// <summary>
        /// Command object for clearing lines.
        /// </summary>
        public ICommand ClearLinesCommand
        {
            get
            {
                if (clearLinesCommand == null)
                    clearLinesCommand = new DelegateCommand(ClearLines);
                return clearLinesCommand;
            }
        }

        /// <summary>
        /// Command object for closeing application.
        /// </summary>
        public ICommand ApplicationCloseCommand
        {
            get
            {
                if (applicationCloseCommand == null)
                    applicationCloseCommand = new DelegateCommand(ApplicationClose);
                return applicationCloseCommand;
            }
        }

        /// <summary>
        /// Field for error messages.
        /// </summary>
        public string ErrorMessage
        {
            get { return this.errorMessage; }
            set { SetProperty(ref this.errorMessage, value); }
        }
        
        /// <summary>
        /// Field for reporting time consumption of the line computation.
        /// </summary>
        public string TimeMessage
        {
            get { return this.timeMessage; }
            set { SetProperty(ref this.timeMessage, value); }
        }

        /// <summary>
        /// Field for reporting the state of the canvas; Busy, Error, Ready.
        /// </summary>
        public string State
        {
            get { return this.state; }
            set { SetProperty(ref this.state, value); }
        }

        /// <summary>
        /// Get the Task wrapper for the asynchronous 
        /// add line operation.
        /// </summary>
        public NotifyTaskCompletion<LineQueryResult> AddLineTask
        {
            get { return this.addLineTask; }
            private set
            {
                SetProperty(ref this.addLineTask, value);
            }
        }

        #endregion

        /// <summary>
        /// Instantiates a new instance of this class.
        /// </summary>
        /// <param name="lineService">Service to use for computing lines.</param>
        public CanvasViewModel(ILineService lineService)
        {
            if (lineService == null)
                new ArgumentNullException("lineService");
            this.lineService = lineService;
            Size canvasSize = this.lineService.GetCanvasSize();
            this.CanvasHeight = canvasSize.Height - 1;
            this.CanvasWidth = canvasSize.Width - 1;
            this.Lines = new ObservableCollection<Point[]>();
            // Default pathfinding algorithm
            this.PathAlgorithm = PathAlgorithm.AStar;
            // Default message
        }

        /// <summary>
        /// Event handler for mouse click event.
        /// </summary>
        /// <param name="e"></param>
        private async Task SelectPoint(MouseEventArgs e)
        {
            // Clear error message
            this.ErrorMessage = null;

            // Get the x.y position of the mouse event.
            int mouseX = (int)e.GetPosition((IInputElement)e.Source).X;
            int mouseY = (int)e.GetPosition((IInputElement)e.Source).Y;

            // If start point has not been set the mouse event sets it.            
            if (StartPoint == null)
            {
                PointQueryResult query = this.lineService.SelectPoint(new Point { X = mouseX, Y = mouseY });

                if (query.Success)
                    StartPoint = query.Result;
                else
                    ErrorMessage = query.Message;
            }
            // Else the mouse event sets the end point.
            else
            {
                PointQueryResult query = this.lineService.SelectPoint(new Point { X = mouseX, Y = mouseY });

                if (query.Success)
                    EndPoint = query.Result;
                else
                    ErrorMessage = query.Message;
            }

            // Start and end point has been set, ready to compute line.
            if(StartPoint != null && EndPoint != null)
            {                
                await AddLine(StartPoint, EndPoint);

                // Reset start and end point for the next line.
                StartPoint = null;
                EndPoint = null;
            }
        }

        /// <summary>
        /// Calls the ILineService with submitted points.
        /// </summary>
        private async Task AddLine(Point startPoint, Point endPoint)
        {
            // Clear error message
            this.State = CanvasState.BusyState;
            this.ErrorMessage = null;
            this.TimeMessage = null;            

            // Query the ILineService with selected start and end points.
            this.AddLineTask = new NotifyTaskCompletion<LineQueryResult>(
                this.lineService.AddLineAsync(startPoint, endPoint, PathAlgorithm, cancelToken.Token));
            LineQueryResult result = await this.AddLineTask.Task;

            // Process the query result.
            if (this.AddLineTask.IsSuccessfullyCompleted && result.Success)
            {
                this.Lines.Add(result.Result);
                this.TimeMessage = result.Time + " MS";
                this.State = CanvasState.ReadyState;
            }
            else
            {
                this.ErrorMessage = result.Message;
                this.State = CanvasState.ErrorState;
            }
        }

        /// <summary>
        /// Clear lines and points on the canvas.
        /// </summary>
        private void ClearLines()
        {
            // Clear lines in the model
            this.lineService.ClearLines();
            // Clear lines in viewmodel
            this.Lines.Clear();
            // Clear points
            this.StartPoint = null;
            this.EndPoint = null;
            // Clear error message
            this.ErrorMessage = null;
            // Set state
            this.State = CanvasState.ReadyState;
        }

        /// <summary>
        /// Shut down the application.
        /// </summary>
        private void ApplicationClose()
        {
            Application.Current.Shutdown();
        }
    }
}
