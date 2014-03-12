using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IrcConnector.IrcTools
{
    public struct IrcMessage
    {

        #region variables
        private string _userName;
        private string _channel;
        private string _message;
        private bool _privateMessage;
        #endregion

        #region getters/settters
        public string UserName
        {
            get { return _userName; }
            set { _userName = value; }
        }

        public String Message
        {
            get { return _message; }
            set { _message = value; }
        }
        

        public String Channel
        {
            get { return _channel; }
            set { _channel = value; }
        }
        public bool PrivateMessage
        {
            get { return _privateMessage; }
            set { _privateMessage = value; }
        }
        #endregion

        public IrcMessage(IrcMessageParser parser)
        {
            string prefix = parser.Prefix;
            string destination = parser.Arguments[0];
            if (destination.StartsWith("#"))
            {
                _privateMessage = false;
                _userName = prefix.Substring(0, prefix.IndexOf("!"));
                _channel = parser.Arguments[0];
            }
            else
            {
                // In private message, username and channel is one and the same
                _privateMessage = true;
                _channel = prefix.Substring(0, prefix.IndexOf("!"));
                _userName = _channel;
            }

            _message = parser.Trailing;
        }
    }
}
