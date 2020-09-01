using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using StalkBot.Utilities;

namespace StalkBot.Modules
{
    public class Processes : BaseCommandModule
    {
        [Command("processes"), Aliases("proc"), Description("Shows 15 processes sorted by Memory Usage.")]
        public async Task GetProcesses(CommandContext ctx)
        {
            if (!Bot.Config.ProcessesEnabled)
            {
                Logger.Log(
                    $"Processes requested by {ctx.User.Username}#{ctx.User.Discriminator}, but it was toggled off.", ctx, LogLevel.Info);
                await ctx.Message.CreateReactionAsync(DiscordEmoji.FromUnicode("🔕"));
                return;
            }

            Logger.Log($"Processes requested by {ctx.User.Username}#{ctx.User.Discriminator}", ctx, LogLevel.Info);
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
    }
}