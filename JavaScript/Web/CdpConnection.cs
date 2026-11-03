using Newtonsoft.Json.Linq;
using System;
using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MiMFa.Engine.JavaScript.Web
{
    internal sealed class CdpConnection : IDisposable
    {
        private readonly ClientWebSocket _socket = new ClientWebSocket();
        private readonly ConcurrentDictionary<int, TaskCompletionSource<JObject>> _pending =
            new ConcurrentDictionary<int, TaskCompletionSource<JObject>>();
        private readonly CancellationTokenSource _shutdown = new CancellationTokenSource();
        private int _nextId;
        private Task _receiver;

        public event Action<string, JObject> EventReceived;

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
                await _socket.SendAsync(new ArraySegment<byte>(payload), WebSocketMessageType.Text, true, _shutdown.Token)
                    .ConfigureAwait(false);
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
                    var builder = new StringBuilder();
                    WebSocketReceiveResult part;
                    do
                    {
                        part = await _socket.ReceiveAsync(new ArraySegment<byte>(buffer), _shutdown.Token)
                            .ConfigureAwait(false);
                        if (part.MessageType == WebSocketMessageType.Close) return;
                        builder.Append(Encoding.UTF8.GetString(buffer, 0, part.Count));
                    } while (!part.EndOfMessage);

                    JObject message = JObject.Parse(builder.ToString());
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

        public void Dispose()
        {
            _shutdown.Cancel();
            var closed = new ObjectDisposedException(nameof(CdpConnection));
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
            _shutdown.Dispose();
        }
    }
}
