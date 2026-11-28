using System;
using System.Collections.Generic;
using System.Linq;
using System.Media;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RAWSimO.Hardware.RFID
{
    /// <summary>
    /// CR501E-S150315 RFID Card: iso15693
    /// DLL version 5.0
    /// MasterCom.dll
    /// MasterRD.dll
    /// </summary>
    public class RfidReader
    {
        //Open Serial Port int WINAPI rf_init_com(int port,long baud);
        [DllImport("MasterRD.dll", EntryPoint = "rf_init_com")]
        private static extern int rf_init_com(Int16 port, Int16 baud);

        //Close Serial Port int WINAPI rf_ClosePort();
        [DllImport("MasterRD.dll", EntryPoint = "rf_ClosePort")]
        private static extern int rf_ClosePort();

        [DllImport("MasterRD.dll", EntryPoint = "rf_thr1064_RequestB")]
        private static extern int rf_thr1064_RequestB(
                                byte icdev,
                                byte req_code,
                                byte AFI,
                                byte N,
                                byte[] ATQB
                            );


        [DllImport("MasterRD.dll", EntryPoint = "rf_thr1064_Attrib")]
        private static extern int rf_thr1064_Attrib(
                                byte icdev,
                                byte[] PUPI,
                                byte PARAM3,
                                byte CID,
                                byte[] pData,
                                ref byte pLen
                            );

        [DllImport("MasterRD.dll", EntryPoint = "rf_thr1064_Write")]
        private static extern int rf_thr1064_Write(byte icdev, byte CID, byte page, byte addr, byte[] pData);


        [DllImport("MasterRD.dll", EntryPoint = "rf_thr1064_read")]
        private static extern int rf_thr1064_read(byte icdev, byte page, byte addr, byte[] pData, ref byte len);

        [DllImport("MasterRD.dll", EntryPoint = "ISO15693_Get_System_Information",
            CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
        private static extern int ISO15693_Get_System_Information(
                            byte icdev,
                            byte model,
                            byte[] UID,
                            byte[] Pdata,
                            ref byte pLen);

        [DllImport("MasterRD.dll", EntryPoint = "rf_init_type")]
        private static extern int rf_init_type(byte icdev, byte type);

        //iso15693
        //int (WINAPI* ISO15693_Inventory)(unsigned short icdev,unsigned char *pData,unsigned char *pLen);
        [DllImport("MasterRD.dll", EntryPoint = "ISO15693_Inventory")]
        private static extern int ISO15693_Inventory(
                           byte icdev,
                           byte[] pData,
                           ref byte pLen
                       );

        public bool IsConnected { get; private set; }
        private Thread _thread;

        public RfidReader()
        {
            IsConnected = false;
        }

        public void Connect(string port_name)
        {
            // for example: COM10, we got 10 as sub
            string sub = port_name.Substring(3);
            int com_port = Int32.Parse(sub);

            int state = 0;
            Int16 port = 0;
            Int16 baud = 19200;
            //Int16 baud = 14400;
            port = (Int16)(com_port);
            state = rf_init_com(port, baud);
            string str_msg;

            if (state != 0)
            {
                str_msg = "Error: rf_init_com";
                Console.WriteLine(str_msg);
                return;
            }

            str_msg = "rf_init_com OK";
            Console.WriteLine(str_msg);
            IsConnected = true;

            ThreadStart _threaddelegate = new ThreadStart(ReceiveMsg);
            _thread = new Thread(_threaddelegate);
            _thread.Start();
        }

        public void Disconnect()
        {
            int state = rf_ClosePort();
            string str_msg;

            if (state != 0)
            {
                str_msg = "Error: rf_ClosePort";
                Console.WriteLine(str_msg);
                return;
            }
            str_msg = "rf_ClosePort OK";
            Console.WriteLine(str_msg);
            IsConnected = false;
        }

        public void ReceiveMsg()
        {
            while (IsConnected)
            {
                // scan 20 times per second
                Thread.Sleep(50);
                ReadISO15693Inventory();
            }
        }

        private string _lastTag;
        private string _currentTag = "";
        /// <summary>
        /// The length in characters of one RFID-tag.
        /// </summary>
        public const int TAG_LENGTH = 16;

        public string GetCurrentTag()
        {
            return _currentTag;
        }

        public void ReadISO15693Inventory()
        {
            int status = 0;
            byte icdev = 0;
            byte[] pData = new byte[256];
            byte len = 0;

            status = ISO15693_Inventory(icdev, pData, ref len);

            if (status != 0)
            {
                //string str_msg;
                //str_msg = "Not Found UID: ISO15693_Inventory";
                //Console.WriteLine(str_msg);
                return;
            }

            // Parse the message
            _currentTag = "";
            for (int i = 0; i < 8; i++)
                _currentTag += pData[i + 1].ToString("X2");

            // Check whether the tag is new and not empty
            if (_currentTag != _lastTag && !string.IsNullOrWhiteSpace(_currentTag))
            {
                // Log it
                Console.WriteLine("ISO15693_Inventory UID: {0}", _currentTag);
                // Remember the tag
                _lastTag = _currentTag;
            }
        }
    }
}
