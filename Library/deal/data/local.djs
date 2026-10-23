// All resource related commands
command Local = {
	folder : function (path = null, recursive = false) {
		if(!path) return this.folder;
		return engine.ResourceProvider.GetFolderContents(path, recursive);
	},
	file : function (path = null, encoding = null) {
		if(!path) return this.file;
		return engine.ResourceProvider.GetFileContents(path, encoding);
	},
	content : function (path = null, recursive = false, encoding = null) {
		if(!path) return this.content;
		return engine.ResourceProvider.GetContents(path, recursive, encoding);
	},
	read : function (path = null, recursive = false, encoding = null) {
		if(!path) return this.read;
		let ent = engine.ResourceProvider.Entity(path)
		if(ent.IsFile) return engine.ResourceProvider.GetFileContents(path, encoding);
		else if(ent.IsFolder) return engine.ResourceProvider.GetFolderContents(path, recursive);
	},
	write : function (path = null, object = null) {
		if(!path) return this.write;
		return engine.ResourceProvider.SetFileContents(path, object);
	},
	prepend : function (path = null, object = null) {
		if(!path) return this.prepend;
		return engine.ResourceProvider.PrependFileContents(path, object);
	},
	append : function (path = null, object = null) {
		if(!path) return this.append;
		return engine.ResourceProvider.AppendFileContents(path, object);
	},
	delete : function (path = null) {
		if(!path) return this.delete;
		return engine.ResourceProvider.Delete(path);
	},
	entity : function (path = null) {
		if(!path) return this.entity;
		return engine.ResourceProvider.Entity(path);
	},
	exists : function (path = null) {
		if(!path) return this.exists;
		return engine.ResourceProvider.Exists(path);
	}
};