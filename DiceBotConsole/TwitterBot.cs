using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CoreTweet;

namespace DiceBotConsole
{
    class TwitterBot
    {
        static string APIKey = "";
        static string APISecret = "";
        public Tokens token;

        public void SessionStart()
        {
            Console.Write("[APIKey]>");
            APIKey = Console.ReadLine();
            Console.Write("[APISecret]>");
            APISecret = Console.ReadLine();


            if (AllInput())
            {
                try
                {
                    OAuth.OAuthSession session = OAuth.Authorize(APIKey, APISecret);

                    Console.WriteLine("承認してください : ");
                    Console.WriteLine(session.AuthorizeUri.AbsoluteUri);
                    Console.WriteLine();
                    Console.Write("[PIN]>");

                    string pin = Console.ReadLine();

                    token = session.GetTokens(pin);
                }
                catch(TwitterException)
                {
                    Console.WriteLine("--- セッションを開始できませんでした。\a");
                }
            }
            else
            {
                Console.WriteLine("--- セッション開始に必要な情報が不足しています。");
            }
        }

        bool AllInput()
        {
            return (APIKey != "" && APISecret != "");
        }
    }
}
