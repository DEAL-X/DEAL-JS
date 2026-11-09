// DEAL Fetch compatibility layer for the ClearScript host.
// This implements the supported Fetch request/response subset over Transfer.
// The hosting V8ScriptEngine must enable EnableTaskPromiseConversion so the
// .NET Task<JObject> returned by FetchAsync becomes awaitable by JavaScript.
// Browser-only features (CORS enforcement, service workers, streaming bodies,
// cache modes and browser-origin policy) are not simulated.

// ------------------------------------------------------------
// Native transport bridge
// ------------------------------------------------------------
function getTransport() {
    const transport = typeof application !== "undefined" && application.Engine && application.Engine.Transfer;
    if (!transport) throw new Error("The DEAL Transfer host is not available.");
    if (!transport.IsAlive) transport.Initial();
    return transport;
}
command #FETCH:start|function(input, init = {}) {
    let request;
    try {
        request = input instanceof Request && init === undefined ? input.clone() : new Request(input, init);
    } catch (error) { return Promise.reject(error); }

    if (request.signal && request.signal.aborted) return Promise.reject(abortError());
    if (request.redirect !== "follow")
        return Promise.reject(new TypeError("The host transport currently supports redirect:'follow' only."));
    if (request.mode !== "cors")
        return Promise.reject(new TypeError("The host Fetch transport supports mode:'cors' only; it does not emulate browser CORS or navigation modes."));
    if (request.credentials !== "omit" && request.credentials !== "same-origin" && request.credentials !== "include")
        return Promise.reject(new TypeError("Invalid credentials mode."));
    // There is no browser-origin context in ClearScript. The host treats
    // same-origin and include identically; omit is enforced by Transfer.
    if (request.cache !== "default")
        return Promise.reject(new TypeError("The host Fetch transport does not implement browser cache modes."));
    if (request.integrity)
        return Promise.reject(new TypeError("Subresource integrity is not implemented by the host Fetch transport."));
    if (request.referrer)
        return Promise.reject(new TypeError("Browser referrer policy is not implemented by the host Fetch transport."));
    if (request.keepalive)
        return Promise.reject(new TypeError("Browser keepalive semantics are not implemented by the host Fetch transport."));

    let transport, id, abortListener, operation;
    try {
        transport = getTransport();
        id = "fetch-" + Date.now() + "-" + Math.random().toString(36).slice(2);
        const bodyBase64 = request._bytes.length ? MAKE.base64FromBytes(request._bytes) : null;
        const headersJson = JSON.stringify(Object.fromEntries(request.headers.entries()));
        if (request.signal && typeof request.signal.addEventListener === "function") {
            abortListener = () => transport.Abort(id);
            request.signal.addEventListener("abort", abortListener, { once: true });
        }
        const pathProvider = typeof application !== "undefined" && application.Engine && application.Engine.Router;
        const baseAddress = typeof location !== "undefined" ? location.href : null;
        const resolvedUrl = pathProvider ? pathProvider.Resolve(request.url, baseAddress) : request.url;
        operation = transport.FetchJsonAsync(request.method, resolvedUrl, null, null, request.timeout, headersJson, id, bodyBase64, request.credentials);
    } catch (error) {
        if (abortListener) request.signal.removeEventListener("abort", abortListener);
        return Promise.reject(error);
    }

    return Promise.resolve(operation).then(nativeResult => {
        if (request.signal && request.signal.aborted) throw abortError();
        const result = JSON.parse(nativeResult.ToString());
        const response = new Response(MAKE.bytesFromBase64(result.data || ""), {
            status: result.status,
            statusText: result.statusText,
            headers: result.headers,
            url: result.url,
            redirected: result.redirected,
            type: result.type || "basic"
        });
        return response;
    }).catch(error => {
        if (request.signal && request.signal.aborted) throw abortError();
        const hostErrorName = error && (
            error.name ||
            (error.GetType && error.GetType().Name) ||
            (error.hostException && error.hostException.GetType && error.hostException.GetType().Name)
        );
        if (request.timeout > 0 && (
            hostErrorName === "TimeoutException" ||
            hostErrorName === "TaskCanceledException" ||
            /task.*cancel|canceled|cancelled/i.test(String(error && error.message || ""))
        )) throw timeoutError();
        throw error;
    }).finally(() => {
        if (abortListener) request.signal.removeEventListener("abort", abortListener);
    });
}

#FETCH.isDealFetch = true;
