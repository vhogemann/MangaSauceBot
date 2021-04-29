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
        public int Id { get; set; }

        public long TweetId { get; set; }

        public DateTime Timestamp { get; set; }
        public MentionStatus Status { get; set; }
    }

    public class MentionsContext:DbContext
    {
        
        public DbSet<MentionEntry> Mentions { get; set; }
        
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite("Data Source=mentions.db");
        }
    }

    public class MentionsRepository
    {

        private readonly MentionsContext _db;
        
        public MentionsRepository()
        {
            _db = new MentionsContext();
            _db.Database.EnsureCreated();
        }

        public async Task<MentionEntry> GetLatestMention()
        {
            return await _db.Mentions
                .OrderByDescending(it => it.Timestamp)
                .Take(1)
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