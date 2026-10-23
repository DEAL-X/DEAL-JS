command #load:start|function(url = null, target = null) {
    if (url == null) return load;
    if(typeof NavigationHistoryEntry !== "undefined" && url instanceof NavigationHistoryEntry) url = url.url;
	if (target === null) window.location.href = url ?? window.location.href;
	else return window.open(url ?? window.location.href, target === true ? "_blank" : (target === false ? "_self" : target));
}

command #reload:start|function() {
	return window.location.reload();
}