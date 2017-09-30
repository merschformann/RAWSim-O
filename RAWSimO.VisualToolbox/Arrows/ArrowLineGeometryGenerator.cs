using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace RAWSimO.VisualToolbox.Arrows
{
    /// <summary>
    /// Enables the generation of arrow line geometries.
    /// </summary>
    public class ArrowLineGeometryGenerator
    {
        /// <summary>
        /// Generates an arrow geometry. (thanks to <see href="http://www.charlespetzold.com/blog/2007/04/191200.html"/>)
        /// </summary>
        /// <param name="arrowStyle">Defines the arrowheads (start, end, none or both).</param>
        /// <param name="p1">The start point of the arrow.</param>
        /// <param name="p2">The end point of the arrow.</param>
        /// <param name="arrowAngle">The angle of the arrow head.</param>
        /// <param name="arrowLength">The length of the arrow head.</param>
        /// <returns>An arrow geometry.</returns>
        public static Geometry GenerateArrowGeometry(ArrowEnds arrowStyle, Point p1, Point p2, double arrowAngle, double arrowLength)
        {
            // Init
            var pathgeo = new PathGeometry();
            var pathfigLine = new PathFigure();
            var polysegLine = new PolyLineSegment();
            pathfigLine.Segments.Add(polysegLine);
            var pathfigHead1 = new PathFigure();
            var polysegHead1 = new PolyLineSegment();
            pathfigHead1.Segments.Add(polysegHead1);
            var pathfigHead2 = new PathFigure();
            var polysegHead2 = new PolyLineSegment();
            pathfigHead2.Segments.Add(polysegHead2);

            // Define a single PathFigure with the points.
            pathfigLine.StartPoint = p1;
            polysegLine.Points.Clear();
            polysegLine.Points.Add(p2);
            pathgeo.Figures.Add(pathfigLine);

            // Add arrow heads
            int count = polysegLine.Points.Count;
            if (count > 0)
            {
                // Draw the arrow at the start of the line.
                if ((arrowStyle & ArrowEnds.Start) == ArrowEnds.Start)
                {
                    Point pt1 = pathfigLine.StartPoint;
                    Point pt2 = polysegLine.Points[0];
                    pathgeo.Figures.Add(CalculateArrow(pathfigHead1, pt2, pt1, arrowAngle, arrowLength));
                }

                // Draw the arrow at the end of the line.
                if ((arrowStyle & ArrowEnds.End) == ArrowEnds.End)
                {
                    Point pt1 = count == 1 ? pathfigLine.StartPoint : polysegLine.Points[count - 2];
                    Point pt2 = polysegLine.Points[count - 1];
                    pathgeo.Figures.Add(CalculateArrow(pathfigHead2, pt1, pt2, arrowAngle, arrowLength));
                }
            }
            return pathgeo;
        }

        /// <summary>
        /// Calculates an arrow head for a line segment.
        /// </summary>
        /// <param name="pathfig">The path to add the arrow to.</param>
        /// <param name="pt1">The first point of the line this arrow is added to.</param>
        /// <param name="pt2">The second point of the line this arrow is added to.</param>
        /// <param name="arrowAngle">The angle of the arrow head.</param>
        /// <param name="arrowLength">The length of the arrow head.</param>
        /// <returns>The path figure with the arrow head attached.</returns>
        private static PathFigure CalculateArrow(PathFigure pathfig, Point pt1, Point pt2, double arrowAngle, double arrowLength)
        {
            Matrix matx = new Matrix();
            Vector vect = pt1 - pt2;
            vect.Normalize();
            vect *= arrowLength;

            PolyLineSegment polyseg = pathfig.Segments[0] as PolyLineSegment;
            polyseg.Points.Clear();
            matx.Rotate(arrowAngle / 2);
            pathfig.StartPoint = pt2 + vect * matx;
            polyseg.Points.Add(pt2);

            matx.Rotate(-arrowAngle);
            polyseg.Points.Add(pt2 + vect * matx);

            return pathfig;
        }

        /// <summary>
        /// Simply calculates the distance between two points.
        /// </summary>
        /// <param name="p1">The first point.</param>
        /// <param name="p2">The second point.</param>
        /// <returns>The distance between the two points.</returns>
        public static double GetDistanceBetweenPoints(Point p1, Point p2)
        {
            double a = p1.X - p2.X;
            double b = p1.Y - p2.Y;
            double distance = Math.Sqrt(a * a + b * b);
            return distance;
        }
    }
}
