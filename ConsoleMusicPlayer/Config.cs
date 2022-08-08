using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ConsoleMusicPlayer.SongSource;
using ConsoleMusicPlayer.MusicPlatformAccount;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System.Net;

namespace ConsoleMusicPlayer
{
    partial class PersistantStorage
    {
        public List<ISongSource> SongSources = new List<ISongSource>
        {
            //new LocalStorage(Environment.GetFolderPath(Environment.SpecialFolder.MyMusic))
        };
        public List<IMusicPlatformAccount> MusicPlatformAccounts = new List<IMusicPlatformAccount>
        {
            
        };
        public PersistantStorage()
        {
            /*var truebigsand_netease = new NeteaseCloudMusicAccount()
            {
                Email = "truebigsand@163.com",
                Password = "Feifei070926"
            };
            truebigsand_netease.Refresh();
            MusicPlatformAccounts.Add(truebigsand_netease);
            var n = new NeteaseCloudMusicShareLink(truebigsand_netease, "https://music.163.com/playlist?id=3119319371&userid=2076181730");
            SongSources.Add(n);*/
        }
        [JsonIgnore]
        public IEnumerable<Song> Songs
        {
            get
            {
                return SongSources.SelectMany(songSource => songSource.Songs);
            }
        }
    }
    internal static class Config
    {
        public static PersistantStorage PersistantStorage = new PersistantStorage();
        public static JsonSerializerSettings settings = new JsonSerializerSettings()
        {
            TypeNameHandling = TypeNameHandling.Auto,
            Formatting = Formatting.Indented,
        };
        public static JsonSerializer jsonSerializer = JsonSerializer.Create(settings);
        public static void RefreshAccount() =>
            PersistantStorage.MusicPlatformAccounts.ForEach(musicPlatformAccount => musicPlatformAccount.Refresh());
        public static void UpdateSongSource() =>
            PersistantStorage.SongSources.ForEach(songSource => songSource.UpdateSongs());
    }
}
