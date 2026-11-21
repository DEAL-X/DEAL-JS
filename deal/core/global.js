var isNull = function (obj) {
	return obj === null;
};

// const isNaN = function (obj) {
// 	return typeof obj != "string" && obj + "" === "NaN";
// }

var isEmpty = function (obj) {
	return obj === null || obj === "" || obj == {} || obj == [];
};

var isDefined = function (obj) {
	return !isUndefined(obj);
};

var isUndefined = function (obj) {
	return obj === undefined;
};

var isHollow = function (obj) {
	return obj === undefined || isNaN(obj) || isEmpty(obj) || (obj + "").trim() === "";
};

var isSet = function (obj) {
	return !isUnset(obj);
};

var isUnset = function (obj) {
	return obj === undefined || obj === null || isNaN(obj);
};

var isString = function (obj) {
	return typeof obj === 'string';
};

var isText = function (obj) {
	return isString(obj) && !obj.trimStart().startsWith("<");
};

var isObject = function (obj) {
	return typeof obj === 'object';
};

var isNumber = function (obj) {
	return parseFloat(obj) + "" === obj + "";
};

var isInt = function (n) {
	return Number(n) === n && n % 1 === 0;
};

var isFloat = function (n) {
	return Number(n) === n && n % 1 !== 0;
};

var isArray = function (obj, dim = 1) {
	if (isHollow(obj)) return false;
	let objt = typeof obj;
	if (objt === 'string') return false;
	if (Array.isArray(obj)) return --dim === 0 || isArray(obj[0], dim);
	if (objt === 'object' && (obj["count"] !== undefined || obj["Length"] !== undefined))
		return --dim === 0 || isArray(obj[0], dim);
	return false;
};

var isIterable = function (obj) {
	// checks for null and undefined
	if (obj == null) return false;
	return typeof obj[Symbol.iterator] === 'function';
}

var apply = function (func, args, maxArgsNum = 1) {
	if (isArray(args))
		if (count(args) > maxArgsNum)
			return each(args, item => apply(func, item, maxArgsNum));
		else return func(...args);
	else return func(args);
};

var between = function () {
	for (const val of arguments)
		if (isSet(val)) return val;
	return arguments[arguments.length - 1];
};

var expand = function* () {
	for (const val of arguments)
		if (Array.isArray(val))
			for (const v of expand(...val))
				yield v;
		else if (isArray(val))
			for (const v of expand(each(val)))
				yield v;
		else yield val;
};

var each = function (arr, func = (o, i) => o) {
	if (isArray(arr))
		return Array.from(
			(function* () {
				let i = 0;
				for (const item of arr)
					yield func(item, i++);
			})()
		);
	else return [func(arr, 0)];
};

var eachIn = function (arr, func = (o, i) => o) {
	return Array.from(
		(function* () {
			let i = 0;
			for (const item in arr)
				yield func(item, i++);
		})()
	);
};

var where = function (arr, condition = (o, i) => true, func = null) {
	if (func === null) {
		if (isArray(arr))
			return Array.from(
				(function* () {
					let i = 0;
					for (const item of arr)
						if (condition(item, i++)) yield item;
				})()
			);
		else if (condition(arr, 0)) return [arr];
	} else {
		if (isArray(arr))
			return Array.from(
				(function* () {
					let i = 0;
					for (const item of arr)
						if (condition(item, i)) yield func(item, i++);
						else i++;
				})()
			);
		else if (condition(arr, 0)) return [func(arr, 0)];
	}
	return [];
};

var loop = function (i, len = null, func = i => i) {
	if (len == null) {
		len = i;
		i = 0;
	}
	return Array.from(
		(function* () {
			for (i; i < len; i++)
				yield func(i);
		})()
	);
};

var count = function () {
	return each(arguments).length;
};

var first = function () {
	if (arguments.length == 0) return null;
	if (arguments.length == 1)
		if (isArray(arguments[0])) return arguments[0][0];
		else return arguments[0];
	if (arguments.length > 1) return arguments[0];
}

