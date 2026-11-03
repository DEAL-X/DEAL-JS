using Newtonsoft.Json.Linq;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace MiMFa.Engine.JavaScript.Web
{
    /// <summary>
    /// A proxy to a page in an already-running Chromium browser. DOM values
    /// remain in the browser process; evaluate methods return serialized data.
    /// </summary>
    public sealed class BrowserPage : IDisposable
    {
        private readonly CdpConnection _connection;
        private readonly TaskCompletionSource<bool> _loadEvent =
            new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        private bool _disposed;

        internal BrowserPage(string targetId, CdpConnection connection)
        {
            TargetId = targetId;
            _connection = connection;
            _connection.EventReceived += OnEvent;
        }

        public string TargetId { get; }
        public string Url { get; private set; }
        public string Title { get; private set; }

        private void OnEvent(string name, JObject data)
        {
            if (name == "Page.loadEventFired") _loadEvent.TrySetResult(true);
        }

        internal async Task InitializeAsync()
        {
            await _connection.SendAsync("Page.enable").ConfigureAwait(false);
            await _connection.SendAsync("Runtime.enable").ConfigureAwait(false);
            await RefreshAsync().ConfigureAwait(false);
        }

        public async Task<string> NavigateAsync(string url, int timeoutMilliseconds = 60000)
        {
            if (string.IsNullOrWhiteSpace(url))
                throw new ArgumentException("A navigation URL is required.", nameof(url));

            var loaded = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            Action<string, JObject> handler = null;
            handler = (name, data) =>
            {
                if (name == "Page.loadEventFired" || name == "Page.navigatedWithinDocument")
                    loaded.TrySetResult(true);
            };
            _connection.EventReceived += handler;

            try
            {
                var navigation = await _connection.SendAsync(
                    "Page.navigate", new JObject { ["url"] = url }).ConfigureAwait(false);
                if (navigation["errorText"] != null)
                    throw new InvalidOperationException(navigation["errorText"].Value<string>());

                if (timeoutMilliseconds <= 0) timeoutMilliseconds = 60000;
                using (var timeout = new CancellationTokenSource(timeoutMilliseconds))
                using (timeout.Token.Register(() => loaded.TrySetException(
                    new TimeoutException("The page did not finish loading before the navigation timeout."))))
                    await loaded.Task.ConfigureAwait(false);

                await RefreshAsync().ConfigureAwait(false);
                return Url;
            }
            finally { _connection.EventReceived -= handler; }
        }

        public async Task<string> EvaluateAsync(string expression)
        {
            var result = await _connection.SendAsync("Runtime.evaluate", new JObject
            {
                ["expression"] = expression ?? "undefined",
                ["awaitPromise"] = true,
                ["returnByValue"] = true,
                ["userGesture"] = true
            }).ConfigureAwait(false);

            if (result["exceptionDetails"] != null)
                throw new InvalidOperationException(result["exceptionDetails"].ToString());

            JObject remote = result["result"] as JObject;
            if (remote == null) return null;
            if (remote["subtype"]?.Value<string>() == "error")
                throw new InvalidOperationException(remote["description"]?.Value<string>() ?? "JavaScript evaluation failed.");

            JToken value = remote["value"];
            if (value == null) return null;
            return value.Type == JTokenType.String ? value.Value<string>() : value.ToString();
        }

        public async Task RefreshAsync()
        {
            string state = await EvaluateAsync(
                "JSON.stringify({url:location.href,title:document.title})").ConfigureAwait(false);
            if (string.IsNullOrEmpty(state)) return;
            JObject value = JObject.Parse(state);
            Url = (string)value["url"];
            Title = (string)value["title"];
        }

        public async Task<string> GetDocumentHtmlAsync() =>
            await EvaluateAsync("document.documentElement.outerHTML").ConfigureAwait(false);

        public async Task<string> GetDocumentTextAsync() =>
            await EvaluateAsync("document.documentElement.innerText").ConfigureAwait(false);

        public void Close()
        {
            if (_disposed) return;
            _connection.SendAsync("Page.close").GetAwaiter().GetResult();
            Dispose();
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            _connection.EventReceived -= OnEvent;
            _connection.Dispose();
        }
    }
}
