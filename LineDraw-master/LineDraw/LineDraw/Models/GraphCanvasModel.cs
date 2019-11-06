﻿using LineDraw.Interfaces;
using LineDraw.Misc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LineDraw.Models
{
    /// <summary>
    /// This class implements a canvas model using a graph based
    /// approach to model the canvas.
    /// </summary>
    public class GraphCanvasModel : ICanvasModel
    {
        private readonly ILineCalculatorFactory lineCalculatorFactory;
        public PriorityQueueNode[][] Graph {get; private set;}

        /// <summary>
        /// Create a new instance of this class using the submitted
        /// ILineCalculatorFactory.
        /// </summary>
        /// <param name="lineCalculator"></param>
        public GraphCanvasModel(ILineCalculatorFactory lineCalculatorFactory)
        {
            this.Height = 500;
            this.Width = 500;
            this.lineCalculatorFactory = lineCalculatorFactory;
            this.InitializeGraph();
        }

        #region Properties
        /// <summary>
        /// The canvas height in this model.
        /// </summary>
        public int Height { get; private set; }

        /// <summary>
        /// The canvas width in this model.
        /// </summary>
        public int Width { get; private set; }
        #endregion

        /// <summary>
        /// Clear all lines from the canvas.
        /// </summary>
        public void ClearLines()
        {
            this.Graph = GraphTools<PriorityQueueNode>.CreateGraph(this.Height, this.Width);
        }

        /// <summary>
        /// Add a line between the submitted points on the canvas
        /// without intersecting other lines using submitted pathfinding algorithm.
        /// </summary>
        /// <param name="startPoint">The start point.</param>
        /// <param name="endPoint">The end point.</param>
        /// <param name="algorithm">Path finding algorithm to use.</param>
        /// <returns>Array of <see cref="Point"/> with the resulting line.</returns>
        public Point[] AddLine(Point startPoint, Point endPoint, PathAlgorithm algorithm)
        {
            // Calculate the line
            Point[] line = lineCalculatorFactory.Create(algorithm).CalculateLine(this.Graph, startPoint, endPoint);

            // Add the line to the graph representation of the canvas
            if (line != null)
                foreach (Point point in line)
                    this.Graph[point.X][point.Y].Occupied = true;
            
            return line;
        }

        /// <summary>
        /// Initilize the internal graph to a new instance.
        /// </summary>
        private void InitializeGraph()
        {
            this.Graph = GraphTools<PriorityQueueNode>.CreateGraph(this.Height, this.Width);
        }

        /// <summary>
        /// Checks whether the submitted point is occupied by a line.
        /// </summary>
        /// <param name="point">Point to check</param>
        /// <returns>True if submitted point is occupied by a line in the canvas model</returns>
        public bool IsOccupied(Point point)
        {
            return this.Graph[point.X][point.Y].Occupied;
        }
    }
}
