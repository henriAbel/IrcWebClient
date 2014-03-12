using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IrcConnector.IrcTools
{
    /// <summary>
    /// Class for defining IRC commands
    /// http://tools.ietf.org/html/rfc1459.htmld
    /// </summary>
    public static class IrcCommands
    {
        public const string PING = "PING";
        public const string PONG = "PONG";
        public const string PRIVMSG = "PRIVMSG";
        public const string QUIT = "QUIT";
        public const string JOIN = "JOIN";
        public const string PART = "PART";
        public const string NICK = "NICK";
        public const string TOPIC = "TOPIC";
    }
}


