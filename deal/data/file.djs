use deal/make/string;
use deal/data/temporary;

// All resource related commands
command FILE = function (path = null) {
	return {
		path: path ?? TEMPORARY.path(),
		directory: function (recursive = false) {
			return application.Engine.Resource.Entity(this.path).Directory;
		},
		fullname: function () {
			return application.Engine.Resource.FileName(this.path).FullName;
		},
		name: function () {
			return application.Engine.Resource.FileName(this.path).Name;
		},
		extension: function () {
			return application.Engine.Resource.FileName(this.path).Extension;
		},
		content: function (recursive = false, encoding = null) {
			return application.Engine.Resource.GetFileContents(this.path, recursive, encoding);
		},
		create: function (object = "") {
			if(!application.Engine.Resource.Exists(this.path)) application.Engine.Resource.SetFileContents(this.path, MAKE.string(object));
			return application.Engine.Resource.Exists(this.path);
		},
		read: function (encoding = null) {
			return application.Engine.Resource.GetFileContents(this.path, encoding);
		},
		write: function (object = null) {
			return application.Engine.Resource.SetFileContents(this.path, MAKE.string(object));
		},
		prepend: function (object = null) {
			return application.Engine.Resource.PrependFileContents(this.path, MAKE.string(object));
		},
		append: function (object = null) {
			return application.Engine.Resource.AppendFileContents(this.path, MAKE.string(object));
		},
		move: function (destination = null, overwrite = false) {
			if(!destination) return false;
			return application.Engine.Resource.Move(this.path, destination, overwrite);
		},
		copy: function (source = null, destination = null, overwrite = false) {
			if(!destination) return false;
			return application.Engine.Resource.Copy(this.path, destination, overwrite);
		},
		rename: function (newName = null, overwrite = false) {
			if(!newName) return false;
			return application.Engine.Resource.Rename(this.path, newName, overwrite);
		},
		delete: function () {
			return application.Engine.Resource.Delete(this.path);
		},
		exists: function () {
			return application.Engine.Resource.Exists(this.path);
		},
		entity: function () {
			return application.Engine.Resource.Entity(this.path);
		}
	};
}