﻿using System;
using System.Net;
using System.Linq;
using System.Text;
using System.DrawingCore;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

using DSharpPlus;
using DSharpPlus.Entities;

using WAV_Bot_DSharp.Threading;
using WAV_Bot_DSharp.Configurations;
using WAV_Bot_DSharp.Services.Interfaces;

using WAV_Osu_Recognizer;

using WAV_Osu_NetApi;
using WAV_Osu_NetApi.Bancho.Models;
using WAV_Osu_NetApi.Gatari.Models;
using WAV_Osu_NetApi.Gatari.Models.Enums;
using WAV_Bot_DSharp.Converters;

using Microsoft.Extensions.Logging;

namespace WAV_Bot_DSharp.Services.Entities
{
    /// <summary>
    /// Реализация сервиса, который будет отслеживать скриншоты и скоры из osu!
    /// </summary>
    public class RecognizerService : IRecognizerService
    {
        private DiscordClient client;
        private ILogger<RecognizerService> logger;
        private WebClient webClient;
        private Recognizer recognizer;

        private BanchoApi api;
        private GatariApi gapi;

        private Dictionary<int, DateTime> ignoreList;
        private OsuEmoji emoji;
        private OsuUtils utils;
        private ShedulerService sheduler;
        private BackgroundQueue queue;

        public RecognizerService(DiscordClient client, 
                                 Settings settings, 
                                 ILogger<RecognizerService> logger, 
                                 OsuEmoji emoji, 
                                 OsuUtils utils,
                                 ShedulerService sheduler)
        {
            this.client = client;
            this.logger = logger;
            this.utils = utils;
            this.sheduler = sheduler;

            this.emoji = emoji;

            recognizer = new Recognizer();
            webClient = new WebClient();

            ignoreList = new Dictionary<int, DateTime>();

            api = new BanchoApi(settings.ClientId, settings.Secret);
            gapi = new GatariApi();

            queue = new BackgroundQueue();

            logger.LogInformation("Osu service started");
            ConfigureFilesInterceptor(client);
        }

        private void ConfigureFilesInterceptor(DiscordClient client)
        {
            client.MessageCreated += Client_OnMessageCreated;
        }

        private async Task Client_OnMessageCreated(DiscordClient sender, DSharpPlus.EventArgs.MessageCreateEventArgs e)
        {
            IReadOnlyCollection<DiscordAttachment> attachments = e.Message.Attachments;

            // Skip messages with no attachments
            if (attachments.Count == 0)
                return;

            logger.LogDebug($"Detected attachments. Count: {attachments.Count}");

            // Skip messages not from osu channels
            if (!(e.Channel.Name.Contains("-osu") || e.Channel.Name.Contains("bot-debug")))
            {
                logger.LogDebug($"Not osu channel");
                return;
            }


            foreach (DiscordAttachment attachment in attachments)
            {
                if (!(attachment.Width >= 750 && attachment.Height >= 550))
                    continue;

                //if (!(attachment.FileName.StartsWith("screenshot") && attachment.FileName.EndsWith(".jpg")))
                    //continue;

                logger.LogInformation($"Beatmap detect attempt");
                ThreadPool.QueueUserWorkItem(new WaitCallback(async delegate (object state)
                {
                    await ExecuteMessageTrack(e.Message, attachment);
                }));
            }
        }

        /// <summary>
        /// Начинает отслеживание реакций сообщения с картинкой.
        /// </summary>
        /// <param name="message">Отслеживаемое изображение</param>
        /// <param name="attachment">Картинка</param>
        private async Task ExecuteMessageTrack(DiscordMessage message, DiscordAttachment attachment)
        {
            var res = await queue.QueueTask(() => DownloadAndRecognizeImage(attachment));

            if (res == null)
            {
                return;
            }

            Beatmapset banchoBeatmapset = res.Item1;
            Beatmap banchoBeatmap = res.Item2;

            // Ignore beatmap for several minutes
            foreach (var kvp in ignoreList)
                if (DateTime.Now - kvp.Value > TimeSpan.FromMinutes(10))
                    ignoreList.Remove(kvp.Key);

            if (ignoreList.ContainsKey(banchoBeatmap.id))
            {
                logger.LogInformation($"Beatmap is in ignore list {banchoBeatmap.id}");
                return;
            }

            ignoreList.Add(banchoBeatmap.id, DateTime.Now);

            // Contruct message
            DiscordEmbedBuilder embedBuilder = new DiscordEmbedBuilder();

            TimeSpan mapLen = TimeSpan.FromSeconds(banchoBeatmap.total_length);

            DiscordEmoji banchoRankEmoji = emoji.RankStatusEmoji(banchoBeatmap.ranked);
            DiscordEmoji diffEmoji = emoji.DiffEmoji(banchoBeatmap.difficulty_rating);

            // Check gatari
            GBeatmap gBeatmap = gapi.TryGetBeatmap(banchoBeatmap.id);

            DiscordEmbed embed = utils.BeatmapToEmbed(banchoBeatmap, banchoBeatmapset, gBeatmap);

            await message.RespondAsync(embed: embed);
        }

