using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RAWSimO.CommFramework
{
    internal class MessageBuffer
    {
        public MessageBuffer(Action<string> messageHandler) { _messageHandler = messageHandler; }

        /// <summary>
        /// The message handler to pass complete messages to.
        /// </summary>
        private Action<string> _messageHandler;
        /// <summary>
        /// The 'buffer' to store incomplete messages in.
        /// </summary>
        private string _buffer;

        public void SubmitFragment(string fragment)
        {
            // Skip empty messages
            if (string.IsNullOrWhiteSpace(fragment))
                return;
            // Check whether a new message is starting
            if (fragment.StartsWith(CommunicationConstants.MSG_PREFIX))
                _buffer = fragment;
            else
                _buffer += fragment;
            // Parse complete messages
            while (_buffer.Contains(CommunicationConstants.MSG_TERMINAL))
            {
                // Remove complete message from buffer
                int lengthOfFirstMessage = _buffer.IndexOf(CommunicationConstants.MSG_TERMINAL) + 1;
                string message = _buffer.Substring(0, lengthOfFirstMessage);
                _buffer = _buffer.Remove(0, lengthOfFirstMessage);
                // Replace special characters
                message = message.Replace(CommunicationConstants.MSG_PREFIX, "");
                message = message.Replace(CommunicationConstants.MSG_TERMINAL, "");
                // Handle the message
                _messageHandler(message);
            }
        }
    }
}
