using NAudio.Wave;
using Sharprompt;
using System;
using System.Text;
using System.ComponentModel.DataAnnotations;
using System.Data.Linq;
using Newtonsoft.Json;

using ConsoleMusicPlayer.SongSource;
using ConsoleMusicPlayer.MusicPlatformAccount;
using static ConsoleMusicPlayer.Config;
using System.Reflection;

namespace ConsoleMusicPlayer
{
    internal class Program
    {
        [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
        class EnumText : Attribute
        {
            public string Text { get; set; }
            public EnumText(string Text)
            {
                this.Text = Text;
            }
        }
        enum MenuOperation
        {
            [EnumText("播放音乐")]
            Play,
            [EnumText("管理歌曲源")]
            ManageSongSource,
            [EnumText("管理音乐平台账户")]
            ManageMusicPlatformAccount,
            [EnumText("退出")]
            Quit
        }
        enum SongSourceOpetation
        {
            [EnumText("列出现有歌曲源")]
            List,
            [EnumText("添加歌曲源")]
            Add,
            [EnumText("删除歌曲源")]
            Delete
        }
        enum MusicPlatformAccountOpetation
        {
            [EnumText("列出现有音乐平台账号")]
            List,
            [EnumText("添加音乐平台账号")]
            Login,
            [EnumText("删除音乐平台账号")]
            Logout
        }

        private static T SelectEnum<T>(string Message) where T : Enum
        {
            var options = new SelectOptions<T>();
            options.Message = Message;
            var enumValues = Enum.GetValues(typeof(T));
            options.Items = (IEnumerable<T>)enumValues;
            options.DefaultValue = enumValues.GetValue(0);
            options.TextSelector = (enumValue) =>
            {
                EnumText? enumText = (EnumText?)typeof(T).GetField(Enum.GetName(typeof(T), enumValue)).GetCustomAttribute(typeof(EnumText));
                if (enumText != null)
                {
                    return enumText.Text;
                }
                return enumValue.ToString();
            };
            return Prompt.Select(options);
        }
        private static async Task MainLoopAsync()
        {
            while (true)
            {
                GC.Collect();
                var operation = SelectEnum<MenuOperation>("Please select an operation");
                if (operation == MenuOperation.Play)
                {
                    if (Config.PersistantStorage.Songs.Count() == 0)
                    {
                        ConsoleWriteErrorLine("No song to play! Please add a song source");
                    }
                    else
                    {
                        //Console.Title = 1.ToString();
                        //todooooooo
                        var song = Prompt.Select<Song>("Please choose a song to play", Config.PersistantStorage.Songs, 10);
                        //Console.Title = 2.ToString();
                        if (song != null)
                            Play(song);
                    }
                }
                else if (operation == MenuOperation.ManageSongSource)
                {
                    var songSourceOperation = SelectEnum<SongSourceOpetation>("Please select an operation");
                    if (songSourceOperation == SongSourceOpetation.List)
                    {
                        foreach(var songSource in Config.PersistantStorage.SongSources)
                        {
                            Console.WriteLine(songSource);
                        }
                    }
                    else if (songSourceOperation == SongSourceOpetation.Add)
                    {
                        var songSourceTypes = from type in Assembly.GetExecutingAssembly().GetTypes()
                                              where type.GetInterface("ISongSource") != null
                                              select type;
                        var songSourceType = Prompt.Select("Please select a song source type", songSourceTypes);
                        var constructors = songSourceType.GetConstructors();
                        ConstructorInfo constructor;
                        if (constructors.Length == 1)
                        {
                            constructor = constructors.First();
                        }
                        else
                        {
                            constructor = Prompt.Select("Please select a way to construct the SongSource", constructors, textSelector: (x) =>
                            {
                                StringBuilder sb = new StringBuilder("Construct with ");
                                var parameters = x.GetParameters();
                                for (int i = 0; i < parameters.Length; i++)
                                {
                                    if (i != 0)
                                        sb.Append(", ");
                                    sb.Append(parameters[i].Name);
                                    sb.Append('(');
                                    sb.Append(parameters[i].ParameterType.Name);
                                    sb.Append(')');
                                }
                                return sb.ToString();
                            });
                        }
                        var constructorParameters = new List<ParameterInfo>(constructor.GetParameters());
                        var constructorArgs = new List<object?>();
                        var requireAccountAttribute = constructor.GetCustomAttribute<RequireMusicPlatformAccountAttribute>();
                        if (requireAccountAttribute != null)
                        {
                            var validAccounts = Config.PersistantStorage.MusicPlatformAccounts
                                .Where(requireAccountAttribute.AccountType.IsInstanceOfType);
                            if (validAccounts.Count() == 0)
                            {
                                ConsoleWriteErrorLine($"This SongSource needs an account of {requireAccountAttribute.AccountType}! Please login and try again.");
                                continue;
                            }
                            IMusicPlatformAccount account;
                            if (validAccounts.Count() > 1)
                            {
                                account = Prompt.Select("Please select an account to use for this SongSource", validAccounts);
                            }
                            else
                            {
                                account = validAccounts.First();
                            }
                            constructorArgs.Add(account);
                            constructorParameters.RemoveAt(0);
                        }
                        foreach (var parameter in constructorParameters)
                        {                            
                            var content = (typeof(Prompt).GetMethod("Input", new Type[] { typeof(string), typeof(object), typeof(string), typeof(IList<Func<object, ValidationResult>>) })
                                ?? throw new Exception("Fatal Error: Package Sharpromt Static Method Input Parameter isn't Corresponding!"))
                                .MakeGenericMethod(new Type[] { parameter.ParameterType })
                                .Invoke(null, new object?[] { $"Please input {parameter.Name} of this SongSource", null, null, null });
                            constructorArgs.Add(content);
                            //Console.Title = content.ToString();
                        }
                        try
                        {
                            Config.PersistantStorage.SongSources.Add((ISongSource)songSourceType.GetConstructors().First().Invoke(constructorArgs.ToArray()));
                            // 不能用Activator，麻了
                        }
                        catch (TargetInvocationException ex)
                        {
                            ConsoleWriteErrorLine(ex.InnerException.Message);
                        }
                    }
                    else if (songSourceOperation == SongSourceOpetation.Delete)
                    {
                        if (Config.PersistantStorage.SongSources.Count == 0)
                        {
                            ConsoleWriteErrorLine("No SongSource to delete!"); // The song source list is empty!
                        }
                        else
                        {
                            var songSource = Prompt.Select("Please select a SongSource to delete", Config.PersistantStorage.SongSources);
                            if (Prompt.Confirm($"Are you sure to delete {songSource}?", false))
                            {
                                Config.PersistantStorage.SongSources.Remove(songSource);
                            }
                        }
                    }
                }
                else if (operation == MenuOperation.ManageMusicPlatformAccount)
                {
                    var musicPlatformAccountOperation = SelectEnum<MusicPlatformAccountOpetation>("Please select an operation");
                    if (musicPlatformAccountOperation == MusicPlatformAccountOpetation.List)
                    {
                        foreach (var account in Config.PersistantStorage.MusicPlatformAccounts)
                        {
                            Console.WriteLine(account);
                        }
                    }
                    else if (musicPlatformAccountOperation == MusicPlatformAccountOpetation.Login)
                    {
                        var musicPlatformAccountTypes = from type in Assembly.GetExecutingAssembly().GetTypes()
                                                        where type.GetInterface("IMusicPlatformAccount") != null
                                                        select type;
                        var musicPlatformAccountType = Prompt.Select("Please select a music platform type", musicPlatformAccountTypes);
                        var musicPlatformAccount = Activator.CreateInstance(musicPlatformAccountType)
                            ?? throw new NotImplementedException("The MusicPlatformAccountType doesn't implement constructor with no parameter");

                        var loginMethods = from method in musicPlatformAccountType.GetMethods()
                                           where method.Name.StartsWith("LoginWith")
                                           select method;
                        MethodInfo loginMethod;
                        if (loginMethods.Count() == 1)
                        {
                            loginMethod = loginMethods.First();
                        }
                        else
                        {
                            loginMethod = Prompt.Select("Please select a method to login to this MusicPlatformAccount", loginMethods, textSelector: x => x.Name);
                        }
                        var loginMethodParameters = loginMethod.GetParameters();
                        var loginMethodArgs = new List<object?>();
                        foreach (var parameter in loginMethodParameters)
                        {
                            var content = (typeof(Prompt).GetMethod("Input", new Type[] { typeof(string), typeof(object), typeof(string), typeof(IList<Func<object, ValidationResult>>) })
                                ?? throw new Exception("Fatal Error: Package Sharpromt Static Method Input Parameter isn't Corresponding!"))
                                .MakeGenericMethod(new Type[] { parameter.ParameterType })
                                .Invoke(null, new object?[] { $"Please input {parameter.Name} of this MusicPlatformAccount", null, null, null });
                            loginMethodArgs.Add(content);
                        }
                        try
                        {
                            loginMethod.Invoke(musicPlatformAccount, loginMethodArgs.ToArray());

                            Config.PersistantStorage.MusicPlatformAccounts.Add((IMusicPlatformAccount)musicPlatformAccount);
                        }
                        catch (TypeInitializationException ex)
                        {
                            ConsoleWriteErrorLine(ex.InnerException.Message);
                        }
                    }
                    else if (musicPlatformAccountOperation == MusicPlatformAccountOpetation.Logout)
                    {
                        if (Config.PersistantStorage.MusicPlatformAccounts.Count == 0)
                        {
                            ConsoleWriteErrorLine("No MusicPlatformAccount to delete!"); // The music platform account list is empty!
                        }
                        else
                        {
                            var songSource = Prompt.Select("Please select a MusicPlatformAccount to delete", Config.PersistantStorage.MusicPlatformAccounts);
                            if (Prompt.Confirm($"Are you sure to delete {songSource}?", false))
                            {
                                Config.PersistantStorage.MusicPlatformAccounts.Remove(songSource);
                            }
                        }
                    }
                    
                }
                else if (operation == MenuOperation.Quit)
                {
                    break;
                }
            }
        }
        private static void Play(Song song)
        {
            using (var ms = new MemoryStream())
            {
                using (var stream = song.GetStreamAsync().Result)
                {
                    stream.CopyTo(ms);
                    stream.Close();
                }
                ms.Position = 0; // nnd，少了这一句我查了几个小时
                using (var reader = new Mp3FileReader(ms))
                {
                    using (var waveOut = new WaveOutEvent())
                    {
                        Console.Clear();
                        Console.CursorVisible = false;
                        var cts = new CancellationTokenSource();
                        waveOut.Init(reader);
                        waveOut.PlaybackStopped += (sender, e) =>
                        {
                            cts.Cancel();
                        };
                        waveOut.Volume = 0.8f;
                        waveOut.Play();
                        Task.Run(() => // Status thread: Title, CUI
                        {
                            const string timeSpanFormatString = @"mm\:ss";
                            TimeSpan totalTime = reader.TotalTime;
                            char[] totalTimeStringBuffer = new char[10];
                            int totalTimeStringBufferLength;
                            totalTime.TryFormat(totalTimeStringBuffer, out totalTimeStringBufferLength, timeSpanFormatString);
                            string totalTimeString = new string(totalTimeStringBuffer, 0, totalTimeStringBufferLength);
                            while (!cts.IsCancellationRequested)
                            {
                                TimeSpan currentTime = reader.TotalTime * ((double)reader.Position / reader.Length);
                                char[] currentTimeStringBuffer = new char[10];
                                int currentTimeStringBufferLength;
                                currentTime.TryFormat(currentTimeStringBuffer, out currentTimeStringBufferLength, timeSpanFormatString);
                                string currentTimeString = new string(currentTimeStringBuffer, 0, currentTimeStringBufferLength);
                                Console.Title = $"{currentTimeString}/{totalTimeString} --- {song.Name}";


                                Thread.Sleep(TimeSpan.FromSeconds(1));
                            }
                        }, cts.Token);

                        Task.Run(() => // Keyboard event thread
                        {
                            while (!cts.IsCancellationRequested)
                            {
                                Console.SetCursorPosition(0, 0);
                                Console.Write("Now Playing: ");
                                Console.ForegroundColor = ConsoleColor.Cyan;
                                Console.WriteLine(song.Name);
                                Console.ForegroundColor = ConsoleColor.Gray;
                                float currentVolume = MathF.Round(waveOut.Volume * 10, 0, MidpointRounding.AwayFromZero);
                                Console.Write("Current Volume: ");
                                Console.ForegroundColor = ConsoleColor.Cyan;
                                Console.WriteLine($"{(currentVolume == 0 ? "" : currentVolume)}0% "); // <-- notice this space, for 100%->90%
                                Console.ForegroundColor = ConsoleColor.Gray;

                                ConsoleKeyInfo key = Console.ReadKey();
                                if (key.Key == ConsoleKey.Escape)
                                    cts.Cancel();
                                if (key.Key == ConsoleKey.UpArrow)
                                    waveOut.Volume = MathF.Min(waveOut.Volume + 0.1f, 1);
                                if (key.Key == ConsoleKey.DownArrow)
                                    waveOut.Volume = MathF.Max(waveOut.Volume - 0.1f, 0);
                                if (key.Key == ConsoleKey.LeftArrow)
                                    reader.Skip(-1);
                                if (key.Key == ConsoleKey.RightArrow)
                                    reader.Skip(1);
                                if (key.Key == ConsoleKey.Spacebar)
                                {
                                    if (waveOut.PlaybackState == PlaybackState.Playing)
                                    {
                                        waveOut.Pause();
                                    }
                                    else
                                    {
                                        waveOut.Play();
                                    }
                                }
                            }
                        }, cts.Token);
                        while (!cts.IsCancellationRequested) Thread.Sleep(TimeSpan.FromSeconds(0.2));
                        Console.Clear();
                    }
                }
            }
        }
        public static async Task Main(string[] args)
        {
            if (File.Exists("config.json"))
            {
                Config.PersistantStorage = jsonSerializer.Deserialize<PersistantStorage>(new JsonTextReader(File.OpenText("config.json")))
                    ?? throw new ArgumentException("Invalid config.json");
                    
            }
            Config.RefreshAccount();
            Config.UpdateSongSource();
            await MainLoopAsync();
            
            
            File.WriteAllText("config.json", JsonConvert.SerializeObject(Config.PersistantStorage, Config.settings));
            //jsonSerializer.Serialize(File.CreateText("config.json"), Config.PersistantStorage);
        }
        private static void ConsoleWriteErrorLine<T>(T content)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(content);
            Console.ForegroundColor = ConsoleColor.Gray;
        }
    }
}