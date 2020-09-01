using System.Speech.Synthesis;
using System.Threading.Tasks;
using System.Timers;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using StalkBot.Utilities;

namespace StalkBot.Modules
{
    public class TextToSpeech : BaseCommandModule
    {
        [Command("say"), Aliases("tts"), Cooldown(1, 5, CooldownBucketType.Global),
         Description("Tells the user something via text-to-speech.")]
        public async Task Say(CommandContext ctx, [RemainingText] string input)
        {
            if (!Bot.Config.TtsEnabled)
            {
                Logger.Log($"TTS requested by {ctx.User.Username}#{ctx.User.Discriminator}, but it was toggled off.", ctx,
                    LogLevel.Info);
                await ctx.Message.CreateReactionAsync(DiscordEmoji.FromUnicode("🔕"));
                return;
            }

            var synth = new SpeechSynthesizer();
            Logger.Log($"TTS requested by {ctx.User.Username}#{ctx.User.Discriminator}. Text: {input}", ctx,
                LogLevel.Info);

            await ctx.Message.CreateReactionAsync(DiscordEmoji.FromUnicode("📣"));
            var p = new Prompt(input);
            var timer = new Timer
            {
                Enabled = true,
                Interval = Bot.Config.Timeout,
                AutoReset = false
            };

            if (Bot.Config.Timeout > 0.0)
            {
                timer.Start();
                timer.Elapsed += async (sender, args) =>
                {
                    await ctx.Message.DeleteOwnReactionAsync(DiscordEmoji.FromUnicode("📣"));
                    await ctx.Message.CreateReactionAsync(DiscordEmoji.FromUnicode("🛑"));
                };
                synth.SpeakAsync(p);
                synth.SpeakCompleted += async (sender, args) =>
                {
                    await ctx.Message.DeleteOwnReactionAsync(DiscordEmoji.FromUnicode("📣"));
                    await ctx.Message.CreateReactionAsync(DiscordEmoji.FromUnicode("✅"));
                    timer.Stop();
                    timer.Dispose();
                };
            }
            else
            {
                synth.Speak(p);
            }
        }
    }
}