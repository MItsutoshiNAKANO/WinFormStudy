﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SampleLibrary
{
    public class TcpServerUtility : IDisposable
    {
        private readonly Logger _logger;

        private TcpListener _listener = null;

        private readonly Dictionary<string, ClientManager> _dicClient = new Dictionary<string, ClientManager>();

        private readonly Encoding _enc;

        private Task _acceptTask = null;

        private CancellationTokenSource _acceptCancelTokenSource = null;

        /// <summary>
        /// キュー
        /// </summary>
        private BlockingCollection<string> _que = new BlockingCollection<string>();

        private class ClientManager
        {
            public TcpClient Client { get; } = null;
            public Task Task { get; } = null;
            public CancellationTokenSource CancelTokenSource { get; } = null;

            public ClientManager(Task task, TcpClient client, CancellationTokenSource cancelTokenSource)
            {
                Task = task;
                Client = client;
                CancelTokenSource = cancelTokenSource;
            }
        }

        public TcpServerUtility(string ipString = null, int port = 2001, string encoding = null)
        {
            _logger = Logger.GetInstance(GetType().Name);

            if (string.IsNullOrEmpty(encoding))
            {
                // Default
                _enc = Encoding.UTF8;
            }
            else
            {
                _enc = Encoding.GetEncoding(encoding);
            }

            Listen(ipString, port);
        }

        private void Listen(string ipString, int port)
        {
            _logger.WriteLine(MethodBase.GetCurrentMethod().Name);

            if (_acceptCancelTokenSource != null)
            {
                _acceptCancelTokenSource.Cancel();
                _acceptCancelTokenSource.Dispose();
                _acceptTask.Wait(10 * 1000);
                _acceptTask.Dispose();
            }

            if (_listener == null)
            {
                if (string.IsNullOrEmpty(ipString))
                {
                    //IPv4とIPv6の全てのIPアドレスをListenする
                    _listener = new TcpListener(System.Net.IPAddress.IPv6Any, port);
                }
                else
                {
                    IPAddress ipAdd;
                    if (!IPAddress.TryParse(ipString, out ipAdd))
                    {
                        // 変換失敗時
                        // ホスト名と判断し、ホスト名からIPアドレスを取得する

                        IPHostEntry ipentry = Dns.GetHostEntry(ipString);
                        foreach (IPAddress ip in ipentry.AddressList)
                        {
                            if (ip.AddressFamily == AddressFamily.InterNetwork)
                            {
                                // IPv4
                                ipString = ip.ToString();
                                break;
                            }
                        }
                        ipAdd = IPAddress.Parse(ipString);
                    }
                    _listener = new TcpListener(ipAdd, port);
                }
            }

            //Listenを開始する
            _logger.WriteLine($"Listen" +
                $"IP:{((IPEndPoint)_listener.LocalEndpoint).Address} Port:{((System.Net.IPEndPoint)_listener.LocalEndpoint).Port})。");
            _listener.Start();

            _acceptCancelTokenSource = new CancellationTokenSource();
            _acceptTask = Task.Run(() => Accept(_acceptCancelTokenSource.Token));
        }

        public async void Accept(CancellationToken token)
        {
            _logger.WriteLine(MethodBase.GetCurrentMethod().Name);

            while (!token.IsCancellationRequested)
            {
                TcpClient client = await _listener.AcceptTcpClientAsync();

                string ip = ((IPEndPoint)client.Client.LocalEndPoint).Address.ToString();
                string port = ((IPEndPoint)client.Client.LocalEndPoint).Port.ToString();

                _logger.WriteLine($"{MethodBase.GetCurrentMethod().Name} " +
                    $"Accept IP:{ip}" +
                    $"Port:{port})。");

                // Task停止用のトークン発行
                CancellationTokenSource cancelTokenSource = new CancellationTokenSource();

                Task task = Task.Run(() => ReadLoopAsync(client, cancelTokenSource.Token), cancelTokenSource.Token);

                _dicClient.Add($"{ip}:{port}", new ClientManager(task, client, cancelTokenSource));
            }
        }

        private async void ReadLoopAsync(TcpClient client, CancellationToken token)
        {
            _logger.WriteLine(MethodBase.GetCurrentMethod().Name);

            while (!token.IsCancellationRequested)
            {
                string resMsg = await Task.Run(() => ReadAsync(client), token);
                if (!_que.TryAdd(resMsg))
                {
                    _logger.WriteLine($"TryAdd Error[{resMsg}]");
                }
            }
        }

        public async Task<string> ReadAsync(TcpClient client)
        {
            _logger.WriteLine(MethodBase.GetCurrentMethod().Name);

            NetworkStream ns = client.GetStream();

            // 10秒でタイムアウト
            ns.ReadTimeout = 1000 * 10;

            string resMsg = string.Empty;
            using (MemoryStream ms = new MemoryStream())
            {
                byte[] resBytes = new byte[256];
                int resSize;
                do
                {
                    //データの一部を受信する
                    resSize = await ns.ReadAsync(resBytes, 0, resBytes.Length);

                    //Readが0を返した時はクライアントが切断したと判断
                    if (resSize == 0)
                    {
                        _logger.WriteLine("クライアントが切断しました。");
                        return null;
                    }

                    //受信したデータを蓄積する
                    ms.Write(resBytes, 0, resSize);

                    //まだ読み取れるデータがあるか、データの最後が\nでない時は、
                    //受信を続ける
                } while (ns.DataAvailable || resBytes[resSize - 1] != '\n');

                //受信したデータを文字列に変換
                resMsg = _enc.GetString(ms.GetBuffer(), 0, (int)ms.Length);

                // クローズ
                ms.Close();
            }
            //末尾の\nを削除
            resMsg = resMsg.TrimEnd('\n');

            _logger.WriteLine($"受信MSG[{resMsg}]");

            return resMsg;
        }

        public void SendAll(string sendMsg)
        {
            foreach (ClientManager mgr in _dicClient.Values)
            {
                TcpClient tcpClient = mgr.Client;
                Send(tcpClient, sendMsg);
            }
        }


        /// <summary>
        /// クライアントにデータを送信する
        /// </summary>
        /// <param name="client"></param>
        /// <param name="sendMsg"></param>
        public void Send(TcpClient client, string sendMsg)
        {
            _logger.WriteLine(MethodBase.GetCurrentMethod().Name);

            NetworkStream ns = client.GetStream();

            // 10秒でタイムアウト
            ns.WriteTimeout = 1000 * 10;

            //文字列をByte型配列に変換
            byte[] sendBytes = _enc.GetBytes(sendMsg + '\n');

            //データを送信する
            ns.Write(sendBytes, 0, sendBytes.Length);

            _logger.WriteLine($"送信MSG[{sendMsg}]");
        }

        public string Read()
        {
            if (_que.Count > 0)
            {
                _logger.WriteLine(MethodBase.GetCurrentMethod().Name);

                if (_que.TryTake(out string retMsg, 1 * 1000))
                {
                    return retMsg;
                }
                else
                {
                    _logger.WriteLine("TryTake Error");
                    return null;
                }
            }
            return null;
        }

        #region IDisposable Support
        private bool disposedValue = false; // 重複する呼び出しを検出するには

        protected virtual void Dispose(bool disposing)
        {
            _logger.WriteLine(MethodBase.GetCurrentMethod().Name);

            if (!disposedValue)
            {
                if (disposing)
                {
                    // マネージ状態を破棄します (マネージ オブジェクト)。

                    // Listenの停止
                    _listener.Stop();

                    // ClientのClose
                    foreach (ClientManager mgr in _dicClient.Values)
                    {
                        TcpClient client = mgr.Client;
                        client.GetStream().Close();
                        client.Close();
                        client.Dispose();
                    }

                    _logger.Dispose();
                }

                // アンマネージ リソース (アンマネージ オブジェクト) を解放し、下のファイナライザーをオーバーライドします。
                // 大きなフィールドを null に設定します。

                disposedValue = true;
            }
        }

        // 上の Dispose(bool disposing) にアンマネージ リソースを解放するコードが含まれる場合にのみ、ファイナライザーをオーバーライドします。
        // ~TcpServer()
        // {
        //   // このコードを変更しないでください。クリーンアップ コードを上の Dispose(bool disposing) に記述します。
        //   Dispose(false);
        // }

        public void Dispose()
        {
            _logger.WriteLine(MethodBase.GetCurrentMethod().Name);

            // このコードを変更しないでください。クリーンアップ コードを上の Dispose(bool disposing) に記述します。
            Dispose(true);

            // 上のファイナライザーがオーバーライドされる場合は、次の行のコメントを解除してください。
            // GC.SuppressFinalize(this);
        }
        #endregion
    }
}
