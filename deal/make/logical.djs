#MAKE.encoder:identifier = typeof TextEncoder !== "undefined" ? new TextEncoder() : null;
#MAKE.decoder:identifier = typeof TextDecoder !== "undefined" ? new TextDecoder() : null;
#MAKE.base64Alphabet:identifier = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789+/";

#MAKE.binary:function = function (object = null) {
    if (object == null) return null;
    return Array.from(String(object), char => char.charCodeAt(0).toString(2)).join(" ");
};

#MAKE.uint8Array:function = function (value = null) {
    if (value == null) return new Uint8Array(0);
    if (value instanceof Uint8Array) return value.slice();
    if (typeof ArrayBuffer !== "undefined" && value instanceof ArrayBuffer)
        return new Uint8Array(value.slice(0));
    if (typeof ArrayBuffer !== "undefined" && ArrayBuffer.isView(value))
        return new Uint8Array(value.buffer.slice(value.byteOffset, value.byteOffset + value.byteLength));
    if (Array.isArray(value)) return new Uint8Array(value);
    if (typeof value === "string") {
        if (MAKE.encoder) return MAKE.encoder.encode(value);
        const encoded = unescape(encodeURIComponent(value));
        const bytes = new Uint8Array(encoded.length);
        for (let i = 0; i < encoded.length; i++) bytes[i] = encoded.charCodeAt(i);
        return bytes;
    }
    throw new TypeError("Unsupported body type. Expected a string, byte array, or ArrayBuffer.");
};

#MAKE.textFromBytes:function = function (bytes) {
    bytes = MAKE.uint8Array(bytes);
    if (MAKE.decoder) return MAKE.decoder.decode(bytes);

    let binary = "";
    for (let i = 0; i < bytes.length; i += 0x8000)
        binary += String.fromCharCode.apply(null, bytes.subarray(i, i + 0x8000));

    try { return decodeURIComponent(escape(binary)); }
    catch { return binary; }
};

#MAKE.base64FromBytes:function = function (value) {
    const bytes = MAKE.uint8Array(value);
    let output = "";
    for (let i = 0; i < bytes.length; i += 3) {
        const a = bytes[i];
        const hasB = i + 1 < bytes.length;
        const hasC = i + 2 < bytes.length;
        const b = hasB ? bytes[i + 1] : 0;
        const c = hasC ? bytes[i + 2] : 0;
        output += MAKE.base64Alphabet[a >> 2];
        output += MAKE.base64Alphabet[((a & 3) << 4) | (b >> 4)];
        output += hasB ? MAKE.base64Alphabet[((b & 15) << 2) | (c >> 6)] : "=";
        output += hasC ? MAKE.base64Alphabet[c & 63] : "=";
    }
    return output;
};

#MAKE.bytesFromBase64:function = function (value) {
    const input = String(value || "").replace(/\\s+/g, "");
    if (input.length % 4 !== 0 || /[^A-Za-z0-9+\/=]/.test(input))
        throw new TypeError("Invalid Base64 input.");

    const output = [];
    for (let i = 0; i < input.length; i += 4) {
        const a = MAKE.base64Alphabet.indexOf(input[i]);
        const b = MAKE.base64Alphabet.indexOf(input[i + 1]);
        const c = input[i + 2] === "=" ? 0 : MAKE.base64Alphabet.indexOf(input[i + 2]);
        const d = input[i + 3] === "=" ? 0 : MAKE.base64Alphabet.indexOf(input[i + 3]);
        if (a < 0 || b < 0 || c < 0 || d < 0)
            throw new TypeError("Invalid Base64 input.");
        output.push((a << 2) | (b >> 4));
        if (input[i + 2] !== "=") output.push(((b & 15) << 4) | (c >> 2));
        if (input[i + 3] !== "=") output.push(((c & 3) << 6) | d);
    }
    return new Uint8Array(output);
};