using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IrcConnector.IrcTools
{
    public class User
    {
        #region
        private string _channel;
        private string _name;
        private bool _op;
        private bool _voice;
        private bool _away;
        #endregion

        #region getters/setters
        public string Name
        {
            get { return _name; }
            set { _name = value; }
        }

        public bool Op
        {
            get { return _op; }
            set { _op = value; }
        }

        public bool Voice
        {
            get { return _voice; }
            set { _voice = value; }
        }

        public bool Away
        {
            get { return _away; }
            set { _away = value; }
        }

        public string Channel
        {
            get { return _channel; }
            set { _channel = value; }
        }
        #endregion

        public User(string name)
        {
            this._name = name;
        }
    }
}
