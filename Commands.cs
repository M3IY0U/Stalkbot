using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Media;
using System.Net;
using System.Speech.Synthesis;
using System.Threading.Tasks;
using System.Windows.Forms;
using AForge.Video.DirectShow;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using Image = SixLabors.ImageSharp.Image;

namespace StalkBot
{
    public class Commands
    {
        [Command("webcam"), Aliases("wc", "cam"), Cooldown(1, 5, CooldownBucketType.Global)]
        public async Task Webcam(CommandContext ctx)
        {
            if (Program.Config.CamEnabled)
            {
                Log($"Webcam requested by {ctx.User.Username}#{ctx.User.Discriminator}");
                // ReSharper disable once CollectionNeverUpdated.Local
                try
                {
                    var videoDevices = new FilterInfoCollection(FilterCategory.VideoInputDevice);
                    var cam = new VideoCaptureDevice(videoDevices[0].MonikerString);
                    var player = new SoundPlayer("webcam.wav");
                    player.Play();
                    cam.Start();
                    await Task.Delay(Program.Config.CamTimer);
                    await ctx.Message.CreateReactionAsync(DiscordEmoji.FromUnicode("📸"));
                    cam.NewFrame += (sender, args) =>
                    {
                        args.Frame.Save("webcam.png");
                        cam.SignalToStop();
                    };
                    while (cam.IsRunning)
                    {
                        cam.WaitForStop();
                    }

                    cam.Stop();
                    await ctx.Channel.SendFileAsync("webcam.png");
                    File.Delete("webcam.png");
                }
                catch (Exception e)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("Error capturing Webcam: " + e.Message);
                    Console.ResetColor();
                }
            }
            else
            {
                await ctx.Message.CreateReactionAsync(DiscordEmoji.FromUnicode("🔕"));
            }
        }

