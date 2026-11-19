// JavaScript-to-.NET object and collection converters.

// Initialize the namespace here as well: importing a leaf module must not depend
// on the host having separately imported the package entry point.
globalThis.Make = globalThis.Make || {};
command #Make:identifier = globalThis.Make;
//
// Requires Microsoft.ClearScript.ExtendedHostFunctions to be exposed by the host
// as the global JavaScript object "host".
//
// Important: the host must NOT enable V8ScriptEngineFlags.EnableArrayConversion.
// That flag converts CLR arrays to JavaScript arrays and strips their CLR methods
// and element type. Strongly typed CLR arrays cannot be created reliably while
// that lossy conversion is enabled.
//
// Examples:
//   Make.arrayObject(["a", "b"], "System.String") -> System.String[]
//   Make.listObject([1, 2], "System.Int32")       -> List<Int32>
//   Make.iteratorObject(["a", "b"], "System.String") -> List<String>
//   Make.dictionaryObject({ name: "DEAL" })       -> Dictionary<String, Object>
//   Make.objectObject({ X: 1, Y: 2 })             -> ExpandoObject

use deal/make;
use deal/make/collection;
use deal/make/logical;

command Make.hostObject () {
    if (typeof host === "undefined" || host == null ||
        typeof host.type !== "function" || typeof host.newObj !== "function" ||
        typeof host.newArr !== "function")
        throw new Error("DEAL CLR conversion requires ClearScript ExtendedHostFunctions exposed as 'host'.");
    return host;
};

command Make.typeObject (type = "System.Object") {
    if (type == null) return Make.hostObject().type("System.Object");
    return typeof type === "string" ? Make.hostObject().type(type) : type;
};

command Make.genericTypeObject (definition, ...arguments) {
    const clr = Make.hostObject();
    return clr.type(definition, ...arguments.map(argument => Make.typeObject(argument)));
};

command Make.isPlainObject (value) {
    if (value === null || typeof value !== "object" || Array.isArray(value)) return false;
    const prototype = Object.getPrototypeOf(value);
    return prototype === Object.prototype || prototype === null;
};

// Convert a JavaScript value into a CLR value. When no concrete type is supplied,
// nested arrays become CLR object arrays and plain objects become ExpandoObjects.
command Make.objectObject (value = null, type = null, memberTypes = null) {
    if (value === null || value === undefined) return null;

    if (type == null || type === "System.Object") {
        if (Array.isArray(value)) return Make.arrayObject(value);
        if (Make.isPlainObject(value)) return Make.expandoObject(value);
        return value;
    }

    if (typeof type === "string" && type.endsWith("[]"))
        return Make.arrayObject(value, type.slice(0, -2));

    const clr = Make.hostObject();
    const typeName = typeof type === "string" ? type : null;

    if (typeName === "System.String") return String(value);
    if (typeName === "System.Boolean") return clr.type("System.Convert").ToBoolean(value);
    if (typeName === "System.Char") {
        const character = String(value);
        if (character.length !== 1)
            throw new TypeError("A System.Char conversion requires exactly one character.");
        return character;
    }

    const scalarTypes = [
        "System.Byte", "System.SByte", "System.Int16", "System.UInt16",
        "System.Int32", "System.UInt32", "System.Int64", "System.UInt64",
        "System.Single", "System.Double", "System.Decimal", "System.DateTime"
    ];
    if (typeName && scalarTypes.includes(typeName))
        return clr.type("System.Convert").ChangeType(value, Make.typeObject(type));

    if (typeName === "System.Guid")
        return clr.type("System.Guid").Parse(String(value));

    if (Make.isPlainObject(value)) {
        const instance = clr.newObj(Make.typeObject(type));
        for (const key of Object.keys(value)) {
            const memberType = memberTypes != null ? memberTypes[key] : null;
            instance[key] = memberType != null
                ? Make.objectObject(value[key], memberType)
                : Make.objectObject(value[key]);
        }
        return instance;
    }

    return value;
};

command Make.valueObject (value = null, type = null, memberTypes = null) {
    return Make.objectObject(value, type, memberTypes);
};

// Create an actual CLR T[] rather than returning the original JavaScript array.
command Make.arrayObject (values = [], elementType = "System.Object", mapper = null) {
    const clr = Make.hostObject();
    const items = Make.array(values, mapper);
    const result = clr.newArr(Make.typeObject(elementType), items.length);

    // EnableArrayConversion turns host arrays into JavaScript arrays, in which case
    // SetValue disappears and the CLR element type is no longer preserved.
    if (typeof result.SetValue !== "function")
        throw new Error("Cannot create a typed CLR array while ClearScript EnableArrayConversion is enabled. Remove V8ScriptEngineFlags.EnableArrayConversion from the host engine.");

    for (let index = 0; index < items.length; index++)
        result.SetValue(Make.objectObject(items[index], elementType), index);

    return result;
};

command Make.stringArrayObject (values = []) {
    return Make.arrayObject(values, "System.String", value => String(value));
};

command Make.listObject (values = [], elementType = "System.Object", mapper = null) {
    const clr = Make.hostObject();
    const items = Make.array(values, mapper);
    const result = clr.newObj(Make.genericTypeObject("System.Collections.Generic.List", elementType));
    const automatic = elementType == null || elementType === "System.Object";

    for (const item of items)
        result.Add(automatic ? Make.objectObject(item) : Make.objectObject(item, elementType));

    return result;
};

