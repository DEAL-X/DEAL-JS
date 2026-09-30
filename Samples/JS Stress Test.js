/*
 * ============================================================
 * MiMFa DaRQ JavaScript Compiler Stress Test
 * ============================================================
 * This file intentionally contains many JavaScript structures.
 * It should remain semantically equivalent after compilation.
 */

// ------------------------------------------------------------
// Constants and primitive values
// ------------------------------------------------------------

const VERSION = "4.0.0";
const EMPTY = null;
const UNKNOWN = undefined;
const ENABLED = true;
const DISABLED = false;

let counter = 0;
var legacyValue = "legacy";

const numberValues = [
    0,
    1,
    -1,
    3.14159,
    1e6,
    0xFF,
    0b101010,
    0o755
];

const stringValues = [
    "simple",
    'single quoted',
    "escaped \"quote\"",
    "line\nbreak",
    `template ${VERSION}`
];


// ------------------------------------------------------------
// Basic expressions
// ------------------------------------------------------------

let arithmetic =
    10 + 20 * 3 - 4 / 2 + (8 ** 2);

let comparison =
    arithmetic >= 100 &&
    arithmetic !== 0 &&
    arithmetic <= 1000;

let logicalValue =
    ENABLED && !DISABLED || false;

let nullableValue =
    UNKNOWN ?? "default value";

let chainedValue =
    EMPTY?.property?.child?.value ?? "fallback";

let conditionalValue =
    comparison ? "valid" : "invalid";


// ------------------------------------------------------------
// Object and nested object structures
// ------------------------------------------------------------

const configuration = {
    name: "MiMFa Scraper",
    version: VERSION,

    enabled: true,

    settings: {
        timeout: 5000,
        retries: 3,
        headers: {
            "Accept": "application/json",
            "User-Agent": "MiMFa-Scraper"
        }
    },

    values: [
        10,
        20,
        30,
        {
            id: 1,
            name: "nested"
        }
    ],

    calculate(value = 0) {
        return value * 2;
    },

    ["dynamic" + "Property"]: "dynamic value"
};


// ------------------------------------------------------------
// Object destructuring
// ------------------------------------------------------------

const {
    name: applicationName,
    version: applicationVersion,
    settings: {
        timeout,
        retries
    }
} = configuration;

const {
    name = "Unknown",
    ...remainingConfiguration
} = configuration;


// ------------------------------------------------------------
// Array destructuring
// ------------------------------------------------------------

const values = [10, 20, 30, 40, 50];

const [
    first,
    second,
    ...remainingValues
] = values;

const [
    ,
    ,
    third
] = values;


// ------------------------------------------------------------
// Spread and rest
// ------------------------------------------------------------

const copiedValues = [...values];

const combinedValues = [
    ...values,
    60,
    70,
    ...numberValues
];

const combinedConfiguration = {
    ...configuration,
    debug: true,
    version: "test"
};

function sum(...items) {
    return items.reduce(
        (total, item) => total + item,
        0
    );
}

const total = sum(
    ...values,
    100,
    200
);


// ------------------------------------------------------------
// Normal functions
// ------------------------------------------------------------

function calculateGrade(score, minimum = 0) {
    if (score == null) {
        return "unknown";
    }

    if (score >= 19) {
        return "A+";
    }

    if (score >= 17) {
        return "A";
    }

    if (score >= 15) {
        return "B";
    }

    if (score >= minimum) {
        return "C";
    }

    return "F";
}


// ------------------------------------------------------------
// Arrow functions
// ------------------------------------------------------------

const double = value => value * 2;

const multiply = (a, b) => a * b;

const createUser = (
    name,
    age = 18
) => ({
    name,
    age,
    active: true
});

const complexArrow = (
    value,
    index
) => {
    const result = value * index;

    return result > 100
        ? result
        : result + 100;
};


// ------------------------------------------------------------
// Immediately invoked functions
// ------------------------------------------------------------

