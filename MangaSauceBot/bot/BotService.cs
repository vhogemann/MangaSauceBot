using MangaSauceBot.manga;
using MangaSauceBot.twitter;

namespace MangaSauceBot.bot
{
    public class BotService
    {

        private readonly TraceMoeService _mangaService;
        private readonly TwitterService _twitterService;

        public BotService(TraceMoeService mangaService, TwitterService twitterService)
        {
            _mangaService = mangaService;
            _twitterService = twitterService;
        }


        public void fetchMentions()
        {
        }

    }
}