using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Microsoft.AspNet.SignalR;
using System.Threading.Tasks;
using IrcConnector;
using System.Collections.Concurrent;
using IrcConnector.IrcTools;
using IrcWebApplication.Models;
using System.Collections;

namespace IrcWebApplication.Hubs
{
    public class IrcConnectorHub : Hub
    {
        private static Dictionary<string, IrcTask> Connections = new Dictionary<string, IrcTask>();
        private LoginModel _loginData;
        private bool _stayLogged;
        private string _connectionId;

        public void Hello()
        {
            Clients.All.hello();
        }

        public void startIrc(LoginModel model)
        {
            _loginData = model;
            _connectionId = Context.ConnectionId;
            foreach (IrcTask t in Connections.Values)
            {
                if (t.User_uuid.Equals(model.Uuid))
                {
                    // Resume old connection 
                    Connections.Remove(t.Connection_id);
                    Connections.Add(_connectionId, t);
                    t.Connection_id = _connectionId;
                    t.sendCommand("motd");
                    t.onConnectionResume();
                    // Manually trigger user list update function
                    t.onUserListUpdate();
                    return;
                }
            }

            IrcConfiguration conf = new IrcConfiguration();
            conf.Server = _loginData.Server;
            conf.NickName = _loginData.Name;
            conf.Channel = _loginData.Channel;
            if (null != _loginData.Port)
            {
                conf.Port = (int)_loginData.Port;
            }
            conf.ChannelPassword = _loginData.ChannelPassword;
            conf.ServerPassword = _loginData.ServerPassword;
            _stayLogged = _loginData.StayLogged;

            IrcTask ircTask = new IrcTask(this, _connectionId, _loginData.Uuid, conf);
            Connections.Add(_connectionId, ircTask);            
        }

        public override Task OnConnected()
        {
            return base.OnConnected();
        }

        public void send(string message, string channel)
        {
            IrcTask connector = getCurrentIrcTask();
            if (null != connector)
            {
                if (message.Trim().StartsWith("/"))
                {
                    connector.sendCommand(message.Substring(1));
                }
                else
                {
                    IrcMessage ircMessage = new IrcMessage();
                    ircMessage.Channel = channel;
                    ircMessage.UserName = connector.User.Name;
                    ircMessage.Message = message;
                    connector.sendPrivMessage(ircMessage);
                    Clients.Client(connector.Connection_id).received(new { channel = channel, message = ircMessage.Message, user = ircMessage.UserName });
                }
            }
        }

        public void requestChannelList()
        {
            IrcTask task = getCurrentIrcTask();
            if (null != task)
            {
                task.updateChannelList();    
            }
        }

        public override Task OnDisconnected()
        {
            string _connectionId2 = Context.ConnectionId;
            if (Connections.ContainsKey(_connectionId2))
            {
                IrcTask connector = Connections.FirstOrDefault(kvp => kvp.Key == _connectionId2).Value;
                if (!connector.Hub._stayLogged)
                {
                    connector.quit();
                    Connections.Remove(_connectionId2);
                }
                else
                {
                    connector.HistoryMode = true;
                }
                    
            }
            return base.OnDisconnected();
        }

        public void doDisconnect()
        {
            IrcTask task = getCurrentIrcTask();
            if (null != task) // Connection already removed
            {
                task.quit();
                Connections.Remove(task.Connection_id);
            }
            
        }

        private IrcTask getCurrentIrcTask()
        {
            return Connections.FirstOrDefault(kvp => kvp.Key == Context.ConnectionId).Value;
        }

    }

    class IrcTask : IrcNetConnector
    {
        #region variables
        private IrcConnectorHub _hub;
        private string _connection_id;
        private string _user_uuid;
        private bool _historyMode = false;
        private Queue<IrcMessage> _messageQueue;
        #endregion

        #region getters/setters

        public String User_uuid
        {
            get { return _user_uuid; }
            set { _user_uuid = value; }
        }

        public String Connection_id
        {
            get { return _connection_id; }
            set { _connection_id = value; }
        }
        public IrcConnectorHub Hub
        {
            get { return _hub; }
            set { _hub = value; }
        }

        public bool HistoryMode
        {
            get { return _historyMode; }
            set { _historyMode = value; }
        }
        #endregion

        public IrcTask(IrcConnectorHub client, string connectionId, string uuid, IrcConfiguration config)
            : base(config)
        {
            _hub = client;
            _connection_id = connectionId;
            _user_uuid = uuid;
            _messageQueue = new Queue<IrcMessage>(2000);

           // Start connection to server
           connect();
        }

        public override void onMessageRecived(IrcMessage message)
        {
            if (_historyMode)
            {
                _messageQueue.Enqueue(message);
                _messageQueue.TrimExcess();
            }
            else
            {
                _hub.Clients.Client(_connection_id).received(new { channel = message.Channel, message = message.Message, user = message.UserName, type = "message" , privateMessage = message.PrivateMessage});
            }
            
        }

        public override void onError(string message, string channel = "server")
        {
            _hub.Clients.Client(_connection_id).received(new { channel = channel, message = message, type = "error" });
        }

        public override void onNotification(string channel, string notificationMessage)
        {
            string typeString = "notification";
            if (channel == "server")
                typeString = "message";
            _hub.Clients.Client(_connection_id).received(new { channel = channel, message = notificationMessage, type = typeString });
        }

        public override void onUserListUpdate()
        {
            _hub.Clients.Client(_connection_id).userList(UserList);
        }

        public void onConnectionResume()
        {
            while (_messageQueue.Count > 0)
            {
                IrcMessage message = _messageQueue.Dequeue();
                _hub.Clients.Client(_connection_id).received(new { channel = message.Channel, message = message.Message, user = message.UserName, type = "message" });
            }            
            foreach (Channel c in ConnectedChannels)
            {
                if (c.Name != "server")
                {
                    _hub.Clients.Client(_connection_id).received(new { message = String.Format("Connection resumed in channel: {0}", c.Name), type = "notification", channel = c.Name });
                    onTopicChange(c);
                }
            }
        }

        public override void onTopicChange(Channel channel)
        {
           _hub.Clients.Client(_connection_id).onTopicChange(channel);
        }
        
        public override void onListUpdate(List<string> channelList) {
            _hub.Clients.Client(_connection_id).channelList(channelList);
        }

    }
     
}