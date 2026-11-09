use deal/make/string;

// All resource related commands
command LOCAL = {};

#LOCAL.folder:function = function (path = null, recursive = false) {
	if(!path) return null;
	return application.Engine.Resource.GetFolderContents(path, recursive);
};
#LOCAL.file:function = function (path = null, encoding = null) {
	if(!path) return null;
	return application.Engine.Resource.GetFileContents(path, encoding);
};
#LOCAL.content:function = function (path = null, recursive = false, encoding = null) {
	if(!path) return null;
	return application.Engine.Resource.GetContents(path, recursive, encoding);
};
#LOCAL.create:function = function (path = null, object = "") {
	if(!path) return false;
	if(!application.Engine.Resource.Exists(path)) application.Engine.Resource.SetFileContents(path, MAKE.string(object));
	return application.Engine.Resource.Exists(path);
};
#LOCAL.directory:function = function (path = null) {
	if(!path) return false;
	application.Engine.Resource.CreateDirectory(path);
	return application.Engine.Resource.Exists(path);
};
#LOCAL.name:function = function (path = null) {
	if(!path) return null;
	let ent = application.Engine.Resource.Entity(path)
	if(ent.IsFile) return application.Engine.Resource.FileName(path);
	else if(ent.IsFolder) return application.Engine.Resource.FolderName(path);
};
#LOCAL.read:function = function (path = null, recursive = false, encoding = null) {
	if(!path) return null;
	let ent = application.Engine.Resource.Entity(path)
	if(ent.IsFile) return application.Engine.Resource.GetFileContents(path, encoding);
	else if(ent.IsFolder) return application.Engine.Resource.GetFolderContents(path, recursive);
};
#LOCAL.write:function = function (path = null, object = null) {
	if(!path) return false;
	return application.Engine.Resource.SetFileContents(path, MAKE.string(object));
};
#LOCAL.prepend:function = function (path = null, object = null) {
	if(!path) return false;
	return application.Engine.Resource.PrependFileContents(path, MAKE.string(object));
};
#LOCAL.append:function = function (path = null, object = null) {
	if(!path) return false;
	return application.Engine.Resource.AppendFileContents(path, MAKE.string(object));
};
#LOCAL.move:function = function (source = null, destination = null, overwrite = false) {
	if(!source || !destination) return false;
	application.Engine.Resource.Move(source, destination, overwrite);
	return true;
};
#LOCAL.copy:function = function (source = null, destination = null, overwrite = false) {
	if(!source || !destination) return false;
	application.Engine.Resource.Copy(source, destination, overwrite);
	return true;
};
#LOCAL.rename = function (path = null, newName = null, overwrite = false) {
	if(!path || !newName) return false;
	application.Engine.Resource.Rename(path, newName, overwrite);
	return true;
};
#LOCAL.delete:function = function (path = null) {
	if(!path) return false;
	return application.Engine.Resource.Delete(path);
};
#LOCAL.entity:function = function (path = null) {
	if(!path) return null;
	return application.Engine.Resource.Entity(path);
};
#LOCAL.exists:function = function (path = null) {
	if(!path) return false;
	return application.Engine.Resource.Exists(path);
};