using System.Text;
using MangaSauceBot.manga;
using Tweetinvi.Core.Extensions;
using Tweetinvi.Models;

namespace MangaSauceBot.bot
{
    public class Reply
    {
        public ITweet Tweet { get; }
        public string Message { get; set; }
        public string VideoUrl { get; }
        public bool IsAdult { get; }

        public Reply(string message)
        {
            Message = message;
        }

        public Reply(ITweet tweet, Document document, string videoUrl)
        {
            var title = document.Anilist?.Title.English ?? document.Anilist?.Title.Native;

            var message = new StringBuilder();
            message.Append($"Hi @{tweet.CreatedBy.ScreenName}, here's my best guess\n{title}\n");

            if (document.Episode != null)
            {
                message.Append($"Episode: {document.Episode}\n");
            }

            if (document.Anilist is {IsAdult: true})
            {
                message.Append("#nsfw #hentai #adult");
            }

            Message = message.ToString();
            VideoUrl = videoUrl;
            IsAdult = document.Anilist is {IsAdult: true};
            Tweet = tweet;
        }
    }
}