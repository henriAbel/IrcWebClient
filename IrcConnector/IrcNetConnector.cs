using IrcConnector.IrcTools;
using IrcConnector.NetTools;
using System;
using System.Collections.Generic;
using System.Collections;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Resources;
using System.Reflection;
using System.Timers;

namespace IrcConnector
{   
    /// <summary>
    /// RFC 1459 IRC protocal partial implementation.
    /// http://tools.ietf.org/html/rfc1459.html
    /// </summary>
    public class IrcNetConnector : IIrcProtocol
    {
        #region variables
        private AsyncConnector _connector;
        private IrcConfiguration _config;
        private User _user;
        private List<User> _userList;
        private List<Channel> _connectedChannels;
        private Timer _timer;
        private string topic;
        private List<string> _availableChannels;
        #endregion

        #region getters
        public AsyncConnector Connector
        {
            get { return _connector; }
        }

        public IrcConfiguration Config
        {
            get { return _config; }
            set { _config = value; }
        }

        public User User
        {
            get { return _user; }
            set { _user = value; }
        }
        public List<User> UserList
        {
            get { return _userList; }
        }
        public string Topic
        {
            get { return topic; }
            set { topic = value; }
        }
        public List<Channel> ConnectedChannels
        {
            get { return _connectedChannels; }
            set { _connectedChannels = value; }
        }
        #endregion
       
        static void Main(string[] args)
        {
            // Only for testing purposes
            IrcConfiguration config = new IrcConfiguration();
            config.Server = "irc.kzfv.eu";
            config.Port = 6668;
            config.Channel = "testChannel";
            config.NickName = "keegiMuu";            
            IrcNetConnector irc = new IrcNetConnector(config);
            irc.connect();

            // Prevent application from closing*/
            bool i = true;
            while (i == true)
            {
                string line = Console.ReadLine();
                if (line.Equals("closeme"))
                {
                    i = false;
                }
                else
                {
                    if (line.StartsWith("/"))
                        irc.sendCommand(line.Substring(1));
                    else
                    {
                        IrcMessage m = new IrcMessage();
                        m.Channel = config.Channel;
                        m.Message = line;
                        m.UserName = config.NickName;
                        irc.sendPrivMessage(m);
                    }
                }
               
            }
        }

        /// <summary>
        /// Constructor for irc connector
        /// </summary>
        /// <param name="config"></param>
        public IrcNetConnector(IrcConfiguration config)
        {
            _config = config;
            _user = new User(config.NickName);
            _userList = new List<User>();
            _userList.Add(_user);
            _availableChannels = new List<string>();
            _connectedChannels = new List<Channel>();
            // System messages goes to server tab
            _connectedChannels.Add(new Channel("server"));
            _connector = new AsyncConnector(_config.Server, _config.Port);

            _connector.lineReceive += new onLineReceived(this.onLineRecived);
            _connector.onConnect += new onConnect(this.onConnect);
            _connector.onError += new onError(this.onError);

            _timer = new Timer();
            _timer.Interval = 60000;
            _timer.Elapsed += new ElapsedEventHandler(onWhoTimer);
            _timer.Enabled = true;
        }    

