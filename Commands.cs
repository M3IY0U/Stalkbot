using System;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Media;
using System.Net;
using System.Speech.Synthesis;
using System.Text.RegularExpressions;
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
        [Command("folder"), Cooldown(1, 5, CooldownBucketType.Global)]
        public async Task Folder(CommandContext ctx)
        {
            if (Program.Config.FolderPath != "")
            {
                var rand = new Random();
                var fileToSend = "";
                try
                {
                    var files = Directory.GetFiles(Program.Config.FolderPath);
                    fileToSend = files[rand.Next(files.Length)];
                    await ctx.Channel.SendFileAsync(fileToSend);
                    Log($"Folder requested by {ctx.User.Username}#{ctx.User.Discriminator}, sent file \"{fileToSend.Substring(fileToSend.LastIndexOf(Path.DirectorySeparatorChar)+1)}\".");
                }
                catch (Exception e)
                {
                    await ctx.Message.CreateReactionAsync(DiscordEmoji.FromUnicode("❌"));
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"Error sending file {fileToSend.Substring(fileToSend.LastIndexOf(Path.DirectorySeparatorChar)+1)}: " + e.Message);
                    Console.ResetColor();
                }
            }
            else
            {
                Log($"Folderpath requested by {ctx.User.Username}#{ctx.User.Discriminator}, but it was not set.");
                await ctx.Message.CreateReactionAsync(DiscordEmoji.FromUnicode("🔕"));
            }
        }

        [Command("webcam"), Aliases("wc", "cam"), Cooldown(1, 5, CooldownBucketType.Global)]
        public async Task WebCam(CommandContext ctx)
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
                Log($"Webcam requested by {ctx.User.Username}#{ctx.User.Discriminator}, but it was toggled off.");
                await ctx.Message.CreateReactionAsync(DiscordEmoji.FromUnicode("🔕"));
            }
        }

        [Command("play"), Cooldown(1, 5, CooldownBucketType.Global)]
        public async Task Play(CommandContext ctx, string url = "")
        {
            if (Program.Config.PlayEnabled)
            {
                if (Program.IsPlaying)
                {
                    await ctx.RespondAsync("Already playing something");
                    return;
                }

                await ctx.Message.CreateReactionAsync(DiscordEmoji.FromUnicode("♨"));

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

                    var downloadUrl = ctx.Message.Attachments.Count == 0 ? url : ctx.Message.Attachments.First().Url;

                    if (string.IsNullOrWhiteSpace(downloadUrl))
                    {
                        await ctx.Message.CreateReactionAsync(DiscordEmoji.FromUnicode("❌"));
                        return;
                    }

                    var fileType = downloadUrl.Substring(downloadUrl.LastIndexOf('.'));

                    var rx = new Regex(@"^\.[\w\d]+$");
                    if (!rx.IsMatch(fileType))
                    {
                        await ctx.Message.CreateReactionAsync(DiscordEmoji.FromUnicode("❌"));
                        Console.ForegroundColor = ConsoleColor.DarkYellow;
                        Console.WriteLine(
                            $"{ctx.User.Username}#{ctx.User.Discriminator} requested a bad/malicious url or file!");
                        Console.ResetColor();
                        return;
                    }

                    using (var client = new WebClient())
                    {
                        client.DownloadFile(downloadUrl, "temp" + fileType);
                        client.Dispose();
                    }

                    try
                    {
                        await ConvertAudio("temp" + fileType);
                    }
                    catch (Exception e)
                    {
                        await ctx.Message.CreateReactionAsync(DiscordEmoji.FromUnicode("❌"));
                        Console.WriteLine("Error converting audio: " + e.Message);
                        return;
                    }

                    await ctx.Message.DeleteOwnReactionAsync(DiscordEmoji.FromUnicode("♨"));
                    using (var player = new SoundPlayer("temp.wav"))
                    {
                        await ctx.Message.CreateReactionAsync(DiscordEmoji.FromUnicode("▶"));
                        player.PlaySync();
                        player.Dispose();
                    }

                    await ctx.Message.DeleteOwnReactionAsync(DiscordEmoji.FromUnicode("▶"));
                    await ctx.Message.CreateReactionAsync(DiscordEmoji.FromUnicode("✅"));
                    File.Delete("temp" + fileType);
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
                Log($"PlaySound requested by {ctx.User.Username}#{ctx.User.Discriminator}, but it was toggled off.");
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
                Log($"TTS requested by {ctx.User.Username}#{ctx.User.Discriminator}, but it was toggled off.");
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
                                image.Mutate(x => x.GaussianBlur((float) Program.Config.BlurAmount));
                                image.Save("ss.png");
                                image.Dispose();
                            }
                        }

                        bitmap.Dispose();
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
                Log($"Screenshot requested by {ctx.User.Username}#{ctx.User.Discriminator}, but it was toggled off.");
                await ctx.Message.CreateReactionAsync(DiscordEmoji.FromUnicode("🔕"));
            }
        }
        
        [Command("cfg")]
        public async Task PrintCfg(CommandContext ctx)
        {
            Log($"Config requested by {ctx.User.Username}#{ctx.User.Discriminator}.");
            await ctx.RespondAsync($"```{Program.Config}```");
        }

        [Command("blacklist"), Aliases("hurensohn"), RequireOwner]
        public async Task BlackList(CommandContext ctx, string option, DiscordUser user = null)
        {
            try
            {
                switch (option.ToLower())
                {
                    case "add":
                        Program.Blacklist.Add(user);
                        break;
                    case "remove":
                        Program.Blacklist.Remove(user);
                        break;
                    case "clear":
                        Program.Blacklist.Clear();
                        break;
                    case "print":
                        if (Program.Blacklist.Count == 0)
                        {
                            await ctx.RespondAsync("Blacklist is empty.");
                        }
                        else
                        {
                            await ctx.RespondAsync($"```{string.Join("\n", Program.Blacklist)}```");
                        }

                        break;
                    default:
                        await ctx.Message.CreateReactionAsync(DiscordEmoji.FromUnicode("❌"));
                        return;
                }

                await ctx.Message.CreateReactionAsync(DiscordEmoji.FromUnicode("✅"));
            }
            catch (Exception e)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Error modifying blacklist: " + e.Message);
                Console.ResetColor();
            }
        }

        [Command("toggle"), RequireOwner]
        public async Task Toggle(CommandContext ctx, string input = "")
        {
            switch (input.ToLower())
            {
                case "off":
                    Program.Config.SsEnabled = false;
                    Program.Config.CamEnabled = false;
                    Program.Config.TtsEnabled = false;
                    Program.Config.PlayEnabled = false;
                    Log("Turned off everything.");
                    break;
                case "cam":
                    Program.Config.CamEnabled = !Program.Config.CamEnabled;
                    Log($"Toggled webcam to: {Program.Config.CamEnabled}.");
                    break;
                case "tts":
                    Program.Config.TtsEnabled = !Program.Config.TtsEnabled;
                    Log($"Toggled tts to: {Program.Config.TtsEnabled}.");
                    break;
                case "ss":
                    Program.Config.SsEnabled = !Program.Config.SsEnabled;
                    Log($"Toggled screenshot to: {Program.Config.SsEnabled}.");
                    break;
                case "screenshot":
                    Program.Config.SsEnabled = !Program.Config.SsEnabled;
                    Log($"Toggled screenshot to: {Program.Config.SsEnabled}.");
                    break;
                case "play":
                    Program.Config.PlayEnabled = !Program.Config.PlayEnabled;
                    Log($"Toggled playsounds to: {Program.Config.PlayEnabled}.");
                    break;
                default:
                    await ctx.RespondAsync("Available toggles: `off`, `cam`, `tts`, `screenshot`, `play`.");
                    return;
            }

            await ctx.Message.CreateReactionAsync(DiscordEmoji.FromUnicode("✅"));
            Config.SaveCfg();
        }

        [Command("blur"), RequireOwner]
        public async Task SetBlur(CommandContext ctx, double amount)
        {
            if (amount > 5 || amount < 0)
            {
                await ctx.Message.CreateReactionAsync(DiscordEmoji.FromUnicode("❌"));
                Console.ForegroundColor = ConsoleColor.DarkYellow;
                Console.WriteLine(
                    $"[{DateTime.Now.ToLongTimeString()}] Tried setting blur to something > 5 or < 1.");
                Console.ResetColor();
                return;
            }

            Program.Config.BlurAmount = amount;
            await ctx.Message.CreateReactionAsync(DiscordEmoji.FromUnicode("✅"));
            Log($"Blur changed to {amount.ToString(CultureInfo.InvariantCulture)}.");
            Config.SaveCfg();
        }

        [Command("timer"), RequireOwner]
        public async Task SetTimer(CommandContext ctx, int amount)
        {
            Program.Config.CamTimer = amount;
            await ctx.Message.CreateReactionAsync(DiscordEmoji.FromUnicode("✅"));
            Log($"Cam timer changed to {amount.ToString()}.");
            Config.SaveCfg();
        }

        private static Task ConvertAudio(string filename)
        {
            using (var exeProcess = Process.Start(new ProcessStartInfo
            {
                FileName = "ffmpeg.exe",
                Arguments = $"-y -i {filename} -af volume=-25dB temp.wav",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            }))
            {
                exeProcess?.WaitForExit();
            }

            return Task.CompletedTask;
        }

        private static void Log(string message)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine(
                $"[{DateTime.Now.ToLongTimeString()}] {message}");
            Console.ResetColor();
        }
    }
}