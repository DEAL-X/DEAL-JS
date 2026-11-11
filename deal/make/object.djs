// JavaScript-to-.NET object and collection converters.
//
// Requires Microsoft.ClearScript.ExtendedHostFunctions to be exposed by the host
// as the global JavaScript object "host".
//
// Examples:
//   Make.arrayObject(["a", "b"], "System.String")       -> System.String[]
//   Make.listObject([1, 2], "System.Int32")             -> List<Int32>
//   Make.iteratorObject(new Set(["a", "b"]), "System.String") -> IEnumerable<String>
//   Make.dictionaryObject({ name: "DEAL" })             -> Dictionary<String, Object>
//   Make.objectObject({ X: 1, Y: 2 }, "My.Namespace.Point") -> a populated .NET object

#Make.hostObject:function = function () {
    if (typeof host === "undefined" || host == null ||
        typeof host.type !== "function" || typeof host.newObj !== "function" ||
        typeof host.newArr !== "function")
        throw new Error("DEAL CLR conversion requires ClearScript ExtendedHostFunctions exposed as 'host'.");
    return host;
};

#Make.typeObject:function = function (type = "System.Object") {
    if (type == null) return Make.hostObject().type("System.Object");
    if (typeof type !== "string") return type;
    return Make.hostObject().type(type);
};

#Make.isPlainObject:function = function (value) {
    if (value === null || typeof value !== "object" || Array.isArray(value)) return false;
    const prototype = Object.getPrototypeOf(value);
    return prototype === Object.prototype || prototype === null;
};

#Make.objectObject:function = function (value = null, type = null, memberTypes = null) {
    if (value === null || value === undefined) return null;

    // Automatic conversion: recursively convert JavaScript containers while
    // preserving already-created .NET objects and JavaScript scalar values.
    if (type == null || type === "System.Object") {
        if (Array.isArray(value)) return Make.arrayObject(value);
        if (Make.isPlainObject(value)) return Make.dictionaryObject(value);
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
        if (character.length !== 1) throw new TypeError("A System.Char conversion requires exactly one character.");
        return character;
    }

    // Convert.ChangeType handles the standard CLR primitive and date/time
    // conversions. Complex object types are instantiated and populated below.
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

#Make.valueObject:function = function (value = null, type = null, memberTypes = null) {
    return Make.objectObject(value, type, memberTypes);
};

#Make.arrayObject:function = function (values = null, elementType = "System.Object") {
    const clr = Make.hostObject();
    const items = Make.array(values);
    const automatic = elementType == null || elementType === "System.Object";
    const clrElementType = Make.typeObject(elementType);
    const result = clr.newArr(clrElementType, items.length);

    for (let index = 0; index < items.length; index++) {
        const item = automatic
            ? Make.objectObject(items[index])
            : Make.objectObject(items[index], elementType);
        result.SetValue(item, index);
    }

    return result;
};

#Make.listObject:function = function (values = null, elementType = "System.Object") {
    const clr = Make.hostObject();
    const items = Make.array(values);
    const automatic = elementType == null || elementType === "System.Object";
    const clrElementType = Make.typeObject(elementType);
    const listType = clr.type("System.Collections.Generic.List", clrElementType);
    const result = clr.newObj(listType);

    for (const item of items)
        result.Add(automatic ? Make.objectObject(item) : Make.objectObject(item, elementType));

    return result;
};

// A List<T> is returned because it implements IEnumerable<T> and can be
// passed directly to .NET methods that accept IEnumerable<T>.
#Make.iteratorObject:function = function (values = null, elementType = "System.Object") {
    return Make.listObject(values, elementType);
};

#Make.enumerableObject:function = function (values = null, elementType = "System.Object") {
    return Make.iteratorObject(values, elementType);
};

#Make.collectionObject:function = function (values = null, elementType = "System.Object") {
    return Make.listObject(values, elementType);
};

