﻿using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;

using OsuParsers.Replays;
using OsuParsers.Decoders;

using Microsoft.Extensions.Logging;

using System;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;

using WAV_Bot_DSharp.Configurations;
using WAV_Bot_DSharp.Converters;

using WAV_Osu_NetApi;
using WAV_Osu_NetApi.Bancho.Models;
using WAV_Osu_NetApi.Gatari.Models;

namespace WAV_Bot_DSharp.Commands
{
    public class OsuCommands : SkBaseCommandModule
    {
        private ILogger<OsuCommands> logger;

        private DiscordChannel wavScoresChannel;
        private DiscordGuild guild;

        private WebClient webClient;
        private OsuUtils utils;
        private OsuEmoji emoji;

        private BanchoApi api;
        private GatariApi gapi;

        private readonly ulong WAV_UID = 708860200341471264;

        public OsuCommands(ILogger<OsuCommands> logger, DiscordClient client, OsuUtils utils, BanchoApi api, GatariApi gapi, OsuEmoji emoji)
        {
            ModuleName = "Osu commands";

            this.logger = logger;
            this.wavScoresChannel = client.GetChannelAsync(829466881353711647).Result;
            this.webClient = new WebClient();

            this.guild = client.GetGuildAsync(WAV_UID).Result;
            this.utils = utils;
            this.api = api;
            this.gapi = gapi;
            this.emoji = emoji;

            logger.LogInformation("OsuCommands loaded");

            client.MessageCreated += Client_MessageCreated;
        }

        private async Task Client_MessageCreated(DiscordClient sender, DSharpPlus.EventArgs.MessageCreateEventArgs e)
        {
            if (!e.Message.Content.Contains("http"))
                return;

            if (!(e.Channel.Name.Contains("-osu") || e.Channel.Name.Contains("map-offer") || e.Channel.Name.Contains("bot-debug") || e.Channel.Name.Contains("dev-announce")))
                return;

            // Check, if it is map url from bancho
            Tuple<int, int> BMSandBMid = utils.GetBMandBMSIdFromBanchoUrl(e.Message.Content);
            if (!(BMSandBMid is null))
            {
                int bms_id = BMSandBMid.Item1,
                    bm_id = BMSandBMid.Item2;

                Beatmap bm = api.GetBeatmap(bm_id);
                Beatmapset bms = api.GetBeatmapset(bms_id);
                GBeatmap gbm = gapi.TryGetBeatmap(bm_id);

                if (!(bm is null || bms is null))
                {
                    DiscordEmbed embed = utils.BeatmapToEmbed(bm, bms, gbm);
                    await e.Message.RespondAsync(embed: embed);
                }

                return;
            }

            // Check, if it is beatmapset url from gatari
            int? BMSid = utils.GetBMSIdFromGatariUrl(e.Message.Content);
            if (!(BMSid is null))
            {
                int bms_id = (int)BMSid;

                Beatmapset bms = api.GetBeatmapset(bms_id);

                int bm_id = bms.beatmaps.First().id;

                Beatmap bm = api.GetBeatmap(bm_id);
                GBeatmap gbm = gapi.TryGetBeatmap(bm_id);

                if (!(bm is null || bms is null))
                {
                    DiscordEmbed embed = utils.BeatmapToEmbed(bm, bms, gbm);
                    await e.Message.RespondAsync(embed: embed);
                }

                return;
            }

            // Check, if it is beatmap url from gatari
            int? BMid = utils.GetBMIdFromGatariUrl(e.Message.Content);
            if (!(BMid is null))
            {
                int bm_id = (int)BMid;

                Beatmap bm = api.GetBeatmap(bm_id);
                Beatmapset bms = api.GetBeatmapset(bm.beatmapset_id);
                GBeatmap gbm = gapi.TryGetBeatmap(bm_id);

                if (!(bm is null || bms is null))
                {
                    DiscordEmbed embed = utils.BeatmapToEmbed(bm, bms, gbm);
                    await e.Message.RespondAsync(embed: embed);
                }

                return;
            }

            // Check, if it is user link from bancho
            int? userId = utils.GetUserIdFromBanchoUrl(e.Message.Content);
            if (!(userId is null))
            {
                int user_id = (int)userId;

                User user = null;
                if (!api.TryGetUser(user_id, ref user))
                    return;

                List<Score> scores = api.GetUserBestScores(user_id, 5);

                if (!(scores is null) && scores.Count == 5)
                {
                    DiscordEmbed embed = utils.UserToEmbed(user, scores);
                    await e.Message.RespondAsync(embed: embed);
                }

                return;
            }
        }

