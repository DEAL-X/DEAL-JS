using Newtonsoft.Json.Linq;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace MiMFa.Engine.JavaScript.Web
{
    /// <summary>
    /// A host-side handle to a remote Chromium page. The actual Window and Document
    /// remain in Chromium and are accessed through the DevTools Protocol.
    /// </summary>
    public sealed class BrowserPage : IDisposable
    {
        private readonly CdpConnection _connection;
        private bool _disposed;

        internal BrowserPage(string targetId, CdpConnection connection)
        {
            TargetId = targetId ?? throw new ArgumentNullException(nameof(targetId));
            _connection = connection ?? throw new ArgumentNullException(nameof(connection));
            _connection.EventReceived += OnEvent;
        }

        public string TargetId { get; }
        public string Url { get; private set; }
        public string Title { get; private set; }

        private void OnEvent(string name, JObject data)
        {
            // Never issue a CDP command synchronously from the receive loop:
            // its response must be processed by that same loop.
            if (name == "Page.frameNavigated")
            {
                var frame = data?["frame"] as JObject;
                if (frame?["parentId"] == null)
                    Url = (string)frame["url"];
            }
            else if (name == "Page.loadEventFired")
            {
                Task.Run(async () =>
                {
                    try { await RefreshAsync().ConfigureAwait(false); }
                    catch { /* A navigation can close the target while refreshing. */ }
                });
            }
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
            Action<string, JObject> handler = (name, data) =>
            {
                if (name == "Page.loadEventFired" || name == "Page.navigatedWithinDocument")
                    loaded.TrySetResult(true);
            };
            _connection.EventReceived += handler;
            try
            {
                var navigation = await _connection.SendAsync("Page.navigate", new JObject { ["url"] = url }).ConfigureAwait(false);
                if (navigation["errorText"] != null)
                    throw new InvalidOperationException(navigation["errorText"].Value<string>());
                if (timeoutMilliseconds <= 0) timeoutMilliseconds = 60000;
                using (var timeout = new CancellationTokenSource(timeoutMilliseconds))
                using (timeout.Token.Register(() => loaded.TrySetException(new TimeoutException("The page did not finish loading before the navigation timeout."))))
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

        internal async Task<string> EvaluateObjectAsync(string expression)
        {
            var response = await _connection.SendAsync("Runtime.evaluate", new JObject
            {
                ["expression"] = expression ?? "undefined",
                ["awaitPromise"] = true,
                ["returnByValue"] = false,
                ["userGesture"] = true
            }).ConfigureAwait(false);
            if (response["exceptionDetails"] != null)
                throw new InvalidOperationException(response["exceptionDetails"].ToString());
            var remote = response["result"] as JObject;
            if (remote == null || remote["objectId"] == null)
                throw new InvalidOperationException(remote?["description"]?.Value<string>() ?? "The expression did not return a remote object.");
            return remote["objectId"].Value<string>();
        }

        internal async Task<string> CallFunctionObjectAsync(string objectId, string functionDeclaration, JArray arguments = null)
        {
            var response = await _connection.SendAsync("Runtime.callFunctionOn", new JObject
            {
                ["objectId"] = objectId,
                ["functionDeclaration"] = functionDeclaration,
                ["arguments"] = arguments ?? new JArray(),
                ["awaitPromise"] = true,
                ["returnByValue"] = false,
                ["userGesture"] = true
            }).ConfigureAwait(false);
            if (response["exceptionDetails"] != null)
                throw new InvalidOperationException(response["exceptionDetails"].ToString());
            var remote = response["result"] as JObject;
            if (remote == null || remote["objectId"] == null)
                throw new InvalidOperationException(remote?["description"]?.Value<string>() ?? "The browser operation did not return an object.");
            return remote["objectId"].Value<string>();
        }

        internal async Task<string> CallFunctionValueAsync(string objectId, string functionDeclaration, JArray arguments = null)
        {
            var response = await _connection.SendAsync("Runtime.callFunctionOn", new JObject
            {
                ["objectId"] = objectId,
                ["functionDeclaration"] = functionDeclaration,
                ["arguments"] = arguments ?? new JArray(),
                ["awaitPromise"] = true,
                ["returnByValue"] = true,
                ["userGesture"] = true
            }).ConfigureAwait(false);
            if (response["exceptionDetails"] != null)
                throw new InvalidOperationException(response["exceptionDetails"].ToString());
            var remote = response["result"] as JObject;
            if (remote == null || remote["value"] == null) return null;
            var value = remote["value"];
            return value.Type == JTokenType.String ? value.Value<string>() : value.ToString();
        }

        internal async Task ReleaseObjectAsync(string objectId)
        {
            if (string.IsNullOrWhiteSpace(objectId) || _disposed) return;
            await _connection.SendAsync("Runtime.releaseObject", new JObject { ["objectId"] = objectId }).ConfigureAwait(false);
        }

        public async Task RefreshAsync()
        {
            string state = await EvaluateAsync("JSON.stringify({url:location.href,title:document.title})").ConfigureAwait(false);
            if (string.IsNullOrEmpty(state)) return;
            JObject value = JObject.Parse(state);
            Url = (string)value["url"];
            Title = (string)value["title"];
        }

        public Task<string> GetDocumentHtmlAsync() => EvaluateAsync("document.documentElement ? document.documentElement.outerHTML : null");
        public Task<string> GetDocumentTextAsync() => EvaluateAsync("document.documentElement ? document.documentElement.innerText : null");
        public Task<string> GetUrlAsync() => EvaluateAsync("location.href");
        public Task<string> GetTitleAsync() => EvaluateAsync("document.title");

        private async Task<string> RunNavigationCommandAsync(Func<Task> command, int timeoutMilliseconds = 30000)
        {
            var loaded = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            Action<string, JObject> handler = (name, data) =>
            {
                if (name == "Page.loadEventFired" || name == "Page.navigatedWithinDocument")
                    loaded.TrySetResult(true);
            };
            _connection.EventReceived += handler;
            try
            {
                await command().ConfigureAwait(false);
                using (var timeout = new CancellationTokenSource(timeoutMilliseconds))
                using (timeout.Token.Register(() => loaded.TrySetException(new TimeoutException("The page navigation timed out."))))
                    await loaded.Task.ConfigureAwait(false);
                await RefreshAsync().ConfigureAwait(false);
                return Url;
            }
            finally { _connection.EventReceived -= handler; }
        }

        public Task<string> GoBackAsync()
        {
            return RunNavigationCommandAsync(async () =>
            {
                var history = await _connection.SendAsync("Page.getNavigationHistory").ConfigureAwait(false);
                int index = history["currentIndex"].Value<int>();
                var entries = history["entries"] as JArray;
                if (entries == null || index <= 0) return;
                await _connection.SendAsync("Page.navigateToHistoryEntry", new JObject { ["entryId"] = entries[index - 1]["id"] }).ConfigureAwait(false);
            });
        }

        public Task<string> GoForwardAsync()
        {
            return RunNavigationCommandAsync(async () =>
            {
                var history = await _connection.SendAsync("Page.getNavigationHistory").ConfigureAwait(false);
                int index = history["currentIndex"].Value<int>();
                var entries = history["entries"] as JArray;
                if (entries == null || index + 1 >= entries.Count) return;
                await _connection.SendAsync("Page.navigateToHistoryEntry", new JObject { ["entryId"] = entries[index + 1]["id"] }).ConfigureAwait(false);
            });
        }

        public Task<string> ReloadAsync()
        {
            return RunNavigationCommandAsync(() =>
                _connection.SendAsync("Page.reload", new JObject { ["ignoreCache"] = false }));
        }

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