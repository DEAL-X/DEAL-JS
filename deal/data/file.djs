use deal/make/string;
use deal/data/temporary;

// All resource related commands
command File (path = null) {
	return {
		path: path ?? Temporary.path(),
		directory: function () {
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
		content: function () {
			return application.Engine.Resource.GetFileContents(this.path);
		},
		create: function (object = "") {
			if(!application.Engine.Resource.Exists(this.path)) application.Engine.Resource.SetFileText(this.path, Make.string(object));
			return application.Engine.Resource.Exists(this.path);
		},
		text: function (encoding = null) {
			return application.Engine.Resource.GetFileText(this.path, encoding);
		},
		lines: function * (encoding = null) {
			for(const line of application.Engine.Resource.GetFileLines(this.path, encoding))
				yield line;
		},
		fields: function * (encoding = null) {
			// [TODO] Get the file as a generator of parsed each lines to an object, or string if its not an object {}
		},
		read: function (encoding = null) {
			return application.Engine.Resource.GetFileText(this.path, encoding);
		},
		write: function (object = null) {
			return application.Engine.Resource.SetFileText(this.path, Make.string(object));
		},
		prepend: function (object = null) {
			return application.Engine.Resource.PrependFileText(this.path, Make.string(object));
		},
		append: function (object = null) {
			return application.Engine.Resource.AppendFileText(this.path, Make.string(object));
		},
		// To return the first finded line or object
		// The query can be one of these multi-types-conditions:
		//		A simple string to search between each line and return the first found line string
		//		A Regex pattern to search between each line and return the first found line string
		//		A XPath pattern to search between each objects and return the first found line object
		//		A graph object to search between each objects and return the first found line object, the graph can be
		//			{
		//				keyName: one of the above multi-types-conditions,
		//				...
		//			}
		find: function (query, encoding = null) {
			return this.first(query, encoding);
		},
		// To return the first finded line or object
		// The query can be one of the previous multi-types-query
		first: function (query = null, encoding = null) {
			// if(query === null) [TODO] return the first line of the file
			// [TODO] Get the first finded line or object according to the query
		},
		// To return the last finded line or object
		// The query can be one of the previous multi-types-query
		last: function (query = null, encoding = null) {
			// if(query === null) [TODO] return the last line of the file
			// [TODO] Get the last finded line or object according to the query
		},
		// To return the generator for each finded lines or objects
		// The query can be one of the previous multi-types-query
		search: function * (query, encoding = null) {
			// [TODO] Get all finded lines or objects according to the query
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