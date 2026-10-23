class Send{
static _SENT = null;
static _RECEIVED = null;

static request (
	method = 'POST',
	url = null,
	data = null,
	selector = 'body',
	success = null,
	error = null,
	message = null,
	progress = null,
	timeout = null,
	asynchronous = true) {

	let noEffects = false;
	if (noEffects = !document.querySelector(selector)) selector = 'body';

	const btns = noEffects ? [] : document.querySelectorAll(selector + ' :is(button, [onclick], [ondblclick], input:is([type=button],[type=submit],[type=image],[type=reset]))');
	const elems = noEffects ? [] : document.querySelectorAll(selector);
	const opacity = noEffects ? 1 : document.querySelector(selector).style.opacity;
	url = url ? url : location.href;
	method = (method || "POST").toUpperCase();
	let isForm = false;
	let contentType = 'application/x-www-form-urlencoded; charset=utf-8';

	switch (method) {
		case "GET":
		case "POST":
		case "PATCH":
		case "PUT":
		case "DELETE":
		case "OPTIONS":
			break;
		default:
			if (data) {
				if (isForm = (data instanceof FormData))
					data.append("__METHOD", method);
				else if (Array.isArray(data) || (typeof data === 'object'))
					data.__METHOD = method;
				else if (isString(data))
					data += "&__METHOD=" + method;
			}
			else data = { __METHOD: method };
			method = "POST";
			break;
	}

	if (_SENT = data) {
		if (method === "GET") {
			url += (url.includes('?') ? '&' : '?') + new URLSearchParams(data).toString();
			contentType = false;
			data = null;
		}
		else if (isForm = data instanceof FormData)
			contentType = false;
		else if (Array.isArray(data) || (typeof data === 'object')) {
			data = JSON.stringify(data);
			contentType = 'application/json; charset=utf-8';
		}
	}

	success = success ?? function (result = null, err = null, xhr = null) {
		if (err) console.error(err);
		if (!isEmpty(result)) {
			result += "";
			result = ((typeof (result) === 'object') ? result.statusText : result) ?? 'Form submitted successfully!';

			// If server returned a full HTML document, replace the whole page instead of appending it.
			// This prevents browsers (notably Firefox) from appending full pages into containers
			// and causing "chained pages" behavior.
			// if (typeof result === 'string' && /<!doctype\s+html|<\s*html|<\s*body/i.test(result)) {
			// 	document.open();
			// 	document.write(result);
			// 	document.close();
			// 	return;
			// }
			if (!isEmpty(result)) {
				if (!result.match(/class\s*\=\s*("|')[\s\S]*\bresult\b[\s\S]*\1/)) result = Struct.result(result);
				if (isForm) _(selector).append(result);
				else _(selector).prepend(result);
			}
		}
	};

	error = error ?? function (result = null, err = null, xhr = null) {
		if (!isEmpty(result)) {
			result = ((typeof (result) === 'object') ? result.statusText : result) ?? 'There was a problem!';
			if (!isEmpty(result)) {
				result += "";
				if (!result.match(/class\s*\=\s*("|')[\s\S]*\bresult\b[\s\S]*\1/)) result = Struct.result(result);
				if (isForm) _(selector).append(result);
				else _(selector).prepend(result);
			}
		}
	};

	message = message ?? function (result = null, err = null, xhr = null) {
		if (!isEmpty(result)) {
			result = (typeof (result) === 'object') ? result.statusText : result;
			if (result) {
				result += "";
				if (isForm) _(selector).append(result);
				else _(selector).prepend(result);
			}
		}
	};

	progress = progress ?? function (result = null, err = null, xhr = null) {
		if (!isEmpty(result)) {
			result = (typeof (result) === 'object') ? result.statusText : result;
			if (result) {
				result += "";
				try { document.querySelector(selector + ' .result').remove(); } catch { }
				console.log(result);
			}
		}
	};

	const xhr = new XMLHttpRequest();
	xhr.open(method, url, asynchronous);

	// Set responseType to blob for file downloads to handle binary data properly
	if (method === 'FILE' || method === 'STREAM') xhr.responseType = 'blob';

	if (contentType) xhr.setRequestHeader('Content-Type', contentType);

	if (asynchronous && timeout) xhr.timeout = timeout || 60000;

	xhr.upload.addEventListener('progress', (xhr) => progress(xhr.response, null, xhr));

	xhr.onload = function () {
		btns.forEach(btn => btn.classList.remove('prevent-events'));
		elems.forEach(elem => elem.style.opacity = opacity);
		_RECEIVED = xhr.response;
		if (xhr.status >= 200 && xhr.status < 300)
			try {
				cDisposition = xhr.getResponseHeader('Content-Disposition');
				if (cDisposition && (/attachment/).test(cDisposition))
					if ((/filename=/).test(cDisposition))
						return downloadContent(xhr.response, cDisposition.split('filename=')[1], xhr.getResponseHeader('Content-Type'));
				let response = xhr.response;
				if (response) try { response = JSON.parse(response); } catch { }
				if (xhr.status <= 201 || xhr.status >= 290) success(response, null, xhr);
				else message(response, null, xhr);
			} catch (e) {
				err = "There was a problem on retrieving data! \n" + e.message;
				if (xhr.status <= 201 || xhr.status >= 290) success(null, err, xhr);
				else message(null, err, xhr);
			}
		else error(xhr.response, xhr.status, xhr);
	};

	xhr.onerror = function () {
		btns.forEach(btn => btn.classList.remove('prevent-events'));
		elems.forEach(elem => elem.style.opacity = opacity);
		error('Network Error' + (xhr.statusText ? " \n" + xhr.statusText : ""), xhr.status, xhr);
	};

	xhr.ontimeout = function () {
		btns.forEach(btn => btn.classList.remove('prevent-events'));
		elems.forEach(elem => elem.style.opacity = opacity);
		error('Timeout' + (xhr.statusText ? " \n" + xhr.statusText : ""), xhr.status ?? 'timeout', xhr);
	};

	xhr.onloadstart = function () {
		//try { document.querySelector(selector + ' .result').remove(); } catch { }
		btns.forEach(btn => btn.classList.add('prevent-events'));
		elems.forEach(elem => elem.style.opacity = '.5');
	};

	xhr.send(data || null);
	
	return xhr;
}
// Retry wrapper for transient network errors (ERR_NETWORK_CHANGED, Network Error, timeouts)
static withRetry (
	method = 'POST',
	url = null,
	data = null,
	selector = 'body',
	success = null,
	error = null,
	message = null,
	progress = null,
	timeout = null,
	retryOptions = null,
	asynchronous = true) {
	const opts = Object.assign({ retries: 3, backoff: 300 }, retryOptions || {});
	let attempt = 0;

	function isTransientError(resp, status) {
		const txt = (typeof resp === 'string') ? resp : (resp && resp.statusText ? resp.statusText : '');
		return status === 0 || status === 'timeout' || /network|ERR_NETWORK_CHANGED/i.test(txt) || /Network Error/i.test(txt);
	}

	function tryOnce(resolve, reject) {
		attempt++;
		const successN = function (res, st) {
			if (success) try { success(res, st); } catch (e) { /* ignore callback errors */ }
			resolve(res);
		};
		const errorN = function (res, st) {
			if (isTransientError(res, st) && attempt <= opts.retries) {
				const delay = opts.backoff * Math.pow(2, attempt - 1);
				setTimeout(() => tryOnce(resolve, reject), delay);
			} else {
				if (error) try { error(res, st); } catch (e) { /* ignore callback errors */ }
				reject({ response: res, status: st });
			}
		};

		// call underlying send (keeps original behavior)
		try {
			request(method, url, data, selector, successN, errorN, message, progress, timeout, asynchronous);
		} catch (e) {
			// if send itself throws, treat as transient and retry if possible
			if (attempt <= opts.retries) setTimeout(() => tryOnce(resolve, reject), opts.backoff * Math.pow(2, attempt - 1));
			else reject(e);
		}
	}

	// If caller provided callbacks (success/error) we behave callback-style and return undefined
	if (isDefined(success) && success !== null) {
		tryOnce(() => { }, () => { });
		return;
	}

	// Promise-style
	return new Promise((resolve, reject) => tryOnce(resolve, reject));
};
static getRequest (url = null, data = null, selector = 'body', success = null, error = null, message = null, progress = null, timeout = null) {
	return request('GET', url, data, selector, success, error, message, progress, timeout);
};
static postRequest (url = null, data = null, selector = 'body', success = null, error = null, message = null, progress = null, timeout = null) {
	return request('POST', url, data, selector, success, error, message, progress, timeout);
};
static putRequest (url = null, data = null, selector = 'body', success = null, error = null, message = null, progress = null, timeout = null) {
	return request('PUT', url, data, selector, success, error, message, progress, timeout);
};
static sendPatch (url = null, data = null, selector = 'body', success = null, error = null, message = null, progress = null, timeout = null) {
	return request('PATCH', url, data, selector, success, error, message, progress, timeout);
};
static deleteRequest (url = null, data = null, selector = 'body', success = null, error = null, message = null, progress = null, timeout = null) {
	return request('DELETE', url, data, selector, success, error, message, progress, timeout);
};
static fileRequest (url = null, data = null, selector = 'body', success = null, error = null, message = null, progress = null, timeout = null) {
	if (data) return request('FILE', url, data, selector, success, error, message, progress, timeout);
	else {
		input.setAttribute('type', 'file');
		res = null;
		input.onchange = evt => {
			for(const file of input.files)
				if (file) {
					const reader = new FileReader();
					reader.addEventListener('load', (event) => {
						res = request('FILE', url, encodeURIComponent(event.target.result), selector, success, error, message, progress, timeout);
					});
					reader.readAsDataURL(file);
				}
		}
		_(input).trigger('click');
		return res;
	}
};
static streamRequest (url = null, data = null, selector = 'body', success = null, error = null, message = null, progress = null, timeout = null) {
	return request('STREAM', url, data, selector, success, error, message, progress, timeout);
};
static internalRequest (url = null, data = null, selector = 'body', success = null, error = null, message = null, progress = null, timeout = null) {
	return request('INTERNAL', url, data, selector, success, error, message, progress, timeout);
};
static externalRequest (url = null, data = null, selector = 'body', success = null, error = null, message = null, progress = null, timeout = null) {
	return request('EXTERNAL', url, data, selector, success, error, message, progress, timeout);
};
}