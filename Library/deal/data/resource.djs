// To Impliment all resource related commands
command resource = {
	read : (path = null, encoding = null) => {
		if(!path) return this.read;
		return compiler.ResourceProvider.GetFileContents(path, encoding);
	},
	write : function (path = null, object = null) {
		if(!path) return this.write;
		return compiler.ResourceProvider.SetFileContents(path, object);
	}
};