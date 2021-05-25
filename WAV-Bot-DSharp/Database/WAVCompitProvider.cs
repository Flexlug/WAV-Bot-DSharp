﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Raven.Client.Documents;
using Raven.Client.Documents.Session;

using WAV_Bot_DSharp.Utils;
using WAV_Bot_DSharp.Database.Models;
using WAV_Bot_DSharp.Database.Interfaces;

using WAV_Osu_NetApi;
using WAV_Osu_NetApi.Models;
using Microsoft.Extensions.Logging;

namespace WAV_Bot_DSharp.Database
{
    public class WAVCompitProvider : IWAVCompitProvider
    {
        private IDocumentStore store;

        private BanchoApi api;
        private GatariApi gapi;

        private ILogger<WAVCompitProvider> logger;

        public WAVCompitProvider(BanchoApi api,
                                 GatariApi gapi,
                                 ILogger<WAVCompitProvider> logger)
        {
            this.store = DocumentStoreProvider.Store;

            this.api = api;
            this.gapi = gapi;

            this.logger = logger;
            logger.LogInformation("WAVCompitProvider loaded");
        }

        public WAVMemberCompitProfile GetCompitProfile(ulong uid)
        {
            using (IDocumentSession session = store.OpenSession(new SessionOptions() { NoTracking = true }))
            {
                WAVMember member = session.Query<WAVMember>()
                                          .Include(x => x.OsuServers)
                                          .FirstOrDefault(x => x.Uid == uid);

                return member.CompitionInfo;
            }
        }

        public void AddCompitProfile(ulong uid, WAVMemberCompitProfile compitProfile)
        {
            using (IDocumentSession session = store.OpenSession())
            {
                WAVMember member = session.Query<WAVMember>()
                                          .Include(x => x.OsuServers)
                                          .FirstOrDefault(x => x.Uid == uid);

                member.CompitionInfo = compitProfile;

                session.SaveChanges();
            }
        }

        public List<CompitScore> GetUserScores(ulong id)
        {
            using (IDocumentSession session = store.OpenSession(new SessionOptions() { NoTracking = true }))
            {
                List<CompitScore> scores = session.Query<CompitScore>()
                                                 .Where(x => x.Player == id)
                                                 .ToList();

                return scores;
            }
        }

        public void ResetScores()
        {
            using (IDocumentSession session = store.OpenSession())
            {
                List<CompitScore> scores = session.Query<CompitScore>()
                                                 .ToList();

                foreach (var score in scores)
                    session.Delete(score);

                session.SaveChanges();
            }
        }

        public void SubmitScore(CompitScore score)
        {
            using (IDocumentSession session = store.OpenSession())
            {
                session.Store(score);
                session.SaveChanges();
            }
        }

        public CompitInfo GetCompitionInfo()
        {
            using (IDocumentSession session = store.OpenSession())
            {
                CompitInfo compitInfo = session.Query<CompitInfo>().FirstOrDefault();

                if (compitInfo is null)
                {
                    compitInfo = new CompitInfo();

                    session.Store(compitInfo);
                    session.SaveChanges();
                }

                return compitInfo;
            }
        }

        public void SetCompitionInfo(CompitInfo info)
        {
            using (IDocumentSession session = store.OpenSession())
            {
                CompitInfo oldInfo = session.Query<CompitInfo>().FirstOrDefault();

                if (oldInfo is null)
                    return;

                session.Delete(oldInfo);
                session.Store(info);

                session.SaveChanges();
            }
        }
    }
}
