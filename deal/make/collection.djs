#Make.array:function = function (value, mapper = null) {
    let result;
    if (value == null) result = [];
    else if (Array.isArray(value)) result = value.slice();
    else if (typeof value !== "string" && typeof value[Symbol.iterator] === "function")
        result = Array.from(value);
    else result = [value];

    return typeof mapper === "function" ? result.map(mapper) : result;
};

#Make.matrix:function = function (object) {
    if (object == null) return [];
    if (!Array.isArray(object)) {
        if (typeof object !== "object") return [[object]];
        const keys = Object.keys(object);
        return [keys, keys.map(key => object[key])];
    }
    if (object.length === 0) return [];

    const isObjectRows = object.every(item =>
        item !== null && typeof item === "object" && !Array.isArray(item)
    );
    if (isObjectRows) {
        const keys = [];
        for (const item of object)
            for (const key of Object.keys(item))
                if (!keys.includes(key)) keys.push(key);

        return [
            keys,
            ...object.map(item => keys.map(key =>
                Object.prototype.hasOwnProperty.call(item, key) ? item[key] : undefined
            ))
        ];
    }

    const isScalarMatrix = object.every(row =>
        Array.isArray(row) && row.every(value => value == null || typeof value !== "object")
    );
    if (isScalarMatrix) return object.map(row => row.slice());

    const result = [];
    for (const item of object) {
        if (Array.isArray(item)) result.push(...MAKE.matrix(item));
        else if (item !== null && typeof item === "object") result.push(...MAKE.matrix(item));
        else result.push([item]);
    }
    return result;
};