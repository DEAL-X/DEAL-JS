use deal/make;
use deal/make/string;
use deal/make/object;

// All resource related commands
command #Local:identifier = {};

command Local.folder (path = null, recursive = false) {
	if(!path) return null;
	return application.Engine.Resource.GetFolderContents(path, recursive);
};
command Local.file (path = null) {
	if(!path) return null;
	return application.Engine.Resource.GetFileContents(path);
};
command Local.content (path = null, recursive = false, encoding = null) {
	if(!path) return null;
	return application.Engine.Resource.GetContents(path, recursive, encoding);
};
command Local.create (path = null, object = "") {
	if(!path) return false;
	if(!application.Engine.Resource.Exists(path)) application.Engine.Resource.SetFileText(path, Make.string(object));
	return application.Engine.Resource.Exists(path);
};
command Local.directory (path = null) {
	if(!path) return false;
	application.Engine.Resource.CreateDirectory(path);
	return application.Engine.Resource.Exists(path);
};
command Local.name (path = null) {
	if(!path) return null;
	let ent = application.Engine.Resource.Entity(path)
	if(ent.IsFile) return application.Engine.Resource.FileName(path);
	else if(ent.IsFolder) return application.Engine.Resource.FolderName(path);
};
command Local.read (path = null, recursive = false, encoding = null) {
	if(!path) return null;
	let ent = application.Engine.Resource.Entity(path)
	if(ent.IsFile) return application.Engine.Resource.GetFileText(path, encoding);
	else if(ent.IsFolder) return application.Engine.Resource.GetFolderContents(path, recursive);
};
command Local.write (path = null, object = null) {
	if(!path) return false;
	return application.Engine.Resource.SetFileContents(path, Make.byteArrayObject(Make.string(object)));
};
command Local.prepend (path = null, object = null) {
	if(!path) return false;
	return application.Engine.Resource.PrependFileText(path, Make.string(object));
};
command Local.append (path = null, object = null) {
	if(!path) return false;
	return application.Engine.Resource.AppendFileText(path, Make.string(object));
};
command Local.move (source = null, destination = null, overwrite = false) {
	if(!source || !destination) return false;
	return application.Engine.Resource.Move(source, destination, overwrite);
};
command Local.copy (source = null, destination = null, overwrite = false) {
	if(!source || !destination) return false;
	return application.Engine.Resource.Copy(source, destination, overwrite);
};
command Local.rename = function (path = null, newName = null, overwrite = false) {
	if(!path || !newName) return false;
	return application.Engine.Resource.Rename(path, newName, overwrite);
};
command Local.delete (path = null) {
	if(!path) return false;
	return application.Engine.Resource.Delete(path);
};
command Local.exists (path = null) {
	if(!path) return false;
	return application.Engine.Resource.Exists(path);
};
command Local.path (path = null) {
	if(!path) return null;
	return application.Engine.Resource.Normalize(path);
};
command Local.entity (path = null) {
	if(!path) return null;
	return application.Engine.Resource.Entity(path);
};