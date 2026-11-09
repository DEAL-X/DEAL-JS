// Project fields from an object, array, iterable, or scalar source.
//
// Supported projections:
//   select("*", source)
//   select("name", source)                    -> [value, ...]
//   select(["name", "price"], source)          -> [{ name, price }, ...]
//   select({ title: "name" }, source)          -> [{ title: value }, ...]
//   select([{ key: "title", value: "name" }], source)
//
// DEAL's "FROM" separator is compiled as an argument separator. The source
// is therefore always the final argument; a trailing empty separator value
// is tolerated for compatibility with generated calls.
command #Select:start|function() {
    if (arguments.length < 1) return null;
    if (arguments.length === 1) return arguments[0];

    const source = arguments[arguments.length - 1];
    let projections = Array.prototype.slice.call(arguments, 0, -1);

    if (projections.length > 0 && projections[projections.length - 1] == null)
        projections.pop();

    if (projections.length === 0 || projections.length === 1 && projections[0] == null)
        return source;

    // Flatten only the outer projection list; object projection descriptors
    // remain intact.
    if (projections.length === 1 && Array.isArray(projections[0]))
        projections = projections[0];

    const isObject = value =>
        value !== null && typeof value === "object" && !Array.isArray(value);

    const toEntries = value => {
        if (Array.isArray(value))
            return value.map((item, index) => [String(index), item]);
        if (isObject(value))
            return Object.keys(value).map(key => [key, value[key]]);
        return [["0", value]];
    };

    const read = (record, key) => {
        if (record == null) return undefined;
        if (Array.isArray(record) || typeof record === "string") {
            const index = Number(key);
            if (Number.isInteger(index) && index >= 0)
                return record[index];
        }
        if (Object.prototype.hasOwnProperty.call(Object(record), key))
            return record[key];
        return undefined;
    };

    const fields = [];
    const addField = (outputKey, sourceKey) => {
        fields.push({ key: String(outputKey), source: sourceKey });
    };

    for (const projection of projections) {
        if (projection === "*") {
            fields.push({ all: true });
        } else if (typeof projection === "string" || typeof projection === "number") {
            addField(projection, projection);
        } else if (Array.isArray(projection)) {
            for (const key of projection) {
                if (key === "*") fields.push({ all: true });
                else addField(key, key);
            }
        } else if (isObject(projection)) {
            if (Object.prototype.hasOwnProperty.call(projection, "key")
                && Object.prototype.hasOwnProperty.call(projection, "value")) {
                addField(projection.key, projection.value);
            } else {
                for (const key of Object.keys(projection))
                    addField(key, projection[key]);
            }
        }
    }

    const project = record => {
        const output = {};
        for (const field of fields) {
            if (field.all) {
                for (const [key, value] of toEntries(record))
                    output[key] = value;
            } else {
                const value = read(record, field.source);
                if (value !== undefined || record != null
                    && Object.prototype.hasOwnProperty.call(Object(record), field.source))
                    output[field.key] = value;
            }
        }
        return output;
    };

    const singleValueProjection = fields.length === 1 && !fields[0].all
        && projections.length === 1
        && (typeof projections[0] === "string" || typeof projections[0] === "number");

    if (Array.isArray(source)
        || source != null && typeof source !== "string"
            && typeof source[Symbol.iterator] === "function") {
        const records = Array.isArray(source) ? source : Array.from(source);
        if (singleValueProjection)
            return records.map(record => read(record, fields[0].source));
        return records.map(project);
    }

    if (singleValueProjection)
        return [read(source, fields[0].source)];

    return project(source);
}