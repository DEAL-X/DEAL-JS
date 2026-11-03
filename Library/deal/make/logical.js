Make.encoder = typeof TextEncoder !== "undefined" ? new TextEncoder() : null;
Make.decoder = typeof TextDecoder !== "undefined" ? new TextDecoder() : null;

Make.binary = function (object = null) {
    if (!object) return null;
    return object.toString().split('').map((char) => char.charCodeAt(0).toString(2)).join(' ');
};


Make.textFromBytes = function (bytes) {
    if (Make.decoder)
        return Make.decoder.decode(bytes);

    let binary = "";

    for (let i = 0; i < bytes.length; i += 0x8000) {
        binary += String.fromCharCode.apply(
            null,
            bytes.subarray(i, i + 0x8000)
        );
    }

    return decodeURIComponent(escape(binary));
};

Make.base64FromBytes = function (bytes) {
    let binary = "";

    for (let i = 0; i < bytes.length; i += 0x8000) {
        binary += String.fromCharCode.apply(
            null,
            bytes.subarray(i, i + 0x8000)
        );
    }

    return btoa(binary);
};

Make.bytesFromBase64 = function (value) {
    const binary = atob(value || "");
    const bytes = new Uint8Array(binary.length);

    for (let i = 0; i < binary.length; i++)
        bytes[i] = binary.charCodeAt(i);

    return bytes;
};