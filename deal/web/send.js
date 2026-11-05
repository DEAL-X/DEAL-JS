class Send {
static _SENT = null;
static _RECEIVED = null;

static _isFormData(value) {
    return typeof FormData !== "undefined" && value instanceof FormData;
}
static _isBinary(value) {
    return value != null && (
        (typeof Blob !== "undefined" && value instanceof Blob) ||
        (typeof ArrayBuffer !== "undefined" && value instanceof ArrayBuffer) ||
        (typeof ArrayBuffer !== "undefined" && ArrayBuffer.isView(value))
    );
}

static _notify(callback, result, error, transport) {
    if (typeof callback === "function") {
        try { callback(result, error, transport); }
        catch (exception) { console.error(exception); }
    }
}

static _defaultMessage(result, selector, append = false) {
    if (typeof document === "undefined" || !document.querySelector) {
        if (result != null) console.log(result);
        return;
    }
    if (typeof Struct === "undefined" || typeof _ !== "function") {
        if (result != null) console.log(result);
        return;
    }
    let text = result;
    if (text && typeof text === "object") text = text.statusText || JSON.stringify(text);
    if (text == null || text === "") return;
    text = String(text);
    if (!/class\s*=\s*(["'])[\s\S]*\bresult\b[\s\S]*\1/.test(text))
        text = Struct.result(text);
    try {
        if (append) _(selector).append(text);
        else _(selector).prepend(text);
    } catch (exception) { console.error(exception); }
}

static _prepare(method, url, data) {
    method = String(method || "POST").toUpperCase();
    url = url || (typeof location !== "undefined" ? location.href : null);
    if (!url) throw new TypeError("A request URL is required outside a browser page.");
    const pathProvider = typeof application !== "undefined" && application.Engine && application.Engine.PathProvider;
    if (pathProvider) url = pathProvider.Resolve(url);

    let contentType = "application/x-www-form-urlencoded; charset=utf-8";
    let formData = Send._isFormData(data);
    const standard = ["GET", "POST", "PATCH", "PUT", "DELETE", "OPTIONS", "HEAD"];
    const pseudo = ["FILE", "STREAM", "INTERNAL", "EXTERNAL"];

    if (pseudo.includes(method)) {
        method = "POST";
    } else if (!standard.includes(method)) {
        if (formData) data.append("__METHOD", method);
        else if (Send._isBinary(data)) {
            throw new TypeError("A custom HTTP method cannot be tunneled with a binary request body.");
        }
        else if (data && typeof data === "object") data = Object.assign({}, data, { __METHOD: method });
        else if (data != null) {
            const value = String(data).replace(/^\?/, "");
            data = value + (value ? "&" : "") + "__METHOD=" + encodeURIComponent(method);
        }
        else data = { __METHOD: method };
        method = "POST";
    }

    if (method === "GET" || method === "HEAD") {
        if (data != null) {
            let query;
            if (typeof URLSearchParams !== "undefined" && data instanceof URLSearchParams) {
                query = data.toString();
            } else if (typeof data === "string") {
                query = data.replace(/^\?/, "");
            } else {
                const entries = Array.isArray(data) ? data : Object.entries(data);
                query = entries.map(entry => {
                    const key = Array.isArray(entry) ? entry[0] : entry[0];
                    const value = Array.isArray(entry) ? entry[1] : entry[1];
                    return encodeURIComponent(key) + "=" + encodeURIComponent(value == null ? "" : value);
                }).join("&");
            }
            if (query) {
                const hashIndex = url.indexOf("#");
                const fragment = hashIndex >= 0 ? url.slice(hashIndex) : "";
                const baseUrl = hashIndex >= 0 ? url.slice(0, hashIndex) : url;
                url = baseUrl + (baseUrl.includes("?") ? "&" : "?") + query + fragment;
            }
        }
        data = null;
        contentType = null;
    }
    else if (formData) contentType = null;
    else if (Send._isBinary(data)) contentType = data.type || null;
    else if (typeof URLSearchParams !== "undefined" && data instanceof URLSearchParams) {
        data = data.toString();
        contentType = "application/x-www-form-urlencoded; charset=utf-8";
    }
    else if (data != null && typeof data === "object") {
        data = JSON.stringify(data);
        contentType = "application/json; charset=utf-8";
    }
    else if (typeof data === "string") {
        contentType = "application/x-www-form-urlencoded; charset=utf-8";
    }

    return { method, url, data, contentType, isForm: formData };
}

static request(method = "POST", url = null, data = null, selector = "body",
    success = null, error = null, message = null, progress = null,
    timeout = null, asynchronous = true) {

    const requestedMethod = String(method || "POST").toUpperCase();
    const prepared = Send._prepare(method, url, data);
    ({ method, url, data } = prepared);
    const { contentType, isForm } = prepared;
    Send._SENT = data;

    const hasErrorCallback = typeof error === "function";
    const onSuccess = success ?? ((result, err, transport) => {
        if (err) console.error(err);
        if (result != null && result !== "")
            Send._defaultMessage(result, selector, isForm);
    });
    const onError = error ?? ((result, err) => {
        if (result != null && result !== "")
            Send._defaultMessage(result, selector, isForm);
        if (err) console.error(err);
    });
    const onMessage = message ?? ((result) => {
        if (result != null && result !== "")
            Send._defaultMessage(result, selector, isForm);
    });
    const onProgress = progress ?? ((event) => {
        if (event && typeof event === "object" && "loaded" in event)
            console.log("Request progress:", event.loaded, event.total);
        else if (event != null) console.log(event);
    });

    const doc = typeof document !== "undefined" ? document : null;
    let elements = [];
    let buttons = [];
    let originalOpacity = null;
    let effectTarget = null;
    if (doc && doc.querySelector) {
        effectTarget = doc.querySelector(selector) || null;
        if (effectTarget) {
            elements = Array.from(doc.querySelectorAll(selector));
            buttons = Array.from(doc.querySelectorAll(
                selector + " :is(button, [onclick], [ondblclick], input:is([type=button],[type=submit],[type=image],[type=reset]))"
            ));
            originalOpacity = effectTarget.style.opacity;
        }
    }
    const setBusy = busy => {
        buttons.forEach(button => button.classList.toggle("prevent-events", busy));
        elements.forEach(element => {
            element.style.opacity = busy ? ".5" : (originalOpacity || "");
        });
    };

    // ClearScript and other non-browser hosts use the Fetch-compatible transport.
    if (typeof XMLHttpRequest === "undefined") {
        if (isForm)
            throw new TypeError("FormData uploads require XMLHttpRequest in this runtime; the ClearScript host transport accepts byte or text bodies.");
        if (typeof fetch !== "function")
            throw new Error("Neither XMLHttpRequest nor fetch is available in this runtime.");
        const headers = {};
        if (contentType) headers["Content-Type"] = contentType;
        const options = { method, headers, timeout: timeout == null ? 0 : Number(timeout) };
        if (data != null) options.body = data;
        if (timeout > 0 && typeof AbortController !== "undefined" && !fetch.isDealFetch) {
            const controller = new AbortController();
            options.signal = controller.signal;
            const timeoutId = setTimeout(() => controller.abort(), timeout);
            return fetch(url, options).then(async response => {
                Send._RECEIVED = response;
                let result;
                if (requestedMethod === "FILE" || requestedMethod === "STREAM") {
                    result = new Uint8Array(await response.arrayBuffer());
                } else {
                    const raw = await response.text();
                    result = raw;
                    if (raw) { try { result = JSON.parse(raw); } catch { } }
                }
                if (response.ok) Send._notify(onSuccess, result, null, response);
                else Send._notify(onError, result, response.status, response);
                return response;
            }).catch(exception => {
                Send._notify(onError, exception.message, exception, null);
                if (hasErrorCallback) return null;
                throw exception;
            }).finally(() => clearTimeout(timeoutId));
        }
        return fetch(url, options).then(async response => {
            Send._RECEIVED = response;
            let result;
            if (requestedMethod === "FILE" || requestedMethod === "STREAM") {
                result = new Uint8Array(await response.arrayBuffer());
            } else {
                const raw = await response.text();
                result = raw;
                if (raw) { try { result = JSON.parse(raw); } catch { } }
            }
            if (response.ok) Send._notify(onSuccess, result, null, response);
            else Send._notify(onError, result, response.status, response);
            return response;
        }).catch(exception => {
                Send._notify(onError, exception.message, exception, null);
                if (hasErrorCallback) return null;
                throw exception;
        });
    }

    const xhr = new XMLHttpRequest();
    xhr.open(method, url, asynchronous !== false);
    if (requestedMethod === "FILE" || requestedMethod === "STREAM") xhr.responseType = "blob";
    if (contentType) xhr.setRequestHeader("Content-Type", contentType);
    if (asynchronous !== false && timeout > 0) xhr.timeout = timeout;
    if (xhr.upload && xhr.upload.addEventListener)
        xhr.upload.addEventListener("progress", event => onProgress(event, null, xhr));

    xhr.onloadstart = () => setBusy(true);
    const finish = () => setBusy(false);
    xhr.onload = () => {
        finish();
        Send._RECEIVED = xhr.response;
        let result = xhr.response;
        if (typeof result === "string" && result) {
            try { result = JSON.parse(result); } catch { }
        }
        if (xhr.status >= 200 && xhr.status < 300) {
            const disposition = xhr.getResponseHeader("Content-Disposition") || "";
            if (/attachment/i.test(disposition) && typeof downloadContent === "function") {
                const match = disposition.match(/filename\*?=(?:UTF-8''|["']?)([^;"']+)/i);
                if (match) {
                    downloadContent(xhr.response, decodeURIComponent(match[1].replace(/["']/g, "")),
                        xhr.getResponseHeader("Content-Type"));
                    return;
                }
            }
            if (xhr.status === 202) Send._notify(onMessage, result, null, xhr);
            else Send._notify(onSuccess, result, null, xhr);
        } else Send._notify(onError, result, xhr.status, xhr);
    };
    xhr.onerror = () => { finish(); Send._notify(onError, "Network Error" + (xhr.statusText ? "\n" + xhr.statusText : ""), xhr.status, xhr); };
    xhr.ontimeout = () => { finish(); Send._notify(onError, "Timeout" + (xhr.statusText ? "\n" + xhr.statusText : ""), "timeout", xhr); };
    xhr.onabort = () => { finish(); Send._notify(onError, "Aborted", "abort", xhr); };
    xhr.send(data == null ? null : data);
    return xhr;
}

static withRetry(method = "POST", url = null, data = null, selector = "body",
    success = null, error = null, message = null, progress = null,
    timeout = null, retryOptions = null, asynchronous = true) {
    const options = Object.assign({ retries: 3, backoff: 300 }, retryOptions || {});
    const retries = Math.max(0, Number(options.retries) || 0);
    const backoff = Math.max(0, Number(options.backoff) || 0);
    const hasCallbacks = typeof success === "function" || typeof error === "function";
    let attempt = 0;

    const run = (resolve, reject) => {
        attempt++;
        const successOnce = (result, err, transport) => {
            Send._notify(success, result, err, transport);
            if (resolve) resolve(result);
        };
        const errorOnce = (result, status, transport) => {
            const code = Number(status);
            const transient = status === 0 || status === "timeout" ||
                status === "abort" || code === 408 || code === 425 || code === 429 || code >= 500 ||
                /network|timeout|ERR_NETWORK_CHANGED/i.test(String(result || ""));
            if (transient && attempt <= retries) {
                setTimeout(() => run(resolve, reject), backoff * Math.pow(2, attempt - 1));
                return;
            }
            Send._notify(error, result, status, transport);
            if (reject) reject({ response: result, status, transport });
        };
        try {
            Send.request(method, url, data, selector, successOnce, errorOnce,
                message, progress, timeout, asynchronous);
        } catch (exception) {
            if (attempt <= retries)
                setTimeout(() => run(resolve, reject), backoff * Math.pow(2, attempt - 1));
            else if (reject) reject(exception);
            else Send._notify(error, exception.message, exception, null);
        }
    };

    if (hasCallbacks) { run(null, null); return undefined; }
    return new Promise((resolve, reject) => run(resolve, reject));
}

static getRequest(url = null, data = null, selector = "body", success = null, error = null, message = null, progress = null, timeout = null) {
    return Send.request("GET", url, data, selector, success, error, message, progress, timeout);
}
static postRequest(url = null, data = null, selector = "body", success = null, error = null, message = null, progress = null, timeout = null) {
    return Send.request("POST", url, data, selector, success, error, message, progress, timeout);
}
static putRequest(url = null, data = null, selector = "body", success = null, error = null, message = null, progress = null, timeout = null) {
    return Send.request("PUT", url, data, selector, success, error, message, progress, timeout);
}
static sendPatch(url = null, data = null, selector = "body", success = null, error = null, message = null, progress = null, timeout = null) {
    return Send.request("PATCH", url, data, selector, success, error, message, progress, timeout);
}
static deleteRequest(url = null, data = null, selector = "body", success = null, error = null, message = null, progress = null, timeout = null) {
    return Send.request("DELETE", url, data, selector, success, error, message, progress, timeout);
}
static fileRequest(url = null, data = null, selector = "body", success = null, error = null, message = null, progress = null, timeout = null) {
    if (data != null) return Send.request("FILE", url, data, selector, success, error, message, progress, timeout);
    if (typeof document === "undefined" || typeof FileReader === "undefined")
        throw new Error("Selecting a local file requires a browser document and FileReader.");
    const input = document.createElement("input");
    input.type = "file";
    return new Promise((resolve, reject) => {
        input.addEventListener("change", () => {
            const file = input.files && input.files[0];
            if (!file) { resolve(null); return; }
            const reader = new FileReader();
            reader.onload = () => resolve(Send.request("FILE", url, reader.result, selector, success, error, message, progress, timeout));
            reader.onerror = () => reject(reader.error || new Error("Could not read the selected file."));
            reader.readAsArrayBuffer(file);
        }, { once: true });
        input.click();
    });
}
static streamRequest(url = null, data = null, selector = "body", success = null, error = null, message = null, progress = null, timeout = null) {
    return Send.request("STREAM", url, data, selector, success, error, message, progress, timeout);
}
static internalRequest(url = null, data = null, selector = "body", success = null, error = null, message = null, progress = null, timeout = null) {
    return Send.request("INTERNAL", url, data, selector, success, error, message, progress, timeout);
}
static externalRequest(url = null, data = null, selector = "body", success = null, error = null, message = null, progress = null, timeout = null) {
    return Send.request("EXTERNAL", url, data, selector, success, error, message, progress, timeout);
}
}