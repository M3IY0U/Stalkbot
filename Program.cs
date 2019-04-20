using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using Newtonsoft.Json;

namespace StalkBot
{
    internal class Program
    {
        public static DiscordClient Client;
        private CommandsNextModule _commands;
        public static Config Config;
        public static bool IsPlaying;
        public static List<DiscordUser> Blacklist;

        public static void Main(string[] args)
            => new Program().MainAsync().GetAwaiter().GetResult();

        public async Task MainAsync()
        {
            try
            {
                Config = JsonConvert.DeserializeObject<Config>(File.ReadAllText("config.json"));
            }
            catch (Exception e)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(e.Message);
                Console.ReadLine();
                Environment.Exit(0);
            }

            if (!File.Exists("ffmpeg.exe"))
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(
                    "ffmpeg.exe not found in directory, please download it from https://ffmpeg.zeranoe.com/builds/");
                Console.ReadLine();
                Environment.Exit(0);
            }

            Blacklist = new List<DiscordUser>();
            IsPlaying = false;
            Client = new DiscordClient(new DiscordConfiguration {Token = Config.Token, TokenType = TokenType.Bot});
            _commands = Client.UseCommandsNext(new CommandsNextConfiguration
                {StringPrefix = Config.Prefix, CaseSensitive = false});
            _commands.RegisterCommands<Commands>();
            await Client.ConnectAsync();
            Console.WriteLine("Connected!");
            await Task.Delay(-1);
        }
    }

    [SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Global")]
    public class Config
    {
        public override string ToString()
        {
            return
                $"Prefix: {Prefix}\n" +
                $"Cam Timer: {CamTimer.ToString()}\n" +
                $"Blur Amount: {BlurAmount.ToString(CultureInfo.InvariantCulture)}\n" +
                $"TTS: {TtsEnabled.ToString()}\n" +
                $"Webcam: {CamEnabled.ToString()}\n" +
                $"Screenshots: {SsEnabled.ToString()}\n" +
                $"PlaySounds: {PlayEnabled.ToString()}\n" +
                $"Cursor: {CursorEnabled}\n" +
                $"Processes: {ProcessesEnabled}\n" +
                $"Timeout: {Timeout.ToString(CultureInfo.InvariantCulture)}" +
                $"Folder: {FolderPath}";
        }

        public static void SaveCfg()
        {
            File.WriteAllText("config.json", JsonConvert.SerializeObject(Program.Config, Formatting.Indented));
        }

        public string Token { get; set; }
        public string Prefix { get; set; }
        public int CamTimer { get; set; }
        public double BlurAmount { get; set; }
        public bool TtsEnabled { get; set; }
        public bool CamEnabled { get; set; }
        public bool SsEnabled { get; set; }
        public bool PlayEnabled { get; set; }
        public bool CursorEnabled { get; set; }
        public bool ProcessesEnabled { get; set; }
        public double Timeout { get; set; }
        public string FolderPath { get; set; }
    }
}