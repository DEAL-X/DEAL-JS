// Build one record from named fields, objects, and positional values.
//
// Accepted inputs:
//   collect({ key: value }, { other: value })
//   collect({ key: "name", value: value })
//   collect(value1, value2) -> { COLUMN1: value1, COLUMN2: value2 }
//
// Arrays are recursively expanded. Later values replace earlier values
// when the same key is supplied more than once.
command #Collect:start|function() {
    if (arguments.length < 1) return null;

    const result = {};
    let column = 1;

    const isRecord = value =>
        value !== null && typeof value === "object" && !Array.isArray(value);

    const add = value => {
        if (Array.isArray(value)) {
            for (const item of value) add(item);
            return;
        }

        if (isRecord(value)) {
            const keys = Object.keys(value);

            // Explicit { key, value } pair.
            if (Object.prototype.hasOwnProperty.call(value, "key")
                && Object.prototype.hasOwnProperty.call(value, "value")
                && keys.every(key => key === "key" || key === "value")) {
                result[String(value.key)] = value.value;
                return;
            }

            // Merge named fields from a regular object.
            for (const key of keys)
                result[key] = value[key];
            return;
        }

        result["COLUMN" + column++] = value;
    };

    for (let i = 0; i < arguments.length; i++)
        add(arguments[i]);

    return result;
}