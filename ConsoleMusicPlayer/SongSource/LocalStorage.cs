using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace ConsoleMusicPlayer.SongSource
{
    public partial class Song
    {
        public static implicit operator Song(FileInfo fileInfo)
            => new Song { Name = fileInfo.Name, Uri = new Uri($"file://{fileInfo.FullName}") };
    }
    
    internal class LocalStorage : ISongSource
    {
        public string Name; // represents a local path
        [JsonIgnore]
        public IEnumerable<Song> Songs { get; private set; }
        public LocalStorage(string DirectoryPath)
        {
            if (DirectoryPath != null)
            {
                Name = DirectoryPath;
                if (DirectoryPath == null)
                {
                    throw new SongSourceConstuctionException("DirectoryPath cannot be null.");
                }
                DirectoryInfo directoryInfo = new DirectoryInfo(DirectoryPath);
                if (!directoryInfo.Exists)
                {
                    throw new SongSourceConstuctionException("The directory does not exist.");
                }
                UpdateSongs();
            }
        }

        // nnd, 为什么不能.Cast<Song>()
        public void UpdateSongs()
        {
            Songs = new DirectoryInfo(Name).GetFiles("*.mp3").Select(o => (Song)o);
        }

        public override string ToString()
        {
            return $"LocalStorage: {Name}";
        }
    }
}
