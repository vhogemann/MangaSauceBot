using System;
using System.IO;
using System.Threading.Tasks;
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
                .WriteTo.File("manga_sauce_bot.log")
                .CreateLogger();
            var root = Directory.GetCurrentDirectory();
            var dotenv = Path.Combine(root, ".env");
            DotEnv.Load(dotenv);
            var manga = new TraceMoeService("https://trace.moe", "https://media.trace.moe");
            var repository = new MentionsRepository();
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
                DotEnv.GetAsInt("BOT_SLEEP_TIMEOUT"));

            var runOnce = "true".Equals(DotEnv.Get("BOT_RUN_ONCE"));
            
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
