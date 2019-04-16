using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using StalkBot;

namespace TTSBot
{
    internal class Program
    {
        public static DiscordClient Client;
        private CommandsNextModule _commands;
        private string prefix;
        public static bool TtsEnabled;
        public static bool CamEnabled;
        public static bool SsEnabled;
        
        
        public static void Main(string[] args)
            => new Program().MainAsync().GetAwaiter().GetResult();

        public async Task MainAsync()
        {
            string token;
            try
            {
                token = File.ReadAllLines("token.txt").FirstOrDefault();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                Console.ReadLine();
                return;
            }

            TtsEnabled = true;
            CamEnabled = true;
            SsEnabled = true;
            
            try
            {
                prefix = File.ReadAllLines("prefix.txt").First();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message + " Using default prefix mei!");
                prefix = "mei!";
            }

            Client = new DiscordClient(new DiscordConfiguration {Token = token, TokenType = TokenType.Bot});
            _commands = Client.UseCommandsNext(new CommandsNextConfiguration {StringPrefix = prefix});
            _commands.RegisterCommands<Commands>();
            await Client.ConnectAsync();
            Console.WriteLine($"Connected!\nUse {prefix}toggle to enable/disable all functionality");
            await Task.Delay(-1);
        }
    }
}