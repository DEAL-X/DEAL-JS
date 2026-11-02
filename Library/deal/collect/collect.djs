command collect() {
	if(arguments.length < 1) return null;
	let results = [];
	// will convert arguments into results using a recurcive function to
	// results can be an array of:
	//		* a new object contains combined key values recieved by arguments if which one of arguments be in template {key:"the key name", value: /*the value*/}
	//		* a new object contains combined key values recieved by arguments, duplicate key will replace by latest value, non-object arguments will add to the object by new key name (combined the word "COLUMN" and its position, for example "COLUMN1")
	return results;
}