using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace MangaSauceBot.twitter
{
    public enum MentionStatus
    {
        Replied = 0,
        Error = 2,
    }

    public class MentionEntry
    {
        public string Id { get; set; }

        public long TweetId { get; set; }

        public DateTime Timestamp { get; set; }
        public MentionStatus Status { get; set; }

        public MentionEntry()
        {
            Id = Guid.NewGuid().ToString();
        }
    }

    public class MentionsContext:DbContext
    {

        private readonly bool _useSqlite;
        private readonly string _accountEndpoint;
        private readonly string _accountKey;
        private readonly string _databaseName;
        public DbSet<MentionEntry> Mentions { get; set; }

        public MentionsContext(bool useSqlite = true)
        {
            _useSqlite = useSqlite;
        }

        public MentionsContext(string accountEndpoint, string accountKey, string databaseName)
        {
            _accountEndpoint = accountEndpoint;
            _accountKey = accountKey;
            _databaseName = databaseName;
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (_useSqlite)
            {
                optionsBuilder.UseSqlite("Data Source=mentions.db");
            }
            else
            {
                optionsBuilder.UseCosmos(
                    "https://twitter-bots-db.documents.azure.com:443",
                    "wrXDB7nxJBuRZq9NhxKlcvB32X6ZJV6iWY6YuLMDehmKsgBaeaXtRDcJYR1bmDZLwpxSojYunyYHERBqb1yGhQ==",
                    "mangasaucebot"
                );
            }
        }
    }

    public class MentionsRepository
    {

        private readonly MentionsContext _db;
        
        public MentionsRepository(MentionsContext mentionsContext)
        {
            _db = mentionsContext;
            _db.Database.EnsureCreated();
        }

        public async Task<MentionEntry> GetLatestMention()
        {
            return await _db.Mentions
                .OrderByDescending(it => it.Timestamp)
                .FirstOrDefaultAsync<MentionEntry>();
        }

        public async Task<MentionEntry[]> FindByTweetId(IEnumerable<long> tweetIds)
        {
            return await _db.Mentions
                .Where(it => tweetIds.Contains(it.TweetId)).ToArrayAsync();
        }

        public void Save(MentionEntry mention)
        {
            Log.Information("Tweet {Id} replied", mention.TweetId);
            _db.Add(mention);
            _db.SaveChanges();
        }
    }
}