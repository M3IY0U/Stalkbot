using System.IO;
using System.Linq;
using System.Media;
using System.Threading.Tasks;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;

namespace StalkBot.Utilities
{
    public static class General
    {
        internal static async Task CommandsNextOnCommandErrored(CommandErrorEventArgs e)
        {
            if (e.Exception.Message.Contains("command was not found"))
            {
                await e.Context.Message.CreateReactionAsync(DiscordEmoji.FromUnicode("❓")); 
                return;
            }
            await e.Context.Message.CreateReactionAsync(DiscordEmoji.FromUnicode("❌"));
            Logger.Log($"Command {e.Command.Name} errored! Message: {e.Exception.Message}", e.Context, LogLevel.Error);
        }
        
        public static string[] BuildFolderList() 
            => !string.IsNullOrEmpty(Bot.Config.FolderPath) ? SearchFiles(Bot.Config.FolderPath) : new []{""};

        private static string[] SearchFiles(string path)
        {
            var files = Directory.GetFiles(path);
            var directories = Directory.GetDirectories(path);
            return directories.Length == 0
                ? files
                : directories.Aggregate(files, (current, directory) => current.Union(SearchFiles(directory)).ToArray());
        }

        public static void PlayAlert(string file)
        {
            if (!File.Exists($"{file}.wav"))
                return;
            using (var player = new SoundPlayer($"{file}.wav"))
                player.Play();
        }
    }
}