using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MangaSauceBot.manga;
using MangaSauceBot.twitter;
using Serilog;
using Tweetinvi.Core.Extensions;
using Tweetinvi.Models;
using Tweetinvi.Models.Entities;

namespace MangaSauceBot.bot
{
    public class Search
    {
        public ITweet Tweet { get; set; }
        public string Image { get; set; }
    }

    public class Bot
    {
        private readonly TwitterService _twitter;
        private readonly TraceMoeService _traceMoe;
        private readonly double _cutOff;
        private readonly int _wait;
        private readonly int _throughput; // replies per minute
        private readonly string[] _adultTags;

        private readonly Queue<Search> _searchQueue;
        
        public Bot(TwitterService twitter, TraceMoeService traceMoe, long? cutOff, int? wait, int? throughput)
        {
            _twitter = twitter;
            _traceMoe = traceMoe;
            _searchQueue = new Queue<Search>();
            _throughput = throughput ?? 2;
            _cutOff = cutOff / 100d ?? 0.7d;
            _wait = wait ?? 1000 * 60 * 1; //Wait for a minute
            _adultTags = new[] {"#nsfw", "#adult", "#porn", "#hentai"};
        }
        
        private Reply[] CreateReplies(ITweet tweet, Response response)
        {
            Reply[] replies = null;
            if (response.Result != null && !response.Result.IsEmpty())
            {
                replies = response.Result
                    .OrderByDescending(it => it.Similarity)
                    .Where(it =>
                    {
                        var withinBounds = it.Similarity >= _cutOff;
                        var okAdult = IsAdult(tweet) || it.Anilist is {IsAdult: false};
                        return withinBounds && okAdult;
                    })
                    .Take(1) //Take the first document 
                    .Select(it => new Reply(tweet, it, it.Video))
                    .ToArray();
            }
            if (replies != null && !replies.IsEmpty())
                return replies;
            Log.Information("No matches found");
            return new[] {new Reply ($"@{tweet.CreatedBy.ScreenName} couldn't find any matches, sorry!")};
        }

        private async Task ParseTweet(ITweet tweet)
        {
            var media = tweet.Media;
            if (media == null || media.IsEmpty())
            {
                Log.Information("Tweet has no media, trying parent");
                var parent = await _twitter.FetchParent(tweet);
                media = parent?.Media;
            }
            media?.Where(it => it.MediaType == "photo")
                .ForEach(it => _searchQueue.Enqueue(new Search {Image = it.MediaURLHttps, Tweet = tweet}));
        }

        private bool IsAdult(ITweet tweet)
        {
            var text = tweet.Text ?? string.Empty;
            var adult = _adultTags.Select(it => text.Contains(it)).Aggregate((a, b) => a || b);
            return tweet.PossiblySensitive || adult;
        }

        private async Task Listen(bool runOnce)
        {
            while (true)
            {
                var delay = Task.Delay(_wait);
                Log.Information("Fetching new tweets");
                var mentions = await _twitter.FetchMentionsAsync(_searchQueue.Select(it => it.Tweet.Id));
                Log.Information("Found mentions {Length}", mentions.Length);
                await Task.WhenAll(mentions.Select(ParseTweet));
                if (runOnce) break;
                await delay;
            }
        }
        
        private async Task Reply(bool runOnce)
        {
            while (true)
            {
                if (!_searchQueue.TryDequeue(out var search)) continue;
                var delay = Task.Delay((60 / _throughput) * 1000);
                var response = await _traceMoe.Search(search.Image);
                var replies = CreateReplies(search.Tweet, response);
                await Task.WhenAll(replies.Select(reply => _twitter.PostReplyAsync(reply.Tweet, reply.Message, reply.VideoUrl, reply.IsAdult)));
                if (runOnce) break;
                await delay;
            }
        }

        public async Task Run(bool runOnce)
        {
            var listen = Listen(runOnce);
            var reply = Reply(runOnce);
            await Task.WhenAll(listen, reply);
        }
    }
}