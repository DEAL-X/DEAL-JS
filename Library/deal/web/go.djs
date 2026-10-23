command #go:start|function(url = null) {
    if (url == null) return go;
    if(typeof NavigationHistoryEntry !== "undefined" && url instanceof NavigationHistoryEntry) url = url.url;
    window.open(url ?? window.location.href, "_blank");
}