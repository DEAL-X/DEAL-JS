use deal/make/string;

// The normalization method could contains
//		* one of NFC, NFD, NFKC, N for the normalization form
//		* s: trim start
//		* S: trim end
//		* b: trim between words
//		* B: trim both sides
//		* t: strict trim everywhere
//		* T: trim everywhere
//		* w: remove all word characters
//		* W: remove all non-word characters
//		* v: remove all digit characters
//		* V: remove all non-digit characters
//		* l: lower case letters
//		* L: upper case letters
//		* u: encode URI component
//		* U: decode URI component
Make.normal = function(text = null, method = "NFKCTB", replacement = "") {
	if(text === null) return text;
	umethod = method.toUpperCase();

	let form = "";
	if(umethod.includes("N")) form += "N";
	if(umethod.includes("F")) form += "F";
	if(umethod.includes("K")) form += "K";
	else if(method.includes("D")) form += "D";
	if(umethod.includes("C")) form += "C";
	text = Make.string(text).normalize(form);

	if(method.includes("s")) text = text.trimStart();
	if(method.includes("S")) text = text.trimEnd();
	if(method.includes("b")) text = text.replace(/\s+/g, "");
	if(method.includes("B")) text = text.trim();
	if(method.includes("t")) text = text.replace(/\s+/g, replacement);
	if(method.includes("T")) text = text.replace(/(^[ \t]+)|([ \t]+$)/mg, replacement).replace(/[ \t]+/g, " ").replace(/[\r\n]+/g, "\n");
	
	if(method.includes("w")) text = text.replace(/\w+/gu, replacement);
	if(method.includes("W")) text = text.replace(/\W+/gu, replacement);
	if(method.includes("v")) text = text.replace(/\d+/gu, replacement);
	if(method.includes("V")) text = text.replace(/\D+/gu, replacement);

	if(method.includes("l")) text = text.toLowerCase();
	if(method.includes("L")) text = text.toUpperCase();
	
	if(method.includes("u")) text = encodeURIComponent(text);
	if(method.includes("U")) text = decodeURIComponent(text);

	return text;
};