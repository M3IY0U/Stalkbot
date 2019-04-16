using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Speech.Synthesis;
using System.Threading.Tasks;
using AForge.Video.DirectShow;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using TTSBot;

namespace StalkBot
{
    public class Commands
    {
        [Command("webcam"),Aliases("wc"), Cooldown(1, 5, CooldownBucketType.Global)]
        public async Task Webcam(CommandContext ctx)
        {
            if (Program.CamEnabled)
            {
                Console.WriteLine(
                    $"[{DateTime.Now.ToLongTimeString()}] Webcam requested by {ctx.User.Username}#{ctx.User.Discriminator}");
                // ReSharper disable once CollectionNeverUpdated.Local
                try
                {
                    var videoDevices = new FilterInfoCollection(FilterCategory.VideoInputDevice);
                    var cam = new VideoCaptureDevice(videoDevices[0].MonikerString);
                    cam.NewFrame += (sender, args) =>
                    {
                        args.Frame.Save("webcam.png");
                        cam.SignalToStop();
                    };
                    cam.Start();
                    while (cam.IsRunning)
                    {
                        cam.WaitForStop();
                    }

                    cam.Stop();
                    await ctx.Message.CreateReactionAsync(DiscordEmoji.FromUnicode("📸"));
                    await ctx.Channel.SendFileAsync("webcam.png");
                    File.Delete("webcam.png");
                }
                catch (Exception e)
                {
                    Console.WriteLine("Error: " + e.Message);
                }
            }
            else
            {
                await ctx.Message.CreateReactionAsync(DiscordEmoji.FromUnicode("🔕"));
            }
        }

        [Command("toggle"), RequireOwner]
        public async Task Toggle(CommandContext ctx, string input="")
        {
            switch (input.ToLower())
            {
                case "all":
                    Program.SsEnabled = !Program.SsEnabled;
                    Program.CamEnabled = !Program.CamEnabled;
                    Program.TtsEnabled = !Program.TtsEnabled;
                    break;
                case "cam":
                    Program.CamEnabled = !Program.CamEnabled;
                    break;
                case "tts":
                    Program.TtsEnabled = !Program.TtsEnabled;
                    break;
                case "ss":
                    Program.SsEnabled = !Program.SsEnabled;
                    break;
                case "screenshot":
                    Program.SsEnabled = !Program.SsEnabled;
                    break;
                default:
                    await ctx.RespondAsync("No or wrong options specified, turning everything off!");
                    Program.SsEnabled = false;
                    Program.CamEnabled = false;
                    Program.TtsEnabled = false;
                    break;
            }
            await ctx.RespondAsync($"Status is now:\nTTS: {Program.TtsEnabled}\nCam:{Program.CamEnabled}\nScreenshot:{Program.SsEnabled}");
        }

        [Command("say"), Aliases("tts"), Cooldown(1, 5, CooldownBucketType.Global)]
        public async Task Say(CommandContext ctx, [RemainingText] string input)
        {
            if (Program.TtsEnabled)
            {
                try
                {
                    Console.WriteLine(
                        $"[{DateTime.Now.ToLongTimeString()}] TTS requested by {ctx.User.Username}#{ctx.User.Discriminator}. Text: {input}");
                    var synth = new SpeechSynthesizer();
                    await ctx.Message.CreateReactionAsync(DiscordEmoji.FromName(Program.Client, ":mega:"));
                    var p = new Prompt(input);
                    synth.SpeakAsync(p);
                    synth.SpeakCompleted += async (sender, args) =>
                    {
                        await ctx.Message.DeleteOwnReactionAsync(DiscordEmoji.FromName(Program.Client, ":mega:"));
                        await ctx.Message.CreateReactionAsync(DiscordEmoji.FromUnicode("✅"));
                    };
                }
                catch (Exception e)
                {
                    Console.WriteLine("Error: " + e.Message);
                }
            }
            else
            {
                await ctx.Message.CreateReactionAsync(DiscordEmoji.FromUnicode("🔕"));
            }
        }

        [Command("screenshot"), Aliases("ss", "sc"), Cooldown(1, 5, CooldownBucketType.Global)]
        public async Task Screen(CommandContext ctx)
        {
            if (Program.SsEnabled)
            {
                try
                {
                    Console.WriteLine(
                        $"[{DateTime.Now.ToLongTimeString()}] Screenshot requested by {ctx.User.Username}#{ctx.User.Discriminator}");
                    var screens = System.Windows.Forms.Screen.AllScreens;
                    var bounds = System.Windows.Forms.Screen.GetBounds(Point.Empty);
                    if (screens.Length > 1)
                    {
                        bounds = screens.Aggregate(bounds,
                            (current, screen) => Rectangle.Union(current, screen.Bounds));
                    }

                    using (var bitmap = new Bitmap(bounds.Width, bounds.Height))
                    {
                        using (var g = Graphics.FromImage(bitmap))
                        {
                            g.CopyFromScreen(Point.Empty, Point.Empty, bounds.Size);
                        }

                        await ctx.Message.CreateReactionAsync(DiscordEmoji.FromUnicode("📸"));
                        bitmap.Save("ss.png");
                    }

                    await ctx.Channel.SendFileAsync("ss.png");
                    File.Delete("ss.png");
                }
                catch (Exception e)
                {
                    Console.WriteLine("Error: " + e.Message);
                }
            }
            else
            {
                await ctx.Message.CreateReactionAsync(DiscordEmoji.FromUnicode("🔕"));
            }
        }
    }
}