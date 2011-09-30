﻿using System;
using System.ComponentModel.Composition;
using System.Linq;
using Acuerdo.Plugin;
using Dulcet.Twitter;
using Inscribe.Common;
using Inscribe.Communication.Posting;
using Inscribe.Core;
using Inscribe.Storage;
using Inscribe.Subsystems;
using Inscribe.Storage.Perpetuation;

namespace SookIkemen
{
    [Export(typeof(IPlugin))]
    public class EntryPoint : IPlugin
    {
        public string Name
        {
            get { return "スークイケメンﾅｰｰｰｰｰｰｰｰｰｰｰｰｰｰｰｰｰｰｰｰｰｰｰｰｰ"; }
        }

        public double Version
        {
            get { return 1.1; }
        }

        public void Loaded()
        {
            KeyAssignCore.RegisterOperation("SookIkemen", () =>
                KeyAssignHelper.ExecuteTabAction(tab =>
                {
                    try
                    {
                        tab.TabProperty.LinkAccountInfos.ForEach(a =>
                            PostOffice.UpdateTweet(a, "スークイケメンﾅｰｰｰｰｰｰｰｰｰｰｰｰｰｰｰｰｰｰｰｰｰｰｰｰｰｰｰｰｰｰｰｰ #sook_ikemen"));
                    }
                    catch (Exception e)
                    {
                        ExceptionStorage.Register(e, ExceptionCategory.PluginError, "スークイケメンﾅｰｰｰｰｰｰｰｰｰｰｰｰｰｰに失敗しました: " + e.Message);
                    }
                }));
            KeyAssignCore.RegisterOperation("SenselessRetweet", () =>
                KeyAssignHelper.ExecuteTVMAction(tvm =>
                    {
                        if (tvm.Tweet.BackEnd.IsDirectMessage) return;
                        KernelService.MainWindowViewModel.InputBlockViewModel.SetOpenText(true, true);
                        KernelService.MainWindowViewModel.InputBlockViewModel.SetText(BuildSenseless(tvm.Tweet.BackEnd));
                    }));
            KeyAssignCore.RegisterOperation("SenselessRetweetFast", () =>
                KeyAssignHelper.ExecuteTVMAction(tvm =>
                {
                    try
                    {
                        if (tvm.Tweet.BackEnd.IsDirectMessage) return;
                        tvm.Parent.TabProperty.LinkAccountInfos.ForEach(
                            ai => PostOffice.UpdateTweet(ai, BuildSenseless(tvm.Tweet.BackEnd)));
                    }
                    catch (Exception e)
                    {
                        ExceptionStorage.Register(e, ExceptionCategory.PluginError, "非常識RTに失敗しました: " + e.Message);
                    }
                }));
        }

        private string BuildSenseless(TweetBackEnd tb)
        {
            var rtos = tb.RetweetedOriginalId != 0 ? TweetStorage.Get(tb.RetweetedOriginalId) : null;
            if (rtos != null)
                tb = rtos.BackEnd;
            var user = UserStorage.Lookup(tb.UserId);
            return "… RT @" + user.BackEnd.ScreenName + ": " + rtos.BackEnd.Text;
        }

        public IConfigurator ConfigurationInterface
        {
            get { return null; }
        }
    }
}
