// Object helpers for DEAL.JS.
// Load explicitly with: Use deal/data/object;
//
// Inspection:
//   object.keys, object.values, object.entries
//   object.size, object.count, object.empty, object.isEmpty
//   object.firstKey, object.lastKey, object.first, object.last
//
// Access and mutation:
//   object.has(key), object.get(key, fallback), object.set(key, value)
//   object.assign(source1, source2), object.merge(source1, source2)
//   object.delete(key), object.remove(key), object.clear()
//
// Iteration and transformation:
//   object.forEach(callback), object.map(callback), object.mapValues(callback)
//   object.filter(callback), object.find(callback), object.findKey(callback)
//   object.reduce(callback, initialValue), object.some(callback), object.every(callback)
//   object.toArray(callback), object.toEntriesArray(), object.pick(keys), object.omit(keys)
//
// Callbacks receive (value, key, object), except reduce which receives
// (accumulator, value, key, object). forEach returns the original object;
// map/mapValues/filter return new objects; assign/set/delete/clear mutate
// the receiver and return it where applicable; merge returns a new object.
//
// The module installs non-enumerable helpers on Object.prototype. Existing
// Object.prototype members are never overwritten. Arrays keep their native
// methods because Array.prototype takes precedence. Own properties shadow
// inherited helpers. Object.create(null) objects do not inherit these helpers.
//
// This is an opt-in global prototype extension. Load it only when wanted.

