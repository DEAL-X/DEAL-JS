command Make.encoder = typeof TextEncoder !== "undefined" ? new TextEncoder() : null;
command Make.decoder = typeof TextDecoder !== "undefined" ? new TextDecoder() : null;
command Make.base64Alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789+/";

command Make.binary (object = null) {
    if (object == null) return null;
    return Array.from(String(object), char => char.charCodeAt(0).toString(2)).join(" ");
};

command Make.uint8Array (value = null) {
    if (value == null) return new Uint8Array(0);
    if (value instanceof Uint8Array) return value.slice();
    if (typeof ArrayBuffer !== "undefined" && value instanceof ArrayBuffer)
        return new Uint8Array(value.slice(0));
    if (typeof ArrayBuffer !== "undefined" && ArrayBuffer.isView(value))
        return new Uint8Array(value.buffer.slice(value.byteOffset, value.byteOffset + value.byteLength));
    if (Array.isArray(value)) return new Uint8Array(value);
    if (typeof value === "string") {
        if (Make.encoder) return Make.encoder.encode(value);
        const encoded = unescape(encodeURIComponent(value));
        const bytes = new Uint8Array(encoded.length);
        for (let i = 0; i < encoded.length; i++) bytes[i] = encoded.charCodeAt(i);
        return bytes;
    }
    throw new TypeError("Unsupported body type. Expected a string, byte array, or ArrayBuffer.");
};

command Make.textFromBytes (bytes) {
    bytes = Make.uint8Array(bytes);
    if (Make.decoder) return Make.decoder.decode(bytes);

    let binary = "";
    for (let i = 0; i < bytes.length; i += 0x8000)
        binary += String.fromCharCode.apply(null, bytes.subarray(i, i + 0x8000));

    try { return decodeURIComponent(escape(binary)); }
    catch { return binary; }
};

command Make.base64FromBytes (value) {
    const bytes = Make.uint8Array(value);
    let output = "";
    for (let i = 0; i < bytes.length; i += 3) {
        const a = bytes[i];
        const hasB = i + 1 < bytes.length;
        const hasC = i + 2 < bytes.length;
        const b = hasB ? bytes[i + 1] : 0;
        const c = hasC ? bytes[i + 2] : 0;
        output += Make.base64Alphabet[a >> 2];
        output += Make.base64Alphabet[((a & 3) << 4) | (b >> 4)];
        output += hasB ? Make.base64Alphabet[((b & 15) << 2) | (c >> 6)] : "=";
        output += hasC ? Make.base64Alphabet[c & 63] : "=";
    }
    return output;
};

command Make.bytesFromBase64 (value) {
    // Normalize Base64 without regular expressions. This also avoids regex
    // character-class parsing differences in embedded DEAL/ClearScript hosts.
    const source = String(value || "");
    let input = "";
    for (let i = 0; i < source.length; i++) {
        let code = source.charCodeAt(i);
        // Ignore the ASCII whitespace permitted in formatted Base64.
        if (code === 9 || code === 10 || code === 13 || code === 32) continue;
        let character = source[i];
        if (character === "-") character = "+";
        else if (character === "_") character = "/";
        else if (character !== "=" && Make.base64Alphabet.indexOf(character) < 0)
            throw new TypeError(
                "Invalid Base64 character at index " + i +
                " (code " + code + ")."
            );
        input += character;
    }

    if (input.length % 4 !== 0)
        throw new TypeError("Invalid Base64 input length: " + input.length + ".");

    const output = [];
    for (let i = 0; i < input.length; i += 4) {
        const isLastBlock = i + 4 === input.length;
        const a = Make.base64Alphabet.indexOf(input[i]);
        const b = Make.base64Alphabet.indexOf(input[i + 1]);
        const cPadding = input[i + 2] === "=";
        const dPadding = input[i + 3] === "=";
        const c = cPadding ? 0 : Make.base64Alphabet.indexOf(input[i + 2]);
        const d = dPadding ? 0 : Make.base64Alphabet.indexOf(input[i + 3]);

        if (a < 0 || b < 0 || c < 0 || d < 0 ||
            (cPadding && !dPadding) ||
            ((cPadding || dPadding) && !isLastBlock))
            throw new TypeError("Invalid Base64 padding or quartet at index " + i + ".");

        output.push((a << 2) | (b >> 4));
        if (!cPadding) output.push(((b & 15) << 4) | (c >> 2));
        if (!dPadding) output.push(((c & 3) << 6) | d);
    }
    return new Uint8Array(output);
};