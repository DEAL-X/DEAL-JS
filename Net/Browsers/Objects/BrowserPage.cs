using MiMFa.Net.Browsers.Sessions;
using MiMFa.Net.Browsers.Protocols.DevTools;
using Newtonsoft.Json.Linq;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace MiMFa.Net.Browsers.Objects
{
    /// <summary>Protocol-independent host facade for a remote browser page.</summary>
    public sealed class BrowserPage : IDisposable
    {
        private readonly BrowserPageSession _session;

        public string TargetId => _session.TargetId;
        public string Url => _session.Url;
        public string Title => _session.Title;

        internal BrowserPage(BrowserPageSession session)
        {
            _session = session ?? throw new ArgumentNullException(nameof(session));
        }

        public void Dispose() => _session.Dispose();

        internal void Initialize() => _session.Initialize();
        internal Task InitializeAsync() => _session.InitializeAsync();
        internal object EvaluateValue(string expression) => _session.EvaluateValue(expression);
        internal Task<object> EvaluateValueAsync(string expression) => _session.EvaluateValueAsync(expression);
        internal JObject SendCommand(string method, JObject parameters = null)
        {
            if (_session is IDevToolsPageCommandSession commandSession)
                return commandSession.SendCommand(method, parameters);
            throw new NotSupportedException("The selected browser page session does not support DevTools commands.");
        }

        internal Task<JObject> SendCommandAsync(string method, JObject parameters = null)
        {
            if (_session is IDevToolsPageCommandSession commandSession)
                return commandSession.SendCommandAsync(method, parameters);
            throw new NotSupportedException("The selected browser page session does not support DevTools commands.");
        }
        internal string EvaluateObject(string expression) => _session.EvaluateObject(expression);
        internal Task<string> EvaluateObjectAsync(string expression) => _session.EvaluateObjectAsync(expression);
        internal string CallFunctionObject(string objectId, string functionDeclaration, JArray arguments = null) => _session.CallFunctionObject(objectId, functionDeclaration, arguments);
        internal Task<string> CallFunctionObjectAsync(string objectId, string functionDeclaration, JArray arguments = null) => _session.CallFunctionObjectAsync(objectId, functionDeclaration, arguments);
        internal string CallFunctionValue(string objectId, string functionDeclaration, JArray arguments = null) => _session.CallFunctionValue(objectId, functionDeclaration, arguments);
        internal Task<string> CallFunctionValueAsync(string objectId, string functionDeclaration, JArray arguments = null) => _session.CallFunctionValueAsync(objectId, functionDeclaration, arguments);
        internal void ReleaseObject(string objectId) => _session.ReleaseObject(objectId);
        internal Task ReleaseObjectAsync(string objectId) => _session.ReleaseObjectAsync(objectId);

        public string Navigate(string url, int timeoutMilliseconds = 60000) => _session.Navigate(url, timeoutMilliseconds);
        public Task<string> NavigateAsync(string url, int timeoutMilliseconds = 60000) => _session.NavigateAsync(url, timeoutMilliseconds);
        public string ReplaceNavigate(string url, int timeoutMilliseconds = 60000) => _session.ReplaceNavigate(url, timeoutMilliseconds);
        public Task<string> ReplaceNavigateAsync(string url, int timeoutMilliseconds = 60000) => _session.ReplaceNavigateAsync(url, timeoutMilliseconds);
        public string Evaluate(string expression) => _session.Evaluate(expression);
        public Task<string> EvaluateAsync(string expression) => _session.EvaluateAsync(expression);
        public void Refresh() => _session.Refresh();
        public Task RefreshAsync() => _session.RefreshAsync();
        public string GetDocumentHtml() => GetDocumentHtmlAsync().GetAwaiter().GetResult();
        public Task<string> GetDocumentHtmlAsync() => EvaluateAsync("document.documentElement ? document.documentElement.outerHTML : null");
        public string GetDocumentText() => GetDocumentTextAsync().GetAwaiter().GetResult();
        public Task<string> GetDocumentTextAsync() => EvaluateAsync("document.documentElement ? document.documentElement.innerText : null");
        public string GetUrl() => GetUrlAsync().GetAwaiter().GetResult();
        public Task<string> GetUrlAsync() => EvaluateAsync("location.href");
        public string GetTitle() => GetTitleAsync().GetAwaiter().GetResult();
        public Task<string> GetTitleAsync() => EvaluateAsync("document.title");
        public string GoBack() => _session.GoBack();
        public Task<string> GoBackAsync() => _session.GoBackAsync();
        public string GoForward() => _session.GoForward();
        public Task<string> GoForwardAsync() => _session.GoForwardAsync();
        public string Go(int delta) => _session.Go(delta);
        public Task<string> GoAsync(int delta) => _session.GoAsync(delta);
        public string Reload() => _session.Reload();
        public Task<string> ReloadAsync() => _session.ReloadAsync();
        public void Close() => _session.Close();
        public Task CloseAsync() => _session.CloseAsync();

        public static object ToClrValue(JToken token)
        {
            if (token == null || token.Type == JTokenType.Null || token.Type == JTokenType.Undefined)
                return null;
            if (token is JObject obj)
                return obj.Properties().ToDictionary(property => property.Name, property => ToClrValue(property.Value));
            if (token is JArray array)
                return array.Select(ToClrValue).ToArray();
            if (token is JValue value)
                return value.Value;
            return token.ToString();
        }
    }
}
