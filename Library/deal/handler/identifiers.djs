reserve window as			"window";
reserve document as			"window.document";
reserve console as			"window.console";

command handle(data = null){
    if (!data) {
        // Remove all sat handlers
        return all;
    }
}