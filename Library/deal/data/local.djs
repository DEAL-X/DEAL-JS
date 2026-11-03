use deal/make/string;

// All resource related commands
command Local = {};

Local.folder = function (path = null, recursive = false) {
	if(!path) return null;
	return application.Engine.ResourceProvider.GetFolderContents(path, recursive);
};
Local.file = function (path = null, encoding = null) {
	if(!path) return null;
	return application.Engine.ResourceProvider.GetFileContents(path, encoding);
};
Local.content = function (path = null, recursive = false, encoding = null) {
	if(!path) return null;
	return application.Engine.ResourceProvider.GetContents(path, recursive, encoding);
};
Local.create = function (path = null, object = "") {
	if(!path) return false;
	if(!application.Engine.ResourceProvider.Exists(path)) application.Engine.ResourceProvider.SetFileContents(path, Make.string(object));
	return application.Engine.ResourceProvider.Exists(path);
};
Local.name = function (path = null) {
	if(!path) return null;
	let ent = application.Engine.ResourceProvider.Entity(path)
	if(ent.IsFile) return application.Engine.ResourceProvider.FileName(path);
	else if(ent.IsFolder) return application.Engine.ResourceProvider.FolderName(path);
};
Local.read = function (path = null, recursive = false, encoding = null) {
	if(!path) return null;
	let ent = application.Engine.ResourceProvider.Entity(path)
	if(ent.IsFile) return application.Engine.ResourceProvider.GetFileContents(path, encoding);
	else if(ent.IsFolder) return application.Engine.ResourceProvider.GetFolderContents(path, recursive);
};
Local.write = function (path = null, object = null) {
	if(!path) return false;
	return application.Engine.ResourceProvider.SetFileContents(path, Make.string(object));
};
Local.prepend = function (path = null, object = null) {
	if(!path) return false;
	return application.Engine.ResourceProvider.PrependFileContents(path, Make.string(object));
};
Local.append = function (path = null, object = null) {
	if(!path) return false;
	return application.Engine.ResourceProvider.AppendFileContents(path, Make.string(object));
};
Local.delete = function (path = null) {
	if(!path) return false;
	return application.Engine.ResourceProvider.Delete(path);
};
Local.entity = function (path = null) {
	if(!path) return null;
	return application.Engine.ResourceProvider.Entity(path);
};
Local.exists = function (path = null) {
	if(!path) return false;
	return application.Engine.ResourceProvider.Exists(path);
};