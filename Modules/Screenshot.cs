using System.Drawing;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using StalkBot.Utilities;
using Image = SixLabors.ImageSharp.Image;

namespace StalkBot.Modules
{
    public class Screenshot : BaseCommandModule
    {
        [Command("screenshot"), Aliases("ss", "sc"), Cooldown(1, 5, CooldownBucketType.Global),
         Description("Captures a screenshot.")]
        public async Task Screen(CommandContext ctx)
        {
            if (!Bot.Config.SsEnabled)
            {
                Logger.Log(
                    $"Screenshot requested by {ctx.User.Username}#{ctx.User.Discriminator}, but it was toggled off.", ctx,
                    LogLevel.Info);
                await ctx.Message.CreateReactionAsync(DiscordEmoji.FromUnicode("🔕"));
                return;
            }
            Logger.Log($"Screenshot requested by {ctx.User.Username}#{ctx.User.Discriminator}", ctx, LogLevel.Info);
            var vScreen = SystemInformation.VirtualScreen;
            
            using (var bm = new Bitmap(vScreen.Width, vScreen.Height))
            {
                using (var g = Graphics.FromImage(bm))
                {
                    g.CopyFromScreen(vScreen.Left, vScreen.Top, 0, 0, bm.Size);
                }

                await ctx.Message.CreateReactionAsync(DiscordEmoji.FromUnicode("📸"));
                bm.Save("ss.png");
            }

            if (Bot.Config.BlurAmount > 0)
            {
                using (var img = Image.Load("ss.png"))
                {
                    img.Mutate(x => x.GaussianBlur((float)Bot.Config.BlurAmount));
                    img.Save("ss.png");
                }
            }

            await ctx.Channel.SendFileAsync("ss.png");
            File.Delete("ss.png");
        }
    }
}