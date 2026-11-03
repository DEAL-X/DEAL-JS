// Load a URL in a new tab of the already-running Chromium/Edge instance.
// The returned BrowserPage is a host proxy. DOM access remains asynchronous
// because the actual Window and Document live in the browser process.
async command #load:start (url = null, timeout = 60000) {
    if (url == null) return load;
    if (application.Engine.BrowserProvider.IsDisposed)
        application.Engine.BrowserProvider.Initial();
    return await application.Engine.BrowserProvider.LoadAsync(String(url), Number(timeout));
}

async command #reload:start (page = null) {
    if (page == null) throw new TypeError("reload requires a loaded BrowserPage.");
    await page.EvaluateAsync("location.reload()");
    return page;
}
