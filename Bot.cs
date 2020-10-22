using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.EventArgs;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using StalkBot.Utilities;
using LogLevel = StalkBot.Utilities.LogLevel;

namespace StalkBot
{
    public class Bot : IDisposable
    {
        internal DiscordClient Client { get; }
        private CommandsNextExtension _commandsNext;
        public static Config Config;

        public Bot()
        {
            RunChecks();
            //load config

            Config = JsonConvert.DeserializeObject<Config>(File.ReadAllText("config.json"));

            //setup client
            Client = new DiscordClient(new DiscordConfiguration
            {
                Token = Config.Token
            });
            var deps = new ServiceCollection();

            deps.AddSingleton(General.BuildFolderList());

            var prefixes = new List<string> {Config.Prefix};

            //setup commands
            _commandsNext = Client.UseCommandsNext(new CommandsNextConfiguration
            {
                CaseSensitive = false,
                EnableDms = false,
                StringPrefixes = prefixes,
                Services = deps.BuildServiceProvider()
            });
            _commandsNext.RegisterCommands(Assembly.GetEntryAssembly());
            _commandsNext.CommandErrored += General.CommandsNextOnCommandErrored;
            Client.MessageCreated += ClientOnMessageCreated;
        }

        private Task ClientOnMessageCreated(MessageCreateEventArgs e)
        {
            if (!e.Message.Content.StartsWith(Config.Prefix)) return Task.CompletedTask;
            if (_commandsNext.RegisteredCommands.TryGetValue(e.Message.Content.Substring(Config.Prefix.Length).Split(' ').First(), out var cmd))
                if(Config.IsEnabled(cmd.Name.ToLower()))
                    General.PlayAlert(cmd.Name.ToLower());

            return Task.CompletedTask;
        }

        private static void RunChecks()
        {
            if (!File.Exists("config.json"))
            {
                Logger.Log("config.json was not found, creating a default one!",
                    null, LogLevel.Warning);
                var cfg = new Config();
                CreateConfig(ref cfg);
                cfg.Save();
            }
            if (File.Exists("ffmpeg.exe")) return;
            Logger.Log("ffmpeg.exe not found in directory, please download it from https://www.gyan.dev/ffmpeg/builds/",
                null, LogLevel.Error);

            Console.ReadLine();
            Environment.Exit(0);
        }

        private static void CreateConfig(ref Config cfg)
        {
            Console.WriteLine("Enter your token: ");
            cfg.Token = Console.ReadLine();
            Console.WriteLine("Enter your prefix: ");
            cfg.Prefix = Console.ReadLine();
            Console.WriteLine("Cam Timer (in milliseconds): ");
            cfg.CamTimer = Convert.ToInt32(Console.ReadLine());
            Console.WriteLine("Enter your blur amount for screenshots (values between 0 and 5 recommended): ");
            cfg.BlurAmount = Convert.ToDouble(Console.ReadLine());
            Console.WriteLine("Enter a timeout for the tts/play command (in milliseconds): ");
            cfg.Timeout = Convert.ToDouble(Console.ReadLine());
        }

        public async Task RunAsync()
        {
            await Client.ConnectAsync();
            await Task.Delay(2500);
            Logger.Log(
                $"Connected successfully to {Client.Guilds.Count} Server(s):\n\t{string.Join("\n\t", Client.Guilds.Values)}",
                null, LogLevel.Info);
            await Task.Delay(-1);
        }

        public void Dispose()
        {
            Client.Dispose();
            _commandsNext = null;
        }
    }
}
