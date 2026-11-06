// Tutorial 02: Browser control and Fetch.
// Browser convenience methods and browser properties are synchronous.
// The .NET host selects Chrome (default), Edge, or Firefox before running this file.
// `Fetch`, `load` and `go` are asynchronous, so await thair results and response body.

Use deal/core;
Use deal/web;

// Open returns a BrowserTab immediately.
Consider tab is browser open https://example.com/;

// Browser properties are ordinary synchronous values.
Consider pageTitle is browser.tab.window.document.title;
Consider pageUrl is browser.tab.window.location.href;
Consider firstHeading is browser.tab.window.document.querySelector("h1");

// Normal browser actions also have synchronous convenience methods.
browser.tab.navigate("https://example.org/");
browser.tab.back();

// Fetch remains asynchronous.
Fetch https://example.com/,
Then
    If argument.ok,
        Consider html is argument.text();
        Print html;
Otherwise
    Log error "The request failed with status " + response.status;