var last = function () {
	if (arguments.length == 0) return null;
	if (arguments.length == 1)
		if (isArray(arguments[0])) return arguments[0][arguments[0].length - 1];
		else return arguments[0];
	if (arguments.length > 1) return arguments[0];
}

var sort = function () {
	if (arguments.length > 2) {
		let func = arguments[2];
		return each(arguments).sort((a, b) => func(a) < func(b) ? -1 : func(a) > func(b));
	}
	return each(arguments).sort((a, b) => a < b ? -1 : a > b);
};

var distinct = function () {
	if (arguments.length > 2) return each(arguments).filter((v, i, a) => arguments[2](v, i, a));
	return each(arguments).filter((v, i, a) => a.indexOf(v) === i);
};

var wait = function (milliseconds) {
	const date = Date.now();
	let currentDate = null;
	do {
		currentDate = Date.now();
	} while (currentDate - date < milliseconds);
};

var copy = function (val) {
	navigator.clipboard.writeText(val);
};
var paste = function () {
	var val = null;
	navigator.clipboard.readText().then(v => val = v);
	return val;
};
var copyFrom = function (id) {
	var input = document.getElementById(id);
	const color = input.style.color;
	const bcolor = input.style.backgroundColor;
	input.style.color = bcolor;
	input.style.backgroundColor = color;
	input.select();
	input.setSelectionRange(0, 999999999);
	navigator.clipboard.writeText(input.value);
	input.style.color = color;
	input.style.backgroundColor = bcolor;
};
var pasteInto = function (id) {
	var input = document.getElementById(id);
	const color = input.style.color;
	const bcolor = input.style.backgroundColor;
	input.style.color = bcolor;
	input.style.backgroundColor = color;
	input.select();
	input.setSelectionRange(0, 999999999);
	navigator.clipboard.readText().then(v => input.value = v);
	input.style.color = color;
	input.style.backgroundColor = bcolor;
};
var clearText = function (id, focus = false) {
	var input = document.getElementById(id);
	const color = input.style.color;
	const bcolor = input.style.backgroundColor;
	input.style.color = bcolor;
	input.style.backgroundColor = color;
	if (isUndefined(input.value))
		input.innerText = "";
	else input.value = "";
	if (focus) input.focus();
	input.style.color = color;
	input.style.backgroundColor = bcolor;
};
var isVisible = (element, partiallyVisible = true) => {
	const { top, left, bottom, right } = element.getBoundingClientRect();
	const { innerHeight, innerWidth } = window;
	return partiallyVisible
		? ((top > 0 && top < innerHeight) ||
			(bottom > 0 && bottom < innerHeight)) &&
		((left > 0 && left < innerWidth) || (right > 0 && right < innerWidth))
		: top >= 0 && left >= 0 && bottom <= innerHeight && right <= innerWidth;
};
var isFocused = (element) => document.activeElement === element;

// Function to generate a unique XPath selector for an element
var getXPath = (element) => {
	let tagName = element.tagName;
	if (!tagName) return null;
	tagName = tagName.toLowerCase();
	if (tagName === 'html') return 'html';
	if (element.parentElement) return `${getXPath(element.parentElement)}/${tagName}[${(Array.from(element.parentElement.children).indexOf(element))}]`;
	return tagName;
}
// Function to generate a unique Path selector for an element
var getFullQuery = (element) => {
	let tagName = element.tagName;
	if (!tagName) return null;
	tagName = tagName.toLowerCase();
	if (tagName === 'html') return 'html';
	if (element.parentElement) return `${getFullQuery(element.parentElement)}>${tagName}:nth-child(${(Array.from(element.parentElement.children).indexOf(element) + 1)})`;
	return tagName;
}
// Function to generate a unique CSS selector for an element
var getQuery = (element) => {
	if (element.id) return "#" + element.id;
	let tagName = element.tagName;
	if (!tagName) return null;
	tagName = tagName.toLowerCase();
	if (tagName === 'html') return 'html';
	if (element.parentElement) return `${getQuery(element.parentElement)}>${tagName}:nth-child(${(Array.from(element.parentElement.children).indexOf(element) + 1)})`;
	return tagName;
}
