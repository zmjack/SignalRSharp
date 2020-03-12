using Appie;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.Http.Connections.Client;
using Microsoft.AspNetCore.SignalR.Client;
using System;

namespace SignalRApp
{
    public class ChatHub : SignalRClient
    {
        #region Constructors
        public ChatHub(string url) : base(url) { }
        public ChatHub(string url, Action<HttpConnectionOptions> configureHttpConnection) : base(url, configureHttpConnection) { }
        public ChatHub(string url, HttpTransportType transports) : base(url, transports) { }
        public ChatHub(string url, HttpTransportType transports, Action<HttpConnectionOptions> configureHttpConnection) : base(url, transports, configureHttpConnection) { }

        public ChatHub(Uri url) : base(url) { }
        public ChatHub(Uri url, Action<HttpConnectionOptions> configureHttpConnection) : base(url, configureHttpConnection) { }
        public ChatHub(Uri url, HttpTransportType transports) : base(url, transports) { }
        public ChatHub(Uri url, HttpTransportType transports, Action<HttpConnectionOptions> configureHttpConnection) : base(url, transports, configureHttpConnection) { }
        #endregion

        public void OnReceiveMessage(Action<string> method)
        {
            Connection.On("ReceiveMessage", method);
        }

        public void SendMessage(string message)
        {
            Connection.SendCoreAsync("SendMessage", new object[] { message });
        }

    }
}
