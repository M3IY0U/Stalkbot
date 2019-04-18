using System;
using System.IO;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using Newtonsoft.Json;

namespace StalkBot
{
    internal class Program
    {
        public static DiscordClient Client;
        private CommandsNextModule _commands;
        public static Config Config;
        public static bool IsPlaying;

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
                Console.WriteLine(e.Message);
                throw;
            }

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

    public class Config
    {
        public override string ToString()
        {
            return
                $"Prefix: {Prefix}\n" +
                $"Cam Timer: {CamTimer.ToString()}\n" +
                $"Blur Amount: {BlurAmount.ToString()}\n" +
                $"TTS: {TtsEnabled.ToString()}\n" +
                $"Webcam: {CamEnabled.ToString()}\n" +
                $"Screenshots: {SsEnabled.ToString()}" +
                $"PlaySounds: {PlayEnabled.ToString()}";
        }

        public void SaveCfg()
        {
            File.WriteAllText("config.json", JsonConvert.SerializeObject(Program.Config, Formatting.Indented));
        }
        
        public string Token { get; set; }
        public string Prefix { get; set; }
        public int CamTimer { get; set; }
        public int BlurAmount { get; set; }
        public bool TtsEnabled { get; set; }
        public bool CamEnabled { get; set; }
        public bool SsEnabled { get; set; }
        public bool PlayEnabled { get; set; }
    }
}