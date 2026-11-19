use deal/make/.djs;
// JavaScript/.NET interop helpers for ClearScript.
// The host must expose Microsoft.ClearScript.ExtendedHostFunctions as global "host".
//
// EnableArrayConversion intentionally remains enabled by the host. Under that mode,
// CLR arrays are surfaced to JavaScript as JavaScript arrays, and JavaScript arrays
// cross back into .NET as object[]. Project-owned .NET APIs provide object-friendly
// overloads where a typed CLR array is required.

use deal/make/collection;
use deal/make/logical;


command Make.hostObject () {
    if (typeof host === "undefined" || host == null ||
        typeof host.type !== "function" || typeof host.newObj !== "function" ||
        typeof host.newArr !== "function")
        throw new Error("DEAL CLR conversion requires ClearScript ExtendedHostFunctions exposed as 'host'.");
    return host;
};

function _clrHost () {
    return Make.hostObject();
}

function _clrType (type = "System.Object") {
    const clr = _clrHost();
    if (type == null) return clr.type("System.Object");
    return typeof type === "string" ? clr.type(type) : type;
}

function _clrGenericType (definition, ...types) {
    const clr = _clrHost();
    return clr.type(definition, ...types.map(_clrType));
}

const _clrTypeNames = new WeakMap();

function _clrTypeName (type) {
    if (typeof type === "string") return type;
    if (type != null && (typeof type === "object" || typeof type === "function"))
        return _clrTypeNames.get(type) || null;
    return null;
}

function _clrConvert (value, typeName) {
    const convert = _clrHost().type("System.Convert");
    switch (typeName) {
        case "System.Byte": return convert.ToByte(value);
        case "System.SByte": return convert.ToSByte(value);
        case "System.Int16": return convert.ToInt16(value);
        case "System.UInt16": return convert.ToUInt16(value);
        case "System.Int32": return convert.ToInt32(value);
        case "System.UInt32": return convert.ToUInt32(value);
        case "System.Int64": return convert.ToInt64(value);
        case "System.UInt64": return convert.ToUInt64(value);
        case "System.Single": return convert.ToSingle(value);
        case "System.Double": return convert.ToDouble(value);
        case "System.Decimal": return convert.ToDecimal(value);
        case "System.Boolean": return convert.ToBoolean(value);
        case "System.DateTime": return convert.ToDateTime(value);
        case "System.String": return convert.ToString(value);
        case "System.Char": {
            const character = String(value);
            if (character.length !== 1)
                throw new TypeError("A System.Char conversion requires exactly one character.");
            return character;
        }
        case "System.Guid": return _clrHost().type("System.Guid").Parse(String(value));
        default: throw new TypeError("Unsupported scalar CLR conversion target: " + typeName);
    }
}

function _clrValues (values, mapper = null, elementType = "System.Object") {
    return Make.array(values).map(value =>
        typeof mapper === "function" ? mapper(value) : Make.objectObject(value, elementType)
    );
}

function _clrCreateCollection (definition, values, elementType, method, mapper = null) {
    const clr = _clrHost();
    const result = clr.newObj(_clrGenericType(definition, elementType));
    for (const value of _clrValues(values, mapper, elementType))
        result[method](value);
    return result;
}

command Make.typeObject (type = "System.Object") {
    const name = _clrTypeName(type);
    const result = _clrType(type);
    if (name && result != null && (typeof result === "object" || typeof result === "function"))
        _clrTypeNames.set(result, name);
    else if (typeof type === "string" && result != null &&
        (typeof result === "object" || typeof result === "function"))
        _clrTypeNames.set(result, type);
    return result;
};

command Make.isPlainObject (value) {
    if (value === null || typeof value !== "object" || Array.isArray(value)) return false;
    const prototype = Object.getPrototypeOf(value);
    return prototype === Object.prototype || prototype === null;
};

// With EnableArrayConversion, arrays crossing the host boundary are JavaScript
// arrays, not typed CLR arrays. The project's .NET APIs accept these arrays through
// object-friendly overloads and perform the final typed conversion inside .NET.
command Make.arrayObject (values = [], elementType = "System.Object", mapper = null) {
    return _clrValues(values, mapper, elementType);
};

command Make.stringArrayObject (values = []) {
    return Make.arrayObject(values, "System.String", value => String(value));
};

command Make.objectObject (value = null, type = null, memberTypes = null) {
    if (value === null || value === undefined) return null;

    if (type == null || type === "System.Object") {
        if (Array.isArray(value)) return Make.arrayObject(value);
        if (Make.isPlainObject(value)) return Make.expandoObject(value);
        return value;
    }

    if (typeof type === "string" && type.endsWith("[]"))
        return Make.arrayObject(value, type.slice(0, -2));

    const clr = _clrHost();
    const typeName = _clrTypeName(type);

    if (typeName === "System.String") return String(value);
    if (typeName === "System.Boolean") return _clrConvert(value, typeName);
    if (typeName === "System.Char") return _clrConvert(value, typeName);

    const scalarTypes = [
        "System.Byte", "System.SByte", "System.Int16", "System.UInt16",
        "System.Int32", "System.UInt32", "System.Int64", "System.UInt64",
        "System.Single", "System.Double", "System.Decimal", "System.DateTime"
    ];
    if (typeName && scalarTypes.includes(typeName))
        return _clrConvert(value, typeName);

    if (typeName === "System.Guid")
        return _clrConvert(value, typeName);

    if (Make.isPlainObject(value)) {
        const instance = clr.newObj(_clrType(type));
        for (const key of Object.keys(value)) {
            const item = value[key];
            const memberType = memberTypes != null ? memberTypes[key] : null;
            instance[key] = memberType != null
                ? Make.objectObject(item, memberType)
                : Make.objectObject(item);
        }
        return instance;
    }

    return value;
};

