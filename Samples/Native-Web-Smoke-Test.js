// Run after Environment has initialized the Engine and loaded deal/web/fetch.js.
async function testDealFetch() {
    const response = await fetch("https://example.com/");
    if (!(response instanceof Response)) throw new Error("fetch did not return Response");
    if (typeof response.status !== "number") throw new Error("Response.status is missing");
    const text = await response.text();
    if (typeof text !== "string") throw new Error("Response.text() did not return a string");
    return { status: response.status, ok: response.ok, length: text.length, url: response.url };
}

async function testDealBrowserLoad() {
    const tab = await browser.load("example.com");
    const title = await tab.window.document.title;
    return { url: tab.Url, title: title, tabId: tab.Id };
}

async function testDealBrowserGo() {
    const url = await browser.go("example.com");
    if (!url || url.indexOf("example.com") < 0)
        throw new Error("browser.go() did not navigate the current tab.");
    return { url: url };
}

async function testDealFetchLocalFile() {
    const response = await fetch("./Samples/Local-Page-Smoke-Test.html");
    if (!response.ok) throw new Error("Local file fetch failed: " + response.status);
    const html = await response.text();
    if (html.indexOf("DEAL local path test") < 0)
        throw new Error("The local file contents were not returned.");
    return { url: response.url, status: response.status, length: html.length };
}

async function testDealBrowserLocalFile() {
    const tab = await browser.load("./Samples/Local-Page-Smoke-Test.html");
    const title = await tab.window.document.title;
    if (title !== "DEAL local path test")
        throw new Error("Chromium did not load the local file.");
    return { url: tab.Url, title: title };
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
