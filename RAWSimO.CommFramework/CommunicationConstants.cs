using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RAWSimO.CommFramework
{
    /// <summary>
    /// Constant values used for communication.
    /// </summary>
    public class CommunicationConstants
    {
        /// <summary>
        /// The main delimiter to seperate the command at top level.
        /// </summary>
        public const char MSG_MAIN_DELIMITER = ':';
        /// <summary>
        /// The second level delimiter to seperate parts of the message.
        /// </summary>
        public const char MSG_SUB_DELIMITER = ';';
        /// <summary>
        /// The character every message starts with.
        /// </summary>
        public const string MSG_PREFIX = "<";
        /// <summary>
        /// The character every message ends with.
        /// </summary>
        public const string MSG_TERMINAL = ">";
        /// <summary>
        /// The first message every client should send to the server to establish future communication.
        /// </summary>
        public const string COMM_INITIATION_MSG = "Init";
        /// <summary>
        /// The message indicating that the client wants to terminate the communication.
        /// </summary>
        public const string COMM_DISCONNECT_MSG = "Disconnect";
        /// <summary>
        /// The formatter to use for network communication.
        /// </summary>
        public static readonly CultureInfo FORMATTER = CultureInfo.InvariantCulture;
    }
}
