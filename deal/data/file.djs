use deal/make/string;
use deal/data/temporary;

function File_parseLine(line) {
	try {
		const value = JSON.parse(line);
		if(value !== null && typeof value === "object" && !Array.isArray(value)) return value;
	} catch(error) {}
	return line;
}

function File_text(value) {
	if(typeof value === "string") return value;
	try { return JSON.stringify(value); }
	catch(error) { return Make.string(value); }
}

function File_isXPath(query) {
	return typeof query === "string" && /^(?:\/|\.\/|\.\.\/|@|(?:count|boolean|contains|starts-with|string|number|not|normalize-space|name|local-name)\s*\()/i.test(query.trim());
}

function File_matches(value, query) {
	if(query === null || query === undefined) return true;
	if(query instanceof RegExp) {
		query.lastIndex = 0;
		return query.test(File_text(value));
	}
	if(typeof query === "string") {
		if(File_isXPath(query)) return application.Engine.XmlQuery.Matches(value, query);
		return File_text(value).includes(query);
	}
	if(typeof query === "object" && !Array.isArray(query)) {
		for(const key of Object.keys(query)) {
			if(value === null || typeof value !== "object" || !Object.prototype.hasOwnProperty.call(value, key) || !File_matches(value[key], query[key]))
				return false;
		}
		return true;
	}
	return value === query;
}

function File_isTextQuery(query) {
	return (typeof query === "string" && !File_isXPath(query)) || query instanceof RegExp;
}

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
			for(const line of this.lines(encoding))
				yield File_parseLine(line);
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
		// String and RegExp queries search raw lines. XPath and object graphs search parsed JSON objects.
		find: function (query, encoding = null) {
			return this.first(query, encoding);
		},
		first: function (query = null, encoding = null) {
			for(const result of this.search(query, encoding)) return result;
			return null;
		},
		last: function (query = null, encoding = null) {
			let result = null;
			for(const item of this.search(query, encoding)) result = item;
			return result;
		},
		search: function * (query = null, encoding = null) {
			const textQuery = File_isTextQuery(query);
			for(const line of this.lines(encoding)) {
				if(query === null || query === undefined) {
					yield line;
					continue;
				}
				const value = textQuery ? line : File_parseLine(line);
				if(File_matches(value, query)) yield value;
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
		exists: function () {
			return application.Engine.Resource.Exists(this.path);
		},
		entity: function () {
			return application.Engine.Resource.Entity(this.path);
		}
	};
}
