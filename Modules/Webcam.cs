using System.IO;
using System.Threading.Tasks;
using AForge.Video.DirectShow;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using StalkBot.Utilities;

namespace StalkBot.Modules
{
    public class Webcam : BaseCommandModule
    {
        [Command("webcam"), Aliases("wc", "cam"), Cooldown(1, 5, CooldownBucketType.Global),
         Description("Captures the webcam.")]
        public async Task Capture(CommandContext ctx, int index = 0)
        {
            if (!Bot.Config.CamEnabled)
            {
                Logger.Log($"Webcam requested by {ctx.User.Username}#{ctx.User.Discriminator}, but it was toggled off.",
                    ctx, LogLevel.Info);
                await ctx.Message.CreateReactionAsync(DiscordEmoji.FromUnicode("🔕"));
                return;
            }

            Logger.Log($"Webcam requested by {ctx.User.Username}#{ctx.User.Discriminator}", ctx, LogLevel.Info);
            var capture =
                new VideoCaptureDevice(new FilterInfoCollection(FilterCategory.VideoInputDevice)[index].MonikerString);
            capture.Start();
            await Task.Delay(Bot.Config.CamTimer);

            await ctx.Message.CreateReactionAsync(DiscordEmoji.FromUnicode("📸"));
            capture.NewFrame += (sender, args) =>
            {
                args.Frame.Save("webcam.png");
                capture.SignalToStop();
            };

            while (capture.IsRunning)
                capture.WaitForStop();
            capture.Stop();

            await ctx.RespondWithFileAsync("webcam.png");
            File.Delete("webcam.png");
        }
    }
}