using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ConsoleMusicPlayer.MusicPlatformAccount;
using System.Net;
using System.Web;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;

namespace ConsoleMusicPlayer.SongSource
{
    
    public class NeteaseCloudMusicShareLink : ISongSource
    {
        // todo 传CookieContainer
        [JsonProperty]
        private HttpClient HttpClient;
        public Uri ShareLinkUri;
        public long Id;
        [JsonIgnore]
        public IEnumerable<Song> Songs { get; private set; }
        [RequireMusicPlatformAccount(typeof(NeteaseCloudMusicAccount))]
        public NeteaseCloudMusicShareLink(IMusicPlatformAccount account, string ShareLinkUri)
        {
            if (account != null && ShareLinkUri != null)
            {
                HttpClient = account.HttpClient;
                this.ShareLinkUri = new Uri(ShareLinkUri);
                string idString = HttpUtility.ParseQueryString(this.ShareLinkUri.Query).Get("id")
                    ?? throw new SongSourceConstuctionException("Illegal Url(don't have id query): ShareLinkUri");
                if (!long.TryParse(idString, out Id))
                {
                    throw new SongSourceConstuctionException("Illegal Url(id is illegal): ShareLinkUri");
                }
                UpdateSongs();
            }
            
        }

        public void UpdateSongs()
        {
            var response = HttpClient.GetAsync($"/playlist/detail?id={Id}").Result;
            
            JObject result;
            { // Validations
                if (!response.IsSuccessStatusCode)
                {
                    throw new SongSourceApiCallFailedException($"NeteaseCloudMusicShareLink get songs api call failed, http code: {(int)response.StatusCode}");
                }
                result = JObject.Parse(response.Content.ReadAsStringAsync().Result);
                JToken? codeJToken;
                if (!result.TryGetValue("code", out codeJToken) || codeJToken == null)
                {
                    throw new SongSourceApiCallFailedException("NeteaseCloudMusicShareLink get songs api call failed: response json doesn't have property code(or code is null)");
                }
                if (codeJToken.ToObject<int>() != 200)
                {
                    throw new SongSourceApiCallFailedException($"NeteaseCloudMusicShareLink get songs api call failed, api code: {codeJToken.ToObject<int>()}, msg: {result.GetValue("msg").ToObject<string>()}");
                }
            }
            Dictionary<int, Song> songs = new Dictionary<int, Song>();
            foreach (JObject track in result["playlist"]["tracks"])
            {
                string name = track["name"].ToObject<string>();
                IEnumerable<string> alia = track["alia"].ToArray().Select(token => token.ToObject<string>());
                IEnumerable<string> singers = track["ar"].ToList().Select(token => token["name"].ToObject<string>());
                string songName = $"{string.Join("、", singers)} - {name}";
                if (alia.Count() > 0)
                {
                    songName += $"({string.Join("/", alia)})";
                }
                songs.Add(track["id"].ToObject<int>(), new Song { Name = songName });
            }
            var idResponse = HttpClient.GetAsync($"/song/url?id={string.Join(',', songs.Keys)}").Result;
            { // Validations
                if (!idResponse.IsSuccessStatusCode)
                {
                    throw new SongSourceApiCallFailedException($"NeteaseCloudMusicShareLink get songs api call failed, http code: {(int)idResponse.StatusCode}");
                }
            }
            JObject idResultsJObject = JObject.Parse(idResponse.Content.ReadAsStringAsync().Result);
            foreach(JObject idResult in idResultsJObject["data"].ToList())
            {
                if (idResult["url"].ToObject<string>() == null)
                {
                    songs.Remove(idResult["id"].ToObject<int>());
                    continue; // cannot get the play url of the song
                }
                songs[idResult["id"].ToObject<int>()].Uri = new Uri(idResult["url"].ToObject<string>());
            }
            Songs = songs.Values;
        }

        public Task<IEnumerable<Song>> GetSongsAsync()
        {
            throw new NotImplementedException();
        }

        public override string ToString()
        {
            return $"NeteaseCloudMusicShareLink: {Id}";
        }
    }
}
