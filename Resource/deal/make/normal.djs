use deal/make/string;

// The normalization method could contain:
//     * one of NFC, NFD, NFKC, NFKD, or N for the normalization form
//     * s: trim start
//     * S: trim end
//     * b: remove whitespace between words
//     * B: trim both sides
//     * t: replace all whitespace
//     * T: normalize whitespace
//     * w: remove word characters
//     * W: remove non-word characters
//     * v: remove digit characters
//     * V: remove non-digit characters
//     * l: lower case letters
//     * L: upper case letters
//     * u: encode URI component
//     * U: decode URI component
#Make.normal:function = function (text = null, method = "NFKCTB", replacement = "") {
    if (text === null || text === undefined) return text;

    const options = String(method);
    const upper = options.toUpperCase();
    let form = null;

    // Unicode normalization forms are exact identifiers. Do not construct
    // them by concatenating every matching letter in the option string.
    for (const candidate of ["NFKC", "NFKD", "NFC", "NFD"]) {
        if (upper.includes(candidate)) {
            form = candidate;
            break;
        }
    }

    text = Make.string(text);
    if (form && typeof text.normalize === "function")
        text = text.normalize(form);

    if (options.includes("s")) text = text.trimStart();
    if (options.includes("S")) text = text.trimEnd();
    if (options.includes("b")) text = text.replace(/\\s+/g, "");
    if (options.includes("B")) text = text.trim();
    if (options.includes("t")) text = text.replace(/\\s+/g, replacement);
    if (options.includes("T")) text = text.replace(/(^[ \\t]+)|([ \\t]+$)/gm, replacement).replace(/[ \\t]+/g, " ").replace(/[\\r\\n]+/g, "\\n");

    if (options.includes("w")) text = text.replace(/\\w+/gu, replacement);
    if (options.includes("W")) text = text.replace(/\\W+/gu, replacement);
    if (options.includes("v")) text = text.replace(/\\d+/gu, replacement);
    if (options.includes("V")) text = text.replace(/\\D+/gu, replacement);

    if (options.includes("l")) text = text.toLowerCase();
    if (options.includes("L")) text = text.toUpperCase();

    if (options.includes("u")) text = encodeURIComponent(text);
    if (options.includes("U")) text = decodeURIComponent(text);

    return text;
};