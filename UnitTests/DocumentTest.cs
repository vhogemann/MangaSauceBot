using System;
using MangaSauceBot.manga;
using Newtonsoft.Json;
using NUnit.Framework;

namespace UnitTests
{
    public class DocumentTest
    {
        private const string Document =
            @"{
                ""from"": 663.17,
                ""to"": 665.42,
                ""anilist_id"": 98444,
                ""at"": 665.08,
                ""season"": ""2018-01"",
                ""anime"": ""搖曳露營"",
                ""filename"": ""[Ohys-Raws] Yuru Camp - 05 (AT-X 1280x720 x264 AAC).mp4"",
                ""episode"": 5,
                ""tokenthumb"": ""bB-8KQuoc6u-1SfzuVnDMw"",
                ""similarity"": 0.9563952960290518,
                ""title"": ""ゆるキャン△"",
                ""title_native"": ""ゆるキャン△"",
                ""title_chinese"": ""搖曳露營"",
                ""title_english"": ""Laid-Back Camp"",
                ""title_romaji"": ""Yuru Camp△"",
                ""mal_id"": 34798,
                ""synonyms"": [""Yurucamp"", ""Yurukyan△""],
                ""synonyms_chinese"": [],
                ""is_adult"": false
            }";

        private const string Response =
            @"{
                ""RawDocsCount"": 3555648,
                ""RawDocsSearchTime"": 14056,
                ""ReRankSearchTime"": 1182,
                ""CacheHit"": false,
                ""trial"": 1,
                ""limit"": 9,
                ""limit_ttl"": 60,
                ""quota"": 150,
                ""quota_ttl"": 86400,
                ""docs"": [ " + Document + @" ]
            }";
        
        [Test]
        public void Response_Should_Desserialize_Correctly()
        {
            var response = JsonConvert.DeserializeObject<Response>(Response);
            Assert.NotNull(response);
            Assert.IsNotEmpty(response.Result);
            Assert.AreEqual(1, response.Result.Count);
        }

        [Test]
        public void Document_Should_Desserialize_Correctly()
        {

            var document = JsonConvert.DeserializeObject<Document>(Document);
            Assert.AreEqual(663.17, document.From);
            Assert.AreEqual(665.42, document.To);
            Assert.AreEqual(98444, document.Anilist);
            Assert.AreEqual("搖曳露營", document.Anime);
            Assert.AreEqual("[Ohys-Raws] Yuru Camp - 05 (AT-X 1280x720 x264 AAC).mp4", document.Filename);
            Assert.AreEqual(5, document.Episode);
            Assert.AreEqual(0.9563952960290518, document.Similarity);
        }
    }
}