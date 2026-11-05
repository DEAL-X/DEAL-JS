# DEAL.JS quick-start sample

This guide introduces the DEAL.JS syntax, its JavaScript interoperability, module loading, and the current browser API. The examples use sentence case for prose and comments; language keywords are intentionally shown in their normal sentence-case form.

## 1. Write a small DEAL program

A DEAL program can express a familiar JavaScript operation using readable instructions:

```deal
use deal/core;

Consider values is [1, 2, 3];
For each value of values,
    print value;
```

The compiler translates DEAL instructions into deterministic JavaScript. Ordinary JavaScript can appear in the same source whenever it is more suitable:

```javascript
const values = [1, 2, 3];
for (const value of values) {
    console.log(value);
}
```

## 2. Load libraries

Use `USE` to load a library or resource into the compiler:

```deal
use deal/core;
use deal/web;
use ./Libraries/Selectors;
```

A resource can be referenced by a library name, a relative path, an absolute local path, or a remote URL. The shared resource/path provider resolves local paths and normalizes web addresses.

`USE` and `REUSE` have the same resolution rules but different loading behavior:

```deal
use ./Libraries/Selectors;
use ./Libraries/Selectors;       // Skipped if already loaded.
reuse ./Libraries/Selectors;     // Processed again, even if already loaded.
reuse https://example.com/modules/selectors.djs;
```

`REUSE` forces the dependency to be processed again. It does not independently force a new download or refresh a remote cache.

## 3. Use the browser object

Browser lifecycle operations belong to `browser`; navigation and history operations belong to the current `browser.tab`:

```javascript
// Open a new tab.
const openedTab = await browser.open("example.com");

// Read current-tab properties synchronously.
const tab = browser.tab;
const title = tab.window.document.title;
const url = tab.window.location.href;
const width = tab.window.innerWidth;
const online = tab.navigator.onLine;

// Replace the current document without adding a history entry.
await tab.load("example.org/start");

// Navigate normally and add a history entry.
await tab.navigate("example.org/next");
await tab.back();
await tab.forward();
await tab.reload();

// Replace or add a same-document history entry.
await tab.locate("example.org/next#section");
await tab.relocate("example.org/next#details");

// Close the current tab. browser.tab then refers to the tab selected next.
await browser.tab.close();

// Close the browser instance.
browser.close();
```

The API is divided by responsibility:

- `browser.open(url)` opens a new tab.
- `browser.close()` closes the browser instance.
- `browser.tab` refers to the current tab.
- `browser.tab.load(url)` replaces the current document without adding a history entry.
- `browser.tab.navigate(url)` performs normal navigation and adds a history entry.
- `browser.tab.back()`, `forward()`, `go(delta)`, and `reload()` control current-tab navigation.
- `browser.tab.locate(url)` replaces the current history entry; `relocate(url)` adds a same-document history entry.
- `browser.tab.close()` closes the current tab and updates `browser.tab` to the adjacent tab selected next.
- `browser.tabs.all` synchronously returns the tabs in the provider's browser window; `browser.tabs.get(id)` retrieves a tab by ID.
- `browser.activate(tab)` activates a tab and makes it current.

### Synchronous properties and asynchronous operations

Every property beneath `browser.tab` returns a value, not a .NET `Task`. Do not write `await` before property access:

```javascript
const title = browser.tab.window.document.title;
const currentUrl = browser.tab.window.location.href;
const historyLength = browser.tab.window.history.length;
const isOnline = browser.tab.window.navigator.onLine;
const screenWidth = browser.tab.window.screen.width;
const frameCount = browser.tab.windows.length;
const allTabs = browser.tabs.all;
```

Primitive values retain their JavaScript types: strings remain strings, numbers remain numbers, and booleans remain booleans. Arrays and objects are returned as CLR arrays and dictionaries that ClearScript exposes to JavaScript. Property reads communicate with Chromium and therefore block the host thread until the value is available; they are synchronous at the JavaScript-facing API boundary, not local in-process DOM reads.

Operations that perform browser actions or communicate asynchronously with Chromium remain awaitable:

```javascript
await browser.open("example.com");
await browser.tab.navigate("example.org");
await browser.tab.window.storage.local.setItem("key", "value");
const value = await browser.tab.window.storage.local.getItem("key");
const fileHandles = await browser.tab.open();
```

## 4. Resolve web addresses and local files

The shared `PathProvider` is used by browser navigation, Fetch, the legacy `Send` request helper, and module/resource resolution.

```javascript
// Incomplete hostnames are normalized to web addresses.
await browser.tab.navigate("example.com");

// Relative local paths resolve against PathProvider.BaseDirectory.
await browser.tab.load("./Samples/Local-Page-Smoke-Test.html");

// Fetch can read a local file through the host transport.
const response = await fetch("./Samples/Local-Page-Smoke-Test.html");
const html = await response.text();
```

Configure the base directory from the .NET host when needed:

```csharp
engine.PathProvider.BaseDirectory = @"D:\Projects\MyScraper";
engine.PathProvider.BaseAddress = "https://example.com/app/";
```

The browser loads local files using Chromium's `file://` support and its normal security restrictions. Host Fetch supports local-file `GET` and `HEAD` requests; local file responses do not use HTTP cookies or request headers.

## 5. Use Fetch

The global `fetch(...)` remains available for HTTP and local-file requests:

```javascript
const response = await fetch("https://example.com/data", {
    method: "GET",
    headers: { "Accept": "application/json" },
    timeout: 15000
});

if (!response.ok) {
    throw new Error("HTTP " + response.status);
}

const data = await response.json();
```

The host transport supports HTTP and HTTPS, common request methods, request and response headers, byte-safe request/response bodies, redirects, timeouts, cancellation, and a host cookie jar. It is not a complete browser Fetch implementation: it does not enforce browser CORS policy or implement service workers, browser cache modes, opaque `no-cors` responses, browser referrer policy, integrity checks, keepalive semantics, or streaming response bodies.

The ClearScript V8 host must enable `V8ScriptEngineFlags.EnableTaskPromiseConversion` so .NET `Task` results from asynchronous operations can be awaited in JavaScript.

## 6. Run the samples

The [Samples](Samples/) directory contains compiler stress tests, language examples, browser tests, and a local HTML fixture:

- [Browser object model smoke test](Samples/Browser-Object-Model-Smoke-Test.js) checks synchronous browser properties, tab navigation, and current-tab selection after closing a tab.
- [Native web smoke test](Samples/Native-Web-Smoke-Test.js) checks Fetch, URL normalization, local files, cancellation, and request-body behavior.
- [Local page fixture](Samples/Local-Page-Smoke-Test.html) is used by the local browser and Fetch tests.
- [Simple sample](Samples/SAMPLE%20-%20Simple.md) and [complex sample](Samples/SAMPLE%20-%20Complex.md) demonstrate DEAL programs.
- The stress-test files exercise DEAL syntax and JavaScript compilation.

Run the browser samples only after the host has initialized the Engine, configured the browser provider, and loaded the required web library.
