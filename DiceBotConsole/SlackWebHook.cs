using System;
using System.Net;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace DiceBotConsole
{
    class SlackWebHook
    {
        static string WebHookUrl = "";

        /// <summary>
        /// SlackBot用
        /// </summary>
        [JsonObject("user")]
        public class UserModel
        {
            [JsonProperty("text")]
            public string Text { get; set; }
            [JsonProperty("icon_emoji")]
            public string Icon { get; set; }
            [JsonProperty("username")]
            public string UserName { get; set; }
        }


        public void Upload(string message)
        {
            if(WebHookUrl == "")
            {
                Console.Write("WEBHOOKURL>");

                WebHookUrl = Console.ReadLine();
            }

            var wc = new WebClient();

            var data = new UserModel();
            data.Text = message;
            data.Icon = ":dicek:";
            data.UserName = "EulerdBotTest";

            string json = JsonConvert.SerializeObject(data);

            wc.Headers.Add(HttpRequestHeader.ContentType, "application/json;charset=UTF-8");
            wc.Encoding = Encoding.UTF8;

            wc.UploadString(WebHookUrl, json);
        }
    }
}
