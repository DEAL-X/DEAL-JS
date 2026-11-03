// Load a URL in a new tab of the already-running Chromium/Edge instance.
// The returned BrowserPage is a host proxy. DOM access remains asynchronous
// because the actual Window and Document live in the browser process.
async command #load:start (url = null, timeout = 60000) {
    if (url == null) return load;
    if (typeof engine.BrowserProvider === "undefined" || !engine.BrowserProvider)
        throw new Error("The DEAL browser provider was not injected.");
    return await engine.BrowserProvider.LoadAsync(String(url), Number(timeout));
}

async command #reload:start (page = null) {
    if (page == null) throw new TypeError("reload requires a loaded BrowserPage.");
    await page.EvaluateAsync("location.reload()");
    return page;
}
