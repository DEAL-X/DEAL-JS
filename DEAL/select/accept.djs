use deal/core;

command #Accept:function = function (value, query) {
	if(query === undefined) return value !== undefined;
	if(query === null) return value === query;
	if(query instanceof RegExp) {
		query.lastIndex = 0;
		return query.test(Make.string(value));
	}

	let qt = typeof query;
	if(qt === "string") {
		if(Accept.isXPath(query)) return application.Engine.XmlQuery.Matches(value, query);
		return Make.string(value).includes(query);
	}

	if(qt === "function") return query(value);
		
	if(qt === "object" && value !== null && "object" === typeof value) {
		if(Array.isArray(query)) { // query is array
			if(Array.isArray(value)) {
				for(const ind of query)
					if(value[ind] === undefined || !accept(value[ind], query[ind]))
						return false;
				return true;
			}
			else {
				let keys = Object.keys(value);
				for(const ind of query)
					if(keys[ind] === undefined || !Object.prototype.hasOwnProperty.call(value, keys[ind]) || !accept(value[keys[ind]], query[ind]))
						return false;
				return true;
			}
		}
		else { // query is object
			if(Array.isArray(value)) {
				for(const key of query)
					if(accept(value, query[key]))
						return key;
				return false;
			}
			else {
				for(const key in query)
					if(!Object.prototype.hasOwnProperty.call(value, key) || !accept(value[key], query[key]))
						return false;
				return true;
			}
		}
	}
	return value === query;
}

#Accept.isXPath:function = function (object) {
	return typeof object === "string" && /^(?:\/|\.\/|\.\.\/|@|(?:count|boolean|contains|starts-with|string|number|not|normalize-space|name|local-name)\s*\()/i.test(object.trim());
}
#Accept.isArray:function = function (object) {
	return Array.isArray(object);
}