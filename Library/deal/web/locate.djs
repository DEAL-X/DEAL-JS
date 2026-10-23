command #locate:start|function(url = null) {
    if (url == null) return locate;
    if(typeof NavigationHistoryEntry !== "undefined" && url instanceof NavigationHistoryEntry) url = url.url;
	window.history.replaceState(null, null, url ?? window.location.href);
}

command #relocate:start|function(url = null) {
    if(typeof NavigationHistoryEntry !== "undefined" && url instanceof NavigationHistoryEntry) url = url.url;
	window.history.pushState(null, null, url ?? window.location.href);
}