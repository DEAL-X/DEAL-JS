using MiMFa.Net.Browsers.Objects;
using MiMFa.Net.Browsers.Protocols.WebDriverBiDi;
using MiMFa.Net.Browsers.Sessions;
using Newtonsoft.Json.Linq;
using System;
using System.Threading.Tasks;

namespace MiMFa.Net.Browsers.Protocols.WebDriverBiDi
{
    /// <summary>Firefox page session implemented with WebDriver BiDi browsing contexts and script commands.</summary>
    internal sealed class FirefoxPageSession : BrowserPageSession
    {
        private readonly BiDiConnection _connection;
        private bool _disposed;

        public override string TargetId { get; }
        public override string Url { get; protected set; }
        public override string Title { get; protected set; }

        internal FirefoxPageSession(string contextId, BiDiConnection connection)
        {
            TargetId = contextId ?? throw new ArgumentNullException(nameof(contextId));
            _connection = connection ?? throw new ArgumentNullException(nameof(connection));
        }

        public override void Dispose()
        {
            _disposed = true;
        }

        internal override async Task InitializeAsync()
        {
            await RefreshAsync().ConfigureAwait(false);
        }

        public override async Task<string> NavigateAsync(string url, int timeoutMilliseconds = 60000)
        {
            if (string.IsNullOrWhiteSpace(url)) throw new ArgumentException("A navigation URL is required.", nameof(url));
            await _connection.SendAsync("browsingContext.navigate", new JObject
            {
                ["context"] = TargetId,
                ["url"] = url,
                ["wait"] = "complete"
            }, timeoutMilliseconds > 0 ? timeoutMilliseconds : 60000).ConfigureAwait(false);
            await RefreshAsync().ConfigureAwait(false);
            return Url;
        }

        public override async Task<string> ReplaceNavigateAsync(string url, int timeoutMilliseconds = 60000)
        {
            if (string.IsNullOrWhiteSpace(url)) throw new ArgumentException("A navigation URL is required.", nameof(url));
            await EvaluateAsync("location.replace(" + Newtonsoft.Json.JsonConvert.ToString(url) + ")").ConfigureAwait(false);
            await Task.Delay(100).ConfigureAwait(false);
            await RefreshAsync().ConfigureAwait(false);
            return Url;
        }

        public override async Task<string> EvaluateAsync(string expression)
        {
            JObject response = await _connection.SendAsync("script.evaluate", new JObject
            {
                ["expression"] = expression ?? "undefined",
                ["target"] = new JObject { ["context"] = TargetId },
                ["awaitPromise"] = true,
                ["resultOwnership"] = "none",
                ["userActivation"] = true
            }).ConfigureAwait(false);
            JObject remote = GetRemoteValue(response);
            if (remote == null) return null;
            JToken value = remote["value"];
            if (value == null || value.Type == JTokenType.Null) return null;
            return value.Type == JTokenType.String ? value.Value<string>() : value.ToString(Newtonsoft.Json.Formatting.None);
        }

        internal override async Task<object> EvaluateValueAsync(string expression)
        {
            JObject response = await _connection.SendAsync("script.evaluate", new JObject
            {
                ["expression"] = expression ?? "undefined",
                ["target"] = new JObject { ["context"] = TargetId },
                ["awaitPromise"] = true,
                ["resultOwnership"] = "none",
                ["userActivation"] = true
            }).ConfigureAwait(false);
            JObject remote = GetRemoteValue(response);
            if (remote == null) return null;
            return BrowserPage.ToClrValue(remote["value"]);
        }

        internal override async Task<string> EvaluateObjectAsync(string expression)
        {
            JObject response = await _connection.SendAsync("script.evaluate", new JObject
            {
                ["expression"] = expression ?? "undefined",
                ["target"] = new JObject { ["context"] = TargetId },
                ["awaitPromise"] = true,
                ["resultOwnership"] = "root",
                ["userActivation"] = true
            }).ConfigureAwait(false);
            JObject remote = GetRemoteValue(response);
            if (remote == null || remote["handle"] == null)
                throw new InvalidOperationException("The expression did not return a remote object.");
            return remote["handle"].Value<string>();
        }

        internal override async Task<string> CallFunctionObjectAsync(string objectId, string functionDeclaration, JArray arguments = null)
        {
            JObject response = await _connection.SendAsync("script.callFunction", new JObject
            {
                ["functionDeclaration"] = functionDeclaration,
                ["target"] = new JObject { ["handle"] = objectId },
                ["arguments"] = ToBiDiArguments(arguments),
                ["awaitPromise"] = true,
                ["resultOwnership"] = "root",
                ["userActivation"] = true
            }).ConfigureAwait(false);
            JObject remote = GetRemoteValue(response);
            if (remote == null || remote["handle"] == null)
                throw new InvalidOperationException("The browser operation did not return an object.");
            return remote["handle"].Value<string>();
        }

