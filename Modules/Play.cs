using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using NAudio.Wave;
using StalkBot.Utilities;
using Timer = System.Timers.Timer;

namespace StalkBot.Modules
{
    public class Play : BaseCommandModule
    {
        [Command("play"), Cooldown(1, 5, CooldownBucketType.Global),
         Description("Plays the attached file or url to a file.")]
        public async Task PlayFile(CommandContext ctx, string url = "")
        {
            if (!Bot.Config.PlayEnabled)
            {
                Logger.Log(
                    $"PlaySound requested by {ctx.User.Username}#{ctx.User.Discriminator}, but it was toggled off.", ctx,
                    LogLevel.Info);
                await ctx.Message.CreateReactionAsync(DiscordEmoji.FromUnicode("🔕"));
                return;
            }

            //Process File Indicator
            await ctx.Message.CreateReactionAsync(DiscordEmoji.FromUnicode("♨"));

            var downloadUrl = CheckFile(ctx, url);
            if (!downloadUrl.HasValue)
            {
                await ctx.Message.CreateReactionAsync(DiscordEmoji.FromUnicode("❌"));
                return;
            }

            using (var client = new WebClient())
            {
                client.DownloadFile(downloadUrl.Value.Key, $"temp{downloadUrl.Value.Value}");
            }

            Logger.Log($"PlaySound requested by {ctx.User.Username}#{ctx.User.Discriminator} | URL: {downloadUrl.Value.Key}", ctx, LogLevel.Info);
            
            await ConvertAudio($"temp{downloadUrl.Value.Value}");
            await ctx.Message.DeleteOwnReactionAsync(DiscordEmoji.FromUnicode("♨"));

            await ctx.Message.CreateReactionAsync(DiscordEmoji.FromUnicode("▶"));
            var abort = false;

            var outputDevice = new WaveOutEvent();
            using (var audioFile = new AudioFileReader("temp.wav"))
            {
                var timer = new Timer
                {
                    Interval = Bot.Config.Timeout + 0.0001,
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

                if (Bot.Config.Timeout > 0.0)
                {
                    timer.Elapsed += async (sender, args) =>
                    {
                        abort = true;
                        outputDevice.Stop();
                        await ctx.Message.DeleteOwnReactionAsync(DiscordEmoji.FromUnicode("▶"));
                        await ctx.Message.CreateReactionAsync(DiscordEmoji.FromUnicode("🛑"));
                    };
                    timer.Start();
                }

                while (outputDevice.PlaybackState == PlaybackState.Playing)
                {
                    Thread.Sleep(1000);
                }
            }

            try
            {
                File.Delete("temp.wav");
                File.Delete($"temp{downloadUrl.Value.Value}");
            }catch (Exception) { /*ignored*/ }
        }
        
        private static KeyValuePair<string, string>? CheckFile(CommandContext ctx, string url)
        {
            //Case 1: No url or attachment
            if (url == "" && ctx.Message.Attachments.Count == 0)
            {
                Logger.Log(
                    $"PlaySound requested by {ctx.User.Username}#{ctx.User.Discriminator}, but no url was provided.", ctx,
                    LogLevel.Warning);
                return null;
            }

            //Case 2: Everything is fine
            var downloadUrl = ctx.Message.Attachments.Count == 0 ? url : ctx.Message.Attachments.First().Url;
            var fileType = downloadUrl.Substring(downloadUrl.LastIndexOf('.'));
            var rx = new Regex(@"^\.[\w\d]+$");
            if (rx.IsMatch(fileType))
                return new KeyValuePair<string, string>(downloadUrl, fileType);

            //Case 3: Non Media File requested
            Logger.Log($"{ctx.User.Username}#{ctx.User.Discriminator} requested a bad/malicious url or file!", ctx, 
                LogLevel.Warning);
            return null;
        }

        private static Task ConvertAudio(string filename)
        {
            using (var exeProcess = Process.Start(new ProcessStartInfo
            {
                FileName = "ffmpeg.exe",
                Arguments = $"-y -i {filename} -af volume=-25dB,loudnorm=tp=0 -ar 44100 -ac 2 temp.wav",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            }))
            {
                exeProcess?.WaitForExit();
            }

            return Task.CompletedTask;
        }
    }
}