        /// <summary>
        /// Начинает распознавание картинки
        /// </summary>
        /// <param name="attachment">Картинка</param>
        private Tuple<Beatmapset, Beatmap> DownloadAndRecognizeImage(DiscordAttachment attachment)
        {
            string webFileName = $"{DateTime.Now.Ticks}-{attachment.FileName}";
            webClient.DownloadFile(attachment.Url, $"downloads/{webFileName}");

            string fileName = $"downloads/{webFileName}";
            Image image = Image.FromFile(fileName);

            string[] rawrecedText = recognizer.RecognizeTopText(image).Split('\n');

            sheduler.AddFileDeleteTask(fileName);

            foreach (string s in rawrecedText)
                logger.LogDebug(s);

            // Searching for first non-empty or almost string
            string recedText = string.Empty;

            foreach (string s in rawrecedText)
                if (!string.IsNullOrWhiteSpace(s) && s.Length > 10)
                {
                    recedText = s;
                    break;
                }

            logger.LogDebug($"Recognized text: {recedText}");

            // Cut artist
            int indexStart = recedText.IndexOf('-');
            if (indexStart == -1)
                indexStart = recedText.IndexOf('–');

            if (indexStart != -1)
            {
                logger.LogDebug("Cutting artist");
                recedText = recedText.Substring(indexStart).TrimStart(new char[] { ' ', '-', '–' });
            }

            logger.LogDebug($"Searching for: {recedText}");
            List<Beatmapset> bmsl = api.Search(recedText, WAV_Osu_NetApi.Bancho.QuerryParams.MapType.Any);


            // Get map diff
            indexStart = recedText.IndexOf('[');
            if (indexStart == -1)
            {
                logger.LogInformation($"Coulnd't get map difficulty");
                return null;
            }

            string diffName = recedText.Substring(indexStart).TrimStart('[').TrimEnd(']');
            logger.LogDebug($"diffName: {diffName}");

            if (bmsl == null || bmsl.Count == 0)
            {
                logger.LogInformation($"Api search return null or empty List");
                return null;
            }

            string mapper = string.Empty;
            
            mapper = rawrecedText.Select(x => x)
                                 .FirstOrDefault(x =>
                                 {
                                     x = x.ToLowerInvariant();
                                     return x.Contains("mapped") || x.Contains("beatmap");
                                 });


            Beatmapset bms = null;
            if (!string.IsNullOrEmpty(mapper))
            {
                mapper = mapper?.Substring(10);
                logger.LogDebug($"Got mapper: {mapper}. Comparing...");
                List<Tuple<Beatmapset, double>> bsm = bmsl.Select(x => Tuple.Create(x, WAV_Osu_Recognizer.RecStringComparer.Compare(x.creator, mapper)))
                                                           .OrderByDescending(x => x.Item2)
                                                           .ToList();


                foreach (var b in bsm)
                    logger.LogDebug($"{b.Item1.creator}: {b.Item2}");

                if (bsm == null || bsm.Count == 0)
                    bms = bmsl.FirstOrDefault();
                else
                    bms = bsm.First().Item1;
            }
            else
            {
                logger.LogInformation($"Couldn't get mapper");
                bms = bmsl.FirstOrDefault();
            }


            if (bms == null)
            {
                logger.LogInformation($"No matching beatmapsets");
                return null; 
            }
            else
            {
                logger.LogDebug($"Beatmapsets count: {bmsl.Count}");
            }


            List<Tuple<Beatmap, double>> bmds = bms.beatmaps.Select(x => Tuple.Create(x, WAV_Osu_Recognizer.RecStringComparer.Compare(x.version, diffName)))
                                                     .OrderByDescending(x => x.Item2)
                                                     .ToList();
            logger.LogDebug("Comparing beatmap versions:");
            foreach (var k in bmds)
                logger.LogDebug($"{k.Item1.version} {k.Item2}");



            var result = Tuple.Create(bms, bmds.First().Item1);

            logger.LogInformation($"Success. bms_id: {result.Item1.id}, bm_id: {result.Item2.id}");
            return result;
        }
    }
}