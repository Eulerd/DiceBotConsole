using CoreTweet;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using System.Timers;

namespace DiceBotConsole
{
    class Program
    {
        static List<Contest> Contests = new List<Contest>();
        static SortedDictionary<DateTime, List<string>> ContestNotifyList = new SortedDictionary<DateTime, List<string>>();

        static GetContest GetCon = new GetContest();
        static TwitterBot bot = new TwitterBot();
        static SlackWebHook WebHook = new SlackWebHook();
        static string xmlfilename = "Reply.xml";

        static Timer AddTimer = new Timer();
        static Timer CheckTimer = new Timer();
        static Timer ReplyTimer = new Timer();

        static void Main(string[] args)
        {
            // TwitterBotを開始

            bot.SessionStart();

            Console.WriteLine("BotStart");

            bot.token.Statuses.Update("起動しました。" + DateTime.Now);

            // タイマー設定
            AddTimer.Elapsed += new ElapsedEventHandler(AddContestForDays);
            CheckTimer.Elapsed += new ElapsedEventHandler(CheckNotifyTime);
            ReplyTimer.Elapsed += new ElapsedEventHandler(CheckReply);

            AddTimer.Interval = (int)TimeSpan.FromDays(1).TotalMilliseconds;
            CheckTimer.Interval = 1000;
            ReplyTimer.Interval = (int)TimeSpan.FromMinutes(30).TotalMilliseconds;

        }

        /// <summary>
        /// XMLファイルから通知するユーザ名を取得する
        /// </summary>
        /// <returns></returns>
        static List<string> GetReplyUserName()
        {
            List<string> ReplyUserName = new List<string>();
            XmlDocument doc = new XmlDocument();

            doc.Load(xmlfilename);

            foreach (XmlElement element in doc.DocumentElement)
            {
                string username = element.InnerText;

                ReplyUserName.Add(username);
            }

            return ReplyUserName;
        }

        static string OldMessage = "";
        static void Response(string Message)
        {
            //リプを送るTextBoxに名前がある　かつ　フォロワーにいる
            //ときにツイート
            if(Message != OldMessage)
            {
                Cursored<User> users = bot.token.Friends.List();
                List<string> usernames = GetReplyUserName();

                foreach (var user in users)
                {
                    for (int j = 0; j < usernames.Count; j++)
                    {
                        if (user.ScreenName == usernames[j])
                        {
                            try
                            {
                                bot.token.Statuses.Update
                                    (new { status = "@" + usernames[j] + Environment.NewLine + Message, in_reply_to_status_id = user.Id });

                                Console.WriteLine("@" + usernames[j] + "へ通知します" + "\r\n");
                            }
                            catch
                            {
                                Console.WriteLine("---:投稿できませんでした。");
                            }
                        }
                    }
                }

                // Slackに通知
                WebHook.Upload(Message);
            }

            OldMessage = Message;
        }


        static void CheckReply(object sender, ElapsedEventArgs e)
        {
            string notify = "通知して";
            string notnotify = "通知しないで";

            try
            {
                var rateLimits = bot.token.Application.RateLimitStatus();
                int remaining = rateLimits["statuses"]["/statuses/mentions_timeline"].Remaining;

                if (remaining > 0)
                {
                    foreach (var status in bot.token.Statuses.MentionsTimeline())
                    {
                        if (status.Text.Contains(notify))
                        {
                            XmlDocument document = new XmlDocument();
                            List<string> InReplyToUsers = new List<string>();

                            try
                            {
                                document.Load(xmlfilename);

                                foreach (XmlElement element in document.DocumentElement)
                                    InReplyToUsers.Add(element.InnerText);

                                if (!InReplyToUsers.Contains(status.User.ScreenName))
                                {
                                    bot.token.Statuses.Update(new
                                    {
                                        status = "@" + status.User.ScreenName + Environment.NewLine + "通知リストに追加します。",
                                        in_reply_to_status_id = status.Id
                                    });

                                    var xmlfile = XElement.Load("./" + xmlfilename);

                                    var user = new XElement("user", status.User.ScreenName);

                                    xmlfile.Add(user);

                                    xmlfile.Save("./" + xmlfilename);
                                }
                            }
                            catch (FileNotFoundException)
                            {
                                XmlDocument writer = new XmlDocument();
                                XmlDeclaration declaration = writer.CreateXmlDeclaration("1.0", "UTF-8", null);

                                XmlElement torepusers = writer.CreateElement("Users");

                                writer.AppendChild(declaration);
                                writer.AppendChild(torepusers);

                                writer.Save(xmlfilename);

                                Console.WriteLine("ファイルが存在しなかったため、ファイルを作成しました。");
                            }
                        }
                        if (status.Text.Contains(notnotify))
                        {
                            XmlDocument xml = new XmlDocument();
                            xml.Load(xmlfilename);

                            XmlNodeList list = xml.GetElementsByTagName("user");
                            foreach (XmlNode xn in list)
                            {
                                if (xn.InnerText == status.User.ScreenName)
                                {
                                    bot.token.Statuses.Update(new
                                    {
                                        status = "@" + status.User.ScreenName + Environment.NewLine + "通知リストから削除します。",
                                        in_reply_to_status_id = status.Id
                                    });

                                    xn.RemoveAll();
                                    break;
                                }
                            }

                            xml.Save(xmlfilename);
                        }
                        break;
                    }
                }
                else
                    Console.WriteLine("--- API切れにより取得に失敗しました。");
            }
            catch
            {
                Console.WriteLine("--- タイムアウトしました.");
            }
        }

