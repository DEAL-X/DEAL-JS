// Run after Environment has initialized the Engine and loaded Resource/deal/web/fetch.js.
async function testDealFetch() {
    const response = await fetch("https://example.com/");
    if (!(response instanceof Response)) throw new Error("fetch did not return Response");
    if (typeof response.status !== "number") throw new Error("Response.status is missing");
    const text = await response.text();
    if (typeof text !== "string") throw new Error("Response.text() did not return a string");
    return { status: response.status, ok: response.ok, length: text.length, url: response.url };
}

async function testDealLoad() {
    const page = await load("https://example.com/");
    const title = await page.EvaluateAsync("document.title");
    return { url: page.Url, title: title };
}


async function testDealFetchAbort() {
    const controller = new AbortController();
    const pending = fetch("https://example.com/", {
        signal: controller.signal,
        timeout: 0
    });
    controller.abort();

    try {
        await pending;
    } catch (error) {
        if (error && error.name === "AbortError")
            return { aborted: true };
        throw error;
    }

    throw new Error("An aborted Fetch request unexpectedly completed.");
}

async function testDealRequestBodyLifecycle() {
    const request = new Request("https://example.com/", {
        method: "POST",
        body: "DEAL"
    });
    const clone = request.clone();
    const originalText = await request.text();
    const cloneText = await clone.text();

    let cloneAfterConsumeRejected = false;
    try {
        request.clone();
    } catch (error) {
        cloneAfterConsumeRejected = error instanceof TypeError;
    }

    if (originalText !== "DEAL" || cloneText !== "DEAL" || !cloneAfterConsumeRejected)
        throw new Error("Request body lifecycle validation failed.");

    return { originalText, cloneText, cloneAfterConsumeRejected };
}
