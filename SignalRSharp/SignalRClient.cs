using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.Http.Connections.Client;
using Microsoft.AspNetCore.SignalR.Client;
using NStandard;
using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Appie
{
    public class SignalRClient
    {
        public TimeSpan ExceptionRetryDelay { get; set; } = TimeSpan.FromSeconds(10);
        /// <summary>
        /// Maximum number of retries. If set to 0, there is no limit. (The default value is 5.)
        /// </summary>
        public int MaxRetry { get; set; } = 5;
        public int RetryCount { get; private set; } = 0;

        private volatile bool Running;
        private bool AutomaticRetry = false;

        public HubConnection Connection { get; private set; }
        public event SignalRDelegate.SRAction Connecting;
        public event SignalRDelegate.SRAction Connected;
        public event SignalRDelegate.SRException Reconnecting;
        public event SignalRDelegate.SRException Reconnected;
        public event SignalRDelegate.SRException Closed;
        public event SignalRDelegate.SRException Exception;
        public event SignalRDelegate.SRException Failed;

        #region Constructors
        public SignalRClient(string url)
        {
            Connection = new HubConnectionBuilder().WithUrl(url).Build();
            InitConnection();
        }
        public SignalRClient(string url, Action<HttpConnectionOptions> configureHttpConnection)
        {
            Connection = new HubConnectionBuilder().WithUrl(url, configureHttpConnection).Build();
            InitConnection();
        }
        public SignalRClient(string url, HttpTransportType transports)
        {
            Connection = new HubConnectionBuilder().WithUrl(url, transports).Build();
            InitConnection();
        }
        public SignalRClient(string url, HttpTransportType transports, Action<HttpConnectionOptions> configureHttpConnection)
        {
            Connection = new HubConnectionBuilder().WithUrl(url, transports, configureHttpConnection).Build();
            InitConnection();
        }

        public SignalRClient(Uri url)
        {
            Connection = new HubConnectionBuilder().WithUrl(url).Build();
            InitConnection();
        }
        public SignalRClient(Uri url, Action<HttpConnectionOptions> configureHttpConnection)
        {
            Connection = new HubConnectionBuilder().WithUrl(url, configureHttpConnection).Build();
            InitConnection();
        }
        public SignalRClient(Uri url, HttpTransportType transports)
        {
            Connection = new HubConnectionBuilder().WithUrl(url, transports).Build();
            InitConnection();
        }
        public SignalRClient(Uri url, HttpTransportType transports, Action<HttpConnectionOptions> configureHttpConnection)
        {
            Connection = new HubConnectionBuilder().WithUrl(url, transports, configureHttpConnection).Build();
            InitConnection();
        }
        #endregion

        protected void HandleClosed(Exception exception)
        {
            Closed?.Invoke(this, exception);
            RetryCount = 0;
            Running = false;
        }

        protected void HandleFailed(Exception exception)
        {
            Failed?.Invoke(this, exception);
            RetryCount = 0;
            Running = false;
        }

        protected void InitConnection()
        {
            Connection.Closed += ex0 => Task.Run(() =>
            {
                if (ex0 == null || !AutomaticRetry)
                {
                    HandleClosed(ex0);
                    return;
                }

                Exception?.Invoke(this, ex0);
                Thread.Sleep(ExceptionRetryDelay);
                try
                {
                    Running = false;
                    StartAsync(AutomaticRetry, ex0).Wait();
                    Reconnected?.Invoke(this, ex0);
                }
                catch (Exception ex)
                {
                    HandleClosed(ex);
                }
            });
        }

        public async Task StartAsync(bool automaticRetry, Exception exception = null)
        {
            if (Running) throw new InvalidOperationException("The SignalR client is running.");
            Running = true;

            AutomaticRetry = automaticRetry;
            await Task.Run(() =>
            {
                while (true)
                {
                    if (exception is null)
                        Connecting?.Invoke(this);
                    else Reconnecting?.Invoke(this, exception);

                    try
                    {
                        Connection.StartAsync().Wait();
                        Connected?.Invoke(this);
                        RetryCount = 0;
                        return;
                    }
                    catch (Exception ex)
                    {
                        RetryCount++;
                        Exception?.Invoke(this, ex);
                        if (!AutomaticRetry || RetryCount >= MaxRetry)
                        {
                            HandleFailed(ex);
                            throw;
                        }

                        Thread.Sleep(ExceptionRetryDelay);
                        continue;
                    }
                }
            });
        }

        public async Task StopAsync()
        {
            await Task.Run(() =>
            {
                if (!Running) throw new InvalidOperationException("The SignalR client is not running.");

                AutomaticRetry = false;
                Connection.StopAsync().Wait();
            });
        }

    }
}
