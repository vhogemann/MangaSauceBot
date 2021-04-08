using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Tweetinvi;
using Tweetinvi.Models;
using Tweetinvi.Parameters;

namespace MangaSauceBot.twitter
{
    public class TwitterService
    {
        private readonly TwitterClient _client;
        
        public TwitterService(string consumerKey, string consumerSecret, string token, string tokenSecret)
        {
            var userCredentials = new TwitterCredentials(consumerKey, consumerSecret, token, token);
            _client = new TwitterClient(userCredentials);
        }
        
        public async Task<ITweet[]> FetchMentionsAsync() {
            var mentions = await _client.Timelines.GetMentionsTimelineAsync();
            return mentions;
        }

        public async Task<ITweet[]> FindTweetsWithImagesInThread(ITweet tweet)
        {
            var replies = await _client.Search.SearchTweetsAsync()
        }

        public async Task<ITweet> PostReplyAsync(ITweet inReplyTo, string message, string videoAttachmentUri)
        {
            var reply = await _client.Tweets.PublishTweetAsync(new PublishTweetParameters(message) {
                InReplyToTweet = inReplyTo
            });
            return reply;
        }
    }
}
