command #locate:start|function(url = null) {
    if (url == null && typeof location !== "undefined") url = location.href;
    if (typeof NavigationHistoryEntry !== "undefined" && url instanceof NavigationHistoryEntry)
        url = url.url;
    if (!url) throw new Error("A URL is required to update the current page location.");

    if (typeof window !== "undefined" && window.history)
        return window.history.replaceState(null, null, url);

    if (typeof application !== "undefined" &&
        application.Engine &&
        application.Engine.BrowserProvider &&
        application.Engine.BrowserProvider.CurrentPage) {
        return application.Engine.BrowserProvider.CurrentPage.EvaluateAsync(
            "history.replaceState(null, null, " + JSON.stringify(url) + "); location.href"
        );
    }

    throw new Error("Updating the current page location requires a browser page or an active DEAL CDP page.");
}

command #relocate:start|function(url = null) {
    if (url == null && typeof location !== "undefined") url = location.href;
    if (typeof NavigationHistoryEntry !== "undefined" && url instanceof NavigationHistoryEntry)
        url = url.url;
    if (!url) throw new Error("A URL is required to add a page history entry.");

    if (typeof window !== "undefined" && window.history)
        return window.history.pushState(null, null, url);

    if (typeof application !== "undefined" &&
        application.Engine &&
        application.Engine.BrowserProvider &&
        application.Engine.BrowserProvider.CurrentPage) {
        return application.Engine.BrowserProvider.CurrentPage.EvaluateAsync(
            "history.pushState(null, null, " + JSON.stringify(url) + "); location.href"
        );
    }

    throw new Error("Updating the current page history requires a browser page or an active DEAL CDP page.");
}