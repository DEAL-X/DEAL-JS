(function (root) {
    "use strict";

    if (!application.Engine.FetchTransport || typeof application.Engine.FetchTransport.SendAsync !== "function")
        throw new Error("DEAL Fetch transport was not injected into this V8 environment.");

    const NativePromise = Promise;
    const encoder = typeof TextEncoder !== "undefined" ? new TextEncoder() : null;
    const decoder = typeof TextDecoder !== "undefined" ? new TextDecoder() : null;

    function toBytes(value) {
        if (value == null) return new Uint8Array(0);
        if (value instanceof Uint8Array) return value;
        if (value instanceof ArrayBuffer) return new Uint8Array(value);
        if (typeof value === "string") {
            if (encoder) return encoder.encode(value);
            const escaped = unescape(encodeURIComponent(value));
            return Uint8Array.from(escaped, c => c.charCodeAt(0));
        }
        return encoder ? encoder.encode(String(value)) : Uint8Array.from(String(value), c => c.charCodeAt(0));
    }

    function base64(bytes) {
        let binary = "";
        for (let i = 0; i < bytes.length; i += 0x8000)
            binary += String.fromCharCode.apply(null, bytes.subarray(i, i + 0x8000));
        return btoa(binary);
    }

    function fromBase64(value) {
        const binary = atob(value || "");
        const bytes = new Uint8Array(binary.length);
        for (let i = 0; i < binary.length; i++) bytes[i] = binary.charCodeAt(i);
        return bytes;
    }

    class Headers {
        constructor(init) {
            this._items = new Map();
            if (init instanceof Headers) init.forEach((v, k) => this.append(k, v));
            else if (Array.isArray(init)) init.forEach(pair => this.append(pair[0], pair[1]));
            else if (init && typeof init.forEach === "function") init.forEach((v, k) => this.append(k, v));
            else if (init && typeof init === "object") Object.keys(init).forEach(k => this.append(k, init[k]));
        }
        append(name, value) {
            const key = String(name).toLowerCase();
            const val = String(value).trim();
            this._items.set(key, this._items.has(key) ? this._items.get(key) + ", " + val : val);
        }
        set(name, value) { this._items.set(String(name).toLowerCase(), String(value).trim()); }
        get(name) { return this._items.get(String(name).toLowerCase()) ?? null; }
        has(name) { return this._items.has(String(name).toLowerCase()); }
        delete(name) { this._items.delete(String(name).toLowerCase()); }
        forEach(callback, thisArg) { this._items.forEach((v, k) => callback.call(thisArg, v, k, this)); }
        *entries() { yield* this._items.entries(); }
        *keys() { yield* this._items.keys(); }
        *values() { yield* this._items.values(); }
        [Symbol.iterator]() { return this.entries(); }
    }

    class Body {
        constructor(bytes, bodyUsed) {
            this._bytes = bytes || new Uint8Array(0);
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
        text() {
            return this._consume().then(bytes => decoder ? decoder.decode(bytes) :
                decodeURIComponent(escape(String.fromCharCode.apply(null, bytes))));
        }
        json() { return this.text().then(text => JSON.parse(text)); }
        blob() {
            return this._consume().then(bytes => new Blob([bytes], { type: this.headers?.get("content-type") || "" }));
        }
        formData() {
            return this.text().then(text => {
                const params = new URLSearchParams(text), data = new FormData();
                for (const [key, value] of params) data.append(key, value);
                return data;
            });
        }
        clone() {
            if (this.bodyUsed) throw new TypeError("Body has already been consumed.");
            return new Body(this._bytes.slice(), false);
        }
    }

    class Request extends Body {
        constructor(input, init) {
            init = init || {};
            const source = input instanceof Request ? input : null;
            const url = source ? source.url : String(input);
            super(source ? source._bytes.slice() : toBytes(init.body), false);
            this.url = url;
            this.method = String(init.method || (source && source.method) || "GET").toUpperCase();
            this.headers = new Headers(init.headers || (source && source.headers));
            this.credentials = init.credentials || (source && source.credentials) || "same-origin";
            this.cache = init.cache || (source && source.cache) || "default";
            this.mode = init.mode || (source && source.mode) || "cors";
            this.redirect = init.redirect || (source && source.redirect) || "follow";
            this.referrer = init.referrer || (source && source.referrer) || "";
            this.integrity = init.integrity || (source && source.integrity) || "";
            this.keepalive = !!init.keepalive;
            this.signal = init.signal || (source && source.signal) || null;
            if ((this.method === "GET" || this.method === "HEAD") && this._bytes.length)
                throw new TypeError("Request with GET or HEAD method cannot have a body.");
        }
        clone() { return new Request(this); }
    }

    class Response extends Body {
        constructor(body, init) {
            init = init || {};
            const bytes = body instanceof Uint8Array ? body : toBytes(body);
            super(bytes, false);
            this.status = init.status === undefined ? 200 : init.status;
            this.statusText = init.statusText || "";
            this.headers = new Headers(init.headers);
            this.url = init.url || "";
            this.redirected = !!init.redirected;
            this.type = init.type || "default";
            this.ok = this.status >= 200 && this.status <= 299;
        }
        clone() { return new Response(this._bytes.slice(), this); }
        static error() { return new Response(null, { status: 0, type: "error" }); }
        static redirect(url, status) {
            if (![301, 302, 303, 307, 308].includes(status || 302))
                throw new RangeError("Invalid redirect status.");
            return new Response(null, { status: status || 302, headers: { Location: String(url) } });
        }
        static json(data, init) {
            const options = Object.assign({}, init || {});
            const headers = new Headers(options.headers);
            if (!headers.has("content-type")) headers.set("content-type", "application/json");
            options.headers = headers;
            return new Response(JSON.stringify(data), options);
        }
    }

    function fetch(input, init) {
        let request;
        try { request = input instanceof Request && init === undefined ? input.clone() : new Request(input, init); }
        catch (error) { return NativePromise.reject(error); }

        if (request.signal && request.signal.aborted)
            return NativePromise.reject(abortError());

        const id = "fetch-" + Date.now() + "-" + Math.random().toString(36).slice(2);
        const bytes = request._bytes;
        const descriptor = {
            id,
            url: request.url,
            method: request.method,
            headers: Array.from(request.headers.entries()),
            bodyBase64: bytes.length ? base64(bytes) : null,
            contentType: request.headers.get("content-type"),
            referrer: request.referrer,
            timeout: init && init.timeout
        };

        let abortListener;
        if (request.signal && typeof request.signal.addEventListener === "function") {
            abortListener = () => application.Engine.FetchTransport.Abort(id);
            request.signal.addEventListener("abort", abortListener, { once: true });
        }

        return NativePromise.resolve(application.Engine.FetchTransport.SendAsync(JSON.stringify(descriptor)))
            .then(raw => {
                const result = JSON.parse(raw);
                return new Response(fromBase64(result.bodyBase64), {
                    status: result.status,
                    statusText: result.statusText,
                    headers: result.headers,
                    url: result.url,
                    redirected: result.redirected
                });
            })
            .catch(error => {
                if (request.signal && request.signal.aborted) throw abortError();
                throw error;
            })
            .finally(() => {
                if (abortListener) request.signal.removeEventListener("abort", abortListener);
            });
    }

    function abortError() {
        const error = new Error("The operation was aborted.");
        error.name = "AbortError";
        return error;
    }

    if (!root.Headers) root.Headers = Headers;
    if (!root.Request) root.Request = Request;
    if (!root.Response) root.Response = Response;
    root.fetch = fetch;
})(typeof globalThis !== "undefined" ? globalThis : this);
