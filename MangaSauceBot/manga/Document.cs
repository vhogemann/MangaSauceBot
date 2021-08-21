using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace MangaSauceBot.manga
{
    public class Response {
        public List<Document> Result { get; set; }
    }

    public class Title
    {
        public string Native { get; set; }
        public string English { get; set; }
        public string Romaji { get; set; }
    }

    public class AnilistInfo
    {
        public string Id { get; set; }
        public string IdMal { get; set; }
        public Title Title { get; set; }
        public string[] Synonyms { get; set; }
        public bool IsAdult { get; set; }
    }

    public class Document
    {
        public string Error { get; set; }
        public double? From { get; set; }
        public double? To { get; set; }
        public double Similarity { get; set; }
        public AnilistInfo? Anilist { get; set; }
        public string Anime { get; set; }
        public string Filename { get; set; }
        public string Episode { get; set; }
        
        public string Video { get; set; }
        
        public string Image { get; set; }
    }
}
