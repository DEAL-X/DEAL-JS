# A simple sample

This example shows a small DEAL workflow with a browser tab and the current browser API.

## DEAL source

```deal
use deal/core;
use deal/web;

Consider targetUrl is "https://example.com";
Consider headings is [];

Try,
    await browser.open(targetUrl);
    Consider pageTitle is browser.tab.window.document.title;
    Consider pageUrl is browser.tab.window.location.href;
    log "Loaded " + pageTitle + " from " + pageUrl;
Catch problem,
    log error problem;
```

## Equivalent JavaScript

```javascript
const targetUrl = "https://example.com";
const headings = [];

try {
    await browser.open(targetUrl);
    const pageTitle = browser.tab.window.document.title;
    const pageUrl = browser.tab.window.location.href;
    console.log("Loaded " + pageTitle + " from " + pageUrl);
} catch (problem) {
    console.error(problem);
}
```

Notice that browser properties such as `document.title` and `location.href` are read synchronously. Browser operations such as `browser.open()` remain asynchronous.