        internal override async Task<string> CallFunctionValueAsync(string objectId, string functionDeclaration, JArray arguments = null)
        {
            JObject response = await _connection.SendAsync("script.callFunction", new JObject
            {
                ["functionDeclaration"] = functionDeclaration,
                ["target"] = new JObject { ["handle"] = objectId },
                ["arguments"] = ToBiDiArguments(arguments),
                ["awaitPromise"] = true,
                ["resultOwnership"] = "none",
                ["userActivation"] = true
            }).ConfigureAwait(false);
            JObject remote = GetRemoteValue(response);
            if (remote == null) return null;
            JToken value = remote["value"];
            return value == null ? null : value.Type == JTokenType.String ? value.Value<string>() : value.ToString(Newtonsoft.Json.Formatting.None);
        }

        internal override async Task ReleaseObjectAsync(string objectId)
        {
            if (string.IsNullOrWhiteSpace(objectId) || _disposed) return;
            await _connection.SendAsync("script.disown", new JObject
            {
                ["target"] = new JObject { ["context"] = TargetId },
                ["handles"] = new JArray(objectId)
            }).ConfigureAwait(false);
        }

        public override async Task RefreshAsync()
        {
            string state = await EvaluateAsync("JSON.stringify({url:location.href,title:document.title})").ConfigureAwait(false);
            if (string.IsNullOrEmpty(state)) return;
            JObject value = JObject.Parse(state);
            Url = (string)value["url"];
            Title = (string)value["title"];
        }

        public override async Task<string> GoBackAsync()
        {
            string before = Url;
            await EvaluateAsync("history.back()").ConfigureAwait(false);
            await WaitForUrlChangeAsync(before).ConfigureAwait(false);
            return Url;
        }

        public override async Task<string> GoForwardAsync()
        {
            string before = Url;
            await EvaluateAsync("history.forward()").ConfigureAwait(false);
            await WaitForUrlChangeAsync(before).ConfigureAwait(false);
            return Url;
        }

        public override async Task<string> GoAsync(int delta)
        {
            string before = Url;
            await EvaluateAsync("history.go(" + delta.ToString(System.Globalization.CultureInfo.InvariantCulture) + ")").ConfigureAwait(false);
            await WaitForUrlChangeAsync(before).ConfigureAwait(false);
            return Url;
        }

        public override async Task<string> ReloadAsync()
        {
            await _connection.SendAsync("browsingContext.reload", new JObject
            {
                ["context"] = TargetId,
                ["ignoreCache"] = false,
                ["wait"] = "complete"
            }).ConfigureAwait(false);
            await RefreshAsync().ConfigureAwait(false);
            return Url;
        }

        public override async Task CloseAsync()
        {
            if (_disposed) return;
            await _connection.SendAsync("browsingContext.close", new JObject { ["context"] = TargetId }).ConfigureAwait(false);
            Dispose();
        }



        private async Task WaitForUrlChangeAsync(string previousUrl)
        {
            var deadline = DateTime.UtcNow.AddSeconds(30);
            do
            {
                await Task.Delay(50).ConfigureAwait(false);
                await RefreshAsync().ConfigureAwait(false);
                if (!string.Equals(previousUrl, Url, StringComparison.Ordinal)) return;
            } while (DateTime.UtcNow < deadline);
        }

        private static JObject GetRemoteValue(JObject commandResult)
        {
            JObject execution = commandResult?["result"] as JObject;
            if (execution == null) return null;
            if (execution["type"]?.Value<string>() == "exception")
                throw new InvalidOperationException(execution["exceptionDetails"]?.ToString() ?? "JavaScript execution failed.");
            return execution["result"] as JObject;
        }

        private static JArray ToBiDiArguments(JArray arguments)
        {
            var result = new JArray();
            if (arguments == null) return result;
            foreach (JToken argument in arguments)
            {
                JObject item = argument as JObject;
                if (item == null)
                {
                    result.Add(argument.DeepClone());
                    continue;
                }
                if (item["objectId"] != null)
                {
                    result.Add(new JObject { ["handle"] = item["objectId"].DeepClone() });
                    continue;
                }
                if (item["value"] == null)
                {
                    result.Add(argument.DeepClone());
                    continue;
                }
                JToken value = item["value"];
                string type = value.Type == JTokenType.Integer || value.Type == JTokenType.Float ? "number"
                    : value.Type == JTokenType.Boolean ? "boolean"
                    : value.Type == JTokenType.Null ? "null"
                    : "string";
                var remote = new JObject { ["type"] = type };
                if (type != "null") remote["value"] = value.DeepClone();
                result.Add(remote);
            }
            return result;
        }
    }
}