// A List<T> is concrete and implements IEnumerable<T>, unlike the interface itself.
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
    const clr = Make.hostObject();
    const result = clr.newObj(Make.genericTypeObject("System.Collections.Generic.Dictionary", keyType, valueType));
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

    const automatic = valueType == null || valueType === "System.Object";
    for (const entry of entries) {
        if (entry == null || typeof entry[Symbol.iterator] !== "function")
            throw new TypeError("Each dictionary entry must be an iterable [key, value] pair.");
        const pair = Array.from(entry);
        if (pair.length < 2)
            throw new TypeError("Each dictionary entry must contain a key and a value.");

        const key = Make.objectObject(pair[0], keyType);
        const rawValue = typeof mapper === "function" ? mapper(pair[1], pair[0]) : pair[1];
        const item = automatic
            ? Make.objectObject(rawValue)
            : Make.objectObject(rawValue, valueType);
        result.Add(key, item);
    }

    return result;
};

command Make.hashSetObject (values = [], elementType = "System.Object", mapper = null) {
    const clr = Make.hostObject();
    const result = clr.newObj(Make.genericTypeObject("System.Collections.Generic.HashSet", elementType));
    const automatic = elementType == null || elementType === "System.Object";

    for (const item of Make.array(values, mapper))
        result.Add(automatic ? Make.objectObject(item) : Make.objectObject(item, elementType));

    return result;
};

command Make.queueObject (values = [], elementType = "System.Object", mapper = null) {
    const clr = Make.hostObject();
    const result = clr.newObj(Make.genericTypeObject("System.Collections.Generic.Queue", elementType));
    const automatic = elementType == null || elementType === "System.Object";

    for (const item of Make.array(values, mapper))
        result.Enqueue(automatic ? Make.objectObject(item) : Make.objectObject(item, elementType));

    return result;
};

command Make.stackObject (values = [], elementType = "System.Object", mapper = null) {
    const clr = Make.hostObject();
    const result = clr.newObj(Make.genericTypeObject("System.Collections.Generic.Stack", elementType));
    const automatic = elementType == null || elementType === "System.Object";

    for (const item of Make.array(values, mapper))
        result.Push(automatic ? Make.objectObject(item) : Make.objectObject(item, elementType));

    return result;
};

command Make.arrayListObject (values = [], mapper = null) {
    const result = Make.hostObject().newObj(Make.typeObject("System.Collections.ArrayList"));
    for (const item of Make.array(values, mapper))
        result.Add(Make.objectObject(item));
    return result;
};

command Make.hashTableObject (value = {}) {
    const clr = Make.hostObject();
    const result = clr.newObj(Make.typeObject("System.Collections.Hashtable"));
    const entries = value instanceof Map
        ? Array.from(value.entries())
        : value == null ? [] : Object.keys(value).map(key => [key, value[key]]);

    for (const entry of entries)
        result.Add(entry[0], Make.objectObject(entry[1]));

    return result;
};

command Make.linkedListObject (values = [], elementType = "System.Object", mapper = null) {
    const clr = Make.hostObject();
    const result = clr.newObj(Make.genericTypeObject("System.Collections.Generic.LinkedList", elementType));
    const automatic = elementType == null || elementType === "System.Object";

    for (const item of Make.array(values, mapper))
        result.AddLast(automatic ? Make.objectObject(item) : Make.objectObject(item, elementType));

    return result;
};

command Make.sortedSetObject (values = [], elementType = "System.Object", mapper = null) {
    const clr = Make.hostObject();
    const result = clr.newObj(Make.genericTypeObject("System.Collections.Generic.SortedSet", elementType));
    const automatic = elementType == null || elementType === "System.Object";

    for (const item of Make.array(values, mapper))
        result.Add(automatic ? Make.objectObject(item) : Make.objectObject(item, elementType));

    return result;
};

// Convert a JavaScript object into a populated System.Dynamic.ExpandoObject.
command Make.expandoObject (value = {}) {
    const clr = Make.hostObject();
    const result = clr.newObj(Make.typeObject("System.Dynamic.ExpandoObject"));
    const dictionaryType = Make.genericTypeObject(
        "System.Collections.Generic.IDictionary",
        "System.String",
        "System.Object"
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
    return Make.hostObject().type("System.Convert").ChangeType(value, Make.typeObject(targetType));
};

command Make.bytesObject (value = null, encoding = null) {
    const clr = Make.hostObject();
    const utf8 = encoding || clr.type("System.Text.Encoding").UTF8;

    if (value == null) return Make.arrayObject([], "System.Byte");
    if (typeof value.GetType === "function" && value.GetType().FullName === "System.Byte[]")
        return value;
    if (typeof value === "string") return utf8.GetBytes(value);

    if (typeof ArrayBuffer !== "undefined" && value instanceof ArrayBuffer)
        value = Array.from(new Uint8Array(value));
    else if (typeof ArrayBuffer !== "undefined" && ArrayBuffer.isView(value))
        value = Array.from(new Uint8Array(value.buffer, value.byteOffset, value.byteLength));

    if (Array.isArray(value) ||
        (value != null && typeof value !== "string" && typeof value[Symbol.iterator] === "function"))
        return Make.arrayObject(value, "System.Byte");

    return utf8.GetBytes(Make.string(value));
};

command Make.byteArrayObject (value = null, encoding = null) {
    return Make.bytesObject(value, encoding);
};

command Make.binaryObject (value = null, encoding = null) {
    return Make.bytesObject(value, encoding);
};
