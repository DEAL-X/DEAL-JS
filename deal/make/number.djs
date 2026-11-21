command Make.float (val = null, defaultVal = 0) {
	return parseFloat(val ?? defaultVal) ?? defaultVal;
};

command Make.int (val = null, defaultVal = 0) {
	return parseInt(val ?? defaultVal) ?? defaultVal;
};


function tryParseFloat (val = null) {
	return isEmpty(val) ? 0 : between(parseFloat(val), val);
}

function tryParseInt (val = null) {
	return isEmpty(val) ? 0 : between(parseInt(val), val);
}
