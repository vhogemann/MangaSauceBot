using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MangaSauceBot.manga;
using MangaSauceBot.twitter;
using Serilog;
using Tweetinvi.Core.Extensions;
using Tweetinvi.Models;

namespace MangaSauceBot.bot
{
    public class Reply
    {
        public ITweet Tweet { get; set; }
        public string Message { get; set; }
        public string VideoUrl { get; set; }
        public bool IsAdult { get; set; }
    }

    public class Bot
    {
        private readonly TwitterService _twitter;
        private readonly TraceMoeService _traceMoe;
        private readonly double _cutOff;
        private readonly int _wait;
        private readonly int _throughput; // replies per minute
        private readonly string[] _adultTags;

        private readonly Queue<Reply> _replyQueue;
        
        public Bot(TwitterService twitter, TraceMoeService traceMoe, long? cutOff, int? wait, int? throghput)
        {
            _twitter = twitter;
            _traceMoe = traceMoe;
            _throughput = throghput ?? 2;
            _cutOff = cutOff / 100d ?? 0.7d;
            _wait = wait ?? 1000 * 60 * 1; //Wait for a minute
            _adultTags = new[] {"#nsfw", "#adult", "#porn", "#hentai"};
            _replyQueue = new Queue<Reply>();
        }

        private Reply CreateReply(ITweet tweet, Document document)
        {
            Log.Information("Replying to {Author}", tweet.CreatedBy.ScreenName);
            var title = document.TitleEnglish ?? document.TitleNative;

            var message = new StringBuilder();
            message.Append($"Hi @{tweet.CreatedBy.ScreenName}, here's my best guess\n{title}");
            if (document.Season != null)
            {
                message.Append($"Season: {document.Season}\n");
            }

            if (document.Episode != null)
            {
                message.Append($"Episode: {document.Episode}\n");
            }

            if (document.IsAdult)
            {
                message.Append("#nsfw #hentai #adult");
            }
            
            var videoUrl = _traceMoe.PreviewUri(document);
            return new Reply
            {
                Message = message.ToString(), 
                VideoUrl = videoUrl, 
                IsAdult = document.IsAdult, 
                Tweet = tweet
            };
        }

        private Reply[] CreateReplies(ITweet tweet, Response response)
        {
            Reply[] replies = null;
            if (response.Docs != null && !response.Docs.IsEmpty())
            {
                replies = response.Docs
                    .OrderByDescending(it => it.Similarity)
                    .Where(it =>
                    {
                        var withinBounds = it.Similarity >= _cutOff;
                        var okAdult = IsAdult(tweet) || !it.IsAdult;
                        return withinBounds && okAdult;
                    })
                    .Take(1) //Take the first document 
                    .Select(it => CreateReply(tweet, it))
                    .ToArray();
            }
            if (replies != null && !replies.IsEmpty())
                return replies;
            Log.Information("No matches found");
            return new[] {new Reply {Message = $"@{tweet.CreatedBy.ScreenName} couldn't find any matches, sorry!"}};
        }

        private async Task<Response[]> TryParent(ITweet tweet)
        {
            var parent = await _twitter.FetchParent(tweet);
            if (parent.Media == null || parent.Media.IsEmpty())
            {
                Log.Information("Parent has no media");
                return null;
            }
            var images = parent.Media
                .Where(it => it.MediaType == "photo")
                .Select(it => _traceMoe.Search(it.MediaURLHttps));
            return await Task.WhenAll(images);
        }

        private async Task<Response[]> ParseTweet(ITweet tweet)
        {
            if (tweet.Media == null || tweet.Media.IsEmpty())
            {
                Log.Information("Tweet has no media, trying parent");
                return await TryParent(tweet);
            }
            var images = tweet.Media
                .Where(it => it.MediaType == "photo")
                .Select(it => _traceMoe.Search(it.MediaURLHttps));
            return await Task.WhenAll(images);
        }

        private bool IsAdult(ITweet tweet)
        {
            var text = tweet.Text ?? string.Empty;
            var adult = _adultTags.Select(it => text.Contains(it)).Aggregate((a, b) => a || b);
            return tweet.PossiblySensitive || adult;
        }

        private async Task ParseAndReply(ITweet tweet)
        {
            var parsed = await ParseTweet(tweet);
            var replies = parsed
                .Where(it => it != null)
                .Select(it => CreateReplies(tweet, it));
            replies.SelectMany(it => it).ForEach(it => _replyQueue.Enqueue(it));
        }

        private async Task Listen(bool runOnce)
        {
            while (true)
            {
                var delay = Task.Delay(_wait);
                Log.Information("Fetching new tweets");
                var mentions = await _twitter.FetchMentionsAsync();
                Log.Information("Found mentions {Length}", mentions.Length);
                await Task.WhenAll(mentions.Select(ParseAndReply));
                if (runOnce) break;
                await delay;
            }
        }

        private int SendDelay()
        {
            return (60 / _throughput) * 1000;
        }
        
        private async Task Reply(bool runOnce)
        {
            while (true)
            {
                var delay = Task.Delay(SendDelay());
                if (_replyQueue.TryDequeue(out var reply))
                {
                    await _twitter.PostReplyAsync(reply.Tweet, reply.Message, reply.VideoUrl, reply.IsAdult);
                    if (runOnce) break;
                }
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