const initialized = (() => {
    const initial = 10;
    const increment = 5;

    return initial + increment;
})();

(function () {
    counter++;

    if (counter > 10) {
        counter = 0;
    }
})();


// ------------------------------------------------------------
// Loops
// ------------------------------------------------------------

for (
    let i = 0;
    i < values.length;
    i++
) {
    const value = values[i];

    if (value === 30) {
        continue;
    }

    if (value > 40) {
        break;
    }

    counter += value;
}

for (
    const value of values
) {
    console.log(
        "for-of:",
        value
    );
}

for (
    const index in configuration
) {
    console.log(
        "for-in:",
        index,
        configuration[index]
    );
}

let whileCounter = 0;

while (whileCounter < 5) {
    whileCounter++;

    if (whileCounter === 3) {
        continue;
    }
}

let doCounter = 0;

do {
    doCounter++;
} while (doCounter < 3);


// ------------------------------------------------------------
// Nested conditions
// ------------------------------------------------------------

function classify(value) {
    if (value == null) {
        return "empty";
    } else if (typeof value === "number") {
        if (value > 100) {
            return "large";
        } else if (value > 50) {
            return "medium";
        } else {
            return "small";
        }
    } else if (typeof value === "string") {
        return value.length > 10
            ? "long string"
            : "short string";
    }

    return "unknown";
}


// ------------------------------------------------------------
// Switch
// ------------------------------------------------------------

function getStatus(code) {
    switch (code) {
        case 200:
        case 201:
            return "success";

        case 301:
        case 302:
            return "redirect";

        case 400:
        case 401:
        case 403:
            return "client error";

        case 500:
        case 502:
        case 503:
            return "server error";

        default:
            return "unknown";
    }
}


// ------------------------------------------------------------
// Try / catch / finally
// ------------------------------------------------------------

function parseConfiguration(text) {
    try {
        const data = JSON.parse(text);

        if (!data || typeof data !== "object") {
            throw new TypeError(
                "Invalid configuration"
            );
        }

        return data;
    } catch (error) {
        console.error(
            "Could not parse configuration:",
            error
        );

        return null;
    } finally {
        console.log(
            "Configuration parsing finished"
        );
    }
}


// ------------------------------------------------------------
// Custom errors
// ------------------------------------------------------------

class ApplicationError extends Error {
    constructor(
        message,
        code = "UNKNOWN"
    ) {
        super(message);

        this.name = "ApplicationError";
        this.code = code;
        this.timestamp = Date.now();
    }

    toJSON() {
        return {
            name: this.name,
            message: this.message,
            code: this.code,
            timestamp: this.timestamp
        };
    }
}


// ------------------------------------------------------------
// Base class
// ------------------------------------------------------------

class Vehicle {
    #speed = 0;

    constructor(
        brand,
        model,
        year = new Date().getFullYear()
    ) {
        this.brand = brand;
        this.model = model;
        this.year = year;
    }

    get speed() {
        return this.#speed;
    }

    set speed(value) {
        this.#speed =
            Number(value) || 0;
    }

    accelerate(amount = 10) {
        this.speed += amount;

        return this.speed;
    }

    brake(amount = 10) {
        this.speed =
            Math.max(
                0,
                this.speed - amount
            );

        return this.speed;
    }

    describe() {
        return `${this.year} ${this.brand} ${this.model}`;
    }

    static createDefault() {
        return new Vehicle(
            "MiMFa",
            "Default"
        );
    }
}


// ------------------------------------------------------------
// Derived class
// ------------------------------------------------------------

class Car extends Vehicle {
    constructor(
        brand,
        model,
        year,
        doors = 4
    ) {
        super(
            brand,
            model,
            year
        );

        this.doors = doors;
    }

    describe() {
        return (
            super.describe() +
            ` (${this.doors} doors)`
        );
    }

