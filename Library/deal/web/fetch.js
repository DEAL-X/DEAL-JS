
if (!application.Engine.FetchTransport.IsAlive)
    application.Engine.FetchTransport.Initial();

const NativePromise = Promise;

function abortError() {
    const error = new Error("The operation was aborted.");
    error.name = "AbortError";
    return error;
}

// ------------------------------------------------------------
// Headers
// ------------------------------------------------------------

class Headers {
    constructor(init) {
        this._items = new Map();

        if (init instanceof Headers) {
            init.forEach((v, k) => this.append(k, v));
        }
        else if (Array.isArray(init)) {
            init.forEach(pair => this.append(pair[0], pair[1]));
        }
        else if (init && typeof init.forEach === "function") {
            init.forEach((v, k) => this.append(k, v));
        }
        else if (init && typeof init === "object") {
            Object.keys(init).forEach(k => this.append(k, init[k]));
        }
    }

    append(name, value) {
        const key = String(name).toLowerCase();
        const val = String(value).trim();

        this._items.set(
            key,
            this._items.has(key)
                ? this._items.get(key) + ", " + val
                : val
        );
    }

    set(name, value) {
        this._items.set(
            String(name).toLowerCase(),
            String(value).trim()
        );
    }

    get(name) {
        return this._items.get(String(name).toLowerCase()) ?? null;
    }

    has(name) {
        return this._items.has(String(name).toLowerCase());
    }

    delete(name) {
        this._items.delete(String(name).toLowerCase());
    }

    forEach(callback, thisArg) {
        this._items.forEach((v, k) =>
            callback.call(thisArg, v, k, this)
        );
    }

    *entries() { yield* this._items.entries(); }
    *keys() { yield* this._items.keys(); }
    *values() { yield* this._items.values(); }

    [Symbol.iterator]() {
        return this.entries();
    }
}

// ------------------------------------------------------------
// Body
// ------------------------------------------------------------

class Body {
    constructor(bytes, bodyUsed) {
        this._bytes = bytes || new Uint8Array(0);
        this.bodyUsed = !!bodyUsed;
    }

    _consume() {
        if (this.bodyUsed) {
            return NativePromise.reject(
                new TypeError("Body has already been consumed.")
            );
        }

        this.bodyUsed = true;
        return NativePromise.resolve(this._bytes);
    }

    arrayBuffer() {
        return this._consume().then(bytes =>
            bytes.buffer.slice(
                bytes.byteOffset,
                bytes.byteOffset + bytes.byteLength
            )
        );
    }

    text() {
        return this._consume().then(Make.textFromBytes);
    }

    json() {
        return this.text().then(text => JSON.parse(text));
    }

    blob() {
        return this._consume().then(bytes =>
            new Blob([bytes], {
                type: this.headers?.get("content-type") || ""
            })
        );
    }

    formData() {
        return this.text().then(text => {
            const params = new URLSearchParams(text);
            const data = new FormData();

            for (const [key, value] of params)
                data.append(key, value);

            return data;
        });
    }

    clone() {
        if (this.bodyUsed)
            throw new TypeError("Body has already been consumed.");

        return new Body(this._bytes.slice(), false);
    }
}

// ------------------------------------------------------------
// Request
// ------------------------------------------------------------

class Request extends Body {
    constructor(input, init) {
        init = init || {};

        const source = input instanceof Request ? input : null;
        const url = source ? source.url : String(input);

        const body = Object.prototype.hasOwnProperty.call(init, "body")
            ? Make.uint8Array(init.body)
            : source
                ? source._bytes.slice()
                : new Uint8Array(0);

        super(body, false);

        this.url = url;

        this.method = String(
            init.method || (source && source.method) || "GET"
        ).toUpperCase();

        this.headers = new Headers(
            init.headers !== undefined
                ? init.headers
                : source && source.headers
        );

        this.credentials = init.credentials ||
            (source && source.credentials) || "same-origin";

        this.cache = init.cache ||
            (source && source.cache) || "default";

        this.mode = init.mode ||
            (source && source.mode) || "cors";

        this.redirect = init.redirect ||
            (source && source.redirect) || "follow";

        this.referrer = init.referrer ||
            (source && source.referrer) || "";

        this.integrity = init.integrity ||
            (source && source.integrity) || "";

        this.keepalive = !!init.keepalive;

        this.signal = init.signal ||
            (source && source.signal) || null;

        this.timeout = init.timeout !== undefined
            ? init.timeout
            : source && source.timeout !== undefined
                ? source.timeout
                : 30000;

        if (
            (this.method === "GET" || this.method === "HEAD") &&
            this._bytes.length
        ) {
            throw new TypeError(
                "Request with GET or HEAD method cannot have a body."
            );
        }
    }

