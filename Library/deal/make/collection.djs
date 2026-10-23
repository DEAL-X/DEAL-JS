make.array = function (object) {
	if (isArray(object))
		if (count(object) === 1)
			return array(object[0]);
		else if (Array.isArray(obj)) return object;
		else return Array.from(
			(function* () {
				let i = 0;
				for (const item of object)
					yield func(item, i++);
			})()
		);
	else return [object];
};

make.matrix = function(object) {
    let res = [];

    if (object == null) return res;
    
    // Convert a single object into a header row and a value row.
    if (!Array.isArray(object)) {
        if (typeof object !== "object") return [[object]];

        let keys = Object.keys(object);
        let values = [];

        for (let key of keys) {
            values.push(object[key]);
        }

        res.push(keys);
        res.push(values);
        return res;
    }

    if (object.length === 0) return res;

    // Check whether the array represents a collection of objects.
    let isObjectRows = object.every(
        item => item !== null &&
                typeof item === "object" &&
                !Array.isArray(item)
    );

    if (isObjectRows) {
        let keys = [];

        // Collect all distinct property names in encounter order.
        for (let item of object) {
            for (let key of Object.keys(item)) {
                if (!keys.includes(key)) keys.push(key);
            }
        }

        res.push(keys);

        // Align each object's values with the shared header.
        for (let item of object) {
            let row = [];
            let lastPresent = -1;

            for (let i = 0; i < keys.length; i++) {
                let key = keys[i];

                if (Object.prototype.hasOwnProperty.call(item, key)) {
                    row[i] = item[key];
                    lastPresent = i;
                } else {
                    row[i] = undefined;
                }
            }

            // Omit trailing columns that this object does not contain.
            row.length = lastPresent + 1;
            res.push(row);
        }

        return res;
    }

    // Preserve an existing matrix of scalar values.
    let isScalarMatrix = object.every(
        row => Array.isArray(row) &&
               row.every(value =>
                   value == null || typeof value !== "object"
               )
    );

    if (isScalarMatrix) {
        return object.map(row => row.slice());
    }

    // Recursively convert nested arrays or objects and append their rows.
    for (let item of object) {
        if (Array.isArray(item)) {
            let nested = convert.to.matrix(item);
            res.push(...nested);
        } else if (item !== null && typeof item === "object") {
            let nested = convert.to.matrix(item);
            res.push(...nested);
        } else {
            res.push([item]);
        }
    }

    return res;
};
