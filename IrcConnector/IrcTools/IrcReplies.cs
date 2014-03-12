using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IrcConnector.IrcTools
{
    /// <summary>
    /// Class for mapping different status codes to human readable value
    /// http://tools.ietf.org/html/rfc1459.html
    /// </summary>
    public static class IrcReplies
    
    {
        public const string ERR_USERONCHANNEL = "433";
        public const string ERR_CHANOPRIVSNEEDED = "482";

        public const string ERR_NOSUCHCHANNEL = "403";
        public const string RPL_NAMREPLY = "353"; // Sended on succesfull channel join
        public const string RPL_NOTOPIC = "331";
        public const string RPL_TOPIC = "332";
        public const string RPL_LUSERCLIENT = "251"; // Use this as welcome message ??
        public const string RPL_WHOREPLY = "352";
        public const string RPL_ENDOFWHO = "315";
        public const string RPL_LISTSTART = "321";
        public const string RPL_LIST = "322";
        public const string RPL_LISTEND = "323";
    }
}
