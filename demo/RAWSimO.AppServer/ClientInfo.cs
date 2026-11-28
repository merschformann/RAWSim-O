using RAWSimO.CommFramework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RAWSimO.AppServer
{
    /// <summary>
    /// Contains quick information about a connected client.
    /// </summary>
    internal class ClientInfo
    {
        /// <summary>
        /// The type of the client.
        /// </summary>
        public ClientType Type { get; set; }
        /// <summary>
        /// The ID of the client.
        /// </summary>
        public int ID { get; set; }
    }
}
