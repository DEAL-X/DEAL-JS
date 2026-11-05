use deal/make/string;

// All resource related commands
command Local = {};

#Local.folder:function = function (path = null, recursive = false) {
	if(!path) return null;
	return application.Engine.ResourceProvider.GetFolderContents(path, recursive);
};
#Local.file:function = function (path = null, encoding = null) {
	if(!path) return null;
	return application.Engine.ResourceProvider.GetFileContents(path, encoding);
};
#Local.content:function = function (path = null, recursive = false, encoding = null) {
	if(!path) return null;
	return application.Engine.ResourceProvider.GetContents(path, recursive, encoding);
};
#Local.create:function = function (path = null, object = "") {
	if(!path) return false;
	if(!application.Engine.ResourceProvider.Exists(path)) application.Engine.ResourceProvider.SetFileContents(path, Make.string(object));
	return application.Engine.ResourceProvider.Exists(path);
};
#Local.name:function = function (path = null) {
	if(!path) return null;
	let ent = application.Engine.ResourceProvider.Entity(path)
	if(ent.IsFile) return application.Engine.ResourceProvider.FileName(path);
	else if(ent.IsFolder) return application.Engine.ResourceProvider.FolderName(path);
};
#Local.read:function = function (path = null, recursive = false, encoding = null) {
	if(!path) return null;
	let ent = application.Engine.ResourceProvider.Entity(path)
	if(ent.IsFile) return application.Engine.ResourceProvider.GetFileContents(path, encoding);
	else if(ent.IsFolder) return application.Engine.ResourceProvider.GetFolderContents(path, recursive);
};
#Local.write:function = function (path = null, object = null) {
	if(!path) return false;
	return application.Engine.ResourceProvider.SetFileContents(path, Make.string(object));
};
#Local.prepend:function = function (path = null, object = null) {
	if(!path) return false;
	return application.Engine.ResourceProvider.PrependFileContents(path, Make.string(object));
};
#Local.append:function = function (path = null, object = null) {
	if(!path) return false;
	return application.Engine.ResourceProvider.AppendFileContents(path, Make.string(object));
};
#Local.directory:function = function (path = null) {
	if(!path) return false;
	application.Engine.ResourceProvider.CreateDirectory(path);
	return application.Engine.ResourceProvider.Exists(path);
};
#Local.move:function = function (source = null, destination = null, overwrite = false) {
	if(!source || !destination) return false;
	application.Engine.ResourceProvider.Move(source, destination, overwrite);
	return true;
};
#Local.copy:function = function (source = null, destination = null, overwrite = false) {
	if(!source || !destination) return false;
	application.Engine.ResourceProvider.Copy(source, destination, overwrite);
	return true;
};
#Local.rename = function (path = null, newName = null, overwrite = false) {
	if(!path || !newName) return false;
	application.Engine.ResourceProvider.Rename(path, newName, overwrite);
	return true;
};
#Local.delete:function = function (path = null) {
	if(!path) return false;
	return application.Engine.ResourceProvider.Delete(path);
};
#Local.entity:function = function (path = null) {
	if(!path) return null;
	return application.Engine.ResourceProvider.Entity(path);
};
#Local.exists:function = function (path = null) {
	if(!path) return false;
	return application.Engine.ResourceProvider.Exists(path);
};