        public void connect()
        {
            _connector.Connect();
        }
        /// <summary>
        /// Called then server sends any line
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="line"></param>
        private void onLineRecived(Object sender, String line)
        {
            Console.WriteLine(line);
            IrcMessageParser parser = new IrcMessageParser(line);
            switch (parser.Command)
            {
                case IrcCommands.PING:
                    _connector.sendMessage("{0} {1}", IrcCommands.PONG, parser.Trailing);
                    break;
                case IrcReplies.RPL_LUSERCLIENT:
                    _connector.sendMessage("JOIN {0} {1}", _config.Channel, _config.ChannelPassword);
                    onNotification("server", "Connected to server. Joining channel: " + _config.Channel);
                    break;
                case IrcCommands.PRIVMSG:
                    IrcMessage message = new IrcMessage(parser);
                    onMessageRecived(message);
                    break;
                case IrcReplies.ERR_USERONCHANNEL: // TODO append some random chars end of name and do login. User can after change nick or smth
                    onError("server", parser.Trailing);
                    break;
                case IrcReplies.RPL_NAMREPLY:
                    string channel = parser.Arguments[2];
                    if (_connectedChannels.Where(v => v.Name == channel).Count() < 1)
                        _connectedChannels.Add(new Channel(channel));
                    updateUserList(channel);
                    onNotification(channel, "successfully joined channel");
                    updateTopic(channel);
                    break;
                case IrcReplies.ERR_NOSUCHCHANNEL:
                    onError(parser.Trailing, "server");
                    break;
                case IrcCommands.QUIT:
                    string user = parser.tryParseUser();
                    var channels = _userList.Where(v => v.Name == user).Select(v => v.Channel).ToList();
                    foreach (string c in channels)
                    {
                        onNotification(c, user + " has left the server");
                        updateUserList(c);
                    }
                    break;
                case IrcCommands.JOIN:
                    if (!parser.tryParseUser().Equals(_config.NickName))
                    {
                        onNotification(parser.Trailing, parser.tryParseUser() + " has joined the channel");
                        updateUserList(parser.Trailing);
                    }
                    
                    break;
                case IrcCommands.PART:
                    /*
                     * IF somebody leaves channel, remove them from _uselist, update that channel user list and fire notification.
                     * Case leaver is this user dont update user list, beaause no need to know information about not connected channels
                     */
                    string userLeaved = parser.tryParseUser();
                    _userList.RemoveAll(v => v.Name == userLeaved && v.Channel == parser.Arguments[0]);
                    if (userLeaved.Equals(_config.NickName))
                    {
                        _connectedChannels.RemoveAll(v => v.Name == parser.Arguments[0]);
                        _userList.RemoveAll(v => v.Channel == parser.Arguments[0]);
                    }
                    else
                    {
                        onNotification(parser.Arguments[0], userLeaved + " has left the channel");
                        updateUserList(parser.Arguments[0]);
                    }
                    break;
                case IrcReplies.RPL_WHOREPLY:
                    string userName = parser.Arguments[5];
                    string lineChannel = parser.Arguments[1];
                    // This user starts with ~
                    if (userName.StartsWith("~"))
                    {
                        userName = userName.Substring(1);
                    }

                    User u = _userList.FirstOrDefault(v => v.Name == userName && (null == v.Channel || v.Channel.Equals(lineChannel)));
                    if (null == u)
                        u = new User(userName);

                    _userList.Remove(u);

                    String modes = parser.Arguments[6];
                    u.Op = modes.Contains("@");
                    u.Voice = modes.Contains("+");
                    u.Away = modes.Contains("G");
                    u.Channel = parser.Arguments[1];
                    _userList.Add(u);
                    break;
                case IrcReplies.RPL_ENDOFWHO:
                    onUserListUpdate();
                    break;
                case IrcCommands.NICK:
                    string prefix = parser.Prefix;
                    string oldUser = prefix.Substring(0, prefix.IndexOf("!"));
                    string newNick = parser.Trailing;
                    var users = _userList.Where(v => v.Name == oldUser).ToList();
                    foreach (User oldu in users) 
                    {
                        oldu.Name = newNick;
                    }
                    onUserListUpdate();
                    var channelss = _userList.Where(v => v.Name == newNick).Select(v => v.Channel).ToList();
                    foreach (string c in channelss)
                    {
                        onNotification(c, oldUser + " is now known as "+ newNick);
                    }
                    break;
                case IrcReplies.RPL_TOPIC: 
                case IrcReplies.RPL_NOTOPIC:
                    Channel chan = _connectedChannels.FirstOrDefault(v => v.Name == parser.Arguments[1]);
                    if (null != chan)
                    {
                        chan.Topic = parser.Trailing;
                        onTopicChange(chan);
                    }
                    break;
                case IrcCommands.TOPIC:
                    Channel chan2 = _connectedChannels.FirstOrDefault(v => v.Name == parser.Arguments[0]);
                    if (null != chan2)
                    {
                        chan2.Topic = parser.Trailing;
                        onTopicChange(chan2);
                    }
                    break;
                case IrcReplies.RPL_LISTEND:
                    onListUpdate(_availableChannels);
                    break;
                case IrcReplies.RPL_LISTSTART:
                    _availableChannels.Clear();
                    break;
                case IrcReplies.RPL_LIST:
                    string replayChannel = parser.Arguments[1];
                    if (_connectedChannels.Where(v => v.Name == replayChannel).Count() < 1 && !_availableChannels.Contains(replayChannel))
                        _availableChannels.Add(replayChannel);
                    break;
                case IrcReplies.ERR_CHANOPRIVSNEEDED:
                    onError(parser.Trailing, parser.Arguments[1]);
                    updateTopic(parser.Arguments[1]);
                    break;
                default:
                    onNotification("server", line);
                    break;
            }    
        }

        private void onConnect() {
            // Send nick and user messages to register connection
            _connector.sendMessage("NICK {0}", _config.NickName);
            // USER <username> <hostname> <servername> <realname> (RFC 1459)
            _connector.sendMessage("USER {0} {1} {2} :{3} ", _config.NickName, "host", "server", _config.RealName);
        }

        public void updateChannelList()
        {
            sendCommand("list");
        }

        public void sendPrivMessage(IrcMessage message) 
        {
            _connector.sendMessage("PRIVMSG {0} :{1}", message.Channel, message.Message);
        }

        public void sendCommand(string command)
        {
            _connector.sendMessage(command);
        }

        public void quit() {
            sendCommand("QUIT");
            _connector.closeConnection();
        }

        public void updateTopic(string channel)
        {
            sendCommand(String.Format("TOPIC {0}", channel));
        }

        private void updateUserList(string channel)
        {
            // Remove all user assosiated with this channel
            _userList.RemoveAll(v => v.Channel == channel);
            sendWho(channel);
        }

        public void sendWho(string channel)
        {
            _connector.sendMessage("WHO {0}", channel);
        }

        private void onWhoTimer(object sender, ElapsedEventArgs e)
        {
            foreach (Channel channel in _connectedChannels)
                sendWho(channel.Name);
        }
        #region abstract methods
        public virtual void onMessageRecived(IrcMessage message) {}

        public virtual void onUserJoin(string channel, string user) { }

        public virtual void onUserLeft(string channel, string user) { }
        
        public virtual void onError(string message, string channel) {}
        
        public virtual void onNotification(string channel, string notification) {}
        
        public virtual void onUserListUpdate() {}

        public virtual void onTopicChange(Channel channel) {}

        public virtual void onListUpdate(List<string> channelList) { }
        #endregion
    }
}
