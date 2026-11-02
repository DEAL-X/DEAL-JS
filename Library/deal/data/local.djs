use deal/make/string;

// All resource related commands
command Local = {};

Local.folder = function (path = null, recursive = false) {
	if(!path) return null;
	return engine.ResourceProvider.GetFolderContents(path, recursive);
};
Local.file = function (path = null, encoding = null) {
	if(!path) return null;
	return engine.ResourceProvider.GetFileContents(path, encoding);
};
Local.content = function (path = null, recursive = false, encoding = null) {
	if(!path) return null;
	return engine.ResourceProvider.GetContents(path, recursive, encoding);
};
Local.create = function (path = null, object = "") {
	if(!path) return false;
	if(!engine.ResourceProvider.Exists(path)) engine.ResourceProvider.SetFileContents(path, Make.string(object));
	return engine.ResourceProvider.Exists(path);
};
Local.name = function (path = null) {
	if(!path) return null;
	let ent = engine.ResourceProvider.Entity(path)
	if(ent.IsFile) return engine.ResourceProvider.FileName(path);
	else if(ent.IsFolder) return engine.ResourceProvider.FolderName(path);
};
Local.read = function (path = null, recursive = false, encoding = null) {
	if(!path) return null;
	let ent = engine.ResourceProvider.Entity(path)
	if(ent.IsFile) return engine.ResourceProvider.GetFileContents(path, encoding);
	else if(ent.IsFolder) return engine.ResourceProvider.GetFolderContents(path, recursive);
};
Local.write = function (path = null, object = null) {
	if(!path) return false;
	return engine.ResourceProvider.SetFileContents(path, Make.string(object));
};
Local.prepend = function (path = null, object = null) {
	if(!path) return false;
	return engine.ResourceProvider.PrependFileContents(path, Make.string(object));
};
Local.append = function (path = null, object = null) {
	if(!path) return false;
	return engine.ResourceProvider.AppendFileContents(path, Make.string(object));
};
Local.delete = function (path = null) {
	if(!path) return false;
	return engine.ResourceProvider.Delete(path);
};
Local.entity = function (path = null) {
	if(!path) return null;
	return engine.ResourceProvider.Entity(path);
};
Local.exists = function (path = null) {
	if(!path) return false;
	return engine.ResourceProvider.Exists(path);
};