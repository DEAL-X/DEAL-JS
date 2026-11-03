// Run after the Environment has injected fetchTransport and loaded ~fetch.js.
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
