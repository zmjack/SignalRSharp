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

        private volatile bool Running;
        private bool AutomaticRetry = false;
        private int RetryFailedCount = 0;

        public HubConnection Connection { get; private set; }
        public event SignalRDelegate.SRAction Connecting;
        public event SignalRDelegate.SRAction Connected;
        public event SignalRDelegate.SRException Reconnecting;
        public event SignalRDelegate.SRException Reconnected;
        public event SignalRDelegate.SRException Closed;
        public event SignalRDelegate.SRException Exception;
        public event SignalRDelegate.SRException Stoped;
        public event SignalRDelegate.SRException RetryFailed;

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

        protected void InitConnection()
        {
            Connection.Closed += ex0 => Task.Run(() =>
            {
                Running = false;

                if (ex0 == null || !AutomaticRetry)
                {
                    Closed?.Invoke(this, ex0);
                    return;
                }

                Exception?.Invoke(this, ex0);
                Thread.Sleep(ExceptionRetryDelay);
                Reconnecting?.Invoke(this, ex0);
                try
                {
                    StartAsync(AutomaticRetry).Wait();
                    Reconnected?.Invoke(this, ex0);
                    RetryFailedCount = 0;
                }
                catch (Exception ex)
                {
                    Closed?.Invoke(this, ex);
                }
            });
        }

        public async Task StartAsync(bool automaticRetry)
        {
            if (Running) throw new InvalidOperationException("The SignalR client is running.");
            Running = true;

            AutomaticRetry = automaticRetry;
            await Task.Run(() =>
            {
                while (true)
                {
                    Connecting?.Invoke(this);
                    try
                    {
                        Connection.StartAsync().Wait();
                        Connected?.Invoke(this);
                        RetryFailedCount = 0;
                        return;
                    }
                    catch (Exception ex)
                    {
                        Exception?.Invoke(this, ex);
                        if (AutomaticRetry)
                        {
                            if (RetryFailedCount < MaxRetry)
                            {
                                Thread.Sleep(ExceptionRetryDelay);
                                RetryFailedCount++;
                                continue;
                            }
                            else
                            {
                                RetryFailed?.Invoke(this, ex);
                                throw;
                            }
                        }
                        else throw;
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
