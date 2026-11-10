async command #load:start|function() {
	BROWSER_initializeHandlers();
	return will application.Engine.Browser.Load(...arguments);
}