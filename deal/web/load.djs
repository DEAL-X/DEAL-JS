async command #Load:start() {
	BROWSER_initializeHandlers();
	return will application.Engine.Browser.Load(...arguments);
}