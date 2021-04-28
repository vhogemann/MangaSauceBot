using System;
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
        public long Id { get; set; }
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

        public Task<MentionEntry> GetLatestMention()
        {
            return _db.Mentions
                .OrderByDescending(it => it.Timestamp)
                .Take(1)
                .FirstOrDefaultAsync<MentionEntry>();
        }

        public void Save(MentionEntry mention)
        {
            Log.Information("Tweet {Id} replied", mention.Id);
            _db.Add(mention);
            _db.SaveChanges();
        }
    }
}