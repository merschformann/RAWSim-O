using RAWSimO.Core.Elements;
using RAWSimO.Core.Info;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace RAWSimO.Core.Geometrics
{
    /// <summary>
    /// The base class for all objects in the simulation that are internally represented as circles.
    /// </summary>
    public abstract class Circle : InstanceElement
    {
        #region Constructor

        /// <summary>
        /// Creates a new circle.
        /// </summary>
        /// <param name="instance">The instance this circle belongs to.</param>
        public Circle(Instance instance) : base(instance) { }

        #endregion

        #region Core

        /// <summary>
        /// States whether there was a change since the last fetch of the information.
        /// </summary>
        protected bool _changed = true;

        /// <summary>
        /// The x-position of the object's center in 2-dimensional space.
        /// </summary>
        private double _x;
        /// <summary>
        /// The x-position of the object's center in 2-dimensional space.
        /// </summary>
        public double X { get { return _x; } internal set { _x = value; _changed = true; Instance.Changed = true; } }

        /// <summary>
        /// The y-position of the object's center in 2-dimensional space.
        /// </summary>
        private double _y;
        /// <summary>
        /// The y-position of the object's center in 2-dimensional space.
        /// </summary>
        public double Y { get { return _y; } internal set { _y = value; _changed = true; Instance.Changed = true; } }

        /// <summary>
        /// The radius of the circle
        /// </summary>
        public double Radius { get; internal set; }

        /// <summary>
        /// The orientation in radians. An element facing east is defined with orientation 0 or equally 2*pi.
        /// </summary>
        private double _orientation;
        /// <summary>
        /// The orientation in radians. An element facing east is defined with orientation 0 or equally 2*pi.
        /// </summary>
        public double Orientation { get { return _orientation; } internal set { _orientation = (value + Math.PI * 2) % (Math.PI * 2); _changed = true; Instance.Changed = true; } }

        /// <summary>
        /// The tier on which the object is (initially) located.
        /// </summary>
        private Tier _tier;
        /// <summary>
        /// The tier on which the object is (initially) located.
        /// </summary>
        public Tier Tier { get { return _tier; } internal set { _tier = value; _changed = true; Instance.Changed = true; } }

        /// <summary>
        /// Indicates whether the object is moving or not.
        /// </summary>
        public bool Moving { get; internal set; }

        /// <summary>
        /// Returns the distance from the circle's center to the specified position.
        /// </summary>
        /// <param name="x">The x-position to measure.</param>
        /// <param name="y">The y-position to measure.</param>
        /// <returns>The distance between the center of this circle and the specified position.</returns>
        public double GetDistance(double x, double y)
        {
            return Math.Sqrt((this.X - x) * (this.X - x) + (this.Y - y) * (this.Y - y));
        }

        /// <summary>
        /// Returns the distance from this circle's center to the center of the other circle.
        /// </summary>
        /// <param name="c">The other circle.</param>
        /// <returns>The distance between the center of this circle and the center of the other circle.</returns>
        public double GetDistance(Circle c)
        {
            return GetDistance(c.X, c.Y);
        }

        /// <summary>
        /// Returns the squared distance from the circles Station center to the specified position.
        /// </summary>
        /// <param name="x">The x-position to measure.</param>
        /// <param name="y">The y-position to measure.</param>
        /// <returns>The squared distance between the center of this circle and the specified position.</returns>
        public double GetSquaredDistance(double x, double y)
        {
            return (this.X - x) * (this.X - x) + (this.Y - y) * (this.Y - y);
        }

        /// <summary>
        /// Returns the distance from this circles Station center to the center of the other circle.
        /// </summary>
        /// <param name="c">The other circle.</param>
        /// <returns>The squared distance between the center of this circle and the center of the other circle.</returns>
        public double GetSquaredDistance(Circle c)
        {
            return GetSquaredDistance(c.X, c.Y);
        }

        /// <summary>
        /// Returns the difference between the given angle and the orientation (angle) of this object.
        /// </summary>
        /// <param name="otherAngle">The other orientation (angle).</param>
        /// <returns>The difference between this orientation and the specified one. Positive if the other is to the left of this one, negative otherwise.</returns>
        public double GetOrientationDifference(double otherAngle)
        {
            return GetOrientationDifference(this.GetInfoOrientation(), otherAngle);

        }

        /// <summary>
        /// Returns the difference of two angles, positive if angle2 is to the left of angle1, negative if angle2 is to the right of angle1.
        /// </summary>
        /// <param name="angle1">The first angle.</param>
        /// <param name="angle2">The second angle.</param>
        /// <returns>Difference of angle2-angle1, normalized to [0,PI].</returns>
        public static double GetOrientationDifference(double angle1, double angle2)
        {
            //difference clockwise
            double difference;
            if (angle2 > angle1)
                difference = angle2 - angle1;
            else
                difference = angle2 - angle1 + Math.PI * 2;


            if (difference < Math.PI)
                return difference; //difference clockwise
            else
                return difference - (Math.PI * 2); //difference counter clockwise
        }

        /// <summary>
        /// Returns the absolute difference of two angles.
        /// </summary>
        /// <param name="angle1">The first angle.</param>
        /// <param name="angle2">The second angle.</param>
        /// <returns>Difference of the angles.</returns>
        public static double GetAbsoluteOrientationDifference(double angle1, double angle2)
        {
            return Math.Min(Math.Abs(angle1 - angle2), (Math.PI * 2) - Math.Abs(angle1 - angle2));
        }

        /// <summary>
        /// Returns the orientation value for a specific object.
        /// Range [0,2*PI]
        /// </summary>
        /// <param name="positionX">object position x</param>
        /// <param name="positionY">object position y</param>
        /// <param name="lookAtX">look at position x</param>
        /// <param name="lookAtY">look at position y</param>
        /// <returns>orientaion value</returns>
        public static double GetOrientation(double positionX, double positionY, double lookAtX, double lookAtY)
        {
            return (Math.Atan2(lookAtY - positionY, lookAtX - positionX) + 2 * Math.PI) % (2 * Math.PI);
        }

        #endregion

        #region Information supply

        /// <summary>
        /// Returns the x-coordinate of the object.
        /// </summary>
        /// <returns>The x-coordinate.</returns>
        public double GetInfoCenterX() { return this.X; }

        /// <summary>
        /// Returns the y-coordinate of the object.
        /// </summary>
        /// <returns>The y-coordinate.</returns>
        public double GetInfoCenterY() { return this.Y; }

        /// <summary>
        /// Returns the x-coordinate of the object.
        /// </summary>
        /// <returns>The x-coordinate.</returns>
        public double GetInfoTLX() { return this.X - this.Radius; }

        /// <summary>
        /// Returns the y-coordinate of the object.
        /// </summary>
        /// <returns>The y-coordinate.</returns>
        public double GetInfoTLY() { return this.Y + this.Radius; }

        /// <summary>
        /// Returns the radius of the object.
        /// </summary>
        /// <returns>The radius.</returns>
        public double GetInfoRadius() { return this.Radius; }

        /// <summary>
        /// Returns the length of the object regarding the x-dimension.
        /// </summary>
        /// <returns>The length.</returns>
        public double GetInfoLength() { return Radius * 2.0; }

        /// <summary>
        /// Returns the width of the object regarding the y-dimension.
        /// </summary>
        /// <returns>The width.</returns>
        public double GetInfoWidth() { return Radius * 2.0; }

        /// <summary>
        /// Returns the orientation of the object.
        /// </summary>
        /// <returns>The orientation.</returns>
        public double GetInfoOrientation() { return this.Orientation; }

        #endregion

        #region Collision detection

        /// <summary>
        /// Checks whether there is a collision given for this element and the other.
        /// </summary>
        /// <param name="c">The other element.</param>
        /// <returns><code>true</code> if the elements collide, <code>false</code> otherwise.</returns>
        public bool IsCollision(Circle c)
        {
            return this != c && IsCollision(c.X, c.Y, c.Radius);
        }

        /// <summary>
        /// Checks whether there is a collision given for this element and the given characteristics of another element.
        /// </summary>
        /// <param name="otherX">The x-value of the position of the other element.</param>
        /// <param name="otherY">The y-value of the position of the other element.</param>
        /// <param name="otherRadius">The radius of the other element.</param>
        /// <returns><code>true</code> if the elements collide, <code>false</code> otherwise.</returns>
        public bool IsCollision(double otherX, double otherY, double otherRadius)
        {
            //return Math.Sqrt(Math.Pow(X - otherX, 2) + Math.Pow(Y - otherY, 2)) <= Radius + otherRadius;
            //return Math.Pow(X - otherX, 2) + Math.Pow(Y - otherY, 2) <= Math.Pow(Radius + otherRadius, 2);
            return (this.X - otherX) * (this.X - otherX) + (this.Y - otherY) * (this.Y - otherY) <= (this.Radius + otherRadius) * (this.Radius + otherRadius);
        }

        #endregion
    }
}
