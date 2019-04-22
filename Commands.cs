using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Media;
using System.Net;
using System.Speech.Synthesis;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using AForge.Video.DirectShow;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using NAudio.Wave;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using Image = SixLabors.ImageSharp.Image;
using Timer = System.Timers.Timer;

namespace StalkBot
{
    public class Commands
    {
        [Command("processes"), Aliases("proc"), Description("Shows 15 processes sorted by Memory Usage.")]
        public async Task ActiveWindow(CommandContext ctx)
        {
            if (Program.Config.ProcessesEnabled)
            {
                Log($"Processes requested by {ctx.User.Username}#{ctx.User.Discriminator}");
                var procs = Process.GetProcesses().Where(x => x.SessionId == Process.GetCurrentProcess().SessionId)
                    .OrderByDescending(x => x.PrivateMemorySize64)
                    .ToList();
                var table = new ConsoleTable("Name", "Memory", "Uptime");
                foreach (var process in procs.Take(15))
                {
                    try
                    {
                        table.AddRow($"{process.ProcessName}", $"{process.PrivateMemorySize64 / 1000000}MB",
                            $"{DateTime.Now - process.StartTime:h'h 'm'm 's's'}");
                    }
                    catch (Exception)
                    {
                        table.AddRow($"{process.ProcessName}", $"{process.PrivateMemorySize64 / 1000000}MB",
                            "Not available!");
                    }
                }

                var response = table.Configure(x => x.NumberAlignment = Alignment.Right).ToMinimalString();
                await ctx.RespondAsync($"```{response}```");
            }
            else
            {
                Log($"Processes requested by {ctx.User.Username}#{ctx.User.Discriminator}, but it was toggled off.");
                await ctx.Message.CreateReactionAsync(DiscordEmoji.FromUnicode("🔕"));
            }
        }
        
        [Command("cursor"),
         Description("Either puts the cursor to random locations for a few seconds or to a specified x/y position."),
         Cooldown(1, 5, CooldownBucketType.Global)]
        [SuppressMessage("ReSharper", "PossibleNullReferenceException")]
        public async Task Cursor(CommandContext ctx, int x = 0, int y = 0)
        {
            if (Program.Config.CursorEnabled)
            {
                Log($"Cursor requested by {ctx.User.Username}#{ctx.User.Discriminator}");
                var height = SystemInformation.VirtualScreen.Height;
                var width = SystemInformation.VirtualScreen.Width;
                var rand = new Random();
                if (x < 0 || x > height)
                {
                    x = 0;
                }

                if (x < 0 || x > width)
                {
                    y = 0;
                }

                PlayAlert("cursor.wav");
                var pc = new PointConverter();
                Point pt;

                if (x == 0 && y == 0)
                {
                    for (var i = 0; i < 25; i++)
                    {
                        pt = (Point) pc.ConvertFromString($"{rand.Next(width)}, {rand.Next(height)}");
                        System.Windows.Forms.Cursor.Position = pt;
                        await Task.Delay(50);
                    }

                    await ctx.Message.CreateReactionAsync(DiscordEmoji.FromUnicode("✅"));
                    return;
                }

                pt = (Point) pc.ConvertFromString($"{x}, {y}");
                System.Windows.Forms.Cursor.Position = pt;
                await ctx.Message.CreateReactionAsync(DiscordEmoji.FromUnicode("✅"));
            }
            else
            {
                Log($"Cursor requested by {ctx.User.Username}#{ctx.User.Discriminator}, but it was toggled off.");
                await ctx.Message.CreateReactionAsync(DiscordEmoji.FromUnicode("🔕"));
            }
        }

        [Command("folder"), Cooldown(1, 5, CooldownBucketType.Global),
         Description("Sends a file from a folder, if set.")]
        public async Task Folder(CommandContext ctx)
        {
            if (Program.Config.FolderPath != "" && !Program.Blacklist.Contains(ctx.User))
            {
                PlayAlert("folder.wav");
                var rand = new Random();
                var fileToSend = "";
                try
                {
                    var files = Directory.GetFiles(Program.Config.FolderPath);
                    fileToSend = files[rand.Next(files.Length)];
                    await ctx.Channel.SendFileAsync(fileToSend);
                    Log(
                        $"Folder requested by {ctx.User.Username}#{ctx.User.Discriminator}, sent file \"{fileToSend.Substring(fileToSend.LastIndexOf(Path.DirectorySeparatorChar) + 1)}\".");
                }
                catch (Exception e)
                {
                    await ctx.Message.CreateReactionAsync(DiscordEmoji.FromUnicode("❌"));
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine(
                        $"Error sending file {fileToSend.Substring(fileToSend.LastIndexOf(Path.DirectorySeparatorChar) + 1)}: " +
                        e.Message);
                    Console.ResetColor();
                }
            }
            else
            {
                Log($"Folderpath requested by {ctx.User.Username}#{ctx.User.Discriminator}, but it was not set.");
                await ctx.Message.CreateReactionAsync(DiscordEmoji.FromUnicode("🔕"));
            }
        }