        [Command("osu"), Description("Get osu profile information"), RequireGuild]
        public async Task OsuProfile(CommandContext commandContext,
            [Description("Osu nickname"), RemainingText] string nickname)
        {
            if (!commandContext.Channel.Name.Contains("-bot"))
            {
                await commandContext.RespondAsync("Использование данной команды запрещено в этом текстовом канале. Используйте специально отведенный канал для ботов, связанных с osu!.");
                return;
            }

            if (string.IsNullOrEmpty(nickname))
            {
                await commandContext .RespondAsync("Вы ввели пустую строку.");
                return;
            }

            User user = null;
            if (!api.TryGetUser(nickname, ref user))
            {
                await commandContext .RespondAsync($"Не удалось получить информацию о пользователе `{nickname}`.");
                return;
            }

            List<Score> scores = api.GetUserBestScores(user.id, 5);

            if (scores is null || scores.Count == 0)
            {
                await commandContext.RespondAsync($"Не удалось получить информацию о лучших скорах пользователя `{nickname}`.");
                return;
            }

            DiscordEmbed embed = utils.UserToEmbed(user, scores);
            await commandContext.RespondAsync(embed: embed);

        }

        [Command("submit"), RequireDirectMessage]
        public async Task SubmitScore(CommandContext commandContext)
        {
            DiscordMessage msg = await commandContext.Channel.GetMessageAsync(commandContext.Message.Id);

            logger.LogInformation($"DM {msg.Author}: {msg.Content} : {msg.Attachments.Count}");

            if (Settings.KOSTYL.IgnoreDMList.Contains(msg.Author.Id))
            {
                await commandContext.RespondAsync("Извините, но вы были внесены в черный список бота.");
                return;
            }

            if (msg.Attachments.Count == 0)
            {
                await commandContext.RespondAsync("Вы не прикрепили к сообщению никаких файлов.");
                return;
            }

            if (msg.Attachments.Count > 1)
            {
                await commandContext.RespondAsync("К сообщению можно прикрепить только один файл.");
                return;
            }

            DiscordAttachment attachment = msg.Attachments.First();

            if (!attachment.FileName.EndsWith("osr"))
            {
                await commandContext.RespondAsync("Файл не является реплеем.");
                return;
            }

            Replay replay = null;

            try
            {
                string fileName = $"{DateTime.Now.Ticks}-{attachment.FileName}";
                webClient.DownloadFile(attachment.Url, $"downloads/{fileName}");

                replay = ReplayDecoder.Decode($"downloads/{fileName}");
            }

            catch (Exception e)
            {
                logger.LogCritical(e, "Exception while parsing score");
            }

            DiscordMember member = await guild.GetMemberAsync(msg.Author.Id);
            string category = member.Roles.Select(x => x.Name)
                                          .Where((x) =>
                                          {
                                              foreach (var xx in (new string[] { "beginner", "alpha", "beta", "gamma", "delta", "epsilon" }))
                                                  if (x.Contains(xx))
                                                      return true;
                                              return false;
                                          })
                                          .FirstOrDefault();

            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"Osu nickname: `{replay.PlayerName}`");
            sb.AppendLine($"Discord nickname: `{msg.Author.Username}`");
            sb.AppendLine($"Score: `{replay.ReplayScore:N0}`"); // Format: 123456789 -> 123 456 789
            sb.AppendLine($"Category: `{category ?? "No category"}`");
            sb.AppendLine($"Mods: `{utils.ModsToString((WAV_Osu_NetApi.Bancho.Models.Enums.Mods)replay.Mods)}`");

            DiscordEmbedBuilder embed = new DiscordEmbedBuilder().WithAuthor(msg.Author.Username, iconUrl: msg.Author.AvatarUrl)
                                                                 .WithTitle($"Added replay {DateTime.Now.ToShortDateString()} {DateTime.Now.ToShortTimeString()}")
                                                                 .WithUrl(attachment.Url)
                                                                 .WithDescription(sb.ToString())
                                                                 .AddField("OSR Link:", attachment.Url)
                                                                 .AddField("File name:", $"`{attachment.FileName}`")
                                                                 .WithTimestamp(DateTime.Now);

            await wavScoresChannel.SendMessageAsync(embed: embed);
            await commandContext.RespondAsync("Ваш скор был отправлен на рассмотрение. Спасибо за участие!");
        }
    }
}
