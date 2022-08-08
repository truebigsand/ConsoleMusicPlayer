using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleMusicPlayer.SongSource
{
    public partial class Song
    {
        private static readonly HttpClient httpClient = new HttpClient();
        public string Name = string.Empty;
        public Uri Uri;
        public Task<Stream> GetStreamAsync()
        {
            if (Uri.IsFile)
            {
                return Task.FromResult((Stream)File.OpenRead(Uri.LocalPath));
            }
            else
            {
                return httpClient.GetStreamAsync(Uri);
            }
        }
        public override string ToString()
        {
            return Name;
        }
    }
    public interface ISongSource
    {
        IEnumerable<Song> Songs { get; }
        void UpdateSongs();
    }
    [System.AttributeUsage(AttributeTargets.Constructor, Inherited = false, AllowMultiple = true)]
    sealed class RequireMusicPlatformAccountAttribute : Attribute
    {
        public Type AccountType;
        public RequireMusicPlatformAccountAttribute(Type accountType)
        {
            AccountType = accountType;
        }
    }
    [Serializable]
    public class SongSourceConstuctionException : Exception
    {
        public SongSourceConstuctionException() { }
        public SongSourceConstuctionException(string message) : base(message) { }
        public SongSourceConstuctionException(string message, Exception inner) : base(message, inner) { }
        protected SongSourceConstuctionException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }

    [Serializable]
    public class SongSourceApiCallFailedException : Exception
    {
        public SongSourceApiCallFailedException() { }
        public SongSourceApiCallFailedException(string message) : base(message) { }
        public SongSourceApiCallFailedException(string message, Exception inner) : base(message, inner) { }
        protected SongSourceApiCallFailedException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }
}
