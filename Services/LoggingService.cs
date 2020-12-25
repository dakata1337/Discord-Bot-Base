using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Discord_Bot.Services
{
    class LoggingService
    {
        private static BlockingCollection<string> sourceQueue = new BlockingCollection<string>();
        private static BlockingCollection<string> messageQueue = new BlockingCollection<string>();
        private static BlockingCollection<ConsoleColor> colorQueue = new BlockingCollection<ConsoleColor>();

        public async static Task InitializeAsync()
        {
            Thread thread = new Thread(() => 
            {
                while (true)
                {
                    Console.ForegroundColor = colorQueue.Take();
                    Console.WriteLine($"{DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss")} [{sourceQueue.Take()}] {messageQueue.Take()}");
                    Console.ResetColor();
                }
            });
            thread.Start();
            Log("Log", "LoggingService initialized");
        }

        public static void Log(string source, string message, ConsoleColor color = ConsoleColor.Gray)
        {
            sourceQueue.Add(source);
            messageQueue.Add(message);
            colorQueue.Add(color);
        }
    }
}
