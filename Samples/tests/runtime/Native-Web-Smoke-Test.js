// Run after the host initializes the engine and loads deal/web/fetch.js.

async function testDealFetch() {
    const response = await fetch("example.com");
    if (!(response instanceof Response))
        throw new Error("fetch did not return a Response.");
    if (typeof response.status !== "number")
        throw new Error("Response.status is missing.");
    if (!response.url.startsWith("http://example.com"))
        throw new Error("The path provider did not normalize the incomplete hostname.");

    const text = await response.text();
    if (typeof text !== "string")
        throw new Error("Response.text() did not return a string.");

    return { status: response.status, ok: response.ok, length: text.length, url: response.url };
}

async function testDealBrowserOpen() {
    const tab = browser.Open("example.com");
    const title = tab.Window.Document.Title;
    const url = tab.Window.Location.Href;

    if (typeof title !== "string" || !title)
        throw new Error("The document title property did not return a synchronous string.");
    if (typeof url !== "string" || !url.includes("example.com"))
        throw new Error("The location href property did not return a synchronous URL.");

    return { url: tab.Url, title: title, tabId: tab.Id };
}

async function testDealBrowserNavigate() {
    browser.Tab.Navigate("example.org");
    const url = browser.Tab.Url;

    if (typeof url !== "string" || !url.includes("example.org"))
        throw new Error("browser.Tab.Navigate() did not navigate the current tab.");

    return { url: url };
}

async function testDealFetchLocalFile() {
    const response = await fetch("./Samples/tests/fixtures/Local-Page-Smoke-Test.html");
    if (!response.ok)
        throw new Error("Local file fetch failed: " + response.status);

    const html = await response.text();
    if (!html.includes("DEAL local path test"))
        throw new Error("The local file contents were not returned.");

    return { url: response.url, status: response.status, length: html.length };
}

async function testDealBrowserLocalFile() {
    browser.Tab.Load("./Samples/tests/fixtures/Local-Page-Smoke-Test.html");
    const title = browser.Tab .Window .Document.Title;

    if (title !== "DEAL local path test")
        throw new Error("Chromium did not load the local file.");

    return { url: browser.Tab.Url, title: title };
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
