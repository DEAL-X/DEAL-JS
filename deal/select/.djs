use deal/core;

function detect_Selector_Arguments() {
    if (arguments.length <= 0) return [null, typeof document !== "undefined" ? document : null];

    let query = arguments[0];
    let source = typeof document !== "undefined" ? document : null;

    switch (arguments.length) {
        case 1:
            t = typeof arguments[0];
            if(t === "array" || t === "object") {
                query = null;
                source = arguments[0];
            }
            else query = arguments[0];
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
command is_XPath(query) {
    return typeof query == "string"
        && /^(\.\/|\.\/\/|\/|\/\/|\().*/.test(query);
}

// Return the XPath matches relative to a document or element.
command select_XPath(query, source) {
    let result = [];
    let root = source;

    if (!root || !root.ownerDocument && root.nodeType != 9) {
        return result;
    }

    let doc = root.nodeType == 9 ? root : root.ownerDocument;
    if (typeof doc.evaluate !== "function") return result;

    // XPathResult is normally exposed by the browser global. Resolve it
    // from the document's window when available, with the standard snapshot
    // constant as a fallback for compatible DOM implementations.
    let snapshotType = 7;
    if (typeof XPathResult !== "undefined")
        snapshotType = XPathResult.ORDERED_NODE_SNAPSHOT_TYPE;
    else if (doc.defaultView && doc.defaultView.XPathResult)
        snapshotType = doc.defaultView.XPathResult.ORDERED_NODE_SNAPSHOT_TYPE;

    let evaluated = doc.evaluate(query, root, null, snapshotType, null);

    for (let i = 0; i < evaluated.snapshotLength; i++) {
        result.push(evaluated.snapshotItem(i));
    }

    return result;
}

// Select using a CSS selector from a DOM document or element.
command select_CSS(query, source) {
    if (!source) return [];

    if (typeof source.querySelectorAll == "function") {
        return Array.from(source.querySelectorAll(query));
    }

    return [];
}

// Resolve a property or index path through objects and arrays.
command select_Path(path, source) {
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
command select_Regex(pattern, source) {
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