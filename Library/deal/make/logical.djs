make.binary = function(object = null) {
    if (!object) return null;
    return object.toString().split('').map((char) => char.charCodeAt(0).toString(2)).join(' ');
};