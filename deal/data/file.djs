use deal/make;
use deal/select/accept;


// All resource related commands
command File (path = null, encoding = null, sequential = true) {

	function _parseLine(line) {
		try {
			const value = JSON.parse(line);
			if(value !== null && (typeof value === "object" || Array.isArray(value))) return value;
		} catch(error) {}
		return line;
	}

	return {
		encoding: encoding,
		sequential: sequential,
		get path () {
			return path ?? Temporary.path();
		},
		set path (newPath) {
			application.Engine.Resource.Move(this.path, newPath);
			path = newPath;
		},
		get directory () {
			return application.Engine.Resource.Entity(this.path).Directory;
		},
		set directory (val) {
			this.path = application.Engine.Resource.Resolve(application.Engine.Resource.Combine(val, this.fullname));
		},
		get fullname () {
			return application.Engine.Resource.Entity(this.path).FullName;
		},
		set fullname (newName) {
			this.path = application.Engine.Resource.Combine(this.directory, newName);
		},
		get name () {
			return application.Engine.Resource.Entity(this.path).Name;
		},
		set name (newName) {
			this.fullname = newName + this.extension;
		},
		get extension () {
			return application.Engine.Resource.Entity(this.path).Extension;
		},
		set extension (newExt) {
			this.fullname = this.name + newExt;
		},
		get count () {
			return Make.array(application.Engine.Resource.GetFileLines(this.path, this.encoding)).length;
		},
		set count (length) {
			let cnt = this.count;
			if(length > cnt) while(length-- > cnt) application.Engine.Resource.AppendFileLine(this.path, "", this.encoding);
			else return application.Engine.Resource.SetFileLines(this.path, Make.array(application.Engine.Resource.GetFileLines(this.path, this.encoding)).slice(0, length), this.encoding);
		},
		get exists () {
			return application.Engine.Resource.Exists(this.path);
		},
		set exists (stay) {
			if(stay) {
				if(!application.Engine.Resource.Exists(this.path))
					application.Engine.Resource.Create(this.path);
			}
			else application.Engine.Resource.Delete(this.path);
		},
		get contents () {
			const f = this;
			return (function * () {
				for(const c of application.Engine.Resource.GetFileContents(f.path)) yield c;
			})();
		},
		set contents (contents) {
			return application.Engine.Resource.SetFileContents(this.path, contents);
		},
		get fields () {
			const f = this;
			return (function * () {
				for(const line of application.Engine.Resource.GetFileLines(f.path, f.encoding)) yield _parseLine(line);
			})();
		},
		set fields (values) {
			return application.Engine.Resource.SetFileLines(this.path, values.map(Make.string), this.encoding);
		},
		get lines() {
			const f = this;
			return (function * () {
				for(const line of application.Engine.Resource.GetFileLines(f.path, f.encoding)) yield line;
			})();
		},
		set lines (values) {
			return application.Engine.Resource.SetFileLines(this.path, values.map(o => Make.string(o)), this.encoding);
		},
		line: function(index, object = null) {
			if(object === null) return application.Engine.Resource.GetFileLine(this.path, index, this.encoding);
			else return application.Engine.Resource.SetFileLine(this.path, index, Make.string(object), this.encoding);
		},
		field: function(index, object = null) {
			if(object === null) return _parseLine(this.line(index));
			else return this.line(index, object);
		},
		create: function (object = "") {
			if(!application.Engine.Resource.Exists(this.path)) application.Engine.Resource.SetFileText(this.path, Make.string(object));
			return application.Engine.Resource.Exists(this.path);
		},
		clear: function (object = null) {
			return application.Engine.Resource.SetFileText(this.path, "", this.encoding);
		},
		read: function () {
			return application.Engine.Resource.GetFileText(this.path, this.encoding);
		},
		write: function (object = null) {
			return application.Engine.Resource.SetFileText(this.path, Make.string(object), this.encoding);
		},
		prepend: function (object = null) {
			if(this.sequential) return application.Engine.Resource.PrependFileLine(this.path, Make.string(object), this.encoding);
			else return application.Engine.Resource.PrependFileText(this.path, Make.string(object), this.encoding);
		},
		append: function (object = null) {
			if(this.sequential) return application.Engine.Resource.AppendFileLine(this.path, Make.string(object), this.encoding);
			else return application.Engine.Resource.AppendFileText(this.path, Make.string(object), this.encoding);
		},
		// String and RegExp queries search raw lines. XPath and object graphs search parsed JSON objects.
		find: function (query) {
			for(const result of this.search(query)) return result;
			return null;
		},
		first: function (query = null) {
			for(const result of this.search(query)) return result;
			return null;
		},
		last: function (query = null) {
			let result = null;
			for(const item of this.search(query)) result = item;
			return result;
		},
		search: function * (query = null, foreward = true) {
			const textQuery = ((typeof query === "string" && !Accept.isXPath(query)) || query instanceof RegExp);
			const lines = foreward ? 
				application.Engine.Resource.GetFileLines(this.path, this.encoding) :
				Make.array(application.Engine.Resource.GetFileLines(this.path, this.encoding)).reverse();
			for(const line of lines) {
				if(query === null || query === undefined) {
					yield line;
					continue;
				}
				const value = textQuery ? line : _parseLine(line);
				if(Accept(value, query)) yield value;
			}
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
		delete: () => application.Engine.Resource.Delete(this.path),
		entity: () => application.Engine.Resource.Entity(this.path),
		toString: () => this.text, 
		get text () {
			return application.Engine.Resource.GetFileText(this.path, this.encoding);
		},
		set text (text) {
			return application.Engine.Resource.SetFileText(this.path, text, this.encoding);
		},
	};
}
