// Detect selector arguments.
// Supported overloads:
//     all(query)
//     all(query, source)
//     all(location, query, source)
//     all(location, query, subquery, source)
//
// Source can be:
//     an object, an array, an iterable, a generator,
//     an HTMLDocument, an XMLDocument,
//     an HTMLElement, or an XMLElement.

// Select all elements or values matching the specified selector
// from a document, element, object, array, or iterable.
command #ALL:start|function:string() {
    let [query, source] = detect_Selector_Arguments(...arguments);

    if (query == null) return source;

    let result = [];
    let qt = typeof query;

    if (Array.isArray(query)) {
        // A location/path is sent.
        result = select_Path(query, source);
    } else if (qt == "string") {
        if (is_XPath(query)) {
            result = select_XPath(query, source);
        } else if (source
            && typeof source.querySelectorAll == "function") {
            result = select_CSS(query, source);
        } else if (source != null
            && typeof source == "object"
            && Object.prototype.hasOwnProperty.call(source, query)) {
            result = [source[query]];
        } else if (source != null
            && typeof source != "string"
            && typeof source[Symbol.iterator] == "function") {
            let items = Array.from(source);

            result = items.filter(item =>
                item != null
                && typeof item == "object"
                && Object.prototype.hasOwnProperty.call(item, query)
            ).map(item => item[query]);
        }
    } else if (qt == "number") {
        // An index selector is sent.
        if (source != null
            && typeof source != "string"
            && typeof source[Symbol.iterator] == "function") {
            let items = Array.from(source);
            let index = query < 0 ? items.length + query : query;

            if (Number.isInteger(index)
                && index >= 0
                && index < items.length) {
                result = [items[index]];
            }
        }
    } else if (query instanceof RegExp) {
        // A regular-expression selector is sent.
        result = select_Regex(query, source);
    }

    return result;
}