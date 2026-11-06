using Newtonsoft.Json.Linq;
using System;
using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MiMFa.Net.Browsers.Protocols.DevTools
{
    internal sealed class DevToolsConnection : IDisposable
    {
        private readonly ClientWebSocket _socket = new ClientWebSocket();
        private readonly ConcurrentDictionary<int, TaskCompletionSource<JObject>> _pending =
            new ConcurrentDictionary<int, TaskCompletionSource<JObject>>();
        private readonly CancellationTokenSource _shutdown = new CancellationTokenSource();
        private readonly SemaphoreSlim _sendLock = new SemaphoreSlim(1, 1);
        private int _nextId;
        private Task _receiver;

        public event Action<string, JObject> EventReceived;

        public void Dispose()
        {
            _shutdown.Cancel();
            var closed = new ObjectDisposedException(nameof(DevToolsConnection));
            foreach (var item in _pending.Values)
                item.TrySetException(closed);
            try
            {
                if (_socket.State == WebSocketState.Open)
                    _socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "DEAL closing", CancellationToken.None)
                        .GetAwaiter().GetResult();
            }
            catch { }
            _socket.Dispose();
            _sendLock.Dispose();
            _shutdown.Dispose();
        }

        public async Task ConnectAsync(Uri websocketUri, CancellationToken cancellationToken)
        {
            await _socket.ConnectAsync(websocketUri, cancellationToken).ConfigureAwait(false);
            _receiver = ReceiveLoopAsync();
        }

        public async Task<JObject> SendAsync(string method, JObject parameters = null, int timeoutMilliseconds = 30000)
        {
            if (_socket.State != WebSocketState.Open)
                throw new InvalidOperationException("The browser debugging WebSocket is not connected.");

            int id = Interlocked.Increment(ref _nextId);
            var completion = new TaskCompletionSource<JObject>(TaskCreationOptions.RunContinuationsAsynchronously);
            if (!_pending.TryAdd(id, completion))
                throw new InvalidOperationException("Could not register the DevTools command.");

            var command = new JObject
            {
                ["id"] = id,
                ["method"] = method,
                ["params"] = parameters ?? new JObject()
            };
            byte[] payload = Encoding.UTF8.GetBytes(command.ToString(Newtonsoft.Json.Formatting.None));
            try
            {
                await _sendLock.WaitAsync(_shutdown.Token).ConfigureAwait(false);
                try
                {
                    await _socket.SendAsync(new ArraySegment<byte>(payload), WebSocketMessageType.Text, true, _shutdown.Token)
                        .ConfigureAwait(false);
                }
                finally { _sendLock.Release(); }
                using (var timeout = CancellationTokenSource.CreateLinkedTokenSource(_shutdown.Token))
                {
                    timeout.CancelAfter(timeoutMilliseconds);
                    using (timeout.Token.Register(() => completion.TrySetException(
                        new TimeoutException("The DevTools command timed out: " + method))))
                    {
                        JObject result = await completion.Task.ConfigureAwait(false);
                        if (result["error"] != null)
                            throw new InvalidOperationException(result["error"].ToString());
                        return result["result"] as JObject ?? new JObject();
                    }
                }
            }
            finally { _pending.TryRemove(id, out _); }
        }

        private async Task ReceiveLoopAsync()
        {
            var buffer = new byte[16 * 1024];
            try
            {
                while (!_shutdown.IsCancellationRequested && _socket.State == WebSocketState.Open)
                {
                    JObject message;
                    using (var stream = new MemoryStream())
                    {
                        WebSocketReceiveResult part;
                        do
                        {
                            part = await _socket.ReceiveAsync(new ArraySegment<byte>(buffer), _shutdown.Token)
                                .ConfigureAwait(false);
                            if (part.MessageType == WebSocketMessageType.Close) return;
                            if (part.MessageType != WebSocketMessageType.Text)
                                throw new WebSocketException("Unexpected binary message from the DevTools protocol.");
                            stream.Write(buffer, 0, part.Count);
                        } while (!part.EndOfMessage);

                        // Decode only after the complete WebSocket message has
                        // been assembled; UTF-8 characters may span fragments.
                        message = JObject.Parse(Encoding.UTF8.GetString(stream.ToArray()));
                    }
                    if (message["id"] != null)
                    {
                        int id = (int)message["id"];
                        if (_pending.TryGetValue(id, out var completion))
                            completion.TrySetResult(message);
                    }
                    else
                    {
                        string name = (string)message["method"];
                        if (name != null)
                        {
                            var handlers = EventReceived;
                            if (handlers != null)
                            {
                                foreach (Action<string, JObject> handler in handlers.GetInvocationList())
                                {
                                    try { handler(name, message["params"] as JObject ?? new JObject()); }
                                    catch { /* A consumer callback must not terminate the CDP receive loop. */ }
                                }
                            }
                        }
                    }
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                foreach (var item in _pending.Values)
                    item.TrySetException(ex);
            }
            finally
            {
                var closed = new WebSocketException("The DevTools WebSocket connection was closed.");
                foreach (var item in _pending.Values)
                    item.TrySetException(closed);
            }
        }


    }
}