    drive(distance) {
        const result = {
            vehicle: this.describe(),
            distance,
            speed: this.speed
        };

        return result;
    }
}


// ------------------------------------------------------------
// Static class fields and computed members
// ------------------------------------------------------------

class Calculator {
    static name = "Calculator";

    static PI = Math.PI;

    static [
        "double"
    ](value) {
        return value * 2;
    }

    static multiply(a, b) {
        return a * b;
    }
}


// ------------------------------------------------------------
// Generator function
// ------------------------------------------------------------

function* numberGenerator(
    start,
    end
) {
    for (
        let value = start;
        value <= end;
        value++
    ) {
        yield value;
    }
}

const generatedNumbers = [];

for (
    const number of numberGenerator(1, 5)
) {
    generatedNumbers.push(number);
}


// ------------------------------------------------------------
// Async functions
// ------------------------------------------------------------

async function delay(milliseconds) {
    return new Promise(
        resolve => {
            setTimeout(
                () => resolve(true),
                milliseconds
            );
        }
    );
}

async function loadData(url) {
    try {
        const response =
            await fetch(url);

        if (!response.ok) {
            throw new ApplicationError(
                "Request failed",
                response.status
            );
        }

        return await response.json();
    } catch (error) {
        console.error(
            "Loading failed:",
            error
        );

        throw error;
    }
}


// ------------------------------------------------------------
// Promise chains
// ------------------------------------------------------------

function processData(url) {
    return fetch(url)
        .then(response => {
            if (!response.ok) {
                throw new Error(
                    "HTTP error"
                );
            }

            return response.json();
        })
        .then(data => {
            return data.items ?? [];
        })
        .filter;
}


// ------------------------------------------------------------
// Promise.all / Promise.race
// ------------------------------------------------------------

async function loadMultiple(urls) {
    const requests = urls.map(
        url => fetch(url)
            .then(response => response.json())
    );

    const results =
        await Promise.all(requests);

    return results;
}

async function loadFirst(urls) {
    const requests = urls.map(
        url => fetch(url)
    );

    return await Promise.race(
        requests
    );
}


// ------------------------------------------------------------
// Regular expressions
// ------------------------------------------------------------

const emailPattern =
    /^[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Za-z]{2,}$/i;

const whitespacePattern =
    /\s+/g;

const normalizedText =
    "  MiMFa   DaRQ  "
        .replace(
            whitespacePattern,
            " "
        )
        .trim();

const isEmail =
    emailPattern.test(
        "developer@mimfa.net"
    );


// ------------------------------------------------------------
// Template literals
// ------------------------------------------------------------

const message = `
    Application: ${applicationName}
    Version: ${applicationVersion}
    Timeout: ${timeout}
    Retries: ${retries}
    Status: ${getStatus(200)}
`;

const dynamicMessage =
    `The result is ${sum(
        10,
        20,
        30
    )}.`;


// ------------------------------------------------------------
// Map
// ------------------------------------------------------------

const users = new Map();

users.set(
    "admin",
    {
        name: "Administrator",
        level: 10
    }
);

users.set(
    "guest",
    {
        name: "Guest",
        level: 1
    }
);

for (
    const [username, user] of users
) {
    console.log(
        username,
        user.name,
        user.level
    );
}


// ------------------------------------------------------------
// Set
// ------------------------------------------------------------

const uniqueNumbers =
    new Set([
        1,
        2,
        2,
        3,
        3,
        4
    ]);

uniqueNumbers.add(5);

if (uniqueNumbers.has(3)) {
    console.log(
        "3 exists"
    );
}


// ------------------------------------------------------------
// Higher-order functions
// ------------------------------------------------------------

const processedValues =
    values
        .filter(
            value => value > 20
        )
        .map(
            value => value * 2
        )
        .reduce(
            (total, value) =>
                total + value,
            0
        );


// ------------------------------------------------------------
// Nested callbacks
// ------------------------------------------------------------

