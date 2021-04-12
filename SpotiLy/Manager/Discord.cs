using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using SpotiLy.Config;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SpotiLy.Manager
{
    public class Discord
    {
        private Configuration spotilyConfig;
        private DiscordConfiguration config;
        private DiscordChannel logChannel;
        public Discord(Configuration spotilyConfig, DiscordConfiguration config)
        {
            this.spotilyConfig = spotilyConfig;
            this.config = config;
        }

        public async Task Initialize()
        {
            DiscordClient client = new DiscordClient(config);
            var act = new DiscordActivity("SpotiLy - Stats", ActivityType.ListeningTo);
            await client.ConnectAsync(act, UserStatus.DoNotDisturb).ConfigureAwait(false);

            client.Ready += this.Discord_Ready;
            client.GuildAvailable += this.Discord_GuildAvailable;
        }

        private Task Discord_Ready(DiscordClient client, ReadyEventArgs e)
        {
            return Task.CompletedTask;
        }

        private async Task Discord_GuildAvailable(DiscordClient client, GuildCreateEventArgs e)
        {
            logChannel = e.Guild.Channels.Where(x => x.Value.Name == "spotily-logs").Select(x => x.Value).FirstOrDefault();
            if(logChannel == null)
            {
                DiscordOverwriteBuilder discordOverwriteBuilder = new DiscordOverwriteBuilder();
                discordOverwriteBuilder.For(e.Guild.EveryoneRole);
                discordOverwriteBuilder.Denied = Permissions.AccessChannels;
                logChannel = await e.Guild.CreateChannelAsync("spotily-logs", ChannelType.Text, overwrites: new List<DiscordOverwriteBuilder>() { discordOverwriteBuilder });
            }
            CancellationTokenSource tokenSource = new CancellationTokenSource();
            Task timerTask = RunPeriodically(client, TimeSpan.FromMinutes(1), tokenSource.Token);
        }

        async Task RunPeriodically(DiscordClient client, TimeSpan interval, CancellationToken token)
        {
            while (true)
            {
                await Task.Run(() => SendMessage(client));
                await Task.Delay(interval, token);
            }
        }

        private async Task SendMessage(DiscordClient client)
        {
            var history = await logChannel.GetMessagesAsync();
            if (history.Count > 0)
            {
                await logChannel.DeleteMessagesAsync(history);
            }

            DiscordEmbedBuilder embed = new DiscordEmbedBuilder()
            {
                Thumbnail = new DiscordEmbedBuilder.EmbedThumbnail()
                {
                    Url = "https://cdn.discordapp.com/attachments/750370252650446848/779303746688909322/SL.png",
                },
                Color = DiscordColor.SpringGreen,
            };

            var memory = new MemoryMetricsClient();
            var metrics = memory.GetMetrics();

            embed.AddField("〽️ CPU Usage", $"0%", true);
            embed.AddField("🧠 Memory Usage", $"{string.Format("{0:n0}", metrics.Used)} / {string.Format("{0:n0}", metrics.Total)} Mo", true);

            embed.AddField("\u200B", "\u200B");

            embed.AddField("🆓 Threads", spotilyConfig.Threads.ToString(), true);

            embed.AddField("\u200B", "\u200B");

            embed.AddField("🆓 accounts", StatManager.Instance.FreeGenerate.ToString(), true);
            embed.AddField("❤️ Attemps", StatManager.Instance.Attemps.ToString(), true);

            embed.AddField("\u200B", "\u200B");

            embed.AddField(
                "👑 streams",
                 $"{string.Format("{0:n0}", StatManager.Instance.PremiumStreams)} in total"
                + $"\n ~ {string.Format("{0:n0}", 60 * 60 * 24 / ((spotilyConfig.MinSkip + spotilyConfig.MaxSkip) / 2) * spotilyConfig.Threads)} per day", true
            );

            embed.AddField("\u200B", "\u200B");

            embed.AddField("💙 Albums likes", StatManager.Instance.AlbumLikes.ToString(), true);
            embed.AddField("💛 Songs likes", StatManager.Instance.SongLikes.ToString(), true);
            embed.AddField("💚 Artists follows", StatManager.Instance.Follows.ToString(), true);

            embed.WithTimestamp(StatManager.Instance.StartDate);
            embed.WithFooter("⚙");
            await logChannel.SendMessageAsync(embed: embed);
        }
    }
}