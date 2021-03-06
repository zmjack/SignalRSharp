﻿using NStandard;
using System;

namespace SignalRApp
{
    class Program
    {
        static void Main(string[] args)
        {
            var chatHub = new ChatHub("http://localhost:60210/ChatHub")
            {
                ExceptionRetryDelay = TimeSpan.FromSeconds(1),
            };

            chatHub.OnReceiveMessage(message =>
            {
                Console.WriteLine(message);
            });
            chatHub.Connecting += sender => Console.WriteLine("Connecting...");
            chatHub.Connected += sender => Console.WriteLine("Connected...");
            chatHub.Reconnecting += (sender, exception) => Console.WriteLine($"Reconnecting... {chatHub.RetryCount + 1}");
            chatHub.Reconnected += (sender, exception) => Console.WriteLine("Reconnected...");
            chatHub.Closed += (sender, exception) => Console.WriteLine("Closed...");
            chatHub.Exception += (sender, exception) => Console.WriteLine($"Exception...");
            chatHub.Failed += (sender, exception) => Console.WriteLine("Failed...");

            chatHub.StartAsync(true).CatchAsync(ex => Console.WriteLine(ex.Message));

            while (true)
            {
                var line = Console.ReadLine();
                if (line == "stop")
                {
                    chatHub.StopAsync().CatchAsync(ex => Console.WriteLine(ex.Message));
                }
                else if (line == "start")
                {
                    chatHub.StartAsync(true).CatchAsync(ex => Console.WriteLine(ex.Message));
                }
                else chatHub.SendMessage(line);
            }
        }

    }
}
