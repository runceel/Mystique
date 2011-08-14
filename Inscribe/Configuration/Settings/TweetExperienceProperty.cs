﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Inscribe.Configuration.Settings
{
    public class TweetExperienceProperty
    {
        public TweetExperienceProperty()
        {
            ShowUnofficialRetweetButton = true;
            ShowQuoteButton = false;
            P3StyleIcon = true;
            NameAreaWidth = 120;
            UserNameViewMode = NameView.ID;
            NotificationNameViewMode = NameView.ID;
            UrlResolveMode = UrlResolve.OnPointed;
            UrlTooltipShowLength = 60 * 1000;
            this.FullLineView = false;
        }

        public bool ShowUnofficialRetweetButton { get; set; }

        public bool ShowQuoteButton { get; set; }

        public bool P3StyleIcon { get; set; }

        public int NameAreaWidth { get; set; }

        public NameView UserNameViewMode { get; set; }

        public NameView NotificationNameViewMode { get; set; }

        public UrlResolve UrlResolveMode { get; set; }

        public int UrlTooltipShowLength { get; set; }

        public bool FullLineView { get; set; }
    }

    public enum NameView
    {
        ID,
        Name,
        Both
    }

    public enum UrlResolve
    {
        /// <summary>
        /// URL短縮を解決しません
        /// </summary>
        Never,
        /// <summary>
        /// ツールチップ上で解決します
        /// </summary>
        OnPointed,
        /// <summary>
        /// テキスト表示で解決します
        /// </summary>
        OnReceived
    }
}
