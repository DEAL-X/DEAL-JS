using Newtonsoft.Json.Linq;
using System;
using System.Threading.Tasks;

namespace MiMFa.Net.Browsers.Sessions
{
    /// <summary>Protocol-neutral operations exposed by one browser page session.</summary>
    public abstract class BrowserPageSession : IDisposable
    {
        public abstract string TargetId { get; }
        public abstract string Url { get; protected set; }
        public abstract string Title { get; protected set; }

        public abstract void Dispose();

        internal void Initialize() => InitializeAsync().GetAwaiter().GetResult();
        internal abstract Task InitializeAsync();
        internal object EvaluateValue(string expression) => EvaluateValueAsync(expression).GetAwaiter().GetResult();
        internal abstract Task<object> EvaluateValueAsync(string expression);
        internal string EvaluateObject(string expression) => EvaluateObjectAsync(expression).GetAwaiter().GetResult();
        internal abstract Task<string> EvaluateObjectAsync(string expression);
        internal string CallFunctionObject(string objectId, string functionDeclaration, JArray arguments = null) => CallFunctionObjectAsync(objectId, functionDeclaration, arguments).GetAwaiter().GetResult();
        internal abstract Task<string> CallFunctionObjectAsync(string objectId, string functionDeclaration, JArray arguments = null);
        internal string CallFunctionValue(string objectId, string functionDeclaration, JArray arguments = null) => CallFunctionValueAsync(objectId, functionDeclaration, arguments).GetAwaiter().GetResult();
        internal abstract Task<string> CallFunctionValueAsync(string objectId, string functionDeclaration, JArray arguments = null);
        internal void ReleaseObject(string objectId) => ReleaseObjectAsync(objectId).GetAwaiter().GetResult();
        internal abstract Task ReleaseObjectAsync(string objectId);

        public string Navigate(string url, int timeoutMilliseconds = 60000) => NavigateAsync(url, timeoutMilliseconds).GetAwaiter().GetResult();
        public abstract Task<string> NavigateAsync(string url, int timeoutMilliseconds = 60000);
        public string ReplaceNavigate(string url, int timeoutMilliseconds = 60000) => ReplaceNavigateAsync(url, timeoutMilliseconds).GetAwaiter().GetResult();
        public abstract Task<string> ReplaceNavigateAsync(string url, int timeoutMilliseconds = 60000);
        public string Evaluate(string expression) => EvaluateAsync(expression).GetAwaiter().GetResult();
        public abstract Task<string> EvaluateAsync(string expression);
        public void Refresh() => RefreshAsync().GetAwaiter().GetResult();
        public abstract Task RefreshAsync();
        public string GoBack() => GoBackAsync().GetAwaiter().GetResult();
        public abstract Task<string> GoBackAsync();
        public string GoForward() => GoForwardAsync().GetAwaiter().GetResult();
        public abstract Task<string> GoForwardAsync();
        public string Go(int delta) => GoAsync(delta).GetAwaiter().GetResult();
        public abstract Task<string> GoAsync(int delta);
        public string Reload() => ReloadAsync().GetAwaiter().GetResult();
        public abstract Task<string> ReloadAsync();
        public void Close() => CloseAsync().GetAwaiter().GetResult();
        public abstract Task CloseAsync();
    }
}
