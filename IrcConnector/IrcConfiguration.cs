using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IrcConnector
{
    /// <summary>
    /// Struct for holding configuration parameters
    /// </summary>
    public struct IrcConfiguration
    {
        #region configuration parameters
        public static int DEFAULT_PORT = 6667;
        private String _server;
        private String _channel;
        private String _channelPassword;
        private String _serverPassword;
        private String _realName;
        private String _nickName;
        private int _port;
        private bool _ssl;
        #endregion
        
        #region getters/Setter

        public String RealName
        {
            get {
                if (null == _realName)
                    return "*";
                else
                    return _realName;
                }

            set { _realName = value; }
        }            

        public String NickName
        {
            get { return _nickName; }
            set { _nickName = value; }
        }

        public String ServerPassword
        {
            get { return _serverPassword; }
            set { _serverPassword = value; }
        }
        public String ChannelPassword
        {
            get {
                if (null == _channelPassword)
                {
                    return "";
                }
                else
                {
                    return _channelPassword;
                }
            }
            set { _channelPassword = value; }
        }
        public String Server
        {
            get { return _server; }
            set { _server = value; }
        }
        public String Channel
        {
            get
            { return _channel; }
            set
            {
                // Channel name must always start with #
                if (value.StartsWith("#"))
                    _channel = value;
                else
                    _channel = "#" + value;
            }
        }

        public int Port
        {
            get {
                if (_port == 0)
                    return DEFAULT_PORT;

                return _port; 
            }
            set { _port = value; }
        }

        #endregion

    }
}