command Make.valueObject (value = null, type = null, memberTypes = null) {
    return Make.objectObject(value, type, memberTypes);
};

command Make.listObject (values = [], elementType = "System.Object", mapper = null) {
    return _clrCreateCollection("System.Collections.Generic.List", values, elementType, "Add", mapper);
};

command Make.iteratorObject (values = [], elementType = "System.Object", mapper = null) {
    return Make.listObject(values, elementType, mapper);
};

command Make.enumerableObject (values = [], elementType = "System.Object", mapper = null) {
    return Make.iteratorObject(values, elementType, mapper);
};

command Make.collectionObject (values = [], elementType = "System.Object", mapper = null) {
    return Make.listObject(values, elementType, mapper);
};

command Make.dictionaryObject (value = {}, keyType = "System.String", valueType = "System.Object", mapper = null) {
    const clr = _clrHost();
    const result = clr.newObj(_clrGenericType(
        "System.Collections.Generic.Dictionary", keyType, valueType
    ));
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
        const key = Make.objectObject(pair[0], keyType);
        const rawValue = typeof mapper === "function" ? mapper(pair[1], pair[0]) : pair[1];
        const item = valueType == null || valueType === "System.Object"
            ? Make.objectObject(rawValue)
            : Make.objectObject(rawValue, valueType);
        result.Add(key, item);
    }
    return result;
};

command Make.hashSetObject (values = [], elementType = "System.Object", mapper = null) {
    return _clrCreateCollection("System.Collections.Generic.HashSet", values, elementType, "Add", mapper);
};

command Make.queueObject (values = [], elementType = "System.Object", mapper = null) {
    return _clrCreateCollection("System.Collections.Generic.Queue", values, elementType, "Enqueue", mapper);
};

command Make.stackObject (values = [], elementType = "System.Object", mapper = null) {
    return _clrCreateCollection("System.Collections.Generic.Stack", values, elementType, "Push", mapper);
};

command Make.linkedListObject (values = [], elementType = "System.Object", mapper = null) {
    return _clrCreateCollection("System.Collections.Generic.LinkedList", values, elementType, "AddLast", mapper);
};

command Make.sortedSetObject (values = [], elementType = "System.Object", mapper = null) {
    return _clrCreateCollection("System.Collections.Generic.SortedSet", values, elementType, "Add", mapper);
};

command Make.arrayListObject (values = [], mapper = null) {
    const result = _clrHost().newObj(_clrType("System.Collections.ArrayList"));
    for (const value of _clrValues(values, mapper)) result.Add(value);
    return result;
};

command Make.hashTableObject (value = {}) {
    const clr = _clrHost();
    const result = clr.newObj(_clrType("System.Collections.Hashtable"));
    const entries = value instanceof Map
        ? Array.from(value.entries())
        : value == null ? [] : Object.keys(value).map(key => [key, value[key]]);
    for (const entry of entries)
        result.Add(entry[0], Make.objectObject(entry[1]));
    return result;
};

command Make.expandoObject (value = {}) {
    const clr = _clrHost();
    const expandoType = typeof Core !== "undefined"
        ? Core.System.Dynamic.ExpandoObject
        : _clrType("System.Dynamic.ExpandoObject");
    const result = clr.newObj(expandoType);
    const dictionaryType = _clrGenericType(
        "System.Collections.Generic.IDictionary", "System.String", "System.Object"
    );
    const dictionary = clr.cast(dictionaryType, result);
    if (value != null) {
        for (const key of Object.keys(value))
            dictionary.Add(key, Make.objectObject(value[key]));
    }
    return result;
};

command Make.objectDictionary (value = {}) {
    return Make.dictionaryObject(value, "System.String", "System.Object");
};

command Make.convertObject (value, targetType) {
    const typeName = _clrTypeName(targetType);
    if (typeName) return _clrConvert(value, typeName);
    throw new TypeError("Make.convertObject expects a type name or a type returned by Make.typeObject.");
};

command Make.bytesObject (value = null, encoding = null) {
    const clr = _clrHost();
    const utf8 = encoding || clr.type("System.Text.Encoding").UTF8;

    if (value == null) return [];
    if (typeof value === "string")
        return Array.from(utf8.GetBytes(value), item => Number(item));

    if (typeof ArrayBuffer !== "undefined" && value instanceof ArrayBuffer)
        value = Array.from(new Uint8Array(value));
    else if (typeof ArrayBuffer !== "undefined" && ArrayBuffer.isView(value))
        value = Array.from(new Uint8Array(value.buffer, value.byteOffset, value.byteLength));

    if (value != null && typeof value !== "string" &&
        typeof value[Symbol.iterator] === "function")
        return Make.array(value, item => Number(clr.type("System.Convert").ToByte(item)));

    return Array.from(utf8.GetBytes(Make.string(value)), item => Number(item));
};

command Make.byteArrayObject (value = null, encoding = null) {
    return Make.bytesObject(value, encoding);
};

command Make.binaryObject (value = null, encoding = null) {
    return Make.bytesObject(value, encoding);
};
