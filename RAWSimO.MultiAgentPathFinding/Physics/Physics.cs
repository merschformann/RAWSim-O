using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RAWSimO.MultiAgentPathFinding.Physic
{
    /// <summary>
    /// calculations of moving and rotation
    /// </summary>
    public class Physics
    {
        /// <summary>
        /// Const containing pi times two.
        /// </summary>
        public const double PI2 = Math.PI * 2;

        /// <summary>
        /// Acceleration in m/s^2.
        /// </summary>
        public double Acceleration { get; internal set; }

        /// <summary>
        /// Deceleration in m/s^2.
        /// </summary>
        public double Deceleration { get; internal set; }

        /// <summary>
        /// The maximal possible speed in m/s.
        /// </summary>
        public double MaxSpeed { get; internal set; }

        /// <summary>
        /// The speed of the robot for turning on the spot.
        /// <remarks>The unit of measure is the simulation time it takes for a full 2*PI turn.</remarks>
        /// </summary>
        public double TurnSpeed { get; internal set; }

        /// <summary>
        /// The time needed to break from full speed to zero
        /// </summary>
        private double _timeToBreakFromFullSpeedToZero;

        /// <summary>
        /// The time needed to break from full speed to zero
        /// </summary>
        private double _timeToAccelerateFromZeroToFullSpeed;

        /// <summary>
        /// The travel distance needed to break from full speed to zero
        /// </summary>
        private double _travelDistanceFromFullSpeedToZero;

        /// <summary>
        /// The travel distance needed to break from full speed to zero
        /// </summary>
        private bool _calledTimeNeededToMove;

        /// <summary>
        /// Acceleration Duration
        /// </summary>
        private double _accelerationDuration;

        /// <summary>
        /// Full speed Duration
        /// </summary>
        private double _fullSpeedDuration;

        /// <summary>
        /// Deceleration Duration
        /// </summary>
        private double _decelerationDuration;

        /// <summary>
        /// Acceleration Distance
        /// </summary>
        private double _accelerationDistance;

        /// <summary>
        /// Full speed Distance
        /// </summary>
        private double _fullSpeedDistance;

        /// <summary>
        /// Deceleration Distance
        /// </summary>
        private double _decelerationDistance;

        /// <summary>
        /// top speed
        /// </summary>
        private double _topSpeed;

        /// <summary>
        /// Initializes a new instance of the <see cref="Physics"/> class.
        /// </summary>
        /// <param name="acceleration">The acceleration in m/s^2.</param>
        /// <param name="deceleration">The deceleration in m/s^2.</param>
        /// <param name="maxSpeed">The maximum speed in m/s.</param>
        /// <param name="turnSpeed">The turn speed in time per turn.</param>
        public Physics(double acceleration, double deceleration, double maxSpeed, double turnSpeed)
        {
            Acceleration = acceleration;
            Deceleration = deceleration;
            MaxSpeed = maxSpeed;
            TurnSpeed = turnSpeed;

            //Acceleration Formula: a = (v1 - v2)/(t1 - t2) where a is acceleration, v is speed, t is time
            //Position after timespan t: st = (a * t * t) / 2 + v0 * t + s0 where st is position at t, a is acceleration, t is timespan, v0 is velocity at t0, s0 is position at t0
            _timeToBreakFromFullSpeedToZero = (-1.0) * ((maxSpeed - 0) / ((-1.0) * deceleration)) + 0.0;
            _travelDistanceFromFullSpeedToZero = (deceleration * _timeToBreakFromFullSpeedToZero * _timeToBreakFromFullSpeedToZero) / 2;

            _timeToAccelerateFromZeroToFullSpeed = maxSpeed / acceleration;

            _calledTimeNeededToMove = false;
        }

        /// <summary>
        /// Gets the time needed to break from full speed to zero.
        /// </summary>
        /// <returns></returns>
        public double getTimeNeededFromFullSpeedToZero()
        {
            return _timeToBreakFromFullSpeedToZero;
        }

        /// <summary>
        /// Gets the time needed to accelerate from zero to full speed.
        /// </summary>
        /// <returns></returns>
        public double getTimeNeededFromZeroToFullSpeed()
        {
            return _timeToAccelerateFromZeroToFullSpeed;
        }

        /// <summary>
        /// Time needed to reach the destination.
        /// </summary>
        /// <param name="speed">The current speed.</param>
        /// <param name="distanceToDestination">The distance to destination.</param>
        /// <returns>time needed to reach the destination</returns>
        public double getTimeNeededToMove(double currentSpeed, double currentTime, double distanceToDestination, List<double> checkPointDistances, out List<double> checkPointTimes)
        {
            checkPointTimes = new List<double>();

            var timeNeededToMove = getTimeNeededToMove(currentSpeed, distanceToDestination);

            foreach (var distance in checkPointDistances)
            {
                //because it is faster and saver due to rounding errors
                if (distance == distanceToDestination)
                {
                    checkPointTimes.Add(currentTime + timeNeededToMove);
                    continue;
                }

                var timeStamp = 0.0;

                if (distance <= _accelerationDistance)
                {
                    //accelerate
                    //t = (sqrt(2as+v^2) - v)/a with s = distance
                    timeStamp = (Math.Sqrt(2 * Acceleration * distance + currentSpeed * currentSpeed) - currentSpeed) / Acceleration;
                }
                else if (distance <= _accelerationDistance + _fullSpeedDistance)
                {
                    //accelerate + full speed
                    //t = (sqrt(2as+v^2) - v)/a with s = distance
                    timeStamp = Math.Sqrt(2 * _accelerationDistance / Acceleration + (currentSpeed / Acceleration) * (currentSpeed / Acceleration)) - currentSpeed / Acceleration;
                    timeStamp += (distance - _accelerationDistance) / MaxSpeed;
                }
                else
                {
                    //accelerate + full speed + decelerate
                    //t = (sqrt(2as+v^2) - v)/a or [t = (sqrt(2as+v^2) + v)/-a] with s = distance
                    timeStamp = (Math.Sqrt(2 * Acceleration * _accelerationDistance + currentSpeed * currentSpeed) - currentSpeed) / Acceleration;
                    timeStamp += (_fullSpeedDistance) / MaxSpeed;
                    timeStamp += (Math.Sqrt(2 * ((-1) * Deceleration) * (distance - _fullSpeedDistance - _accelerationDistance) + MaxSpeed * MaxSpeed) - MaxSpeed) / ((-1) * Deceleration);
                }

                checkPointTimes.Add(currentTime + timeStamp);
            }

            return timeNeededToMove;
        }
        /// <summary>
        /// Time needed to reach the destination.
        /// </summary>
        /// <param name="speed">The current speed.</param>
        /// <param name="distanceToDestination">The distance to destination.</param>
        /// <returns>time needed to reach the destination</returns>
        public double getTimeNeededToMove(double currentSpeed, double distanceToDestination)
        {
            _calledTimeNeededToMove = true;

            // Agenda: First accelerate, then full speed, then break.
            //
            // speed
            // ^
            // |   _____________
            // |  /             \
            // | /               \
            // |/                 \
            // +-------------------X--> distance or time
            // |--|                     accelerationDuration  / acceleartionDistance
            //    |-------------|       fullspeedDuration     / fullspeedDistance
            //                  |--|    decelearationDuration / decelearationDistance

            // Situation 1: Only Break is necessary
            //
            // speed
            // ^
            // |
            // |
            // |\
            // | \
            // +--X-------------------> distance or time

            //t2 = (v2 - v1) / a + t1
            var timeToBreakFromCurrentSpeedToZero = ((0 - currentSpeed) / ((-1.0) * Deceleration)) + 0.0;
            var travelDistanceFromCurrentSpeedToZero = (Deceleration * timeToBreakFromCurrentSpeedToZero * timeToBreakFromCurrentSpeedToZero) / 2;

            if (travelDistanceFromCurrentSpeedToZero >= distanceToDestination)
            {
                _accelerationDuration = _accelerationDistance = 0.0;
                _fullSpeedDuration = _fullSpeedDistance = 0.0;
                _decelerationDuration = timeToBreakFromCurrentSpeedToZero;
                _decelerationDistance = travelDistanceFromCurrentSpeedToZero;
                _topSpeed = currentSpeed;
                return _accelerationDuration + _fullSpeedDuration + _decelerationDuration;
            }

            // Situation 2: Drive full speed and then stop
            //
            // speed
            // ^
            // |__________
            // |           \
            // |            \
            // |             \
            // +--------------X-------> distance or time

            if (currentSpeed == MaxSpeed)
            {
                _accelerationDuration = _accelerationDistance = 0.0;
                _fullSpeedDistance = distanceToDestination - _travelDistanceFromFullSpeedToZero;
                _fullSpeedDuration = _fullSpeedDistance / MaxSpeed;
                _decelerationDistance = _travelDistanceFromFullSpeedToZero;
                _decelerationDuration = _timeToBreakFromFullSpeedToZero;
                _topSpeed = MaxSpeed;
                return _accelerationDuration + _fullSpeedDuration + _decelerationDuration;
            }

            // Situation 3: First accelerate, then full speed, then break
            //
            // speed
            // ^
            // |   _____________
            // |  /             \
            // | /               \
            // |/                 \
            // +-------------------X--> distance or time

            //t2 = - (v1 - v2) / a + t1
            var timeUntilFullAcceleartion = (-1.0) * ((currentSpeed - MaxSpeed) / Acceleration) + 0.0;
            var travelDistanceUntilFullAccelartion = (Acceleration * timeUntilFullAcceleartion * timeUntilFullAcceleartion) / 2 + currentSpeed * timeUntilFullAcceleartion;

            if (travelDistanceUntilFullAccelartion + _travelDistanceFromFullSpeedToZero <= distanceToDestination)
            {
                _accelerationDistance = travelDistanceUntilFullAccelartion;
                _accelerationDuration = timeUntilFullAcceleartion;
                _fullSpeedDistance = distanceToDestination - travelDistanceUntilFullAccelartion - _travelDistanceFromFullSpeedToZero;
                _fullSpeedDuration = _fullSpeedDistance / MaxSpeed;
                _decelerationDistance = _travelDistanceFromFullSpeedToZero;
                _decelerationDuration = _timeToBreakFromFullSpeedToZero;
                _topSpeed = MaxSpeed;
                return _accelerationDuration + _fullSpeedDuration + _decelerationDuration;
            }

            // Situation 4: No full acceleration possible
            //
            // speed
            // ^
            // |   
            // | 
            // |/\
            // |  \
            // +---X------------------> distance or time

            //Question 1: How to calculate the Distance, if the agent has speed 0?
            //Answer:     s = af/2 * t1^2 + ab/2 * t2^2; af*t1=ab*t2 (peek) known: d = distance, af = acceleration, ab = deceleration
            //        <=> d = af/2 * t1^2 + ab/2 * t2^2 and t2=af*t1/ab
            //        <=> d = af/2 * t1^2 + ab/2 * (af*t1/ab)^2
            //        <=> d = af/2 * t1^2 + af*t1^2/(2*ab)
            //        <=> t1^2 = d/(af/2 + af/(2*ab))
            //        <=> t1 = sqrt(d/(af/2 + af/(2*ab)))
            //        => t = t1 + t2 = sqrt(d/(af/2 + af/(2*ab))) + sqrt(d/(ab/2 + ab/(2*af)))
            //
            //        with current speed > 0 => t' = t - tx and d' = d + af/2 * tx^2
            //
            // 0 < currentSpeed < MaxSpeed is untested
            var tx = currentSpeed / Acceleration;
            _accelerationDuration = Math.Sqrt((distanceToDestination + (Acceleration / 2) * tx * tx) / (Acceleration / 2.0 + (Acceleration * Acceleration) / (2.0 * Deceleration))) - tx;
            _accelerationDistance = (Acceleration * _accelerationDuration * _accelerationDuration) / 2 + currentSpeed * _accelerationDuration;
            _fullSpeedDuration = _fullSpeedDistance = 0.0; //no full speed
            _decelerationDuration = Math.Sqrt((distanceToDestination + (Acceleration / 2) * tx * tx) / (Deceleration / 2.0 + (Deceleration * Deceleration) / (2.0 * Acceleration)));
            _decelerationDistance = distanceToDestination - _accelerationDistance;
            _topSpeed = Acceleration * _accelerationDuration + currentSpeed;
            return _accelerationDuration + _fullSpeedDuration + _decelerationDuration;
        }

        /// <summary>
        /// Traveled distance after time step. Call timeNeededToMove first
        /// </summary>
        /// <param name="currentOrientation">The current orientation.</param>
        /// <param name="targetOrientation">The target orientation.</param>
        /// <param name="timeSpan">The time span.</param>
        /// <returns>Orientation after time step</returns>
        public void GetDistanceTraveledAfterTimeStep(double currentSpeed, double timeSpan, out double distanceTraveled, out double newSpeed)
        {
            if (!_calledTimeNeededToMove)
                throw new Exception("Call timeNeededToMove first!");

            if (_accelerationDuration > 0.0)
            {
                //accelerate
                var duration = Math.Min(timeSpan, _accelerationDuration);

                //Position after timespan t: st = (a * t * t) / 2 + v0 * t + s0
                distanceTraveled = (Acceleration * duration * duration) / 2 + currentSpeed * duration;

                //speed after timespan: a = (v1 - v2)/(t1 - t2)
                //<=> a * (t1 - t2) = v1 - v2 <=> v2 = a * (t2 - t1) + v1
                newSpeed = Acceleration * (duration - 0) + currentSpeed;

                //reduce times
                _accelerationDuration = Math.Max(0.0, _accelerationDuration - duration);

                var distanceAfterAccelerationTraveled = 0.0;

                //is there more time to drive?
                if (duration < timeSpan)
                    GetDistanceTraveledAfterTimeStep(newSpeed, timeSpan - duration, out distanceAfterAccelerationTraveled, out newSpeed);

                //add the travel distance after acceleration
                distanceTraveled += distanceAfterAccelerationTraveled;
            }
            else if (_fullSpeedDuration > 0.0)
            {
                //full speed
                var duration = Math.Min(timeSpan, _fullSpeedDuration);

                //Position after timespan t: st = (a * t * t) / 2 + v0 * t + s0
                distanceTraveled = MaxSpeed * duration;

                //speed
                newSpeed = MaxSpeed;

                //reduce times
                _fullSpeedDuration = Math.Max(0.0, _fullSpeedDuration - duration);

                var distanceAfterFullSpeedTraveled = 0.0;

                //is there more time to drive?
                if (duration < timeSpan)
                    GetDistanceTraveledAfterTimeStep(newSpeed, timeSpan - duration, out distanceAfterFullSpeedTraveled, out newSpeed);

                //add the travel distance after acceleration
                distanceTraveled += distanceAfterFullSpeedTraveled;
            }
            else
            {
                //decelerate
                var duration = Math.Min(timeSpan, _decelerationDuration);

                //Position after timespan t: st = (a * t * t) / 2 + v0 * t + s0
                distanceTraveled = ((-1) * Deceleration * duration * duration) / 2 + currentSpeed * duration;

                //speed after timespan: a = (v1 - v2)/(t1 - t2)
                //<=> a * (t1 - t2) = v1 - v2 <=> v2 = a * (t2 - t1) + v1
                newSpeed = (-1) * Deceleration * (duration - 0) + currentSpeed;

                //reduce times
                _decelerationDuration = Math.Max(0.0, _decelerationDuration - duration);

            }

            _calledTimeNeededToMove = false;

        }

        /// <summary>
        /// Time needed to turn in the target orientation.
        /// </summary>
        /// <param name="currentOrientation">The current orientation.</param>
        /// <param name="TargetOrientation">The target orientation.</param>
        /// <returns>time needed to turn</returns>
        public double getTimeNeededToTurn(double currentOrientation, double targetOrientation)
        {
            //difference clockwise
            return Math.Abs(_getOrientationDifference(currentOrientation, targetOrientation)) / PI2 * TurnSpeed;
        }

        /// <summary>
        /// Orientation after time step.
        /// </summary>
        /// <param name="currentOrientation">The current orientation.</param>
        /// <param name="targetOrientation">The target orientation.</param>
        /// <param name="timeSpan">The time span.</param>
        /// <returns>Orientation after time step</returns>
        public double getOrientationAfterTimeStep(double currentOrientation, double targetOrientation, double timeSpan)
        {
            //orientation parameter
            double orientationAfterTimeStep;
            double orientationChange = timeSpan * (PI2 / TurnSpeed);
            double orientationDifference = _getOrientationDifference(currentOrientation, targetOrientation);

            //do not turn over
            orientationChange = Math.Min(Math.Abs(orientationDifference), orientationChange);

            //check the rotation direction
            if (_getOrientationDifference(currentOrientation, targetOrientation) < 0)
                orientationAfterTimeStep = currentOrientation - orientationChange; //turn counter clockwise
            else
                orientationAfterTimeStep = currentOrientation + orientationChange; //turn clockwise

            return orientationAfterTimeStep;
        }

        /// <summary>
        /// Returns the difference of two angles, positive if angle2 is to the left of angle1, negative if angle2 is to the right of angle1.
        /// </summary>
        /// <param name="currentOrientation">The first angle.</param>
        /// <param name="targetOrientation">The second angle.</param>
        /// <returns>Difference of angle2-angle1, normalized to [0,PI].</returns>
        private static double _getOrientationDifference(double currentOrientation, double targetOrientation)
        {
            //difference clockwise
            double difference;
            if (targetOrientation > currentOrientation)
                difference = targetOrientation - currentOrientation;
            else
                difference = targetOrientation - currentOrientation + PI2;


            if (difference < Math.PI)
                return difference; //difference clockwise
            else
                return difference - PI2; //difference counter clockwise
        }

        /// <summary>
        /// Gets the distance needed to stop.
        /// </summary>
        /// <param name="currentSpeed">The current speed.</param>
        /// <returns>The distance needed to stop.</returns>
        public double getDistanceToStop(double currentSpeed)
        {
            //t2 = (v2 - v1) / a + t1
            var timeToBreakFromCurrentSpeedToZero = ((0 - currentSpeed) / ((-1.0) * Deceleration)) + 0.0;
            //st = (a*t*t) / 2;
            return (Deceleration * timeToBreakFromCurrentSpeedToZero * timeToBreakFromCurrentSpeedToZero) / 2;
        }


        /// <summary>
        /// Gets the distance needed to accelerate to full speed.
        /// </summary>
        /// <param name="currentSpeed">The current speed.</param>
        /// <returns>The distance needed to stop.</returns>
        public double getDistanceToFullSpeed(double currentSpeed)
        {
            //t2 = (v2 - v1) / a + t1
            var timeUntilFullAcceleartion = (-1.0) * ((currentSpeed - MaxSpeed) / Acceleration) + 0.0;
            //st = (a*t*t) / 2;
            return (Acceleration * timeUntilFullAcceleartion * timeUntilFullAcceleartion) / 2 + currentSpeed * timeUntilFullAcceleartion;
        }

        /// <summary>
        /// Maximum speed to break within distance.
        /// </summary>
        /// <param name="distance">The distance.</param>
        /// <returns>The speed.</returns>
        public double maxSpeedToBreakWithinDistance(double distance)
        {
            var timeToBreak = Math.Sqrt(2 * distance / Deceleration);
            return Deceleration * timeToBreak;
        }
    }
}
