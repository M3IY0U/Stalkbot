using System.Threading.Tasks;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using StalkBot.Utilities;

namespace StalkBot.Modules
{
    public class Configuration : BaseCommandModule
    {
        [Command("toggle"), RequireOwner, Description("Toggle specific settings on or off.")]
        public async Task Toggle(CommandContext ctx, string input = "")
        {
            switch (input.ToLower())
            {
                case "off":
                    Bot.Config.SsEnabled = false;
                    Bot.Config.CamEnabled = false;
                    Bot.Config.TtsEnabled = false;
                    Bot.Config.PlayEnabled = false;
                    Bot.Config.ProcessesEnabled = false;
                    Logger.Log("Turned off everything.", ctx, LogLevel.Info);
                    break;
                case "cam":
                case "wc":    
                    Bot.Config.CamEnabled = !Bot.Config.CamEnabled;
                    Logger.Log($"Toggled webcam to: {Bot.Config.CamEnabled}.", ctx, LogLevel.Info);
                    break;
                case "tts":
                case "say":
                    Bot.Config.TtsEnabled = !Bot.Config.TtsEnabled;
                    Logger.Log($"Toggled tts to: {Bot.Config.TtsEnabled}.", ctx, LogLevel.Info);
                    break;
                case "ss":
                case "screenshot":
                    Bot.Config.SsEnabled = !Bot.Config.SsEnabled;
                    Logger.Log($"Toggled screenshot to: {Bot.Config.SsEnabled}.", ctx, LogLevel.Info);
                    break;
                case "play":
                    Bot.Config.PlayEnabled = !Bot.Config.PlayEnabled;
                    Logger.Log($"Toggled playsounds to: {Bot.Config.PlayEnabled}.", ctx, LogLevel.Info);
                    break;
                case "proc":
                    Bot.Config.ProcessesEnabled = !Bot.Config.ProcessesEnabled;
                    Logger.Log($"Toggled processes to: {Bot.Config.ProcessesEnabled}.", ctx, LogLevel.Info);
                    break;
                default:
                    await ctx.RespondAsync(
                        "Available toggles: `off`, `cam`, `tts`, `screenshot`, `play`, `proc`.");
                    return;
            }

            await ctx.Message.CreateReactionAsync(DiscordEmoji.FromUnicode("✅"));
            Bot.Config.Save();
        }

        [Command("cfg")]
        public async Task PrintConfig(CommandContext ctx)
        {
            await ctx.RespondAsync(Bot.Config.ToString());
        }
    }
}