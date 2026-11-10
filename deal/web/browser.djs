use deal/core;
use deal/make/logical.djs;

command #browser:identifier = application.Engine.Browser;

command tab = null;
command window = null;
command document = null;

function BROWSER_initializeHandlers () {
	if(tab === null) {
		tab = application.Engine.Browser.Tab;
		window = application.Engine.Browser.Tab.Window;
		document = application.Engine.Browser.Tab.Window.Document;
	}
}