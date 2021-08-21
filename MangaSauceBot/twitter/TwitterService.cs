using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Flurl.Http;
using Serilog;
using Tweetinvi;
using Tweetinvi.Models;
using Tweetinvi.Parameters;

namespace MangaSauceBot.twitter
{
    public class TwitterService
    {
        private readonly TwitterClient _client;
        private readonly MentionsRepository _repository;
        
        public TwitterService(string consumerKey, string consumerSecret, string token, string tokenSecret, MentionsRepository repository)
        {
            _repository = repository;
            var userCredentials = new TwitterCredentials(consumerKey, consumerSecret, token, tokenSecret);
            _client = new TwitterClient(userCredentials);
        }
        
        public async Task<ITweet[]> FetchMentionsAsync(IEnumerable<long> inFlight) {
            try
            {
                var parameters = new GetMentionsTimelineParameters();
                var latestMention = await _repository.GetLatestMention();
                if (latestMention != null)
                {
                    Log.Information("Latest mention {Mention} on {Date}", latestMention.TweetId, latestMention.Timestamp);
                    parameters.SinceId = latestMention.TweetId;
                }
                var mentions = await _client.Timelines.GetMentionsTimelineAsync(parameters);

                var alreadyReplied = await _repository.FindByTweetId(mentions.Select(it => it.Id));
                var repliedIds = alreadyReplied.Select(it => it.TweetId);
                return mentions
                    .Where(it => !repliedIds.Contains(it.Id))
                    .Where(it => !inFlight.Contains(it.Id))
                    .ToArray();
            }
            catch (Exception e)
            {
                Log.Error(e, "Failed to fetch tweets");
                return Array.Empty<ITweet>();
            }
        }

        public async Task<ITweet> PostReplyAsync(ITweet inReplyTo, string message, string videoAttachmentUri, bool adult)
        {
            ITweet reply = null;
            try
            {
                var publishParameters = new PublishTweetParameters(message)
                {
                    InReplyToTweet = inReplyTo, 
                    PossiblySensitive = adult
                };
                if (videoAttachmentUri != null)
                {
                    var video = await videoAttachmentUri.GetBytesAsync();
                    var upload = await _client.Upload.UploadTweetVideoAsync(video);
                    await _client.Upload.WaitForMediaProcessingToGetAllMetadataAsync(upload);
                    publishParameters.Medias = new List<IMedia> { upload };
                }
                reply = await _client.Tweets.PublishTweetAsync(publishParameters);
                Log.Information("Reply sent to {Author}", inReplyTo.CreatedBy.ScreenName);
            }
            catch (Exception e)
            {
                Log.Error(e, "Error submitting reply");
            }
            try
            {
                var entry = new MentionEntry()
                {
                    TweetId = inReplyTo.Id,
                    Status = reply == null ? MentionStatus.Error : MentionStatus.Replied,
                    Timestamp = reply?.CreatedAt.DateTime ?? DateTime.Now
                };
                _repository.Save(entry);
            }
            catch (Exception e)
            {
                Log.Error(e, "Failed to persist MentionEntry");
            }

            return reply;
        }

        public async Task<ITweet> FetchParent(ITweet tweet)
        {
            if (tweet.InReplyToStatusId == null) return null;
            try
            {
                return await _client.Tweets.GetTweetAsync((long) tweet.InReplyToStatusId);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to fetch parent tweet");
                return null;
            }
        }
    }
}
