// DEAL Fetch compatibility layer for the ClearScript host.
// This implements the useful Fetch request/response subset over FetchTransport.
// Browser-only features (CORS enforcement, service workers, streaming bodies,
// cache modes and browser credentials policy) are intentionally not simulated.

const NativePromise = Promise;
const NativeMap = Map;

function createError(name, message) {
    const error = new Error(message);
    error.name = name;
    return error;
}
function abortError() { return createError("AbortError", "The operation was aborted."); }
function timeoutError() { return createError("TimeoutError", "The request timed out."); }

// ------------------------------------------------------------
// Headers
// ------------------------------------------------------------
class Headers {
    constructor(init) {
        this._items = new NativeMap();
        if (init instanceof Headers) init.forEach((v, k) => this.append(k, v));
        else if (Array.isArray(init)) init.forEach(pair => {
            if (!Array.isArray(pair) || pair.length !== 2) throw new TypeError("Each header pair must contain a name and value.");
            this.append(pair[0], pair[1]);
        });
        else if (init && typeof init.forEach === "function") init.forEach((v, k) => this.append(k, v));
        else if (init && typeof init === "object") Object.keys(init).forEach(k => this.append(k, init[k]));
        else if (init != null) throw new TypeError("Invalid Headers initializer.");
    }
    _name(name) {
        const key = String(name);
        if (!/^[!#$%&'*+.^_`|~0-9A-Za-z-]+$/.test(key)) throw new TypeError("Invalid header name: " + key);
        return key.toLowerCase();
    }
    _value(value) {
        const val = String(value).trim();
        if (/[\r\n]/.test(val)) throw new TypeError("Invalid header value.");
        return val;
    }
    append(name, value) {
        const key = this._name(name), val = this._value(value);
        this._items.set(key, this._items.has(key) ? this._items.get(key) + ", " + val : val);
    }
    set(name, value) { this._items.set(this._name(name), this._value(value)); }
    get(name) { return this._items.get(this._name(name)) ?? null; }
    has(name) { return this._items.has(this._name(name)); }
    delete(name) { this._items.delete(this._name(name)); }
    forEach(callback, thisArg) {
        if (typeof callback !== "function") throw new TypeError("Headers.forEach callback must be a function.");
        this._items.forEach((v, k) => callback.call(thisArg, v, k, this));
    }
    *entries() { yield* this._items.entries(); }
    *keys() { yield* this._items.keys(); }
    *values() { yield* this._items.values(); }
    [Symbol.iterator]() { return this.entries(); }
}

// ------------------------------------------------------------
// Body and Blob fallback
// ------------------------------------------------------------
class DealBlob {
    constructor(parts = [], options = {}) {
        const chunks = parts.map(part => {
            if (part instanceof DealBlob) return part._bytes;
            if (typeof Blob !== "undefined" && part instanceof Blob) return null;
            return Make.uint8Array(part);
        });
        if (chunks.some(chunk => chunk === null))
            throw new TypeError("Native Blob parts cannot be mixed in this host fallback.");
        const size = chunks.reduce((sum, chunk) => sum + chunk.length, 0);
        this._bytes = new Uint8Array(size);
        let offset = 0;
        chunks.forEach(chunk => { this._bytes.set(chunk, offset); offset += chunk.length; });
        this.size = size;
        this.type = String(options.type || "").toLowerCase();
    }
    arrayBuffer() { return NativePromise.resolve(this._bytes.slice().buffer); }
    text() { return NativePromise.resolve(Make.textFromBytes(this._bytes)); }
    slice(start = 0, end = this.size, type = "") {
        const from = start < 0 ? Math.max(this.size + start, 0) : Math.min(start, this.size);
        const to = end < 0 ? Math.max(this.size + end, 0) : Math.min(end, this.size);
        return new DealBlob([this._bytes.slice(from, Math.max(from, to))], { type });
    }
}
const BlobType = typeof Blob !== "undefined" ? Blob : DealBlob;

class Body {
    constructor(bytes, bodyUsed = false) {
        this._bytes = Make.uint8Array(bytes);
        this.bodyUsed = !!bodyUsed;
    }
    _consume() {
        if (this.bodyUsed) return NativePromise.reject(new TypeError("Body has already been consumed."));
        this.bodyUsed = true;
        return NativePromise.resolve(this._bytes);
    }
    arrayBuffer() {
        return this._consume().then(bytes => bytes.buffer.slice(bytes.byteOffset, bytes.byteOffset + bytes.byteLength));
    }
    text() { return this._consume().then(Make.textFromBytes); }
    json() { return this.text().then(text => JSON.parse(text)); }
    blob() { return this._consume().then(bytes => new BlobType([bytes], { type: this.headers?.get("content-type") || "" })); }
    formData() {
        return this.text().then(text => {
            const contentType = this.headers?.get("content-type") || "";
            if (!/^application\/x-www-form-urlencoded(?:\s*;|$)/i.test(contentType))
                throw new TypeError("formData() currently supports application/x-www-form-urlencoded bodies only.");
            if (typeof URLSearchParams === "undefined" || typeof FormData === "undefined")
                throw new TypeError("URLSearchParams and FormData are not available in this runtime.");
            const data = new FormData();
            for (const [key, value] of new URLSearchParams(text)) data.append(key, value);
            return data;
        });
    }
}

// ------------------------------------------------------------
// Request
// ------------------------------------------------------------
class Request extends Body {
    constructor(input, init = {}) {
        if (input instanceof Request && input.bodyUsed)
            throw new TypeError("Cannot construct a Request from a body that has already been consumed.");
        const source = input instanceof Request ? input : null;
        const hasBody = Object.prototype.hasOwnProperty.call(init, "body");
        const body = hasBody ? Make.uint8Array(init.body) : source ? source._bytes.slice() : new Uint8Array(0);
        super(body, false);
        this.url = source ? source.url : String(input);
        this.method = String(init.method || (source && source.method) || "GET").toUpperCase();
        if (!/^[!#$%&'*+.^_`|~0-9A-Za-z-]+$/.test(this.method)) throw new TypeError("Invalid request method.");
        this.headers = new Headers(init.headers !== undefined ? init.headers : source && source.headers);
        this.credentials = init.credentials || (source && source.credentials) || "same-origin";
        this.cache = init.cache || (source && source.cache) || "default";
        this.mode = init.mode || (source && source.mode) || "cors";
        this.redirect = init.redirect || (source && source.redirect) || "follow";
        this.referrer = init.referrer || (source && source.referrer) || "";
        this.integrity = init.integrity || (source && source.integrity) || "";
        this.keepalive = init.keepalive === undefined ? !!(source && source.keepalive) : !!init.keepalive;
        this.signal = init.signal || (source && source.signal) || null;
        this.timeout = init.timeout !== undefined ? Number(init.timeout) : source ? source.timeout : 30000;
        if (!Number.isFinite(this.timeout) || this.timeout < 0) throw new TypeError("Request timeout must be a non-negative number.");
        if ((this.method === "GET" || this.method === "HEAD") && this._bytes.length)
            throw new TypeError("Request with GET or HEAD method cannot have a body.");
        if (this.method === "GET" || this.method === "HEAD") this._bytes = new Uint8Array(0);
    }
    clone() { return new Request(this); }
}

// ------------------------------------------------------------
// Response
// ------------------------------------------------------------
class Response extends Body {
    constructor(body = null, init = {}) {
        const bytes = body == null ? new Uint8Array(0) : Make.uint8Array(body);
        super(bytes, false);
        this.status = init.status === undefined ? 200 : Number(init.status);
        if (!Number.isInteger(this.status) || this.status < 0 || this.status > 599)
            throw new RangeError("Response status must be an integer between 0 and 599.");
        this.statusText = init.statusText || "";
        this.headers = new Headers(init.headers);
        this.url = init.url || "";
        this.redirected = !!init.redirected;
        this.type = init.type || "default";
        this.ok = this.status >= 200 && this.status <= 299;
    }
    clone() {
        if (this.bodyUsed) throw new TypeError("Body has already been consumed.");
        return new Response(this._bytes.slice(), this);
    }
    static error() { return new Response(null, { status: 0, type: "error" }); }
    static redirect(url, status = 302) {
        if (![301, 302, 303, 307, 308].includes(status)) throw new RangeError("Invalid redirect status.");
        return new Response(null, { status, headers: { Location: String(url) } });
    }
    static json(data, init = {}) {
        const options = Object.assign({}, init);
        const headers = new Headers(options.headers);
        if (!headers.has("content-type")) headers.set("content-type", "application/json");
        options.headers = headers;
        return new Response(JSON.stringify(data), options);
    }
}

// ------------------------------------------------------------
// Native transport bridge
// ------------------------------------------------------------
function createNativeHeaders(headers) {
    if (typeof Core === "undefined" || !Core.Newtonsoft || !Core.Newtonsoft.Json || !Core.Newtonsoft.Json.Linq)
        throw new Error("The ClearScript Newtonsoft host types are not available.");
    return Core.Newtonsoft.Json.Linq.JObject.Parse(JSON.stringify(Object.fromEntries(headers.entries())));
}
function getTransport() {
    const transport = typeof application !== "undefined" && application.Engine && application.Engine.FetchTransport;
    if (!transport) throw new Error("The DEAL FetchTransport host is not available.");
    if (!transport.IsAlive) transport.Initial();
    return transport;
}
function fetch(input, init) {
    let request;
    try {
        request = input instanceof Request && init === undefined ? input.clone() : new Request(input, init);
    } catch (error) { return NativePromise.reject(error); }

    if (request.signal && request.signal.aborted) return NativePromise.reject(abortError());
    if (request.redirect !== "follow")
        return NativePromise.reject(new TypeError("The host transport currently supports redirect:'follow' only."));
    if (request.mode === "no-cors")
        return NativePromise.reject(new TypeError("The host transport does not emulate browser no-cors opaque responses."));
    if (request.credentials !== "omit" && request.credentials !== "same-origin" && request.credentials !== "include")
        return NativePromise.reject(new TypeError("Invalid credentials mode."));
    // There is no browser-origin context in ClearScript. The host treats
    // same-origin and include identically; omit is enforced by FetchTransport.
    if (request.mode !== "cors" && request.mode !== "same-origin" && request.mode !== "navigate")
        return NativePromise.reject(new TypeError("Invalid request mode."));

    let transport, id, abortListener, operation;
    try {
        transport = getTransport();
        id = "fetch-" + Date.now() + "-" + Math.random().toString(36).slice(2);
        const bodyBase64 = request._bytes.length ? Make.base64FromBytes(request._bytes) : null;
        const headers = createNativeHeaders(request.headers);
        if (request.signal && typeof request.signal.addEventListener === "function") {
            abortListener = () => transport.Abort(id);
            request.signal.addEventListener("abort", abortListener, { once: true });
        }
        operation = transport.FetchAsync(request.method, request.url, null, null, request.timeout, headers, id, bodyBase64, request.credentials);
    } catch (error) {
        if (abortListener) request.signal.removeEventListener("abort", abortListener);
        return NativePromise.reject(error);
    }

    return NativePromise.resolve(operation).then(nativeResult => {
        if (request.signal && request.signal.aborted) throw abortError();
        const result = JSON.parse(nativeResult.ToString());
        const response = new Response(Make.bytesFromBase64(result.data || ""), {
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
        if (error && (error.name === "TimeoutException" || error.name === "TaskCanceledException") && request.timeout > 0)
            throw timeoutError();
        throw error;
    }).finally(() => {
        if (abortListener) request.signal.removeEventListener("abort", abortListener);
    });
}

// ------------------------------------------------------------
// Global exports
// ------------------------------------------------------------
globalThis.Headers = Headers;
globalThis.Request = Request;
globalThis.Response = Response;
globalThis.fetch = fetch;
