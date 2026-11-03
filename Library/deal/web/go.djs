command #go:start|function(url = null) {
    if (url == null) return go;
    if (typeof NavigationHistoryEntry !== "undefined" && url instanceof NavigationHistoryEntry)
        url = url.url;

    if (url == null && typeof location !== "undefined")
        url = location.href;

    // Use the current browser tab when running in a real browser page.
    if (typeof window !== "undefined" && typeof window.open === "function")
        return window.open(url, "_blank");

    // ClearScript has no window object. Use the host's Chromium CDP provider
    // to create a page instead of attempting to access browser globals.
    if (typeof application !== "undefined" &&
        application.Engine &&
        application.Engine.BrowserProvider) {
        const browser = application.Engine.BrowserProvider;
        if (!browser.IsAlive) browser.Initial();
        return browser.LoadAsync(url || "about:blank");
    }

    throw new Error("Opening a page requires window.open or the DEAL BrowserProvider.");
}