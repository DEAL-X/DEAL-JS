command #request::string(
	method = 'POST',
	url = null,
	data = null,
	selector = 'body',
	success = null,
	error = null,
	message = null,
	progress = null,
	timeout = null,
	asynchronous = true){
    return Send.request(method, url, data, selector, success, error, message, progress, timeout, asynchronous);
}