(function () {
    "use strict";

    const objectPrototype = Object.prototype;
    const hasOwn = (object, key) => objectPrototype.hasOwnProperty.call(object, key);
    const isObject = value => value !== null && (typeof value === "object" || typeof value === "function");
    const keysOf = object => Object.keys(Object(object));
    const valuesOf = object => keysOf(object).map(key => object[key]);
    const entriesOf = object => keysOf(object).map(key => [key, object[key]]);
    const safeKey = key => key !== "__proto__" && key !== "prototype" && key !== "constructor";

    const defineOwn = (target, key, value) => {
        Object.defineProperty(target, key, {
            value: value,
            enumerable: true,
            configurable: true,
            writable: true
        });
        return target;
    };

    const toKeyPath = key => Array.isArray(key) ? key : String(key).split(".");
    const getPath = (target, key, fallback) => {
        const path = toKeyPath(key);
        let current = target;
        for (const part of path) {
            if (!safeKey(String(part)) || current == null || !hasOwn(Object(current), part))
                return fallback;
            current = current[part];
        }
        return current === undefined ? fallback : current;
    };

    const setPath = (target, key, value) => {
        const path = toKeyPath(key);
        if (!path.length || path.some(part => !safeKey(String(part))))
            throw new TypeError("set does not allow unsafe property names.");

        let current = target;
        for (let index = 0; index < path.length - 1; index++) {
            const part = path[index];
            if (!hasOwn(Object(current), part) || !isObject(current[part]))
                defineOwn(current, part, {});
            current = current[part];
        }
        defineOwn(current, path[path.length - 1], value);
        return target;
    };

    const each = (object, callback, thisArg) => {
        if (typeof callback !== "function")
            throw new TypeError("Expected a callback function.");
        for (const key of keysOf(object))
            callback.call(thisArg, object[key], key, object);
    };

    const define = (name, descriptor) => {
        if (hasOwn(objectPrototype, name))
            return false;
        Object.defineProperty(objectPrototype, name, {
            enumerable: false,
            configurable: true,
            ...descriptor
        });
        return true;
    };

    // Collection-style inspection properties.
    define("keys", { get: function () { return keysOf(this); } });
    define("values", { get: function () { return valuesOf(this); } });
    define("entries", { get: function () { return entriesOf(this); } });
    define("size", { get: function () { return keysOf(this).length; } });
    define("count", { get: function () { return keysOf(this).length; } });
    define("empty", { get: function () { return keysOf(this).length === 0; } });
    define("isEmpty", { get: function () { return keysOf(this).length === 0; } });
    define("firstKey", { get: function () { return keysOf(this)[0]; } });
    define("lastKey", { get: function () { const keys = keysOf(this); return keys[keys.length - 1]; } });
    define("first", { get: function () { const keys = keysOf(this); return keys.length ? this[keys[0]] : undefined; } });
    define("last", { get: function () { const keys = keysOf(this); return keys.length ? this[keys[keys.length - 1]] : undefined; } });

    // Access and mutation.
    define("has", {
        value: function (key) { return hasOwn(Object(this), key); },
        writable: true
    });
    define("contains", {
        value: function (key) { return hasOwn(Object(this), key); },
        writable: true
    });
    define("get", {
        value: function (key, fallback) { return getPath(this, key, fallback); },
        writable: true
    });
    define("set", {
        value: function (key, value) { return setPath(this, key, value); },
        writable: true
    });
    define("assign", {
        value: function () {
            for (const source of arguments) {
                if (source == null) continue;
                for (const key of keysOf(source))
                    defineOwn(this, key, source[key]);
            }
            return this;
        },
        writable: true
    });
    define("merge", {
        value: function () {
            const result = {};
            for (const key of keysOf(this)) defineOwn(result, key, this[key]);
            for (const source of arguments) {
                if (source == null) continue;
                for (const key of keysOf(source)) defineOwn(result, key, source[key]);
            }
            return result;
        },
        writable: true
    });
    define("defaults", {
        value: function () {
            for (const source of arguments) {
                if (source == null) continue;
                for (const key of keysOf(source))
                    if (!hasOwn(Object(this), key)) defineOwn(this, key, source[key]);
            }
            return this;
        },
        writable: true
    });
    define("clone", {
        value: function () {
            const result = {};
            for (const key of keysOf(this)) defineOwn(result, key, this[key]);
            return result;
        },
        writable: true
    });
    define("invert", {
        value: function () {
            const result = {};
            for (const key of keysOf(this))
                defineOwn(result, String(this[key]), key);
            return result;
        },
        writable: true
    });
    define("hasValue", {
        value: function (value) {
            return keysOf(this).some(key => Object.is(this[key], value));
        },
        writable: true
    });
    define("delete", {
        value: function (key) {
            const path = toKeyPath(key);
            if (!path.length || path.some(part => !safeKey(String(part)))) return this;
            let current = this;
            for (let index = 0; index < path.length - 1; index++) {
                if (current == null || !hasOwn(Object(current), path[index])) return this;
                current = current[path[index]];
            }
            if (current != null) delete current[path[path.length - 1]];
            return this;
        },
        writable: true
    });
    define("remove", {
        value: function (key) { return this.delete(key); },
        writable: true
    });
    define("clear", {
        value: function () {
            for (const key of keysOf(this)) delete this[key];
            return this;
        },
        writable: true
    });

    // Iteration and transformation.
    define("forEach", {
        value: function (callback, thisArg) {
            each(this, callback, thisArg);
            return this;
        },
        writable: true
    });
    define("map", {
        value: function (callback, thisArg) {
            if (typeof callback !== "function") throw new TypeError("map expects a callback function.");
            const result = {};
            each(this, (value, key, object) => defineOwn(result, key, callback.call(thisArg, value, key, object)));
            return result;
        },
        writable: true
    });
    define("mapValues", {
        value: function (callback, thisArg) {
            if (typeof callback !== "function") throw new TypeError("mapValues expects a callback function.");
            const result = {};
            each(this, (value, key, object) => defineOwn(result, key, callback.call(thisArg, value, key, object)));
            return result;
        },
        writable: true
    });
    define("filter", {
        value: function (callback, thisArg) {
            if (typeof callback !== "function") throw new TypeError("filter expects a callback function.");
            const result = {};
            each(this, (value, key, object) => { if (callback.call(thisArg, value, key, object)) defineOwn(result, key, value); });
            return result;
        },
        writable: true
    });
    define("find", {
        value: function (callback, thisArg) {
            if (typeof callback !== "function") throw new TypeError("find expects a callback function.");
            for (const key of keysOf(this))
                if (callback.call(thisArg, this[key], key, this)) return this[key];
            return undefined;
        },
        writable: true
    });
    define("findKey", {
        value: function (callback, thisArg) {
            if (typeof callback !== "function") throw new TypeError("findKey expects a callback function.");
            for (const key of keysOf(this))
                if (callback.call(thisArg, this[key], key, this)) return key;
            return undefined;
        },
        writable: true
    });
    define("reduce", {
        value: function (callback, initialValue) {
            if (typeof callback !== "function") throw new TypeError("reduce expects a callback function.");
            const keys = keysOf(this);
            let index = 0;
            let result = initialValue;
            if (arguments.length < 2) {
                if (!keys.length) throw new TypeError("Reduce of empty object with no initial value.");
                result = this[keys[index++]];
            }
            for (; index < keys.length; index++) {
                const key = keys[index];
                result = callback(result, this[key], key, this);
            }
            return result;
        },
        writable: true
    });
    define("some", {
        value: function (callback, thisArg) {
            if (typeof callback !== "function") throw new TypeError("some expects a callback function.");
            return keysOf(this).some(key => callback.call(thisArg, this[key], key, this));
        },
        writable: true
    });
    define("every", {
        value: function (callback, thisArg) {
            if (typeof callback !== "function") throw new TypeError("every expects a callback function.");
            return keysOf(this).every(key => callback.call(thisArg, this[key], key, this));
        },
        writable: true
    });
    define("toArray", {
        value: function (callback, thisArg) {
            const keys = keysOf(this);
            if (callback == null) return keys.map(key => this[key]);
            if (typeof callback !== "function") throw new TypeError("toArray expects a callback function.");
            return keys.map(key => callback.call(thisArg, this[key], key, this));
        },
        writable: true
    });
    define("toEntriesArray", { value: function () { return entriesOf(this); }, writable: true });
    define("pick", {
        value: function (selected) {
            const result = {};
            const keys = typeof selected === "string" ? [selected] : (selected || []);
            for (const key of keys)
                if (hasOwn(Object(this), key)) defineOwn(result, key, this[key]);
            return result;
        },
        writable: true
    });
    define("omit", {
        value: function (excluded) {
            const result = {};
            const omitted = new Set(excluded || []);
            for (const key of keysOf(this))
                if (!omitted.has(key)) defineOwn(result, key, this[key]);
            return result;
        },
        writable: true
    });
})();