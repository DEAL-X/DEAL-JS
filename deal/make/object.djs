// Explicit conversion for APIs that require a CLR Dictionary<TKey, TValue>.
// JavaScript arrays, scalar values, and ordinary objects should otherwise be passed directly.
command Make.dictionaryObject (value = null, keyType = "System.String", valueType = "System.Object") {
    if (typeof host === "undefined" || host == null ||
        typeof host.type !== "function" || typeof host.newObj !== "function")
        throw new Error("CLR dictionary conversion requires ClearScript ExtendedHostFunctions exposed as 'host'.");

    const clrType = type => typeof type === "string" ? host.type(type) : type;
    const dictionaryType = host.type(
        "System.Collections.Generic.Dictionary",
        clrType(keyType),
        clrType(valueType)
    );
    const result = host.newObj(dictionaryType);
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
        result.Add(entry[0], entry[1]);
    }
    return result;
};
