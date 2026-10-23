let #loaded = false;
let #fetched = false;
let #response = null;
let #data = null;

// Update the current execution-context handlers using the inputted data.
// When called without data, reset all handlers to their initial state.
//
// A loaded document updates LOADED and DATA.
// A web response updates FETCHED, RESPONSE, and DATA when successful.
// Ordinary data updates DATA unless status is explicitly false.
command handle(inputtedData = null, status = null) {
    // Reset all handlers when no input data is provided.
    if (inputtedData == null) {
        if(status == null) {
            loaded = false;
            fetched = false;
            response = null;
        }
        data = null;
        return handle;
    }

    // Determine whether the input represents a document.
    let inputtedDocument = null;

    if (inputtedData.nodeType === 9) {
        inputtedDocument = inputtedData;
    } else if (inputtedData.document && inputtedData.document.nodeType === 9) {
        inputtedDocument = inputtedData.document;
    }

    // Handle an HTML or XML document.
    if (inputtedDocument != null) {
        loaded = (status ?? true);

        if (status == null || status === true) {
            response = inputtedData;
            data = inputtedDocument;
        }

        return data;
    }

    // Recognize a Fetch API Response or an XMLHttpRequest response.
    let isFetchResponse =
        typeof inputtedData.ok === "boolean" &&
        typeof inputtedData.status === "number";

    let isXHRResponse =
        typeof inputtedData.status === "number" &&
        (
            "responseText" in inputtedData ||
            "responseXML" in inputtedData ||
            "response" in inputtedData
        );

    if (isFetchResponse || isXHRResponse) {
        // Determine whether the web request completed successfully.
        let successful = false;

        if (isFetchResponse) {
            successful = inputtedData.ok;
        } else {
            successful =
                (inputtedData.status >= 200 && inputtedData.status < 300) ||
                inputtedData.status === 304;
        }

        // Preserve the original response for subsequent response operations.
        response = inputtedData;

        // An explicit false status overrides a successful response.
        fetched = successful && (status == null || status === true);

        if (fetched) {
            // Prefer an available parsed document or response payload.
            if ("responseXML" in inputtedData && inputtedData.responseXML != null) {
                data = inputtedData.responseXML;
            } else if ("response" in inputtedData && inputtedData.response != null) {
                data = inputtedData.response;
            } else if ("content" in inputtedData && inputtedData.content != null) {
                data = inputtedData.content;
            } else if ("data" in inputtedData && inputtedData.data != null) {
                data = inputtedData.data;
            } else if ("responseText" in inputtedData && inputtedData.responseText != null) {
                data = inputtedData.responseText;
            } else {
                // A native Fetch Response has an asynchronously readable body.
                // Preserve it when its content is not synchronously available.
                data = inputtedData;
            }

            // Update LOADED only when the response payload is a document.
            loaded = data != null && data.nodeType === 9;
        } else if (status == null || status === true) {
            // Retain the unsuccessful response as data when no failure status
            // was explicitly supplied.
            loaded = false;
            data = inputtedData;
        }

        return data;
    }

    // Handle ordinary values without treating falsy values as missing data.
    if (status == null || status === true) {
        data = inputtedData;
    } else {
        loaded = false;
        fetched = false;
    }

    return data;
}