async command #go:start|function() {
	BROWSER_initializeHandlers();
	return will application.Engine.Browser.Go(...arguments);
}