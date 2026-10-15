// To Impliment all resource related commands
command local = {
	folder : function (path = null, recursive = false) {
		if(!path) return this.folder;
		return engine.ResourceProvider.GetFolderContents(path, recursive);
	},
	file : function (path = null, encoding = null) {
		if(!path) return this.file;
		return engine.ResourceProvider.GetFileContents(path, encoding);
	},
	save : function (object = null, path = null) {
		if(!path) return this.save;
		return engine.ResourceProvider.SetFileContents(path, object);
	},
	prepend : function (object = null, path = null) {
		if(!path) return this.prepend;
		return engine.ResourceProvider.PrependFileContents(path, object);
	},
	append : function (object = null, path = null) {
		if(!path) return this.append;
		return engine.ResourceProvider.AppendFileContents(path, object);
	},
	delete : function (path = null) {
		if(!path) return this.delete;
		return engine.ResourceProvider.Delete(path);
	},
	exists : function (path = null) {
		if(!path) return this.exists;
		return engine.ResourceProvider.Exists(path);
	}
};