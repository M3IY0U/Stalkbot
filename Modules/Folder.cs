using System;
using System.Threading.Tasks;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using StalkBot.Utilities;

namespace StalkBot.Modules
{
    public class Folder : BaseCommandModule
    {
        public string[] Files { get; }

        public Folder(string[] files)
        {
            Files = files;
        }
        
        [Command("folder"), Aliases("f"), Cooldown(1, 3, CooldownBucketType.Global),
         Description("Sends a file from a folder, if set.")]
        public async Task SendImage(CommandContext ctx)
        {
            if (string.IsNullOrEmpty(Bot.Config.FolderPath))
            {
                Logger.Log($"Folderpath requested by {ctx.User.Username}#{ctx.User.Discriminator}, but it was not set.", ctx, 
                    LogLevel.Info);
                await ctx.Message.CreateReactionAsync(DiscordEmoji.FromUnicode("🔕"));
                return;
            }
            var rand = new Random();
            var file = Files[rand.Next(Files.Length)];
            await ctx.RespondWithFileAsync(file);
            Logger.Log($"Folder requested by {ctx.User.Username}#{ctx.User.Discriminator}, sent file {file}", ctx, LogLevel.Info);
        }
    }
}