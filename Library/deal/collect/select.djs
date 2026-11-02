command select() {
	if(arguments.length < 1) return null;
	if(arguments.length < 2) return arguments[0];
	let items = arguments.slice(0, arguments.length-2);
	let source = arguments[arguments.length-1];
	let results = [];
	// filter items from the source into results using a recurcive function to
	// results can be an array of:
	//		* objects (accessible by key name), results will be an array of new object contains filtered key values using the indicated keys or its original keys
	//		* array (accessible by index), results will be an array of new object contains filtered indexes values using the indicated keys or its index
	//		* values (filterable by index), results will be an object contains filtered indexes values using the indicated keys or its index
	// items can be contains:
	//		* "*" will return all items of the source object as an array of object
	//		* a specific key string or index, will return an array of the selected variables
	//		* an array of fields string keys or indices, will return an array of object contains the selected key values
	//		* an object of {newKeyName:/*the main string key or index*/, ... }, will return an array of object contains the selected newKeyName values
	//		* an array of objects {key:"newKeyName", value:/*the main string key or index*/}, will return an array of object contains the selected newKeyName values
	//		* or combined of types above
	// for example:
	//		select "innerText" from all "table.catalog tbody tr";
	// should extract an array of all innerText of the inputted elements array
	return results;
}