        [Command("play")]
        public async Task Play(CommandContext ctx, string url = "")
        {
            if (Program.Config.PlayEnabled)
            {
                if (Program.IsPlaying)
                {
                    await ctx.RespondAsync("Already playing something");
                    return;
                }
                try
                {
                    if (url == "" && ctx.Message.Attachments.Count == 0)
                    {
                        await ctx.Message.CreateReactionAsync(DiscordEmoji.FromUnicode("❓"));
                        Log(
                            $"PlaySound requested by {ctx.User.Username}#{ctx.User.Discriminator}, but no url was provided.");
                        return;
                    }

                    Log(
                        $"PlaySound requested by {ctx.User.Username}#{ctx.User.Discriminator} | URL: {ctx.Message.Attachments.FirstOrDefault()?.Url ?? url}");
                    using (var client = new WebClient())
                    {
                        if (ctx.Message.Attachments.Count != 0)
                        {
                            client.DownloadFile(ctx.Message.Attachments.First().Url, "temp.wav");
                        }
                        else if (url != "")
                        {
                            client.DownloadFile(url, "temp.wav");
                        }
                    }
                    try
                    {
                        var player = new SoundPlayer("temp.wav");
                        player.Play();
                    }
                    catch (Exception)
                    {
                        await ctx.RespondAsync("Only .wav files are supported at the moment, sorry!");
                    }
                }
                catch (Exception e)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("Error playing TTS: " + e.Message);
                    Console.ResetColor();
                }
            }
            else
            {
                await ctx.Message.CreateReactionAsync(DiscordEmoji.FromUnicode("🔕"));
            }
        }

        [Command("say"), Aliases("tts"), Cooldown(1, 5, CooldownBucketType.Global)]
        public async Task Say(CommandContext ctx, [RemainingText] string input)
        {
            if (Program.Config.TtsEnabled)
            {
                if (Program.IsPlaying)
                {
                    await ctx.RespondAsync("Already playing something");
                    return;
                }
                try
                {
                    Log($"TTS requested by {ctx.User.Username}#{ctx.User.Discriminator}. Text: {input}");
                    var synth = new SpeechSynthesizer();
                    await ctx.Message.CreateReactionAsync(DiscordEmoji.FromName(Program.Client, ":mega:"));
                    var p = new Prompt(input);
                    synth.SpeakAsync(p);
                    Program.IsPlaying = true;
                    synth.SpeakCompleted += async (sender, args) =>
                    {
                        await ctx.Message.DeleteOwnReactionAsync(DiscordEmoji.FromName(Program.Client, ":mega:"));
                        await ctx.Message.CreateReactionAsync(DiscordEmoji.FromUnicode("✅"));
                        Program.IsPlaying = false;
                    };
                }
                catch (Exception e)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("Error playing TTS: " + e.Message);
                    Console.ResetColor();
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
            if (Program.Config.SsEnabled)
            {
                try
                {
                    Log($"Screenshot requested by {ctx.User.Username}#{ctx.User.Discriminator}");
                    var vScreen = SystemInformation.VirtualScreen;
                    var player = new SoundPlayer("screenshot.wav");
                    player.Play();
                    using (var bitmap = new Bitmap(vScreen.Width, vScreen.Height))
                    {
                        using (var g = Graphics.FromImage(bitmap))
                        {
                            g.CopyFromScreen(SystemInformation.VirtualScreen.Left, SystemInformation.VirtualScreen.Top,
                                0, 0, bitmap.Size);
                        }

                        await ctx.Message.CreateReactionAsync(DiscordEmoji.FromUnicode("📸"));
                        bitmap.Save("ss.png");
                        if (Program.Config.BlurAmount > 0)
                        {
                            using (var image = Image.Load("ss.png"))
                            {
                                var w = image.Width;
                                var h = image.Height;
                                image.Mutate(
                                    x => x.Resize(h / Program.Config.BlurAmount, h / Program.Config.BlurAmount));
                                image.Mutate(x => x.Resize(w, h));
                                image.Save("ss.png");
                            }
                        }
                    }

                    await ctx.Channel.SendFileAsync("ss.png");
                    player.Dispose();
                    File.Delete("ss.png");
                }
                catch (Exception e)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("Error taking Screenshot: " + e.Message);
                    Console.ResetColor();
                }
            }
            else
            {
                await ctx.Message.CreateReactionAsync(DiscordEmoji.FromUnicode("🔕"));
            }
        }

        private static void Log(string message)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine(
                $"[{DateTime.Now.ToLongTimeString()}] {message}");
            Console.ResetColor();
        }

        [Command("toggle"), RequireOwner]
        public async Task Toggle(CommandContext ctx, string input = "")
        {
            switch (input.ToLower())
            {
                case "off":
                    Program.Config.SsEnabled = !Program.Config.SsEnabled;
                    Program.Config.CamEnabled = !Program.Config.CamEnabled;
                    Program.Config.TtsEnabled = !Program.Config.TtsEnabled;
                    break;
                case "cam":
                    Program.Config.CamEnabled = !Program.Config.CamEnabled;
                    break;
                case "tts":
                    Program.Config.TtsEnabled = !Program.Config.TtsEnabled;
                    break;
                case "ss":
                    Program.Config.SsEnabled = !Program.Config.SsEnabled;
                    break;
                case "screenshot":
                    Program.Config.SsEnabled = !Program.Config.SsEnabled;
                    break;
                case "play":
                    Program.Config.PlayEnabled = !Program.Config.PlayEnabled;
                    break;
                default:
                    await ctx.RespondAsync("Available toggles: `off`, `cam`, `tts`, `screenshot`, `play`.");
                    return;
            }

            await ctx.Message.CreateReactionAsync(DiscordEmoji.FromUnicode("✅"));
            Program.Config.SaveCfg();
        }

        [Command("blur"), RequireOwner]
        public async Task SetBlur(CommandContext ctx, int amount)
        {
            Program.Config.BlurAmount = amount;
            await ctx.Message.CreateReactionAsync(DiscordEmoji.FromUnicode("✅"));
            Program.Config.SaveCfg();
        }

        [Command("timer"), RequireOwner]
        public async Task SetTimer(CommandContext ctx, int amount)
        {
            Program.Config.CamTimer = amount;
            await ctx.Message.CreateReactionAsync(DiscordEmoji.FromUnicode("✅"));
            Program.Config.SaveCfg();
        }

        [Command("cfg")]
        public async Task PrintCfg(CommandContext ctx)
        {
            await ctx.RespondAsync($"```{Program.Config}```");
        }

        [Command("savecfg"), RequireOwner]
        public async Task SaveCfg(CommandContext ctx)
        {
            Program.Config.SaveCfg();
            await ctx.Message.CreateReactionAsync(DiscordEmoji.FromUnicode("✅"));
        }
    }
}