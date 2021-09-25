using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using TiuLib.Model;

namespace CommonObjects.BL
{
    public static class ServerInteraction
    {
        public static async Task InitAsync()
        {
            try
            {
                var clientJson = await HttpClient.GetStringAsync(ServerAddress + "/clients");
                Client = (Client) JsonConvert.DeserializeObject(clientJson, typeof(Client));
                IsOnline = true;
            }
            catch (Exception)
            {
                IsOnline = false;
            }
        }

        public static Client Client { get; set; }
#if DEBUG
        private const string ServerAddress = "http://localhost:23832";
#else
        const string ServerAddress = "http://tiuexport.doc-e.ru";
#endif

        public static bool IsOnline { get; set; }
        public static bool IsAuthorized => Client?.Permit ?? false;
        private static readonly HttpClient HttpClient = new HttpClient();

        public static async Task WriteLogAsync(string logMessage)
        {
            
            var values = new Dictionary<string, string>
            {
                {nameof(EventLog.ClientId), Client?.Id},
                {nameof(EventLog.LogMessage), logMessage}
            };

            var content = new FormUrlEncodedContent(values);
            if (IsOnline)
                try
                {
                    var response = await HttpClient.PostAsync(ServerAddress + "/EventLogs/Create", content);
                }
                catch (Exception)
                {
                    IsOnline = false;
                }
        }
    }
}