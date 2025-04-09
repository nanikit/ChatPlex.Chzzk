using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Net.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Linq;

namespace ChatPlex.Chzzk.SocketIO
{
    public class ChzzkSocketClient : IDisposable
    {
        private readonly HttpClient httpClient;
        private readonly string baseUrl;
        private readonly CancellationTokenSource cts;
        private string sessionId;
        private bool isConnected;
        private long lastSequenceNumber;

        public event EventHandler<string> OnMessage;
        public event EventHandler OnConnected;
        public event EventHandler OnDisconnected;
        public event EventHandler<Exception> OnError;

        private const int POLLING_INTERVAL = 25000; // 25 seconds

        public ChzzkSocketClient(string url)
        {
            baseUrl = url.TrimEnd('/');
            cts = new CancellationTokenSource();
            httpClient = new HttpClient();
            httpClient.Timeout = TimeSpan.FromSeconds(60);
        }

        public async Task ConnectAsync()
        {
            try
            {
                if (isConnected) return;

                // Initial handshake
                var handshakeResponse = await httpClient.GetAsync($"{baseUrl}/socket.io/?EIO=3&transport=polling&t={DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}");
                handshakeResponse.EnsureSuccessStatusCode();
                
                var handshakeContent = await handshakeResponse.Content.ReadAsStringAsync();
                // Remove the Socket.IO protocol prefix (usually "97:0")
                handshakeContent = handshakeContent.Substring(handshakeContent.IndexOf('{'));
                var handshakeData = JsonConvert.DeserializeObject<JObject>(handshakeContent);
                
                sessionId = handshakeData["sid"].ToString();
                isConnected = true;
                
                OnConnected?.Invoke(this, EventArgs.Empty);
                
                // Start polling
                _ = StartPolling();
            }
            catch (Exception ex)
            {
                OnError?.Invoke(this, ex);
                throw;
            }
        }

        private async Task StartPolling()
        {
            while (isConnected && !cts.Token.IsCancellationRequested)
            {
                try
                {
                    var pollUrl = $"{baseUrl}/socket.io/?EIO=3&transport=polling&t={DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}&sid={sessionId}";
                    var response = await httpClient.GetAsync(pollUrl, cts.Token);
                    
                    if (!response.IsSuccessStatusCode)
                    {
                        throw new Exception($"Polling failed with status code: {response.StatusCode}");
                    }

                    var content = await response.Content.ReadAsStringAsync();
                    if (!string.IsNullOrEmpty(content))
                    {
                        ProcessPollingResponse(content);
                    }

                    // Wait before next poll
                    await Task.Delay(POLLING_INTERVAL, cts.Token);
                }
                catch (TaskCanceledException)
                {
                    // Normal cancellation, don't report as error
                    break;
                }
                catch (Exception ex)
                {
                    if (!cts.Token.IsCancellationRequested)
                    {
                        OnError?.Invoke(this, ex);
                        await HandleDisconnection();
                        break;
                    }
                }
            }
        }

        private void ProcessPollingResponse(string response)
        {
            if (string.IsNullOrEmpty(response)) return;

            // Remove Socket.IO protocol prefix if present
            if (char.IsDigit(response[0]))
            {
                var colonIndex = response.IndexOf(':');
                if (colonIndex != -1)
                {
                    response = response.Substring(colonIndex + 1);
                }
            }

            if (response.StartsWith("2")) // Socket.IO event message
            {
                var messageData = response.Substring(1);
                OnMessage?.Invoke(this, messageData);
            }
        }

        public async Task EmitAsync(string eventName, params object[] args)
        {
            if (!isConnected) throw new InvalidOperationException("Client is not connected");

            var payload = new
            {
                type = 2,
                nsp = "/",
                data = new object[] { eventName }.Concat(args).ToArray()
            };

            var message = "42" + JsonConvert.SerializeObject(payload.data);
            var content = new StringContent(message, Encoding.UTF8, "text/plain");
            
            var postUrl = $"{baseUrl}/socket.io/?EIO=3&transport=polling&t={DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}&sid={sessionId}";
            var response = await httpClient.PostAsync(postUrl, content, cts.Token);
            response.EnsureSuccessStatusCode();
        }

        private async Task HandleDisconnection()
        {
            if (!isConnected) return;

            isConnected = false;
            OnDisconnected?.Invoke(this, EventArgs.Empty);
        }

        public async Task DisconnectAsync()
        {
            if (!isConnected) return;
            await HandleDisconnection();
            cts.Cancel();
        }

        public void Dispose()
        {
            cts.Cancel();
            httpClient.Dispose();
            cts.Dispose();
        }

        public bool IsConnected => isConnected;
    }
}
