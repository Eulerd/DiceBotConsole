using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using HtmlAgilityPack;

namespace DiceBotConsole
{
    class GetContest
    {
        HtmlDocument HomePage;
        HtmlDocument ContestPage;

        /// <summary>
        /// Atcoderのコンテスト情報を取得する
        /// </summary>
        /// <returns>新しいAtcoderのコンテスト</returns>
        public List<Contest> GetAtcoderNewContests(List<Contest> OldContests)
        {
            List<Contest> contests = new List<Contest>();

            HomePage = Scraping("https://atcoder.jp/?lang=ja", "utf-8");

            //予定されたコンテスト部分の情報を取得
            if (HomePage != null)
            {
                var nodes = HomePage.DocumentNode.SelectNodes("//div[2]/table[@class='table table-default table-striped table-hover table-condensed']/tbody/tr/td[2]/small/a")
                                    .Select(a => new
                                    {
                                        Link = a.Attributes["href"].Value.Trim(),
                                        Name = a.InnerText.Trim(),
                                    });

                //予定されたコンテスト部が存在するか確認
                string title = HomePage.DocumentNode.SelectSingleNode("//h4[2]").InnerText;
                if (!title.Contains("終了"))
                {
                    Contest con = new Contest();
                    foreach (var node in nodes.Take(nodes.Count()))
                    {
                        con = new Contest();

                        // リンクと名前を追加
                        con.Link = node.Link;
                        con.Name = node.Name;


                        // 開始時間と終了時間を取得
                        ContestPage = new HtmlDocument();

                        string html = "";
                        try
                        {
                            WebClient wc = new WebClient();
                            html = wc.DownloadString(con.Link);
                            ContestPage.LoadHtml(html);
                        }
                        catch
                        {
                            Console.WriteLine("--- URLが正しく読み取ることが出来ませんでした\a");
                        }

                        try
                        {
                            var StartTime = ContestPage.DocumentNode.SelectSingleNode(@"//*[@id='contest-start-time']");
                            var EndTime = ContestPage.DocumentNode.SelectSingleNode(@"//*[@id='contest-end-time']");

                            // 開始時間と終了時間を追加
                            con.StartTime = DateTime.Parse(StartTime.InnerText);
                            con.EndTime = DateTime.Parse(EndTime.InnerText);

                            // 新しいコンテストを追加
                            contests.Add(con);

                            // それっぽく
                            Console.Write(".");
                        }
                        catch (NullReferenceException)
                        {
                            Console.WriteLine("コンテストを追加できませんでした。");
                            Console.WriteLine(" ------- " + con.Name);
                            Console.WriteLine(" ------- " + con.Link);
                            Console.WriteLine(" ------- " + con.StartToEnd);
                        }
                    }
                }
            }
            else
                Console.WriteLine(" ---- コンテストを取得できませんでした。");

            Console.WriteLine();

            Dictionary<string, string> Names = new Dictionary<string, string>();
            Names.Add("AtCoder Beginner Contest", "ABC");
            Names.Add("AtCoder Regular Contest", "ARC");
            Names.Add("AtCoder Grand Contest", "AGC");

            // コンテスト名をABC,ARC,AGCに短縮
            for (int i = 0;i < contests.Count();i++)
            {
                string name = contests[i].Name;
                
                foreach(var ShortName in Names)
                    name = name.Replace(ShortName.Key, ShortName.Value);

                contests[i].Name = name;
            }

            // コンテストをまとめる
            contests = SumContest(contests);

            // 既存のコンテストなら追加しない
            for (int i = 0;i < OldContests.Count;i++)
            {
                contests.Remove(OldContests[i]);
            }
            
            return contests;
        }
        
        /// <summary>
        /// urlのページからhtml情報を取得する
        /// </summary>
        /// <param name="url">指定するURL</param>
        /// <param name="codepage">文字コード指定</param>
        /// <returns></returns>
        private HtmlDocument Scraping(string url,string codepage)
        {
            HtmlDocument doc = new HtmlDocument();
            WebClient wc = new WebClient();
            string html = "";

            wc.Encoding = Encoding.GetEncoding(codepage);

            try
            {
                html = wc.DownloadString(url);
            }
            catch
            {
                Console.WriteLine("--- 無効なURLです\a");
            }

            doc.LoadHtml(html);

            return doc;
        }


        /// <summary>
        /// 開始時間と終了時間が同じであるコンテストをまとめる
        /// </summary>
        /// <param name="contests">まとめたいコンテスト</param>
        /// <returns>まとめられたコンテスト</returns>
        private List<Contest> SumContest(List<Contest> contests)
        {
            for (int i = 0; i < contests.Count; i++)
            {
                DateTime start = contests[i].StartTime;
                DateTime end = contests[i].EndTime;

                for (int j = i + 1; j < contests.Count; j++)
                {
                    if (!contests[j].Name.Contains(contests[i].Name) && !contests[i].Name.Contains(contests[j].Name))
                    {
                        if (start == contests[j].StartTime && end == contests[i].EndTime)
                        {
                            contests[i].Name += " / " + contests[j].Name;
                            contests[i].Link += "\r\n" + contests[j].Link;
                            contests.RemoveAt(j);
                        }
                    }
                }
            }

            return contests;
        }
    }
}