        static void CheckNotifyTime(object sender, ElapsedEventArgs e)
        {
            DateTime now = DateTime.Now;
            List<DateTime> KeyTimes = new List<DateTime>(ContestNotifyList.Keys);

            foreach (DateTime time in KeyTimes)
            {
                if (time <= now)
                {
                    CheckTimer.Stop();

                    string OldMsg = "";
                    
                    foreach (string Message in ContestNotifyList[time])
                    {
                        if(Message != OldMsg)
                        {
                            Response(Message);

                            Console.WriteLine(" ++++++++++ ");
                            Console.WriteLine(Message);
                            Console.WriteLine(" ++++++++++ ");

                            OldMsg = Message;
                            System.Threading.Thread.Sleep(3000);
                        }
                    }

                    Console.WriteLine("Remove : " + ContestNotifyList[time][0]);
                    ContestNotifyList.Remove(time);

                    PrintLimitTime();

                    CheckTimer.Start();
                    break;
                }
            }
        }

        static void AddContestForDays(object sneder, ElapsedEventArgs e)
        {
            Contests.AddRange(GetCon.GetAtcoderContests(Contests));

            Console.WriteLine(" +++++ Update : " + DateTime.Now.ToString() + "+++++");

            ContestNotifyList = new SortedDictionary<DateTime, List<string>>();
            foreach (Contest con in Contests)
            {
                string[] Messages =
                    {
                        "コンテスト開始まであと10分です\n" + con.Message, "コンテスト開始まであと60分です\n" + con.Message,
                        "明日、コンテストがあります。\n" + con.Message, "一週間後、コンテストがあります。\n" + con.Message
                    };

                DateTime[] Times = 
                    {
                        con.StartTime.AddMinutes(-10), con.StartTime.AddHours(-1),
                        con.StartTime.AddDays(-1), con.StartTime.AddDays(-7)
                    };

                for(int i = 0;i < Times.Count();i ++)
                {
                    if (Times[i] < DateTime.Now)
                        continue;

                    // 時間が追加されていなかったらメッセージリストを初期化
                    if (!ContestNotifyList.ContainsKey(Times[i]))
                        ContestNotifyList.Add(Times[i], new List<string>());

                    // 時間に対応付けてメッセージを追加
                    if (!ContestNotifyList[Times[i]].Contains(Messages[i]))
                        ContestNotifyList[Times[i]].Add(Messages[i]);
                }
            }

            PrintLimitTime();
            PrintContests();

        }

        /// <summary>
        /// 通知リストの中身を表示
        /// </summary>
        static void PrintContests()
        {
            foreach (var Notify in ContestNotifyList)
            {
                Console.WriteLine("Notify time : " + Notify.Key);
                foreach (string Message in Notify.Value)
                {
                    Console.WriteLine(Message);
                }
                Console.WriteLine("__________");
            }
        }

        static void PrintLimitTime()
        {
            // 次のコンテスト通知時間を表示
            Console.WriteLine("====================");
            Console.WriteLine(DateTime.Now);
            if (ContestNotifyList.Any())
                Console.WriteLine("次のコンテスト通知まで後 : " + (ContestNotifyList.First().Key - DateTime.Now));
            else
                Console.WriteLine("コンテストがありません。");
            Console.WriteLine("====================");
        }
    }
}
