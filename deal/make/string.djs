#MAKE.string:function = function (object = null) {
    if (object === null || object === undefined)
        return String(object);

    if (typeof object === "string")
        return object;

    if (typeof object === "object") {
        // DOM values must be handled before JSON serialization.
        const isInstance = name =>
            typeof globalThis[name] === "function" && object instanceof globalThis[name];

        if (isInstance("HTMLInputElement")) return object.value;
        if (isInstance("HTMLImageElement")) return object.src;
        if (isInstance("HTMLSourceElement")) return object.src;
        if (isInstance("HTMLIFrameElement")) return object.src;
        if (isInstance("HTMLScriptElement")) return object.src || object.textContent;
        if (isInstance("HTMLStyleElement")) return object.textContent;
        if (isInstance("HTMLProgressElement")) return object.value;
        if (isInstance("HTMLElement")) return object.innerText ?? object.textContent ?? "";

        return JSON.stringify(object);
    }

    return String(object);
};