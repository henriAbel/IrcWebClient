using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IrcConnector.IrcTools
{
    public class Channel
    {
        #region variables
        private string _name;
        private string _topic;
        #endregion

        #region getters/setters
        public string Name
        {
            get { return _name; }
            set { _name = value; }
        }
        
        public string Topic
        {
            get { return _topic; }
            set { _topic = value; }
        }
        #endregion

        public Channel(string name) 
        {
            this._name = name;
        }

        public Channel(string name, string topic)
        {
            this._name = name;
            this._topic = topic;
        }
    }
}
