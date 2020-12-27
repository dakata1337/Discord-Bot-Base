using Discord;
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

        public static void Initialize()
        {
            Thread thread = new Thread(() => 
            {
                while (true)
                {
                    string src = sourceQueue.Take();
                    string msg = messageQueue.Take();

                    Console.Write($"{DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss")} ");

                    Console.ForegroundColor = colorQueue.Take();
                    Console.Write($"[{src}] ");
                    Console.ResetColor();

                    Console.Write($"{msg}\n");
                }
            });
            thread.Start();
            Log("Log", "LoggingService initialized", ConsoleColor.Cyan);
        }

        /// <summary>
        /// Log messages to the Console. Console colors are controlled by Severity level
        /// </summary>
        /// <param name="source">Log source</param>
        /// <param name="message">Log message</param>
        /// <param name="severity">Log severity</param>
        public static void Log(string source, string message, LogSeverity severity)
        {
            sourceQueue.Add(source);
            messageQueue.Add(message);
            colorQueue.Add(GetConsoleColor(severity));
        }

        /// <summary>
        /// Log messages to the Console. Console colors are controlled by the specified ConsoleColor (if not set it will default to Cyan)
        /// </summary>
        /// <param name="source">Log source</param>
        /// <param name="message">Log message</param>
        /// <param name="color">Log color</param>
        public static void Log(string source, string message, ConsoleColor? color = null)
        {
            sourceQueue.Add(source);
            messageQueue.Add(message);
            colorQueue.Add((ConsoleColor)(color == null ? ConsoleColor.Cyan : color));
        }

        private static ConsoleColor GetConsoleColor(LogSeverity severity)
        {
            switch (severity)
            {
                case LogSeverity.Critical:
                    return ConsoleColor.Red;

                case LogSeverity.Error:
                    return ConsoleColor.Red;

                case LogSeverity.Warning:
                    return ConsoleColor.Yellow;

                case LogSeverity.Verbose:
                    return ConsoleColor.DarkYellow;

                case LogSeverity.Debug:
                    return ConsoleColor.Yellow;

                default:
                    return ConsoleColor.Cyan;
            }
        }
    }
}
