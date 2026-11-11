use deal/make/collection;

// ClearScript interop helpers.
// The host must expose Microsoft.ClearScript.ExtendedHostFunctions as "host".
function _clrHost() {
    if (typeof host === "undefined" || host == null ||
        typeof host.type !== "function" || typeof host.newArr !== "function" ||
        typeof host.newObj !== "function")
        throw new Error("CLR object conversion requires ExtendedHostFunctions exposed as the global 'host' object.");
    return host;
}

function _clrType(type = "System.Object") {
    const clr = _clrHost();
    if (type == null) return clr.type("System.Object");
    if (typeof type === "string") return clr.type(type);
    if (typeof type === "object" && typeof type.FullName === "string") return type;
    throw new TypeError("A CLR type name or System.Type instance is required.");
}

function _clrGenericType(definition, ...arguments) {
    const clr = _clrHost();
    const types = clr.newArr(clr.type("System.Type"), arguments.length);
    for (let index = 0; index < arguments.length; index++)
        types.SetValue(_clrType(arguments[index]), index);
    return clr.type(definition).MakeGenericType(types);
}

function _clrValues(values, mapper = null) {
    const result = Make.array(values);
    return typeof mapper === "function" ? result.map(mapper) : result;
}

function _clrCreateCollection(definition, values, elementType, method, mapper = null) {
    const clr = _clrHost();
    const type = _clrGenericType(definition, elementType);
    const result = clr.newObj(type);
    for (const value of _clrValues(values, mapper))
        result[method](value);
    return result;
}

// Resolve a CLR System.Type from a type name or an existing Type object.
#Make.typeObject:function = function (type = "System.Object") {
    return _clrType(type);
};

// Convert a JavaScript array or iterable into a real CLR array (T[]).
// Values must be assignable to the selected element type.
#Make.arrayObject:function = function (values = [], elementType = "System.Object", mapper = null) {
    const items = _clrValues(values, mapper);
    const result = _clrHost().newArr(_clrType(elementType), items.length);
    for (let index = 0; index < items.length; index++)
        result.SetValue(items[index], index);
    return result;
};

// Convert values into System.Collections.Generic.List<T>.
#Make.listObject:function = function (values = [], elementType = "System.Object", mapper = null) {
    return _clrCreateCollection("System.Collections.Generic.List`1", values, elementType, "Add", mapper);
};

// Return a concrete List<T>, which implements IEnumerable<T>.
// .NET interfaces cannot be instantiated directly.
#Make.iteratorObject:function = function (values = [], elementType = "System.Object", mapper = null) {
    return Make.listObject(values, elementType, mapper);
};

// Convert a JavaScript object, Map, or iterable of [key, value] pairs into Dictionary<TKey, TValue>.
#Make.dictionaryObject:function = function (value = {}, keyType = "System.String", valueType = "System.Object", mapper = null) {
    const clr = _clrHost();
    const type = _clrGenericType("System.Collections.Generic.Dictionary`2", keyType, valueType);
    const result = clr.newObj(type);
    let entries = [];

    if (value == null) {
        entries = [];
    } else if (value instanceof Map) {
        entries = Array.from(value.entries());
    } else if (Array.isArray(value)) {
        entries = value;
    } else if (typeof value === "object" && typeof value[Symbol.iterator] === "function") {
        entries = Array.from(value);
    } else if (typeof value === "object") {
        entries = Object.keys(value).map(key => [key, value[key]]);
    } else {
        throw new TypeError("Dictionary conversion expects an object, Map, or iterable of key/value pairs.");
    }

    for (const entry of entries) {
        if (entry == null || typeof entry[Symbol.iterator] !== "function")
            throw new TypeError("Each dictionary entry must be an iterable [key, value] pair.");
        const pair = Array.from(entry);
        if (pair.length < 2)
            throw new TypeError("Each dictionary entry must contain a key and a value.");
        const mapped = typeof mapper === "function" ? mapper(pair[1], pair[0]) : pair[1];
        result.Add(pair[0], mapped);
    }
    return result;
};

// Convert values into System.Collections.Generic.HashSet<T>.
#Make.hashSetObject:function = function (values = [], elementType = "System.Object", mapper = null) {
    return _clrCreateCollection("System.Collections.Generic.HashSet`1", values, elementType, "Add", mapper);
};

// Convert values into System.Collections.Generic.Queue<T>.
#Make.queueObject:function = function (values = [], elementType = "System.Object", mapper = null) {
    return _clrCreateCollection("System.Collections.Generic.Queue`1", values, elementType, "Enqueue", mapper);
};

// Convert values into System.Collections.Generic.Stack<T>.
#Make.stackObject:function = function (values = [], elementType = "System.Object", mapper = null) {
    return _clrCreateCollection("System.Collections.Generic.Stack`1", values, elementType, "Push", mapper);
};

// Convert values into the non-generic System.Collections.ArrayList.
#Make.arrayListObject:function = function (values = [], mapper = null) {
    const result = _clrHost().newObj(_clrType("System.Collections.ArrayList"));
    for (const value of _clrValues(values, mapper)) result.Add(value);
    return result;
};

// Convert a JavaScript object or Map into the non-generic System.Collections.Hashtable.
#Make.hashTableObject:function = function (value = {}) {
    const clr = _clrHost();
    const result = clr.newObj(_clrType("System.Collections.Hashtable"));
    const entries = value instanceof Map
        ? Array.from(value.entries())
        : value == null ? [] : Object.keys(value).map(key => [key, value[key]]);
    for (const entry of entries) result.Add(entry[0], entry[1]);
    return result;
};

// Convert values into System.Collections.Generic.LinkedList<T>.
#Make.linkedListObject:function = function (values = [], elementType = "System.Object", mapper = null) {
    return _clrCreateCollection("System.Collections.Generic.LinkedList`1", values, elementType, "AddLast", mapper);
};

// Convert values into System.Collections.Generic.SortedSet<T>.
#Make.sortedSetObject:function = function (values = [], elementType = "System.Object", mapper = null) {
    return _clrCreateCollection("System.Collections.Generic.SortedSet`1", values, elementType, "Add", mapper);
};

// Convert a JavaScript object into Dictionary<string, object>.
#Make.objectDictionary:function = function (value = {}) {
    return Make.dictionaryObject(value, "System.String", "System.Object");
};

// Convert a value using System.Convert.ChangeType. This is intended for
// IConvertible types; complex objects should use the collection converters above.
#Make.convertObject:function = function (value, targetType) {
    const clr = _clrHost();
    return clr.type("System.Convert").ChangeType(value, _clrType(targetType));
};
