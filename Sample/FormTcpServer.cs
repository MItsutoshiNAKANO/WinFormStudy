﻿using SampleLibrary;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Sample
{
    public partial class FormTcpServer : Sample.Base.BaseForm
    {
        private TcpServerUtility _tcpServer = null;

        private Task _task = null;

        private Logger _logger = null;

        private CancellationTokenSource _cancelTokenSource = null;

        public FormTcpServer()
        {
            _logger = Logger.GetInstance(GetType().Name);
            _logger.WriteLine(MethodBase.GetCurrentMethod().Name);

            InitializeComponent();

            // 使わないボタンを非表示にする
            //buttonF1.Visible = false;
            //buttonF2.Visible = false;
            buttonF3.Visible = false;
            buttonF4.Visible = false;
            //buttonF5.Visible = false;
            buttonF6.Visible = false;
            buttonF7.Visible = false;
            buttonF8.Visible = false;
            buttonF9.Visible = false;
            buttonF10.Visible = false;
            //buttonF11.Visible = false;

            // ボタンの有効無効を設定
            SetButtonEnabled(ActionMode.Init);
        }

        /// <summary>
        /// 動作モード
        /// </summary>
        private enum ActionMode
        {
            Init = 0,
            Listen,
        }

        /// <summary>
        /// ボタン状態の設定
        /// </summary>
        /// <param name="mode"></param>
        private void SetButtonEnabled(ActionMode mode)
        {
            _logger.WriteLine($"{MethodBase.GetCurrentMethod().Name} ActionMode:{mode}");

            // まず全てのボタンを無効にする
            SetAllBaseButtonEnabled(false);

            // 必要なボタンのみ有効にする
            buttonF11.Enabled = true;
            buttonF12.Enabled = true;
            switch (mode)
            {
                case ActionMode.Listen:
                    buttonF1.Enabled = false;
                    buttonF2.Enabled = true;
                    buttonF5.Enabled = true;
                    buttonF6.Enabled = true;
                    break;
                case ActionMode.Init:
                default:
                    buttonF1.Enabled = true;
                    buttonF2.Enabled = false;
                    buttonF5.Enabled = false;
                    buttonF6.Enabled = false;
                    break;
            }
        }

        /// <summary>
        /// 開始
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected override void ButtonF1_Click(object sender, EventArgs e)
        {
            _logger.WriteLine(MethodBase.GetCurrentMethod().Name);

            base.ButtonF1_Click(sender, e);
            // ▼▼▼ 業務処理 ▼▼▼

            if (_tcpServer != null)
            {
                // 既に接続している場合、再接続前に破棄する
                _tcpServer.Dispose();
            }

            // 接続
            string ip = tboxIP.Text;
            int.TryParse(tboxPort.Text, out int port);
            _tcpServer = new TcpServerUtility(ip, port);

            // 読み込みループ開始
            // Task停止用のトークン発行
            _cancelTokenSource = new CancellationTokenSource();
            CancellationToken cToken = _cancelTokenSource.Token;
            _task = Task.Run(() => ReadLoop(cToken), cToken);

            // ボタンの有効無効を設定
            SetButtonEnabled(ActionMode.Listen);
            // ▲▲▲ 業務処理 ▲▲▲
        }
        private async void ReadLoop(CancellationToken cToken)
        {
            _logger.WriteLine(MethodBase.GetCurrentMethod().Name);

            while (!cToken.IsCancellationRequested)
            {
                string s = _tcpServer.Read();

                if (!string.IsNullOrEmpty(s))
                {
                    _logger.WriteLine(s);

                    Invoke((Action)(() =>
                    {
                        listViewLog.Items.Add(s);
                    }));
                }
                else
                {
                    await Task.Delay(1 * 1000);
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected override void ButtonF2_Click(object sender, EventArgs e)
        {
            base.ButtonF2_Click(sender, e);
            // ▼▼▼ 業務処理 ▼▼▼

            // ▲▲▲ 業務処理 ▲▲▲
        }

        #region F3～F4 未使用
        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected override void ButtonF3_Click(object sender, EventArgs e)
        {
            base.ButtonF3_Click(sender, e);
            // ▼▼▼ 業務処理 ▼▼▼

            // ▲▲▲ 業務処理 ▲▲▲
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected override void ButtonF4_Click(object sender, EventArgs e)
        {
            base.ButtonF4_Click(sender, e);
            // ▼▼▼ 業務処理 ▼▼▼

            // ▲▲▲ 業務処理 ▲▲▲
        }
        #endregion

        /// <summary>
        /// Send
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected override void ButtonF5_Click(object sender, EventArgs e)
        {
            base.ButtonF5_Click(sender, e);
            // ▼▼▼ 業務処理 ▼▼▼

            string sendMsg = tboxMessage.Text;

            _tcpServer.SendAll(sendMsg);
            // ▲▲▲ 業務処理 ▲▲▲
        }

        #region F6～F10 未使用
        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected override void ButtonF6_Click(object sender, EventArgs e)
        {
            base.ButtonF6_Click(sender, e);
            // ▼▼▼ 業務処理 ▼▼▼

            // ▲▲▲ 業務処理 ▲▲▲
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected override void ButtonF7_Click(object sender, EventArgs e)
        {
            base.ButtonF7_Click(sender, e);
            // ▼▼▼ 業務処理 ▼▼▼

            // ▲▲▲ 業務処理 ▲▲▲
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected override void ButtonF8_Click(object sender, EventArgs e)
        {
            base.ButtonF8_Click(sender, e);
            // ▼▼▼ 業務処理 ▼▼▼

            // ▲▲▲ 業務処理 ▲▲▲
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected override void ButtonF9_Click(object sender, EventArgs e)
        {
            base.ButtonF9_Click(sender, e);
            // ▼▼▼ 業務処理 ▼▼▼

            // ▲▲▲ 業務処理 ▲▲▲
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected override void ButtonF10_Click(object sender, EventArgs e)
        {
            base.ButtonF10_Click(sender, e);
            // ▼▼▼ 業務処理 ▼▼▼

            // ▲▲▲ 業務処理 ▲▲▲
        }
        #endregion

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected override void ButtonF11_Click(object sender, EventArgs e)
        {
            base.ButtonF11_Click(sender, e);
            // ▼▼▼ 業務処理 ▼▼▼

            // ▲▲▲ 業務処理 ▲▲▲
        }
    }
}
