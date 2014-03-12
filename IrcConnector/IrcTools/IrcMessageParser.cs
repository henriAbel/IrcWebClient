using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IrcConnector.IrcTools
{
    /// <summary>
    /// Parses message recived from server
    /// </summary>
    public class IrcMessageParser
    {

        #region variables
        String _prefix;
        String _command;
        String _trailing;
        String _raw;
        String[] _arguments;
        #endregion

        #region setters/getters
        public String Prefix
        {
            get { return _prefix; }
            set { _prefix = value; }
        }

        public String Command
        {
            get { return _command; }
            set { _command = value; }
        }

        public String Trailing
        {
            get { return _trailing; }
            set { _trailing = value; }
        }

        public String Raw
        {
            get { return _raw; }
            set { _raw = value; }
        }
        public String[] Arguments
        {
            get { return _arguments; }
            set { _arguments = value; }
        }
        

        #endregion

        public IrcMessageParser(String line)
        {
            line = line.Trim();
            _arguments = new String[0];
            _raw = line;
            string[] parts = line.Split(' ');
            Stack<string> tokenStack = new Stack<string>(parts.Reverse());
            ArrayList arguments = new ArrayList();
            string token;

            token = tokenStack.Pop();
            if (token.StartsWith(":"))
            {
                _prefix = token.Substring(1);
                _command = tokenStack.Pop();
            }
            else
            {
                _command = token;
            }

            while (tokenStack.Count > 0)
            {
                token = tokenStack.Pop();

                if (token.StartsWith(":"))
                {
                    StringBuilder sb = new StringBuilder();
                    sb.Append(token.Substring(1));
                    while (tokenStack.Count > 0)
                    {
                        sb.Append(" ");
                        sb.Append(tokenStack.Pop());
                    }
                    _trailing = sb.ToString();
                }
                else
                {
                    arguments.Add(token);
                }
            }
            if (arguments.Count > 0)
                _arguments = arguments.ToArray(typeof(string)) as string[];
        }

        /// <summary>
        /// Try-s parsing username from prefix
        /// </summary>
        /// <returns>username from prefix</returns>
        public string tryParseUser()
        {
            if (_prefix.Contains("!")) {
                return _prefix.Substring(0, _prefix.IndexOf("!"));
            }

            return "";
        }
    }
}
