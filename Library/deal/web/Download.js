class Download {
    static _RECEIVED = null;

    static content(content = null, name = null, type = null, encoding = null, status = null) {
        content = content ?? _RECEIVED;
        name = name ?? 'download.txt';
        type = type ?? 'application/octet-stream';
        const link = document.createElement('a');
        const blob = content instanceof Blob ? content : new Blob([content], { type: type });
        link.href = URL.createObjectURL(blob);
        link.download = name.replace(/['"]/g, '');
        link.click();
        link.remove();
        return name;
    }
}