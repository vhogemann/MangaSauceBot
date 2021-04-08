using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Encodings.Web;
using Flurl;
using Flurl.Http;
using System.Threading.Tasks;
using System.Web;

namespace MangaSauceBot.manga
{
    public class TraceMoeService
    {
        // "https://trace.moe/api/search?url=";
        // "https://media.trace.moe/video/${anilist_id}/${encodeURIComponent(filename)}?t=${at}&token=${tokenthumb}&mute";

        private readonly string _searchUrl;
        private readonly string _previewUrl;

        public TraceMoeService(string searchUrl, string previewUrl)
        {
            _searchUrl = searchUrl;
            _previewUrl = previewUrl;
        }

        public async Task<Response> Search(string imageUri)
        {
            var url = $"${_searchUrl}?=${imageUri}";
            var response = await url.GetJsonAsync<Response>();
            return response;
        }

        public string PreviewUri(Document document)
        {
            var url = $"${_previewUrl}/video/${document.AnilistId}/${HttpUtility.UrlEncode(document.Filename)}?t=${document.At}&token=${document.Tokenthumb}&mute";
            return url;
        }
    }
}
