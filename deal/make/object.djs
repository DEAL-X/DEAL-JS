// Explicit conversions for CLR object types that ClearScript cannot bind directly.
// JavaScript arrays and scalar values should otherwise be passed to .NET APIs directly.

function _clrHost() {
    if (typeof host === "undefined" || host == null ||
        typeof host.type !== "function" || typeof host.newObj !== "function")
        throw new Error("CLR object conversion requires ClearScript ExtendedHostFunctions exposed as 'host'.");
    return host;
}

function _clrType(type = "System.Object") {
    if (type == null) return _clrHost().type("System.Object");
    if (typeof type !== "string") return type;
    return _clrHost().type(type);
}

function _isPlainObject(value) {
    if (value === null || typeof value !== "object" || Array.isArray(value)) return false;
    const prototype = Object.getPrototypeOf(value);
    return prototype === Object.prototype || prototype === null;
}

// Convert a plain JavaScript object to a CLR dictionary, or populate a specified CLR class.
// Arrays and primitive values remain ordinary JavaScript values and rely on ClearScript conversion.
command Make.objectObject (value = null, type = null, memberTypes = null) {
    if (value == null) return null;
    if (type == null || type === "System.Object") {
        if (_isPlainObject(value)) return Make.dictionaryObject(value);
        return value;
    }

    if (!_isPlainObject(value)) return value;

    const instance = _clrHost().newObj(_clrType(type));
    for (const key of Object.keys(value)) {
        const memberType = memberTypes != null ? memberTypes[key] : null;
        instance[key] = memberType != null
            ? Make.objectObject(value[key], memberType)
            : value[key];
    }
    return instance;
};

// Create a CLR Dictionary<TKey, TValue> when an API explicitly requires one.
// Its keys and values are passed through ClearScript's normal value conversion.
command Make.dictionaryObject (value = null, keyType = "System.String", valueType = "System.Object") {
    const clr = _clrHost();
    const dictionaryType = clr.type(
        "System.Collections.Generic.Dictionary",
        _clrType(keyType),
        _clrType(valueType)
    );
    const result = clr.newObj(dictionaryType);
    let entries = [];

    if (value instanceof Map) entries = Array.from(value.entries());
    else if (Array.isArray(value)) entries = value;
    else if (value != null && typeof value === "object")
        entries = Object.keys(value).map(key => [key, value[key]]);
    else if (value != null)
        throw new TypeError("Dictionary conversion expects an object, Map, or array of key/value pairs.");

    for (const entry of entries) {
        if (!Array.isArray(entry) || entry.length < 2)
            throw new TypeError("Dictionary input arrays must contain [key, value] pairs.");
        const item = _isPlainObject(entry[1]) ? Make.objectObject(entry[1]) : entry[1];
        result.Add(entry[0], item);
    }
    return result;
};
