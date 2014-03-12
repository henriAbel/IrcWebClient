using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IrcConnector
{
    public delegate void onConnect();

    public delegate void onLineReceived(object sender, String message);

    public delegate void onError(String errorMessage, string channel = "server");
}