function executePipeline(
    input,
    onSuccess,
    onError
) {
    try {
        const result =
            input
                .map(
                    value => value * 2
                )
                .filter(
                    value => value > 10
                );

        onSuccess(
            result
        );
    } catch (error) {
        onError(
            error
        );
    }
}


// ------------------------------------------------------------
// Optional chaining and nullish assignment
// ------------------------------------------------------------

let application = {
    settings: {
        display: {
            theme: "dark"
        }
    }
};

const theme =
    application
        ?.settings
        ?.display
        ?.theme
        ?? "light";

application.settings ??= {};

application.settings.display ??= {};

application.settings.display.theme ??=
    "light";


// ------------------------------------------------------------
// Logical assignments
// ------------------------------------------------------------

let a = null;
let b = false;
let c = 10;

a ??= 100;
b ||= true;
c &&= 20;


// ------------------------------------------------------------
// Delete / typeof / instanceof / in
// ------------------------------------------------------------

const temporaryObject = {
    name: "temporary",
    value: 123
};

delete temporaryObject.value;

const type =
    typeof temporaryObject;

const vehicle =
    new Car(
        "MiMFa",
        "Scraper",
        2026
    );

const isVehicle =
    vehicle instanceof Vehicle;

const hasBrand =
    "brand" in vehicle;


// ------------------------------------------------------------
// Labelled statement
// ------------------------------------------------------------

outerLoop:
for (
    let i = 0;
    i < 10;
    i++
) {
    innerLoop:
    for (
        let j = 0;
        j < 10;
        j++
    ) {
        if (i === 5 && j === 5) {
            break outerLoop;
        }

        if (j === 2) {
            continue innerLoop;
        }
    }
}


// ------------------------------------------------------------
// Nested object with functions and expressions
// ------------------------------------------------------------

const applicationObject = {
    metadata: {
        name: "MiMFa",
        version: VERSION
    },

    state: {
        running: false,
        count: 0
    },

    start() {
        this.state.running = true;

        return this;
    },

    stop() {
        this.state.running = false;

        return this;
    },

    increment(value = 1) {
        this.state.count += value;

        return this.state.count;
    },

    getStatus() {
        return {
            ...this.metadata,
            ...this.state
        };
    }
};


// ------------------------------------------------------------
// Chained method calls
// ------------------------------------------------------------

const chainResult =
    applicationObject
        .start()
        .increment(5);

applicationObject
    .increment(10)
    .increment(20)
    .stop();


// ------------------------------------------------------------
// Computed access
// ------------------------------------------------------------

const propertyName = "model";

const computedValue =
    vehicle[propertyName];

vehicle[
    "speed"
] = 80;


// ------------------------------------------------------------
// Complex conditional expression
// ------------------------------------------------------------

const finalResult =
    vehicle.speed > 100
        ? "fast"
        : vehicle.speed > 50
            ? "medium"
            : vehicle.speed > 0
                ? "slow"
                : "stopped";


// ------------------------------------------------------------
// Nested async IIFE
// ------------------------------------------------------------

(async () => {
    const car = new Car(
        "MiMFa",
        "Scraper",
        2026
    );

    car.speed = 60;

    const before =
        car.speed;

    await delay(10);

    const after =
        car.accelerate(20);

    console.log(
        car.describe(),
        before,
        after
    );
})();


// ------------------------------------------------------------
// Complex final expression
// ------------------------------------------------------------

const finalObject = {
    version: VERSION,

    application: applicationObject,

    vehicle: {
        description: vehicle.describe(),
        speed: vehicle.speed,
        isVehicle
    },

    calculations: {
        arithmetic,
        total,
        processedValues,
        chainResult
    },

    flags: {
        isEmail,
        hasBrand,
        type
    },

    generatedNumbers,

    nested: {
        theme,
        finalResult
    }
};

console.log(
    "Final object:",
    finalObject
);