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

namespace MangaSauceBot
{
    public class Reply
    {
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
        private readonly string[] _adultTags;

        public Bot(TwitterService twitter, TraceMoeService traceMoe, long? cutOff, int? wait)
        {
            _twitter = twitter;
            _traceMoe = traceMoe;
            _cutOff = cutOff / 100d ?? 0.7d;
            _wait = wait ?? 1000 * 60 * 1; //Wait for a minute
            _adultTags = new[] {"#nsfw", "#adult", "#porn", "#hentai"};
        }

        private Reply CreateReply(string author, Document document)
        {
            Log.Information("Replying to {Author}", author);
            var title = document.TitleEnglish ?? document.TitleNative;

            var message = new StringBuilder();
            message.Append($"Hi @{author}, here's my best guess\n{title}");
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
            return new Reply {Message = message.ToString(), VideoUrl = videoUrl, IsAdult = document.IsAdult};
        }

        private Reply[] CreateReplies(string author, bool adult, Response response)
        {
            Reply[] replies = null;
            if (response.Docs != null && !response.Docs.IsEmpty())
            {
                replies = response.Docs
                    .OrderByDescending(it => it.Similarity)
                    .Where(it =>
                    {
                        var withinBounds = it.Similarity >= _cutOff;
                        var adultFilter = adult || !it.IsAdult;
                        return withinBounds && adult;
                    })
                    .Take(1) //Take the first document 
                    .Select(it => CreateReply(author, it))
                    .ToArray();
            }
            if (replies != null && !replies.IsEmpty())
                return replies;
            Log.Information("No matches found");
            return new[] {new Reply {Message = $"@{author} couldn't find any matches, sorry!"}};
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

        private async Task<ITweet[]> SendReplies(ITweet tweet, IEnumerable<Reply> replies)
        {
            return await Task.WhenAll(replies.Select(it => _twitter.PostReplyAsync(tweet, it.Message, it.VideoUrl, it.IsAdult)));
        }
        
        private bool IsAdult(ITweet tweet)
        {
            var text = tweet.Text ?? string.Empty;
            var adult = _adultTags.Select(it => text.Contains(it)).Aggregate((a, b) => a || b);
            return tweet.PossiblySensitive || adult;
        }

        private async Task<ITweet[][]> ParseAndReply(ITweet tweet)
        {
            var parsed = await ParseTweet(tweet);
            var replies = parsed
                .Where(it => it != null)
                .Select(it => CreateReplies(tweet.CreatedBy.ScreenName, IsAdult(tweet), it));
            return await Task.WhenAll(replies.Select(it => SendReplies(tweet, it)));
        }

        public async Task Run(bool runOnce)
        {
            while (true)
            {
                var delay = Task.Delay(_wait);
                Log.Information("Fetching new tweets");
                var mentions = await _twitter.FetchMentionsAsync();
                Log.Information("Found mentions {Length}", mentions.Length);
                var result = await Task.WhenAll(mentions.Select(ParseAndReply));
                if (runOnce) break;
                await delay;
            }
        }
    }
}