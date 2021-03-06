﻿using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Inscribe.Configuration;
using Inscribe.ViewModels.Common;
using System.Windows.Data;
using Inscribe.Algorithm.DPMatching;
using Inscribe;
using Livet;

namespace Mystique.Views.Common
{
    /// <summary>
    /// IntelliSenseTextBox.xaml の相互作用ロジック
    /// </summary>
    public partial class IntelliSenseTextBox : UserControl
    {
        #region Dependency property

        public bool IsOpening
        {
            get { return (bool)GetValue(IsOpeningProperty); }
            set { SetValue(IsOpeningProperty, value); }
        }

        // Using a DependencyProperty as the backing store for IsOpening.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty IsOpeningProperty =
            DependencyProperty.Register("IsOpening", typeof(bool), typeof(IntelliSenseTextBox), new UIPropertyMetadata(false, IsOpeningChanged));

        private static void IsOpeningChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
        {
            var item = o as IntelliSenseTextBox;
            if (item == null) return;
            if (item.intelliSensePopup.IsOpen != (bool)e.NewValue)
                item.intelliSensePopup.IsOpen = (bool)e.NewValue;
            System.Diagnostics.Debug.WriteLine("IsOpening" + e.NewValue);
        }

        #endregion

        #region Constructors and public properties

        public IntelliSenseTextBox()
        {
            InitializeComponent();
            inputTextBox.TextChanged += new TextChangedEventHandler(InputTextBox_TextChanged);
            intelliSensePopup.Closed += new EventHandler(intelliSensePopup_Closed);
            intelliSenseList.SelectionChanged += new SelectionChangedEventHandler(intelliSenseList_SelectionChanged);
            Binding bind = new Binding("IsItemOpening");
            bind.Mode = BindingMode.TwoWay;
            this.SetBinding(IsOpeningProperty, bind);
        }

        private IntelliSenseTextBoxViewModel ViewModel
        {
            get { return this.DataContext as IntelliSenseTextBoxViewModel; }
        }

        #endregion

        #region Event callbacks

        string prevText = String.Empty;
        void InputTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (IsOpening)
            {
                UpdateCurrentToken();
            }
            else
            {
                if (inputTextBox.CaretIndex > 0)
                {
                    // 直前に入力された内容を取得する
                    var prevChar = inputTextBox.Text[inputTextBox.CaretIndex - 1];
                    var ppChar = prevText.Length > inputTextBox.CaretIndex - 1 ?
                        prevText[inputTextBox.CaretIndex - 1] : '\0';
                    if (ViewModel.SuggestTriggers.Contains(prevChar) && prevChar != ppChar)
                    {
                        OpenIntelliSense(inputTextBox.CaretIndex);
                    }
                }
            }
            prevText = inputTextBox.Text;
        }


