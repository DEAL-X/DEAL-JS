globalThis.Log = globalThis.Log || {};
command #Log:identifier = {};

command Log.path = application.Engine.Resource.TempDirectory+"logs.log";
command Log.separator = " ";

application.Engine.Resource.Delete(Log.path);

command Log.message (title = null, message = null) {
	let t = (new Date()).toLocaleString() + Log.separator;
	if(title != null) t += title + ":" + Log.separator;
	if(message != null) t += message;
	application.Engine.Resource.AppendFileContents(Log.path, t);
	return t;
}
command Log.subject (message = null) => Log.message(message, "");
command Log.error (message = null) => Log.message("Error", message);
command Log.success (message = null) => Log.message("Success", message);
command Log.warn (message = null) => Log.message("Warning", message);
command Log.warning (message = null) => Log.warn(message);
command Log.info (message = null) => Log.message(null, message);
