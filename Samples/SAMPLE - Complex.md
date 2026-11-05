# A complex sample

This example demonstrates a larger workflow with input data, Fetch, browser navigation, and error handling. It is a structural example; configure the source data and destination for your host before running it.

## DEAL source

```deal
use deal/core;
use deal/web;

Consider products is ["item-1", "item-2", "item-3"];
Consider destination is [];

For each product of products,
    Try,
        Consider response is await fetch("https://example.com/search?q=" + product);
        If response.ok,
            Consider data is await response.json();
            Append destination, data;
        Else,
            log warning "The request failed for " + product;
    Catch problem,
        log error problem;

Consider tab is await browser.open("https://example.com");
Consider title is browser.tab.window.document.title;
Consider currentUrl is browser.tab.window.location.href;

await browser.tab.navigate("https://example.org/results");
await browser.tab.back();

log "Collected " + destination.length + " result sets.";
```

## Equivalent JavaScript

```javascript
const products = ["item-1", "item-2", "item-3"];
const destination = [];

for (const product of products) {
    try {
        const response = await fetch(
            "https://example.com/search?q=" + encodeURIComponent(product)
        );

        if (response.ok) {
            destination.push(await response.json());
        } else {
            console.warn("The request failed for " + product);
        }
    } catch (problem) {
        console.error(problem);
    }
}

await browser.open("https://example.com");
const title = browser.tab.window.document.title;
const currentUrl = browser.tab.window.location.href;

await browser.tab.navigate("https://example.org/results");
await browser.tab.back();

console.log("Collected " + destination.length + " result sets.");
```

## API notes

- `browser.open(url)` opens a new tab.
- `browser.tab.load(url)` replaces the current document without adding a history entry.
- `browser.tab.navigate(url)` performs normal navigation and adds a history entry.
- `browser.tab.window.document.title` and `browser.tab.window.location.href` are synchronous properties.
- `browser.tab.close()` closes the current tab and updates `browser.tab` to the tab selected next.
- `USE` skips a dependency that has already been loaded; `REUSE` processes it again.
