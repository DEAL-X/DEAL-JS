async command #Go:start() {
	BROWSER_initializeHandlers();
	return will application.Engine.Browser.Go(...arguments);
}