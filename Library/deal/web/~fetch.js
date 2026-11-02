(function (root) {
    const previousFetch = root.fetch;
    const nativeFetch = previousFetch &&
        !previousFetch.isFetchOverload
        ? previousFetch.bind(root)
        : previousFetch && previousFetch.nativeFetch
            ? previousFetch.nativeFetch
            : null;

    const XHR = root.XMLHttpRequest;

    function fetch(url, methodOrOptions = "GET") {
        if (url == null || url === "") {
            return Promise.reject(new TypeError("A URL is required."));
        }

        const options = typeof methodOrOptions === "string"
            ? { method: methodOrOptions }
            : methodOrOptions || {};

        options.method = String(options.method || "GET").toUpperCase();

        // Use native fetch whenever the runtime provides it.
        if (nativeFetch) {
            return nativeFetch(url, options);
        }

        // Otherwise, fall back to XMLHttpRequest.
        if (typeof XHR !== "function") {
            return Promise.reject(new TypeError(
                "Neither native fetch nor XMLHttpRequest is available."
            ));
        }

        return new Promise(function (resolve, reject) {
            const request = new XHR();
            let settled = false;

            function fail(error) {
                if (settled) return;
                settled = true;
                reject(error);
            }

            try {
                request.open(options.method, String(url), true);

                if (options.credentials === "include" &&
                    "withCredentials" in request) {
                    request.withCredentials = true;
                }

                const headers = options.headers;

                if (headers) {
                    if (typeof headers.forEach === "function") {
                        headers.forEach(function (value, name) {
                            request.setRequestHeader(name, value);
                        });
                    } else {
                        Object.keys(headers).forEach(function (name) {
                            request.setRequestHeader(name, headers[name]);
                        });
                    }
                }
            } catch (error) {
                fail(error);
                return;
            }

            request.onload = function () {
                if (settled) return;

                const status = request.status === 1223
                    ? 204
                    : request.status;

                if (status === 0) {
                    fail(new TypeError("The request returned no HTTP response."));
                    return;
                }

                settled = true;

                const response = {
                    status: status,
                    statusText: request.statusText || "",
                    ok: status >= 200 && status < 300,
                    url: request.responseURL || String(url),
                    headers: {
                        get: function (name) {
                            return request.getResponseHeader(name);
                        }
                    },
                    text: function () {
                        return Promise.resolve(request.responseText);
                    },
                    json: function () {
                        return Promise.resolve().then(function () {
                            return JSON.parse(request.responseText);
                        });
                    }
                };

                resolve(response);
            };

            request.onerror = function () {
                fail(new TypeError("Network error while fetching the URL."));
            };

            request.onabort = function () {
                const error = new Error("The request was aborted.");
                error.name = "AbortError";
                fail(error);
            };

            try {
                request.send(
                    options.body == null ? null : options.body
                );
            } catch (error) {
                fail(error);
            }
        });
    }

    fetch.isFetchOverload = true;
    fetch.nativeFetch = nativeFetch;

    // Install the overload and also expose a named alias.
    root.fetch = fetch;
    root.fetchWithMethod = fetch;

})(
    typeof globalThis !== "undefined"
        ? globalThis
        : typeof window !== "undefined"
            ? window
            : this
);