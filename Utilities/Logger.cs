using System;
using DSharpPlus.CommandsNext;

namespace StalkBot.Utilities
{
    public static class Logger
    {
        public static void Log(string message, CommandContext ctx, LogLevel level)
        {
            var channel = ctx == null ? "" : $"[{ctx.Guild.Name}, #{ctx.Channel.Name}]\n\t";
            Console.ForegroundColor = Color(level);
            Console.WriteLine($"[{DateTime.Now.ToLongTimeString()}] {channel}{message}");
            Console.ForegroundColor = ConsoleColor.White;
        }

        private static ConsoleColor Color(LogLevel level)
        {
            switch (level)
            {
                case LogLevel.Info:
                    return ConsoleColor.DarkGreen;
                case LogLevel.Error:
                    return ConsoleColor.Red;
                case LogLevel.Warning:
                    return ConsoleColor.Yellow;
                default:
                    return ConsoleColor.White;
            }
        }
    }

    public enum LogLevel : ushort
    {
        Info = 0,
        Error = 1,
        Warning = 2
    }
}