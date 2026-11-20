use deal/make;
use deal/data/temporary;
use deal/select/accept;


// All resource related commands
command File (path = null, encoding = null, sequential = true) {

	function _resource(method, args, encodingValue) {
		const resource = application.Engine.Resource;
		return encodingValue == null
			? resource[method](...args)
			: resource[method](...args, encodingValue);
	}

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
			return Make.array(_resource("GetFileLines", [this.path], this.encoding)).length;
		},
		set count (length) {
			let cnt = this.count;
			if(length > cnt) while(length-- > cnt) _resource("AppendFileLine", [this.path, ""], this.encoding);
			else return _resource("SetFileLines", [this.path, Make.array(_resource("GetFileLines", [this.path], this.encoding)).slice(0, length)], this.encoding);
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
		get text () {
			return _resource("GetFileText", [this.path], this.encoding);
		},
		set text (text) {
			return _resource("SetFileText", [this.path, text], this.encoding);
		},
		get contents () {
			return application.Engine.Resource.GetFileContents(this.path);
		},
		set contents (contents) {
			return application.Engine.Resource.SetFileContents(this.path, contents);
		},
		get lines() {
			const file = this;
			return (function * () {
				for(const line of _resource("GetFileLines", [file.path], file.encoding)) yield line;
			})();
		},
		set lines (values) {
			return _resource("SetFileLines", [this.path, values.map(o => Make.string(o))], this.encoding);
		},
		get fields () {
			const file = this;
			return (function * () {
				for(const line of _resource("GetFileLines", [file.path], file.encoding)) yield _parseLine(line);
			})();
		},
		set fields (values) {
			return _resource("SetFileLines", [this.path, values.map(Make.string)], this.encoding);
		},
		line: function(index, object = null) {
			if(object === null) return _resource("GetFileLine", [this.path, index], this.encoding);
			else return _resource("SetFileLine", [this.path, index, Make.string(object)], this.encoding);
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
			return _resource("SetFileText", [this.path, ""], this.encoding);
		},
		read: function () {
			return _resource("GetFileText", [this.path], this.encoding);
		},
		write: function (object = null) {
			return _resource("SetFileText", [this.path, Make.string(object)], this.encoding);
		},
		prepend: function (object = null) {
			if(this.sequential) return _resource("PrependFileLine", [this.path, Make.string(object)], this.encoding);
			else return _resource("PrependFileText", [this.path, Make.string(object)], this.encoding);
		},
		append: function (object = null) {
			if(this.sequential) return _resource("AppendFileLine", [this.path, Make.string(object)], this.encoding);
			else return _resource("AppendFileText", [this.path, Make.string(object)], this.encoding);
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
				_resource("GetFileLines", [this.path], this.encoding) :
				Make.array(_resource("GetFileLines", [this.path], this.encoding)).reverse();
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
		delete: function () {
			return application.Engine.Resource.Delete(this.path);
		},
		entity: function () {
			return application.Engine.Resource.Entity(this.path);
		},
		toString: ()=>this.text
	};
}
