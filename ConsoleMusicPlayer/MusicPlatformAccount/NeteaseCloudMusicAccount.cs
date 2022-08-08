using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.Net;

namespace ConsoleMusicPlayer.MusicPlatformAccount
{
    public class NeteaseCloudMusicAccount : IMusicPlatformAccount
    {
        private static readonly string BASE_API_ADDRESS_STRING = "http://cloud-music.pl-fe.cn"; // Https: https://pl-fe.cn/cloud-music-api
        private static readonly CookieContainer cookieContainer = new CookieContainer();
        private HttpClient httpClient;
        public HttpClient HttpClient { get { return httpClient; } }
        public string? Phone;
        public string? Email;
        public string Password;

        private string AccountName;
        public NeteaseCloudMusicAccount()
        {
            Password = string.Empty;
            httpClient = new HttpClient(new HttpClientHandler() { CookieContainer = cookieContainer })
            {
                BaseAddress = new Uri(BASE_API_ADDRESS_STRING)
            };
            AccountName = string.Empty;
            Refresh();
        }
        public void LoginWithEmailAndPassword(string Email, string Password)
        {
            this.Email = Email;
            this.Password = Password;
            SetCookiesAndAccountNameWithHttpGet($"/login?email={Email}&password={Password}");
        }
        public void LoginWithPhoneAndPassword(string Phone, string Password)
        {
            this.Phone = Phone;
            this.Password = Password;
            SetCookiesAndAccountNameWithHttpGet($"/login/cellphone?phone={Phone}&password={Password}");
        }
        private void SetCookiesAndAccountNameWithHttpGet(string Url)
        {
            using (var response = HttpClient.GetAsync(Url).Result)
            {
                if (!response.IsSuccessStatusCode)
                {
                    throw new MusicPlatformConstructionException($"NeteaseCloudMusicAccount login with phone and password api call failed, http code: {(int)response.StatusCode}.");
                }
                JObject result = JObject.Parse(response.Content.ReadAsStringAsync().Result);
                JToken? codeJToken;
                if (!result.TryGetValue("code", out codeJToken) || codeJToken == null)
                {
                    throw new MusicPlatformConstructionException("NeteaseCloudMusicAccount login with phone and password api call failed: response doesn't have code property.");
                }
                int code = codeJToken.ToObject<int>();
                if (code != 200)
                {
                    throw new MusicPlatformLoginFailedException($"NeteaseCloudMusicAccount login with phone and password api call failed: {result.GetValue("msg").ToObject<string>()}.");
                }
                AccountName = result["profile"]["nickname"].ToString();
            }
        }
        public void Refresh()
        {
            if (Phone != null)
            {
                LoginWithPhoneAndPassword(Phone, Password);
            }
            else if (Email != null)
            {
                LoginWithEmailAndPassword(Email, Password);
            }
        }
        public override string ToString()
        {
            return $"NeteaseCloudMusicAccount: {AccountName}({Phone ?? Email})";
        }
    }
}
