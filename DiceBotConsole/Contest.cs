﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiceBotConsole
{
    class Contest
    {
        /// <summary>
        /// コンテスト開始時間
        /// </summary>
        public DateTime StartTime { get; set; }

        /// <summary>
        /// コンテスト終了時間
        /// </summary>
        public DateTime EndTime { get; set; }

        /// <summary>
        /// コンテストへのリンク
        /// </summary>
        public string Link { get; set; }

        /// <summary>
        /// コンテスト名
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// 現在時刻からの残り時間
        /// </summary>
        public TimeSpan RemitTime
        {
            get
            {
                return StartTime - DateTime.Now;
            }
        }

        /// <summary>
        /// 開始時間から終了時間の文字列
        /// </summary>
        public string StartToEnd
        {
            get
            {
                return StartTime.ToString() + " - " + EndTime.ToString("HH:mm:ss");
            }
        }

        /// <summary>
        /// メッセージ内容
        /// </summary>
        public string Message
        {
            get
            {
                return Name + "\r\n" + StartToEnd + "\r\n" + Link;
            }
        }
        
        /// <summary>
        /// すべての要素を削除
        /// </summary>
        public void Clear()
        {
            Link = "";
            Name = "";
        }
    }
}
