using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IrcConnector.IrcTools
{
    interface IIrcProtocol
    {
        void sendPrivMessage(IrcMessage message);
        void onMessageRecived(IrcMessage message);
        void onUserJoin(string channel, string user);
        void onUserLeft(string channel, string user);
        void onError(string message, string channel);
        void onNotification(string channel, string notification);
        void onUserListUpdate();
        void onTopicChange(Channel channel);
        void onListUpdate(List<string> channelList);
    }
}
