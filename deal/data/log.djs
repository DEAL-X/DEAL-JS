use environment/console;

command LOG = {};

LOG.path = application.Engine.Resource.TempDirectory+"logs.log";
LOG.separator = " ";

application.Engine.Resource.Delete(LOG.path);

LOG.message = function (title = null, message = null) {
	let t = (new Date()).toLocaleString() + LOG.separator;
	if(title != null) t += title + ":" + LOG.separator;
	if(message != null) t += message;
	application.Engine.Resource.AppendFileContents(LOG.path, t);
	return t;
}
LOG.subject = function (message = null) => LOG.message(message, "");
LOG.error = function (message = null) => LOG.message("Error", message);
LOG.success = function (message = null) => LOG.message("Success", message);
LOG.warn = function (message = null) => LOG.message("Warning", message);
LOG.warning = function (message = null) => LOG.warn(message);
LOG.info = function (message = null) => LOG.message(null, message);