        [Command("webcam"), Aliases("wc", "cam"), Cooldown(1, 5, CooldownBucketType.Global),
         Description("Captures the webcam.")]
        public async Task WebCam(CommandContext ctx)
        {
            if (Program.Config.CamEnabled && !Program.Blacklist.Contains(ctx.User))
            {
                Log($"Webcam requested by {ctx.User.Username}#{ctx.User.Discriminator}");
                // ReSharper disable once CollectionNeverUpdated.Local
                try
                {
                    var videoDevices = new FilterInfoCollection(FilterCategory.VideoInputDevice);
                    var cam = new VideoCaptureDevice(videoDevices[0].MonikerString);
                    PlayAlert("webcam.wav");
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

        [Command("play"), Cooldown(1, 5, CooldownBucketType.Global),
         Description("Plays the attached file or url to a file.")]
        public async Task Play(CommandContext ctx, string url = "")
        {
            if (Program.Config.PlayEnabled && !Program.Blacklist.Contains(ctx.User))
            {
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

                    await ctx.Message.CreateReactionAsync(DiscordEmoji.FromUnicode("▶"));
                    var abort = false;

                    using (var audioFile = new AudioFileReader("temp.wav"))
                    using (var outputDevice = new WaveOutEvent())
                    {
                        var timer = new Timer
                        {
                            Interval = Program.Config.Timeout + 0.0001,
                            AutoReset = false
                        };
                        outputDevice.Init(audioFile);
                        outputDevice.Play();
                        outputDevice.PlaybackStopped += async (sender, args) =>
                        {
                            timer.Stop();
                            if (abort) return;
                            await ctx.Message.DeleteOwnReactionAsync(DiscordEmoji.FromUnicode("▶"));
                            await ctx.Message.CreateReactionAsync(DiscordEmoji.FromUnicode("✅"));
                        };

                        if (Program.Config.Timeout > 0.0)
                        {
                            timer.Start();
                            timer.Elapsed += async (sender, args) =>
                            {
                                abort = true;
                                // ReSharper disable once AccessToDisposedClosure
                                outputDevice.Stop();
                                await ctx.Message.DeleteOwnReactionAsync(DiscordEmoji.FromUnicode("▶"));
                                await ctx.Message.CreateReactionAsync(DiscordEmoji.FromUnicode("🛑"));
                            };
                        }

                        while (outputDevice.PlaybackState == PlaybackState.Playing)
                        {
                            Thread.Sleep(1000);
                        }
                    }
                    
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

        [Command("say"), Aliases("tts"), Cooldown(1, 5, CooldownBucketType.Global),
         Description("Tells the user something via text-to-speech.")]
        public async Task Say(CommandContext ctx, [RemainingText] string input)
        {
            if (Program.Config.TtsEnabled && !Program.Blacklist.Contains(ctx.User))
            {
                try
                {
                    Log($"TTS requested by {ctx.User.Username}#{ctx.User.Discriminator}. Text: {input}");
                    var synth = new SpeechSynthesizer();
                    await ctx.Message.CreateReactionAsync(DiscordEmoji.FromName(Program.Client, ":mega:"));
                    var p = new Prompt(input);
                    var timer = new Timer
                    {
                        Enabled = true,
                        Interval = Program.Config.Timeout,
                        AutoReset = false
                    };
                    if (Program.Config.Timeout > 0.0)
                    {
                        timer.Start();
                        timer.Elapsed += async (sender, args) =>
                        {
                            synth.Dispose();
                            await ctx.Message.DeleteOwnReactionAsync(DiscordEmoji.FromName(Program.Client, ":mega:"));
                            await ctx.Message.CreateReactionAsync(DiscordEmoji.FromUnicode("🛑"));
                        };
                        synth.SpeakAsync(p);
                        synth.SpeakCompleted += async (sender, args) =>
                        {
                            await ctx.Message.DeleteOwnReactionAsync(DiscordEmoji.FromName(Program.Client, ":mega:"));
                            await ctx.Message.CreateReactionAsync(DiscordEmoji.FromUnicode("✅"));
                            synth.Dispose();
                            timer.Stop();
                            timer.Dispose();
                        };
                    }
                    else
                    {
                        synth.Speak(p);
                        synth.Dispose();
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
                Log($"TTS requested by {ctx.User.Username}#{ctx.User.Discriminator}, but it was toggled off.");
                await ctx.Message.CreateReactionAsync(DiscordEmoji.FromUnicode("🔕"));
            }
        }

        [Command("screenshot"), Aliases("ss", "sc"), Cooldown(1, 5, CooldownBucketType.Global),
         Description("Captures a screenshot.")]
        public async Task Screen(CommandContext ctx)
        {
            if (Program.Config.SsEnabled && !Program.Blacklist.Contains(ctx.User))
            {
                try
                {
                    Log($"Screenshot requested by {ctx.User.Username}#{ctx.User.Discriminator}");
                    var vScreen = SystemInformation.VirtualScreen;
                    PlayAlert("screenshot.wav");
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

        [Command("cfg"), Description("Shows the bots config.")]
        public async Task PrintCfg(CommandContext ctx)
        {
            Log($"Config requested by {ctx.User.Username}#{ctx.User.Discriminator}.");
            await ctx.RespondAsync($"```{Program.Config}```");
        }

        [Command("blacklist"), Aliases("hurensohn"), RequireOwner, Description("Manage your user blacklist.")]
        public async Task BlackList(CommandContext ctx, string option, DiscordUser user = null)
        {
            try
            {
                switch (option.ToLower())
                {
                    case "add":
                        if (ctx.User == user)
                        {
                            await ctx.RespondAsync("Bad Idea.");
                            return;
                        }

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

        [Command("toggle"), RequireOwner, Description("Toggle specific settings on or off.")]
        public async Task Toggle(CommandContext ctx, string input = "")
        {
            switch (input.ToLower())
            {
                case "off":
                    Program.Config.SsEnabled = false;
                    Program.Config.CamEnabled = false;
                    Program.Config.TtsEnabled = false;
                    Program.Config.PlayEnabled = false;
                    Program.Config.CursorEnabled = false;
                    Program.Config.ProcessesEnabled = false;
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
                case "cursor":
                    Program.Config.CursorEnabled = !Program.Config.CursorEnabled;
                    Log($"Toggled cursorcontrol to: {Program.Config.CursorEnabled}.");
                    break;
                case "proc":
                    Program.Config.ProcessesEnabled = !Program.Config.ProcessesEnabled;
                    Log($"Toggled processes to: {Program.Config.ProcessesEnabled}.");
                    break;
                default:
                    await ctx.RespondAsync(
                        "Available toggles: `off`, `cam`, `tts`, `screenshot`, `play`, `cursor`, `proc`.");
                    return;
            }

            await ctx.Message.CreateReactionAsync(DiscordEmoji.FromUnicode("✅"));
            Config.SaveCfg();
        }

        [Command("timeout"), RequireOwner, Description("Set a timeout for TTS/Playsounds.")]
        public async Task Timeout(CommandContext ctx, int timeout)
        {
            Program.Config.Timeout = timeout;
            await ctx.Message.CreateReactionAsync(DiscordEmoji.FromUnicode("✅"));
            Config.SaveCfg();
        }

        [Command("blur"), RequireOwner, Description("Set a blur for your screenshot. Only 0.0 to 5.0 is allowed.")]
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

        [Command("timer"), RequireOwner, Description("Sets the time the webcam waits before taking a photo.")]
        public async Task SetTimer(CommandContext ctx, int amount)
        {
            Program.Config.CamTimer = amount;
            await ctx.Message.CreateReactionAsync(DiscordEmoji.FromUnicode("✅"));
            Log($"Cam timer changed to {amount.ToString()}.");
            Config.SaveCfg();
        }

        private static void PlayAlert(string filename)
        {
            if (!File.Exists(filename)) return;
            var player = new SoundPlayer(filename);
            player.Play();
            player.Dispose();
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