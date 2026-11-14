// Adds convenient, read-only object inspection properties.
// Load explicitly with: Use deal/data/object;
//
// Examples:
//   Consider person is { A: 1, B: 2 }
//   Print person.keys       // ["A", "B"]
//   Print person.values     // [1, 2]
//   Print person.entries    // [["A", 1], ["B", 2]]
//   Print person.size       // 2
//   Print person.first      // 1
//   Print person.has("A")   // true
//
// These properties are installed on Object.prototype as non-enumerable
// getters/methods. Existing Object.prototype members are never overwritten.
// An own property with the same name takes precedence. Arrays retain their
// native keys(), values(), and entries() methods. Objects created with
// Object.create(null) do not inherit these enhancements.
//
// Since this module extends Object.prototype, load it only when this behavior
// is wanted in the current JavaScript environment.

(function () {
    "use strict";

    const ownKeys = function () {
        return Object.keys(this);
    };

    const ownValues = function () {
        const keys = Object.keys(this);
        return keys.map(key => this[key]);
    };

    const ownEntries = function () {
        return Object.keys(this).map(key => [key, this[key]]);
    };

    const define = function (name, descriptor) {
        if (Object.prototype.hasOwnProperty.call(Object.prototype, name))
            return false;

        Object.defineProperty(Object.prototype, name, {
            enumerable: false,
            configurable: true,
            ...descriptor
        });
        return true;
    };

    define("keys", { get: ownKeys });
    define("values", { get: ownValues });
    define("entries", { get: ownEntries });

    define("size", {
        get: function () {
            return Object.keys(this).length;
        }
    });
    define("count", {
        get: function () {
            return Object.keys(this).length;
        }
    });
    define("empty", {
        get: function () {
            return Object.keys(this).length === 0;
        }
    });
    define("isEmpty", {
        get: function () {
            return Object.keys(this).length === 0;
        }
    });

    define("firstKey", {
        get: function () {
            const keys = Object.keys(this);
            return keys.length ? keys[0] : undefined;
        }
    });
    define("lastKey", {
        get: function () {
            const keys = Object.keys(this);
            return keys.length ? keys[keys.length - 1] : undefined;
        }
    });
    define("first", {
        get: function () {
            const key = this.firstKey;
            return key === undefined ? undefined : this[key];
        }
    });
    define("last", {
        get: function () {
            const key = this.lastKey;
            return key === undefined ? undefined : this[key];
        }
    });

    define("has", {
        value: function (key) {
            return Object.prototype.hasOwnProperty.call(this, key);
        },
        writable: true
    });
    define("contains", {
        value: function (key) {
            return Object.prototype.hasOwnProperty.call(this, key);
        },
        writable: true
    });
    define("mapValues", {
        value: function (callback, thisArg) {
            if (typeof callback !== "function")
                throw new TypeError("mapValues expects a callback function.");

            const result = {};
            for (const key of Object.keys(this))
                result[key] = callback.call(thisArg, this[key], key, this);
            return result;
        },
        writable: true
    });
})();
