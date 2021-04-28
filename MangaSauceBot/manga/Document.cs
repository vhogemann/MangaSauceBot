using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace MangaSauceBot.manga
{
    public class Response {
        public List<Document> Docs { get; set; }
    }
    [JsonObject(NamingStrategyType = typeof(SnakeCaseNamingStrategy))]
    public class Document
    {
        public double? From { get; set; }
        public double? To { get; set; }
        public int? AnilistId { get; set; }
        public double? At { get; set; }
        public string Season { get; set; }
        public string Anime { get; set; }
        public string Filename { get; set; }
        public int? Episode { get; set; }
        public string Tokenthumb { get; set; }
        public double? Similarity { get; set; }
        public string Title { get; set; }
        public string TitleNative { get; set; }
        public string TitleChinese { get; set; }
        public string TitleEnglish { get; set; }
        public string TitleRomaji { get; set; }
        public int? MalId { get; set; }
        public List<string> Synonyms { get; set; }
        public List<string> SynonymsChinese { get; set; }
        public bool IsAdult { get; set; }
    }
}
