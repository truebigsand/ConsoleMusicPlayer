using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;

namespace ConsoleMusicPlayer.MusicPlatformAccount
{
    public interface IMusicPlatformAccount
    {
        void Refresh();
        HttpClient HttpClient { get; }
    }

    [Serializable]
    public class MusicPlatformConstructionException : Exception
    {
        public MusicPlatformConstructionException() { }
        public MusicPlatformConstructionException(string message) : base(message) { }
        public MusicPlatformConstructionException(string message, Exception inner) : base(message, inner) { }
        protected MusicPlatformConstructionException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }

    [Serializable]
    public class MusicPlatformNotLoggedInException : Exception
    {
        public MusicPlatformNotLoggedInException() { }
        public MusicPlatformNotLoggedInException(string message) : base(message) { }
        public MusicPlatformNotLoggedInException(string message, Exception inner) : base(message, inner) { }
        protected MusicPlatformNotLoggedInException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }

    [Serializable]
    public class MusicPlatformLoginFailedException : Exception
    {
        public MusicPlatformLoginFailedException() { }
        public MusicPlatformLoginFailedException(string message) : base(message) { }
        public MusicPlatformLoginFailedException(string message, Exception inner) : base(message, inner) { }
        protected MusicPlatformLoginFailedException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }
}