    clone() {
        return new Request(this);
    }
}

// ------------------------------------------------------------
// Response
// ------------------------------------------------------------

class Response extends Body {
    constructor(body, init) {
        init = init || {};

        const bytes = body instanceof Uint8Array
            ? body
            : Make.uint8Array(body);

        super(bytes, false);

        this.status = init.status === undefined ? 200 : init.status;
        this.statusText = init.statusText || "";
        this.headers = new Headers(init.headers);
        this.url = init.url || "";
        this.redirected = !!init.redirected;
        this.type = init.type || "default";

        this.ok = this.status >= 200 && this.status <= 299;
    }

    clone() {
        if (this.bodyUsed)
            throw new TypeError("Body has already been consumed.");

        return new Response(this._bytes.slice(), this);
    }

    static error() {
        return new Response(null, {
            status: 0,
            type: "error"
        });
    }

    static redirect(url, status) {
        status = status || 302;

        if (![301, 302, 303, 307, 308].includes(status))
            throw new RangeError("Invalid redirect status.");

        return new Response(null, {
            status,
            headers: {
                Location: String(url)
            }
        });
    }

    static json(data, init) {
        const options = Object.assign({}, init || {});
        const headers = new Headers(options.headers);

        if (!headers.has("content-type"))
            headers.set("content-type", "application/json");

        options.headers = headers;

        return new Response(
            JSON.stringify(data),
            options
        );
    }
}

// ------------------------------------------------------------
// C# integration
// ------------------------------------------------------------

function createNativeHeaders(headers) {
    const json = JSON.stringify(
        Object.fromEntries(headers.entries())
    );

    return Core.Newtonsoft.Json.Linq.JObject.Parse(json);
}

function fetch(input, init) {
    let request;

    try {
        request = input instanceof Request && init === undefined
            ? input.clone()
            : new Request(input, init);
    }
    catch (error) {
        return NativePromise.reject(error);
    }

    if (request.signal && request.signal.aborted)
        return NativePromise.reject(abortError());

    const transport = application.Engine.FetchTransport;

    const id = "fetch-" +
        Date.now() + "-" +
        Math.random().toString(36).slice(2);

    // Always send the request body as Base64.
    // This preserves binary data without text conversion.
    const bodyBase64 = request._bytes.length
        ? Make.base64FromBytes(request._bytes)
        : null;

    const headers = createNativeHeaders(request.headers);

    let abortListener = null;

    if (
        request.signal &&
        typeof request.signal.addEventListener === "function"
    ) {
        abortListener = () => transport.Abort(id);

        request.signal.addEventListener(
            "abort",
            abortListener,
            { once: true }
        );
    }

    let operation;

    try {
        // FetchTransport.FetchAsync:
        //
        // FetchAsync(
        //     method,
        //     url,
        //     data,
        //     contentType,
        //     timeout,
        //     headers,
        //     id,
        //     bodyBase64
        // )
        //
        // data = null because the body is transported through Base64.

        operation = transport.FetchAsync(
            request.method,
            request.url,
            null,
            null,
            request.timeout,
            headers,
            id,
            bodyBase64
        );
    }
    catch (error) {
        if (abortListener)
            request.signal.removeEventListener("abort", abortListener);

        return NativePromise.reject(error);
    }

    return NativePromise.resolve(operation)
        .then(nativeResult => {
            if (request.signal && request.signal.aborted)
                throw abortError();

            // FetchAsync returns a Newtonsoft JObject.
            // Its byte[] data property is serialized as Base64.
            const result = JSON.parse(nativeResult.ToString());

            return new Response(
                Make.bytesFromBase64(result.data),
                {
                    status: result.status,
                    statusText: result.statusText,
                    headers: result.headers,
                    url: result.url,
                    redirected: result.redirected,
                    type: result.type || "basic"
                }
            );
        })
        .catch(error => {
            if (request.signal && request.signal.aborted)
                throw abortError();

            throw error;
        })
        .finally(() => {
            if (abortListener) {
                request.signal.removeEventListener(
                    "abort",
                    abortListener
                );
            }
        });
}

// ------------------------------------------------------------
// Global exports
// ------------------------------------------------------------

globalThis.Headers = Headers;
globalThis.Request = Request;
globalThis.Response = Response;
globalThis.fetch = fetch;