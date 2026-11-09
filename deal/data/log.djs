use environment/console;

command Log = {};

Log.path = application.Engine.Resource.TempDirectory+"logs.log";
Log.separator = " ";

application.Engine.Resource.Delete(Log.path);

Log.message = function (title = null, message = null) {
	let t = (new Date()).toLocaleString() + Log.separator;
	if(title != null) t += title + ":" + Log.separator;
	if(message != null) t += message;
	application.Engine.Resource.AppendFileContents(Log.path, t);
	return t;
}
Log.subject = function (message = null) => Log.message(message, "");
Log.error = function (message = null) => Log.message("Error", message);
Log.success = function (message = null) => Log.message("Success", message);
Log.warn = function (message = null) => Log.message("Warning", message);
Log.warning = function (message = null) => Log.warn(message);
Log.info = function (message = null) => Log.message(null, message);
