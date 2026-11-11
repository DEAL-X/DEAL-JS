use deal/make/string;
use deal/make/object;

// All resource related commands
command Local = {};

#Local.folder:function = function (path = null, recursive = false) {
	if(!path) return null;
	return application.Engine.Resource.GetFolderContents(path, recursive);
};
#Local.file:function = function (path = null) {
	if(!path) return null;
	return application.Engine.Resource.GetFileContents(path);
};
#Local.content:function = function (path = null, recursive = false, encoding = null) {
	if(!path) return null;
	return application.Engine.Resource.GetContents(path, recursive, encoding);
};
#Local.create:function = function (path = null, object = "") {
	if(!path) return false;
	if(!application.Engine.Resource.Exists(path)) application.Engine.Resource.SetFileText(path, Make.string(object));
	return application.Engine.Resource.Exists(path);
};
#Local.directory:function = function (path = null) {
	if(!path) return false;
	application.Engine.Resource.CreateDirectory(path);
	return application.Engine.Resource.Exists(path);
};
#Local.name:function = function (path = null) {
	if(!path) return null;
	let ent = application.Engine.Resource.Entity(path)
	if(ent.IsFile) return application.Engine.Resource.FileName(path);
	else if(ent.IsFolder) return application.Engine.Resource.FolderName(path);
};
#Local.read:function = function (path = null, recursive = false, encoding = null) {
	if(!path) return null;
	let ent = application.Engine.Resource.Entity(path)
	if(ent.IsFile) return application.Engine.Resource.GetFileText(path, encoding);
	else if(ent.IsFolder) return application.Engine.Resource.GetFolderContents(path, recursive);
};
#Local.write:function = function (path = null, object = null) {
	if(!path) return false;
	return application.Engine.Resource.SetFileContents(path, Make.bytesObject(object));
};
#Local.prepend:function = function (path = null, object = null) {
	if(!path) return false;
	return application.Engine.Resource.PrependFileText(path, Make.string(object));
};
#Local.append:function = function (path = null, object = null) {
	if(!path) return false;
	return application.Engine.Resource.AppendFileText(path, Make.string(object));
};
#Local.move:function = function (source = null, destination = null, overwrite = false) {
	if(!source || !destination) return false;
	return application.Engine.Resource.Move(source, destination, overwrite);
};
#Local.copy:function = function (source = null, destination = null, overwrite = false) {
	if(!source || !destination) return false;
	return application.Engine.Resource.Copy(source, destination, overwrite);
};
#Local.rename = function (path = null, newName = null, overwrite = false) {
	if(!path || !newName) return false;
	return application.Engine.Resource.Rename(path, newName, overwrite);
};
#Local.delete:function = function (path = null) {
	if(!path) return false;
	return application.Engine.Resource.Delete(path);
};
#Local.entity:function = function (path = null) {
	if(!path) return null;
	return application.Engine.Resource.Entity(path);
};
#Local.exists:function = function (path = null) {
	if(!path) return false;
	return application.Engine.Resource.Exists(path);
};