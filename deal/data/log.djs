globalThis.Log = globalThis.Log || {};
command #Log:identifier = {};

command Log.path = application.Engine.Resource.TempDirectory+"logs.log";
command Log.separator = " ";

application.Engine.Resource.Delete(Log.path);

command Log.message = function (title = null, message = null) {
	let t = (new Date()).toLocaleString() + Log.separator;
	if(title != null) t += title + ":" + Log.separator;
	if(message != null) t += message;
	application.Engine.Resource.AppendFileContents(Log.path, t);
	return t;
}
command Log.subject = function (message = null) => Log.message(message, "");
command Log.error = function (message = null) => Log.message("Error", message);
command Log.success = function (message = null) => Log.message("Success", message);
command Log.warn = function (message = null) => Log.message("Warning", message);
command Log.warning = function (message = null) => Log.warn(message);
command Log.info = function (message = null) => Log.message(null, message);
