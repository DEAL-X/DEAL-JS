let isNull = function (obj) {
	return obj === null;
};

// const isNaN = function (obj) {
// 	return typeof obj != "string" && obj + "" === "NaN";
// }

let isEmpty = function (obj) {
	return obj === null || obj === "" || obj == {} || obj == [];
};

let isDefined = function (obj) {
	return !isUndefined(obj);
};

let isUndefined = function (obj) {
	return obj === undefined;
};

let isHollow = function (obj) {
	return obj === undefined || isNaN(obj) || isEmpty(obj) || (obj + "").trim() === "";
};

let isSet = function (obj) {
	return !isUnset(obj);
};

let isUnset = function (obj) {
	return obj === undefined || obj === null || isNaN(obj);
};

let isString = function (obj) {
	return typeof obj === 'string';
};

let isText = function (obj) {
	return isString(obj) && !obj.trimStart().startsWith("<");
};

let isObject = function (obj) {
	return typeof obj === 'object';
};

let isNumber = function (obj) {
	return parseFloat(obj) + "" === obj + "";
};

let isInt = function (n) {
	return Number(n) === n && n % 1 === 0;
};

let isFloat = function (n) {
	return Number(n) === n && n % 1 !== 0;
};

let isArray = function (obj, dim = 1) {
	if (isHollow(obj)) return false;
	let objt = typeof obj;
	if (objt === 'string') return false;
	if (Array.isArray(obj)) return --dim === 0 || isArray(obj[0], dim);
	if (objt === 'object' && (obj["count"] !== undefined || obj["Length"] !== undefined))
		return --dim === 0 || isArray(obj[0], dim);
	return false;
};

let isIterable = function (obj) {
	// checks for null and undefined
	if (obj == null) return false;
	return typeof obj[Symbol.iterator] === 'function';
}

let apply = function (func, args, maxArgsNum = 1) {
	if (isArray(args))
		if (count(args) > maxArgsNum)
			return each(args, item => apply(func, item, maxArgsNum));
		else return func.apply(null, args);
	else return func(args);
};

let between = function () {
	for (const val of arguments)
		if (isSet(val)) return val;
	return arguments[arguments.length - 1];
};

let expand = function* () {
	for (const val of arguments)
		if (Array.isArray(val))
			for (const v of expand.apply(null, val))
				yield v;
		else if (isArray(val))
			for (const v of expand(each(val)))
				yield v;
		else yield val;
};

let each = function (arr, func = (o, i) => o) {
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

let eachIn = function (arr, func = (o, i) => o) {
	return Array.from(
		(function* () {
			let i = 0;
			for (const item in arr)
				yield func(item, i++);
		})()
	);
};

let where = function (arr, condition = (o, i) => true, func = null) {
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

let any = function (arr, condition = (o, i) => true, func = null) {
	if (func === null) {
		if (isArray(arr))
			for (const item of arr)
				if (condition(item, i++))
					return item;
				else if (condition(arr, 0)) return arr;
	} else {
		if (isArray(arr))
			for (const item of arr)
				if (condition(item, i))
					return func(item, i++);
				else i++;
		else if (condition(arr, 0)) return func(arr, 0);
	}
};

let select = function (arr, func = (o, i) => o) {
	if (isArray(arr)) return each(arr, func);
	else return eachIn(arr, func);
};

let loop = function (i, len = null, func = i => i) {
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

let count = function () {
	return each.apply(null, arguments).length;
};

let first = function () {
	if (arguments.length == 0) return null;
	if (arguments.length == 1)
		if (isArray(arguments[0])) return arguments[0][0];
		else return arguments[0];
	if (arguments.length > 1) return arguments[0];
}

let last = function () {
	if (arguments.length == 0) return null;
	if (arguments.length == 1)
		if (isArray(arguments[0])) return arguments[0][arguments[0].length - 1];
		else return arguments[0];
	if (arguments.length > 1) return arguments[0];
}

let sort = function () {
	if (arguments.length > 2) {
		let func = arguments[2];
		return each.apply(null, arguments).sort((a, b) => func(a) < func(b) ? -1 : func(a) > func(b));
	}
	return each.apply(null, arguments).sort((a, b) => a < b ? -1 : a > b);
};

let distinct = function () {
	if (arguments.length > 2) return each.apply(null, arguments).filter((v, i, a) => arguments[2](v, i, a));
	return each.apply(null, arguments).filter((v, i, a) => a.indexOf(v) === i);
};

let wait = function (milliseconds) {
	const date = Date.now();
	let currentDate = null;
	do {
		currentDate = Date.now();
	} while (currentDate - date < milliseconds);
};

let copy = function (val) {
	navigator.clipboard.writeText(val);
};
let paste = function () {
	var val = null;
	navigator.clipboard.readText().then(v => val = v);
	return val;
};
let copyFrom = function (id) {
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
let pasteInto = function (id) {
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
let clearText = function (id, focus = false) {
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
let isVisible = (element, partiallyVisible = true) => {
	const { top, left, bottom, right } = element.getBoundingClientRect();
	const { innerHeight, innerWidth } = window;
	return partiallyVisible
		? ((top > 0 && top < innerHeight) ||
			(bottom > 0 && bottom < innerHeight)) &&
		((left > 0 && left < innerWidth) || (right > 0 && right < innerWidth))
		: top >= 0 && left >= 0 && bottom <= innerHeight && right <= innerWidth;
};
let isFocused = (element) => document.activeElement === element;

// Function to generate a unique XPath selector for an element
let getXPath = (element) => {
	let tagName = element.tagName;
	if (!tagName) return null;
	tagName = tagName.toLowerCase();
	if (tagName === 'html') return 'html';
	if (element.parentElement) return `${getXPath(element.parentElement)}/${tagName}[${(Array.from(element.parentElement.children).indexOf(element))}]`;
	return tagName;
}
// Function to generate a unique Path selector for an element
let getFullQuery = (element) => {
	let tagName = element.tagName;
	if (!tagName) return null;
	tagName = tagName.toLowerCase();
	if (tagName === 'html') return 'html';
	if (element.parentElement) return `${getFullQuery(element.parentElement)}>${tagName}:nth-child(${(Array.from(element.parentElement.children).indexOf(element) + 1)})`;
	return tagName;
}
// Function to generate a unique CSS selector for an element
let getQuery = (element) => {
	if (element.id) return "#" + element.id;
	let tagName = element.tagName;
	if (!tagName) return null;
	tagName = tagName.toLowerCase();
	if (tagName === 'html') return 'html';
	if (element.parentElement) return `${getQuery(element.parentElement)}>${tagName}:nth-child(${(Array.from(element.parentElement.children).indexOf(element) + 1)})`;
	return tagName;
}
