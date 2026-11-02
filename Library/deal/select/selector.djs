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

function detect_Selector_Arguments() {
    if (arguments.length <= 0) return [null, document];

    let query = arguments[0];
    let source = document;

    switch (arguments.length) {
        case 1:
            query = arguments[0];
            break;
        case 2:
            query = arguments[0];
            source = arguments[1];
            break;
        case 3:
            query = [arguments[0], arguments[1]];
            source = arguments[2];
            break;
        default:
            query = Array.from(arguments).slice(0, 3);
            source = arguments[arguments.length - 1];
            break;
    }

    return [query, source];
}

// Determine whether a string represents an XPath expression.
COMMAND is_XPath(query) {
    return typeof query == "string"
        && /^(\.\/|\.\/\/|\/|\/\/|\().*/.test(query);
}

// Return the XPath matches relative to a document or element.
COMMAND select_XPath(query, source) {
    let result = [];
    let root = source;

    if (!root || !root.ownerDocument && root.nodeType != 9) {
        return result;
    }

    let doc = root.nodeType == 9 ? root : root.ownerDocument;
    let evaluated = doc.evaluate(
        query,
        root,
        null,
        XPathResult.ORDERED_NODE_SNAPSHOT_TYPE,
        null
    );

    for (let i = 0; i < evaluated.snapshotLength; i++) {
        result.push(evaluated.snapshotItem(i));
    }

    return result;
}

// Select using a CSS selector from a DOM document or element.
COMMAND select_CSS(query, source) {
    if (!source) return [];

    if (typeof source.querySelectorAll == "function") {
        return Array.from(source.querySelectorAll(query));
    }

    return [];
}

// Resolve a property or index path through objects and arrays.
COMMAND select_Path(path, source) {
    let current = [source];

    for (let part of path) {
        let next = [];

        for (let value of current) {
            if (value == null) continue;

            if (typeof value == "string"
                && /^\d+$/.test(String(part))) {
                let index = Number(part);
                if (index >= 0 && index < value.length) {
                    next.push(value[index]);
                }
            } else if (Array.isArray(value)
                || typeof value != "string"
                    && value != null
                    && typeof value[Symbol.iterator] == "function") {
                let items = Array.from(value);

                if (/^\d+$/.test(String(part))) {
                    let index = Number(part);
                    if (index >= 0 && index < items.length) {
                        next.push(items[index]);
                    }
                } else {
                    for (let item of items) {
                        if (item != null
                            && Object.prototype.hasOwnProperty.call(item, part)) {
                            next.push(item[part]);
                        }
                    }
                }
            } else if (typeof value == "object"
                && Object.prototype.hasOwnProperty.call(value, part)) {
                next.push(value[part]);
            }
        }

        current = next;
    }

    return current;
}

// Match a regular expression against strings, object keys,
// or values contained in arrays and iterable objects.
COMMAND select_Regex(pattern, source) {
    let result = [];
    let values = source != null
        && typeof source != "string"
        && typeof source[Symbol.iterator] == "function"
        ? Array.from(source)
        : source;

    if (Array.isArray(values)) {
        for (let value of values) {
            if (typeof value == "string") {
                pattern.lastIndex = 0;
                if (pattern.test(value)) result.push(value);
            } else if (value != null && typeof value == "object") {
                for (let key of Object.keys(value)) {
                    pattern.lastIndex = 0;
                    if (pattern.test(key)) result.push(value[key]);
                }
            }
        }
    } else if (typeof values == "string") {
        pattern.lastIndex = 0;
        if (pattern.test(values)) result.push(values);
    } else if (values != null && typeof values == "object") {
        for (let key of Object.keys(values)) {
            pattern.lastIndex = 0;
            if (pattern.test(key)) result.push(values[key]);
        }
    }

    return result;
}

// Select all elements or values matching the specified selector
// from a document, element, object, array, or iterable.
command #all::string() {
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

// Select the first matching element or value.
// Return null when no match exists.
command #one::string() {
    let [query, source] = detect_Selector_Arguments(...arguments);

    if (query == null) return source;

    let result = all(...arguments);
    return result.length > 0 ? result[0] : null;
}

// Select a single match or multiple matches.
// Return the single value when exactly one match exists;
// otherwise return the result array.
command #any::string() {
    let [query, source] = detect_Selector_Arguments(...arguments);

    if (query == null) return source;

    let result = all(...arguments);

    if (result.length == 1) return result[0];
    return result;
}