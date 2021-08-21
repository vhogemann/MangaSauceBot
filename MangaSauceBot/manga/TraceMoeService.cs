using System;
using System.Threading.Tasks;
using System.Web;
using Flurl.Http;
using Serilog;

namespace MangaSauceBot.manga
{
    public class TraceMoeService
    {
        // "https://trace.moe/api/search?anilistInfo&url=";
        // "https://media.trace.moe/video/${anilist_id}/${encodeURIComponent(filename)}?t=${at}&token=${tokenthumb}&mute";

        private readonly string _searchUrl;
        private readonly string _apiKey;

        public TraceMoeService(string searchUrl, string previewUrl, string apiKey)
        {
            _searchUrl = searchUrl;
            _apiKey = apiKey;
        }

        public async Task<Response> Search(string imageUri)
        {
            var url = $"{_searchUrl}/search?anilistInfo&url={imageUri}";
            try
            {
                var response = await url.GetJsonAsync<Response>();
                return response;
            }
            catch (Exception e)
            {
                Log.Error(e, "Failed to search for matches");
                return null;
            }
        }
    }
}
