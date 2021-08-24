using System;
using System.IO;
using System.Threading.Tasks;
using MangaSauceBot.bot;
using MangaSauceBot.manga;
using MangaSauceBot.twitter;
using Serilog;

namespace MangaSauceBot
{
    internal static class Program
    {
        static async Task Main(string[] args)
        {
            Log.Logger = new LoggerConfiguration()
                .WriteTo.Console()
                //.WriteTo.File("manga_sauce_bot.log")
                .CreateLogger();
            var root = Directory.GetCurrentDirectory();
            var dotenv = Path.Combine(root, ".env");
            
            DotEnv.Load(dotenv);
            var apiKey = DotEnv.Get("TRACE_MOE_API_KEY");
            var manga = new TraceMoeService(
                "https://api.trace.moe", 
                "https://media.trace.moe",
                apiKey);

            var mentionsContext = DotEnv.GetAsBool("USE_SQLITE")
                ? new MentionsContext(true)
                : new MentionsContext(
                    DotEnv.Get("COSMOS_DB_ACCOUNT_ENDPOINT"),
                    DotEnv.Get("COSMOS_DB_ACCOUNT_KEY"),
                    DotEnv.Get("COSMOS_DB_DATABASE_NAME"));
            
            var repository = new MentionsRepository(
                mentionsContext);
            var twitter = new TwitterService(
                DotEnv.Get("TWITTER_CONSUMER_KEY"), 
                DotEnv.Get("TWITTER_CONSUMER_SECRET"), 
                DotEnv.Get("TWITTER_TOKEN"), 
                DotEnv.Get("TWITTER_TOKEN_SECRET"),
                repository);
            
            var bot = new Bot(
                twitter, 
                manga, 
                DotEnv.GetAsLong("SEARCH_SIMILARITY_CUTOFF"),
                DotEnv.GetAsInt("BOT_SLEEP_TIMEOUT"),
                DotEnv.GetAsInt("BOT_REPLY_THROUGHPUT"));

            var runOnce = "true".Equals(DotEnv.Get("BOT_RUN_ONCE"));

            while (true)
            {
                try
                {
                    await bot.Run(runOnce);
                }
                catch (Exception e)
                {
                    Log.Error(e, "Exiting");
                    throw;
                }
            }
        }
    }
}
