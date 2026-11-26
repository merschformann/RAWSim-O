using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RAWSimO.Hardware.RobotHardware
{
    /// <summary>
    /// 
    /// </summary>
    public struct Radius
    {
        private readonly int m_iValue;
        public const int Straight = 32768;

        public const int Maximum_Right = -2000;
        public const int Maximum_Left = 2000;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="iValue"></param>
        /// <returns></returns>
        public static implicit operator Radius(int iValue)
        {
            return new Radius(iValue);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="iAngle"></param>
        public Radius(int iAngle)
        {
            if ((iAngle > 2000) & (iAngle != 32768)) { iAngle = 2000; }
            if (iAngle < -2000) { iAngle = -2000; }
            if (iAngle == 0) { iAngle = Straight; }

            this.m_iValue = iAngle;
        }

        /// <summary>
        /// 
        /// </summary>
        public int ToInt
        {
            get
            {
                return this.m_iValue;
            }
        }
    }

    /// <summary>
    /// This structure is used to read and set Roomba's velocity. This structure is designed to be used as a variable.
    /// This structure also serves to keep any assigned variables within the limits of the SCI spec
    /// The limits are: -500mm/s - 500 mm/s
    /// 
    /// </summary>
    /// <example>
    ///  Velocity x = 250; //if the programmer sets this to a value > 500, then x will automatically set itself to 500
    ///  Radius y = -400;
    ///  this.CurrentRoomba.Drive(x, y);
    /// </example>
    public struct Velocity
    {
        private readonly int m_iValue;

        public const int Maximum_Forward = 500;
        public const int Maximum_Reverse = -500;

        public static implicit operator Velocity(int iValue)
        {
            return new Velocity(iValue);
        }
        public Velocity(int iSpeed)
        {
            if (iSpeed > 500) { iSpeed = 500; };
            if (iSpeed < -500) { iSpeed = -500; };

            this.m_iValue = iSpeed;
        }
        public int ToInt
        {
            get
            {
                return this.m_iValue;
            }
        }

    }
}
