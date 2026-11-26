using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RAWSimO.Hardware.RobotControl
{
    public class PidCtrl
    {
        public PidCtrl() { Kp = 2; Ki = 6; }

        /// <summary>
        /// Max speed of the robot.
        /// </summary>
        private  int _maxSpeed = 280;
        /// <summary>
        /// This is the speed at which the motors should spin when the robot is perfectly on the line.
        /// </summary>
        private int _baseSpeed = 200;
        /// <summary>
        /// This is the speed at which the motors should spin when the robot is perfectly on the line.
        /// </summary>
        public int BaseSpeed { get { return _baseSpeed; } set
            {
                if (value > 300) _baseSpeed = 300;
                else if (value < 50) _baseSpeed = 50;
                else _baseSpeed = value;
                _maxSpeed = Convert.ToInt32(_baseSpeed * 1.4);
            } }

        // Here the possible combinations represent exact position like:
        // 00100 - On the centre of the line
        // 00001 - To the left of the line
        // 10000 - To the right of the line
        // There will be other possible combinations such as 00110 and 00011 that can provide us data on how far to the right is the robot from the centre of the line(same follows for left). 

        /// <summary>
        /// Kp: experiment to determine this, start by something small that just makes your bot follow the line at a slow speed
        /// </summary>
        public int Kp { get; set; }
        /// <summary>
        /// Ki: experiment to determine this, slowly increase the speeds and adjust this value. ( Note: Kp < Ki) 
        /// </summary>
        public int Ki { get; set; }
        /// <summary>
        /// The current position is assumed to be fixed.
        /// </summary>
        private int currentPos = 0;
        /// <summary>
        /// The error of the previous calculation.
        /// </summary>
        private int previosError = 0;

        #region Performance measuring

        /// <summary>
        /// The number of PID loops completed.
        /// </summary>
        internal int PIDLoopsDone { get; private set; }

        #endregion

        /// <summary>
        /// Set new Kp and Ki values.
        /// </summary>
        /// <param name="newKp">Kp</param>
        /// <param name="newKi">Ki</param>
        public void SetKpKi(int newKp, int newKi) { Kp = newKp; Ki = newKi; }

        /// <summary>
        /// Calculates the correction for the givcen target position.
        /// </summary>
        /// <param name="targetPos">Th position to target.</param>
        /// <returns>The correction to get to the position.</returns>
        public int CalcPid(int targetPos)
        {
            // The algorithm for a PID control for line followers would be something like this:
            // Error = target_pos – current_pos     //calculate error
            // P = Error * Kp                       //error times proportional constant gives P
            // I = I + Error                        //integral stores the accumulated error
            // I = I * Ki                           //calculates the integral value
            // D = Error – Previos_error            //stores change in error to derivate
            // Correction = P + I + D

            int Error = targetPos - currentPos;     //calculate error
            int P = 0;
            P = Error * Kp;                       //error times proportional constant gives P
            int I = 0;
            I = I + Error;                        //integral stores the accumulated error
            I = I * Ki;                           //calculates the integral value
            int D = Error - previosError;        //stores change in error to derivate
            int Correction = P + I + D;

            previosError = Error;
            return Correction;
        }

        /// <summary>
        /// Calculates the target position depending on data received from the camera.
        /// </summary>
        /// <param name="lineDetectList">The result of CV.</param>
        /// <returns>The resulting target position.</returns>
        public int CalcTargetPos(LineRecognitionResult lineDetectList)
        {
            int targetPos = 0;
            int maxAbsTargetPos = 10;
            double middle = 0.5;

            if (lineDetectList.ProminentBlock.X < middle)
                targetPos = (int)-((middle - lineDetectList.ProminentBlock.X) / middle * maxAbsTargetPos);
            else
                targetPos = (int)((lineDetectList.ProminentBlock.X - middle) / middle * maxAbsTargetPos);

            return targetPos;
        }

        /// <summary>
        /// Calculates the new speeds for the wheels depending on the perceived line position.
        /// </summary>
        /// <param name="lineDetectList">The result of CV.</param>
        /// <param name="rightSpeed">The new speed for the right wheel.</param>
        /// <param name="leftSpeed">The new speed for the left wheel.</param>
        public void DoPidLoop(LineRecognitionResult lineDetectList, bool backwardDrive, ref int rightSpeed, ref int leftSpeed)
        {
            // Determine target position by using the CV feedback
            int targetPos = CalcTargetPos(lineDetectList);
            // We have to turn the other way around if we want to drive backwards
            if (backwardDrive)
                targetPos = -targetPos;
            // Calculate corrected motor speeds
            int motorSpeed = CalcPid(targetPos);

            // Measure performance
            PIDLoopsDone++;

            // Init motor speeds
            int rightMotorSpeed = 0;
            int leftMotorSpeed = 0;

            // Check whether we want to go forward or backward
            if (!backwardDrive)
            {
                // Set motor speeds
                rightMotorSpeed = _baseSpeed - motorSpeed;
                leftMotorSpeed = _baseSpeed + motorSpeed;

                // Prevent the motor from going beyond max speed
                if (rightMotorSpeed > _maxSpeed) rightMotorSpeed = _maxSpeed;
                if (leftMotorSpeed > _maxSpeed) leftMotorSpeed = _maxSpeed;
                // Keep the motor speed positive
                if (rightMotorSpeed < 0) rightMotorSpeed = 0;
                if (leftMotorSpeed < 0) leftMotorSpeed = 0;
            }
            else
            {
                // Set motor speeds
                rightMotorSpeed = -(_baseSpeed / 2) + motorSpeed;
                leftMotorSpeed = -(_baseSpeed / 2) - motorSpeed;

                // Prevent the motor from going beyond max speed
                if (Math.Abs(rightMotorSpeed) > _maxSpeed) rightMotorSpeed = -_maxSpeed;
                if (Math.Abs(leftMotorSpeed) > _maxSpeed) leftMotorSpeed = -_maxSpeed;
                // Keep the motor speed negative
                if (rightMotorSpeed > 0) rightMotorSpeed = 0;
                if (leftMotorSpeed > 0) leftMotorSpeed = 0;
            }

            // Now move forward with appropriate speeds!!!!
            rightSpeed = rightMotorSpeed;
            leftSpeed = leftMotorSpeed;
        }

    }
}
