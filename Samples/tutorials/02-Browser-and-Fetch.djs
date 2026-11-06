// Tutorial 02: Browser control and Fetch.
// Browser convenience methods and browser properties are synchronous.
// Fetch is asynchronous, so await its result and response body.

Use deal/core;
Use deal/web;

// Open returns a BrowserTab immediately.
Consider tab is browser.open("https://example.com/");

// Browser properties are ordinary synchronous values.
Consider pageTitle is browser.tab.window.document.title;
Consider pageUrl is browser.tab.window.location.href;
Consider firstHeading is browser.tab.window.document.querySelector("h1");

// Normal browser actions also have synchronous convenience methods.
browser.tab.navigate("https://example.org/");
browser.tab.back();

// Fetch remains asynchronous.
Consider response is await fetch("https://example.com/");
If response.ok,
    Consider html is await response.text();
    Print html;
Otherwise,
    Log error "The request failed with status " + response.status;
