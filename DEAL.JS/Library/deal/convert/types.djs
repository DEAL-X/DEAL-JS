command binary(object = null) {
    if (!object) return binary;
    return object.toString().split('').map((char) => char.charCodeAt(0).toString(2)).join(' ');
}