        void intelliSenseList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (intelliSenseList.SelectedItem != null)
                intelliSenseList.ScrollIntoView(intelliSenseList.SelectedItem);
        }

        void intelliSensePopup_Closed(object sender, EventArgs e)
        {
            this.IsOpening = false;
        }

        private void inputTextBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            // IntelliSense キーをハンドリングする
            if (this.IsOpening)
            {
                if (e.Key == Key.Down || e.Key == Key.Up)
                {
                    if (e.Key == Key.Down)
                    {
                        intelliSenseList.SelectedIndex++;
                    }
                    else
                    {
                        intelliSenseList.SelectedIndex =
                            intelliSenseList.SelectedIndex > 0 ? intelliSenseList.SelectedIndex - 1 :
                                0;
                    }
                    e.Handled = true;
                }
                else if (e.Key == Key.Enter || e.Key == Key.Tab)
                {
                    ApplyIntelliSense();
                    e.Handled = true;
                }
                else if (e.Key == Key.Space)
                {
                    // スペースキーの打鍵ならサジェストを適用
                    ApplyIntelliSense();
                    // Handled しない
                }
                else if (e.Key == Key.Escape)
                {
                    // エスケープキーの打鍵ならサジェストを撤去
                    this.IsOpening = false;
                    e.Handled = true;
                }
            }
            else
            {
                // Ctrl+Space で補完を呼び出す
                if (e.Key == Key.Space && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
                {
                    this.IsOpening = true;
                    e.Handled = true;
                }
            }
        }

        private void inputTextBox_SelectionChanged(object sender, RoutedEventArgs e)
        {
            // トークンの更新
            if (IsOpening)
            {
                UpdateCurrentToken();
            }
        }

        private void intelliSenseList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            // サジェスト内容の適用
            ApplyIntelliSense();
        }

        #endregion

        private void OpenIntelliSense(int selectionStart)
        {
            if (this.IsOpening || !Setting.Instance.InputExperienceProperty.UseInputSuggesting) return;
            if (selectionStart > 0)
            {
                // 現在選択されている文字の1つ前からスタート
                selectionStart--;
                // 開始トリガーのキャラクタを探す
                var ctext = inputTextBox.Text;
                while (selectionStart > 0)
                {
                    if (ViewModel.SuggestTriggers.Contains(ctext[selectionStart]) ||
                        ViewModel.Splitters.Contains(ctext[selectionStart]))
                        break;
                }
            }

            // 位置のセット
            intelliSensePopup.PlacementTarget = inputTextBox;
            intelliSensePopup.PlacementRectangle = inputTextBox
                .GetRectFromCharacterIndex(inputTextBox.CaretIndex - 1);

            ViewModel.RaiseOnItemsOpening();
            InitCurrentToken();
            this.IsOpening = true;
        }

        int tokenStartPoint;

        private void InitCurrentToken()
        {
            var area = this.GetTokenArea(this.inputTextBox.Text, this.inputTextBox.CaretIndex);
            tokenStartPoint = area.Item1 + 1;
            UpdateCurrentToken();
        }

        private void UpdateCurrentToken()
        {
            if (this.inputTextBox.CaretIndex < this.tokenStartPoint)
            {
                IsOpening = false;
            }
            else
            {
                var text = this.inputTextBox.Text;
                var ctoken = this.GetToken(text, this.inputTextBox.CaretIndex);
                var otoken = this.GetToken(text, this.tokenStartPoint);
                if (ctoken != otoken)
                {
                    // 有効範囲が変わりました
                    IsOpening = false;
                }
                else
                {
                    ViewModel.CurrentToken = ctoken;
                    // 現在のトークン更新で表示要素が無くなったら閉じる
                    // or トークン長がIntelliSenseサジェストアイテムの最大長を上回ったら閉じる
                    if (ViewModel.FilteredItems == null ||
                        ViewModel.FilteredItems.Count() == 0 ||
                        ctoken.Length > ViewModel.FilteredItems.Select(t => t.ItemText.Length).Max())
                    {
                        IsOpening = false;
                    }
                    else
                    {
                        // 最も近いものを選択する
                        string cstr;
                        var nearidx = GetNearestIndex(ctoken, out cstr);
                        if (nearidx == -1)
                        {
                            this.intelliSenseList.SelectedIndex = 0;
                            DispatcherHelper.BeginInvoke(() => this.intelliSenseList.SelectedIndex = -1);
                        }
                        else
                        {
                            this.intelliSenseList.SelectedIndex = nearidx;
                        }
                    }
                }
            }
        }

        private string GetToken(string source, int index)
        {
            return GetTokenFromArea(GetTokenArea(source, index), source);
        }

        private string GetTokenFromArea(Tuple<int, int> areaTuple, string source)
        {
            if (areaTuple.Item1 + areaTuple.Item2 >= source.Length)
            {

                return source.Substring(areaTuple.Item1);
            }
            else if (areaTuple.Item2 == 0)
            {
                return String.Empty;
            }
            else
            {
                return source.Substring(areaTuple.Item1, areaTuple.Item2);
            }
        }

        /// <summary>
        /// 現在キャレットがある位置の直前から継続するトークンを取得します。
        /// </summary>
        /// <param name="source">テキストソース</param>
        /// <param name="index">キャレットインデックス</param>
        /// <returns></returns>
        private Tuple<int, int> GetTokenArea(string source, int index)
        {
            if (index < 0 || source.Length < index)
                throw new ArgumentOutOfRangeException("index");
            int tokenLen = 0;
            // スタート位置を探す
            while (index > 0)
            {
                index--;
                if (ViewModel.SuggestTriggers.Contains(source[index]))
                {
                    // input trigger文字は含む
                    tokenLen = 1; // 1文字以上のトークン
                    break;
                }
                if (ViewModel.Splitters.Contains(source[index]))
                {
                    // splitter文字の直前の文字から探す
                    tokenLen = 0; // 0文字の場合がある
                    index++;
                    break;
                }
            }
            while (index + tokenLen < source.Length)
            {
                if (ViewModel.Splitters.Contains(source[index + tokenLen]) || ViewModel.SuggestTriggers.Contains(source[index + tokenLen]))
                {
                    break;
                }
                tokenLen++;
            }
            return new Tuple<int, int>(index, tokenLen);
        }

        /// <summary>
        /// 現在の選択内容を使って入力補完を完了します。
        /// </summary>
        private void ApplyIntelliSense()
        {
            var item = intelliSenseList.SelectedItem as IntelliSenseItemViewModel;
            if (item != null)
            {
                var text = this.inputTextBox.Text;
                var tokArea = this.GetTokenArea(text, this.inputTextBox.CaretIndex);
                var ctoken = this.GetTokenFromArea(tokArea, text);

                    inputTextBox.SelectionStart = tokArea.Item1;
                    inputTextBox.SelectionLength = tokArea.Item2;
                    inputTextBox.SelectedText = item.ItemText;
                    inputTextBox.CaretIndex = tokArea.Item1 + item.ItemText.Length;
                /*
                // 最も近いものを選択する
                if (item.ItemText.IndexOf(ctoken, StringComparison.CurrentCultureIgnoreCase) >= 0)
                {
                    // 上書きするトークンは現在のトークンに含まれる
                    inputTextBox.SelectionStart = tokArea.Item1;
                    inputTextBox.SelectionLength = tokArea.Item2;
                    inputTextBox.SelectedText = item.ItemText;
                    inputTextBox.CaretIndex = tokArea.Item1 + item.ItemText.Length;
                }
                else
                {
                    // 上書きするトークンは現在のトークンに含まれない
                    // => 何もしない
                }
                */
            }
            IsOpening = false;
        }

        private int GetNearestIndex(string token, out string nearest)
        {
            nearest = ViewModel.FilteredItems.First().ItemText;
            if (DPMatcher.DPMatchingCore(token, nearest) < Define.DPMatchingThreshold)
                return 0;
            else
                return -1;

            /*
            var items = ViewModel.FilteredItems.Select(i => i.ItemText).ToArray();
            var nitems = from item in items
                         let idx = IntelliSenseTextBoxUtil.CheckIndexOf(item, token, ViewModel.SuggestTriggers)
                         where idx >= 0
                         orderby idx
                         select item;
            var retItem = nitems.FirstOrDefault();
            if (retItem != null)
            {
                nearest = retItem;
                var index = Array.BinarySearch<string>(items, retItem);
                if (index >= 0) return index;
            }

            nearest = null;
            return -1;
            */
        }

        private void inputTextBox_DragOver(object sender, DragEventArgs e)
        {
        }

        private void inputTextBox_Drop(object sender, DragEventArgs e)
        {
            string[] files = e.Data.GetData(DataFormats.FileDrop) as string[];
            if (files != null)
            {
                ViewModel.RaiseOnDrop(files[0]);
            }
        }

        private void inputTextBox_PreviewDragOver(object sender, DragEventArgs e)
        {
            e.Handled = e.Data.GetDataPresent(DataFormats.FileDrop);
        }

    }

}
