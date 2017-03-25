using CoreTweet;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Xml;
using System.Xml.Linq;

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

        static void Main(string[] args)
        {
            // TwitterBotを開始

            bot.SessionStart();

            Console.WriteLine("BotStart");

            bot.token.Statuses.Update("起動しました。" + DateTime.Now);

            // タイマー設定
            AutoResetEvent CheckAutoEve = new AutoResetEvent(false);
            AutoResetEvent AddAutoEve = new AutoResetEvent(false);
            AutoResetEvent ReplyAutoEve = new AutoResetEvent(false);

            TimerCallback CheckTimerDelegate = new TimerCallback(CheckNotifyTime);
            TimerCallback AddTimerDelegate = new TimerCallback(AddContestForDays);
            TimerCallback ReplyTimerDelegate = new TimerCallback(CheckReply);

            Timer AddTimer = new Timer(AddTimerDelegate, AddAutoEve, 0, (int)TimeSpan.FromDays(1).TotalMilliseconds);
            Timer CheckTimer = new Timer(CheckTimerDelegate, CheckAutoEve, 0, 1000);
            Timer ReplyTimer = new Timer(ReplyTimerDelegate, ReplyAutoEve, 0, (int)TimeSpan.FromMinutes(30).TotalMilliseconds);

            AddAutoEve.WaitOne(-1);
            CheckAutoEve.WaitOne(-1);
            ReplyAutoEve.WaitOne(-1);
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


        static void Response(string Message)
        {
            //リプを送るTextBoxに名前がある　かつ　フォロワーにいる
            //ときにツイート
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


        static void CheckReply(Object info)
        {
            AutoResetEvent AutoEve = (AutoResetEvent)info;

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


        static void CheckNotifyTime(Object info)
        {
            AutoResetEvent AutoEve = (AutoResetEvent)info;

            for (int i = 0; i < Contests.Count; i++)
            {
                if (Contests[i].RemitTime.TotalSeconds < 0)
                    Contests.RemoveAt(i);
            }

            DateTime now = DateTime.Now;
            List<DateTime> KeyTimes = new List<DateTime>(ContestNotifyList.Keys);

            foreach (DateTime time in KeyTimes)
            {
                if (now.AddSeconds(-0.5) > time)
                {
                    ContestNotifyList.Remove(time);
                    continue;
                }
                else if(time <= now.AddSeconds(0.5))
                {
                    foreach (string Message in ContestNotifyList[time])
                    {
                        Response(Message);

                        Console.WriteLine(" ++++++++++ ");
                        Console.WriteLine(Message);
                        Console.WriteLine(" ++++++++++ ");
                    }

                    ContestNotifyList.Remove(time);
                    break;
                }
            }
        }

        static void AddContestForDays(Object Info)
        {
            AutoResetEvent AutoEve = (AutoResetEvent)Info;

            Contests.AddRange(GetCon.GetAtcoderContests(Contests));

            Console.WriteLine(" +++++ Update : {0} +++++", DateTime.Now.ToString());
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

            // 通知リストの中身を表示
            /*
            foreach(var Notify in ContestNotifyList)
            {
                Console.WriteLine("{0} : ", Notify.Key);
                foreach(string Message in Notify.Value)
                {
                    Console.WriteLine(Message);
                }
                Console.WriteLine("__________");
            }
            */
        }
    }
}