#Make.dictionaryObject:function = function (value = null, keyType = "System.String", valueType = "System.Object") {
    const clr = Make.hostObject();
    const clrKeyType = Make.typeObject(keyType);
    const clrValueType = Make.typeObject(valueType);
    const dictionaryType = clr.type("System.Collections.Generic.Dictionary", clrKeyType, clrValueType);
    const result = clr.newObj(dictionaryType);
    let entries = [];

    if (value instanceof Map) {
        entries = Array.from(value.entries());
    } else if (Array.isArray(value)) {
        entries = value;
    } else if (value != null && typeof value === "object") {
        entries = Object.keys(value).map(key => [key, value[key]]);
    }

    const automatic = valueType == null || valueType === "System.Object";
    for (const entry of entries) {
        if (!Array.isArray(entry) || entry.length < 2)
            throw new TypeError("Dictionary input arrays must contain [key, value] pairs.");

        const key = Make.objectObject(entry[0], keyType);
        const item = automatic
            ? Make.objectObject(entry[1])
            : Make.objectObject(entry[1], valueType);
        result.Add(key, item);
    }

    return result;
};

#Make.hashSetObject:function = function (values = null, elementType = "System.Object") {
    const clr = Make.hostObject();
    const clrElementType = Make.typeObject(elementType);
    const collectionType = clr.type("System.Collections.Generic.HashSet", clrElementType);
    const result = clr.newObj(collectionType);
    const automatic = elementType == null || elementType === "System.Object";

    for (const item of Make.array(values))
        result.Add(automatic ? Make.objectObject(item) : Make.objectObject(item, elementType));

    return result;
};

#Make.queueObject:function = function (values = null, elementType = "System.Object") {
    const clr = Make.hostObject();
    const clrElementType = Make.typeObject(elementType);
    const collectionType = clr.type("System.Collections.Generic.Queue", clrElementType);
    const result = clr.newObj(collectionType);
    const automatic = elementType == null || elementType === "System.Object";

    for (const item of Make.array(values))
        result.Enqueue(automatic ? Make.objectObject(item) : Make.objectObject(item, elementType));

    return result;
};

#Make.stackObject:function = function (values = null, elementType = "System.Object") {
    const clr = Make.hostObject();
    const clrElementType = Make.typeObject(elementType);
    const collectionType = clr.type("System.Collections.Generic.Stack", clrElementType);
    const result = clr.newObj(collectionType);
    const automatic = elementType == null || elementType === "System.Object";

    for (const item of Make.array(values))
        result.Push(automatic ? Make.objectObject(item) : Make.objectObject(item, elementType));

    return result;
};

#Make.stringArrayObject:function = function (values = null) {
    return Make.arrayObject(values, "System.String");
};

#Make.bytesObject:function = function (value = null, encoding = null) {
    const clr = Make.hostObject();
    const utf8 = encoding || clr.type("System.Text.Encoding").UTF8;

    if (value == null) return clr.newArr(clr.type("System.Byte"), 0);
    if (typeof value.GetType === "function" && value.GetType().FullName === "System.Byte[]") return value;
    if (typeof value === "string") return utf8.GetBytes(value);

    if (typeof ArrayBuffer !== "undefined" && value instanceof ArrayBuffer)
        value = Array.from(new Uint8Array(value));
    else if (typeof ArrayBuffer !== "undefined" && ArrayBuffer.isView(value))
        value = Array.from(new Uint8Array(value.buffer, value.byteOffset, value.byteLength));

    if (Array.isArray(value) || (value != null && typeof value !== "string" && typeof value[Symbol.iterator] === "function"))
        return Make.arrayObject(value, "System.Byte");

    return utf8.GetBytes(Make.string(value));
};

#Make.byteArrayObject:function = function (value = null, encoding = null) {
    return Make.bytesObject(value, encoding);
};

#Make.binaryObject:function = function (value = null, encoding = null) {
    return Make.bytesObject(value, encoding);
};
