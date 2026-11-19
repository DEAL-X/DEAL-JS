command Make.float (val = null, def = 0) {
	return parseFloat(val ?? def) ?? def;
};

command Make.int (val = null, def = 0) {
	return parseInt(val ?? def) ?? def;
};


function tryParseFloat (val = null) {
	return isEmpty(val) ? 0 : between(parseFloat(val), val);
}

function tryParseInt (val = null) {
	return isEmpty(val) ? 0 : between(parseInt(val), val);
}
