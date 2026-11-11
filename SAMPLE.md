# DEAL.JS quick-start and practical guide

This guide introduces DEAL.JS through executable DEAL source. The browser API follows the familiar JavaScript browser-object shape, and ClearScript resolves public .NET member names without regard to casing. Therefore, DEAL examples use conventional JavaScript-style names such as `browser.tab`, `window.document`, and `getFile()`; PascalCase remains the public .NET naming convention.

## 1. Your first DEAL program

```deal
Use deal/core;

Consider values is [1, 2, 3];
For each value of values,
    Print value;
```

DEAL instructions compile deterministically to JavaScript. Ordinary JavaScript may still be embedded when it is the clearest expression of a particular operation, but DEAL is the recommended source language for examples and application logic.

## 2. Load modules

`Use` loads a module into the compiler. `Reuse` explicitly processes it again:

```deal
Use deal/core;
Use deal/web;
Use ./Libraries/Selectors;

Reuse ./Libraries/Selectors;
Reuse https://example.com/modules/selectors.djs;
```

Both statements use the shared resource and path resolution. `Use` skips a dependency already registered by the compiler; `Reuse` processes it again. Reuse does not independently guarantee a fresh remote download.

## 3. Open and control a browser tab

The browser-facing convenience methods are synchronous. They complete the host operation and return the resulting value directly; do not put `await` before them.

```deal
Use deal/core;
Use deal/web;

Consider tab is browser.open("https://example.com");

Consider title is browser.tab.window.document.title;
Consider currentUrl is browser.tab.window.location.href;
Consider online is browser.tab.window.navigator.onLine;

browser.tab.navigate("https://example.org/results");
browser.tab.back();
browser.tab.forward();
browser.tab.reload();

browser.tab.close();
// browser.tab now refers to the tab the selected browser displays next.
```

The same browser-object API is available through Chrome, Edge, and Firefox. The default .NET host configuration selects Chrome; the selected driver is injected when constructing the browser provider. The browser API is organized by responsibility:

- `browser.open(url)` opens a tab and returns its `BrowserTab`.
- `browser.close()` closes the browser instance.
- `browser.tab` resolves the currently displayed tab.
- `browser.tab.load(url)` replaces the current document.
- `browser.tab.navigate(url)` performs normal navigation.
- `browser.tab.back()`, `forward()`, `go(delta)`, and `reload()` control navigation history.
- `browser.tab.close()` closes the current tab and refreshes the current-tab reference.
- `browser.tabs` returns the `BrowserTabs` collection directly; each item is a `BrowserTab` handle.
- `browser.tabs.get(id)` retrieves a tab by ID.
- `browser.activate(tab)` activates a tab and updates the current-tab reference.

Select the browser from the .NET host. Chrome is the backward-compatible default; Edge shares the Chromium driver implementation, while Firefox uses WebDriver BiDi:

```csharp
using MiMFa.Net.Browsers;
using MiMFa.Net.Browsers.Drivers;

// Replace the default browser created by the engine.
engine.Browser.Dispose();
engine.Browser = new Browser(driver: new EdgeDriver());

// Or select Firefox (geckodriver must be installed or driverPath supplied).
engine.Browser.Dispose();
engine.Browser = new Browser(driver: new FirefoxDriver());
```

`BrowserTabs` is itself the tab collection (`List<BrowserTab>`); there is no separate tab-info DTO. Each `BrowserTab` is a lightweight handle that resolves its page session only when a page operation or page-backed property is used.

The browser provider delegates common operations to a protocol-neutral browser session. Chromium-only services such as download behavior, permission overrides, and browser runtime information are optional capabilities; unsupported capabilities throw `NotSupportedException` rather than sending CDP commands to Firefox.

## 4. Synchronous properties and methods

Browser properties return values, not .NET `Task` objects. This includes properties such as:

```deal
Consider title is browser.tab.window.document.title;
Consider url is browser.tab.window.location.href;
Consider historyLength is browser.tab.window.history.length;
Consider isOnline is browser.tab.window.navigator.onLine;
Consider screenWidth is browser.tab.window.screen.width;
Consider frameCount is browser.tab.windows.length;
Consider tabs is browser.tabs;
```

The non-`Async` convenience methods are synchronous too:

```deal
Consider tab is browser.open("https://example.com");
Consider heading is browser.tab.window.document.querySelector("h1");
Consider allHeadings is browser.tab.window.document.querySelectorAll("h1");

Consider stored is browser.tab.window.storage.local.getItem("sample");
browser.tab.window.storage.local.setItem("sample", "value");

Consider handles is browser.tab.open();
Consider file is handles[0].getFile();
Consider content is file.text();
```

The corresponding explicitly named `...Async()` methods remain asynchronous and return Tasks. Use them from JavaScript with `await` when asynchronous composition is desired. For example, `GetFileAsync()`, `ReadTextAsync()`, and `NavigateAsync()` remain awaitable; `GetFile()`, `Text()`, and `Navigate()` are synchronous convenience methods.

Synchronous calls that access the selected browser block the calling host thread until the operation completes. They are synchronous at the JavaScript-facing API boundary; they are not local, in-process DOM reads.

## 5. Fetch web resources

Fetch remains asynchronous because it represents a network or file request:

```deal
Consider response is await fetch("https://example.com/data", {
    method: "GET",
    headers: { "Accept": "application/json" },
    timeout: 15000
});

If response.ok,
    Consider data is await response.json();
    Print data;
Otherwise,
    Log error "Request failed with status " + response.status;
```

The host transport supports HTTP and HTTPS, common request methods, request and response headers, byte-safe bodies, redirects, timeouts, cancellation, and a host cookie jar. It is not a complete browser Fetch implementation: browser CORS policy, service workers, browser cache modes, opaque `no-cors` responses, browser referrer policy, integrity checks, keepalive semantics, and streaming response bodies are not implemented.

At the .NET layer, the transport types live in `MiMFa.Net.Fetch` (`Transfer` and `FetchRequest`), separately from the browser provider in `MiMFa.Net.Browsers`. The shared address `Router` remains in `MiMFa.Net`. The DEAL `fetch()` function uses this transport internally.

The ClearScript V8 host must enable `V8ScriptEngineFlags.EnableTaskPromiseConversion` so explicitly asynchronous .NET Task results can be awaited from JavaScript.

## 6. Work with local files

Browser navigation, Fetch, the legacy `Send` helper, and module resolution share the configured `Router`:

```deal
browser.tab.load("./Samples/tests/fixtures/Local-Page-Smoke-Test.html");

Consider response is await fetch("./Samples/tests/fixtures/Local-Page-Smoke-Test.html");
Consider html is await response.text();
```

Configure the base directory from the .NET host when needed:

```csharp
engine.Router.RootDirectory = @"D:\Projects\MyScraper";
engine.Router.RootAddress = "https://example.com/app/";
```

The selected browser loads local files through `file://` and applies its normal security restrictions. Host Fetch supports local-file `GET` and `HEAD` requests. Local file reads do not use HTTP cookies or HTTP request headers.

## 7. File System Access API

The browser's native file pickers are exposed through `browser.tab.open()` and `browser.tab.save()` when supported by the selected browser and security context. Their non-`Async` forms return file handles synchronously:

```deal
Consider handles is browser.tab.open();
Consider selectedFile is handles[0].getFile();
Consider selectedText is selectedFile.text();

Consider saveHandle is browser.tab.save("{\"suggestedName\":\"deal-output.txt\"}");
saveHandle.writeText("Created through DEAL.JS.");
```

The explicit asynchronous counterparts remain available as `OpenAsync()`, `SaveAsync()`, `GetFileAsync()`, `ReadTextAsync()`, and `WriteTextAsync()`. The browser enforces its secure-context, permission, and transient-user-activation requirements. Dispose remote file handles when they are no longer needed.

## 8. Current-tab tracking

The provider does not assume that the first item returned by a browser protocol is active. Reading `browser.tab` resolves the page the selected browser currently displays. Chromium uses CDP and Firefox uses WebDriver BiDi; both resolve the visible page through `document.visibilityState`. This also tracks tab switches performed directly in the browser UI.

After `browser.tab.close()` closes the current tab, the provider waits for the browsing context to disappear, identifies the remaining visible page, and updates its current-tab reference. If the browser is minimized and no visible page is reported, it falls back to an adjacent tab. Closing the last tab creates a replacement blank tab so the browser remains available.

## 9. Sample directory

The samples are grouped by purpose:

- [tests/](Samples/tests/) — language/compiler stress tests, expected JavaScript, actual result snapshots, and browser/Fetch smoke tests.
- [tutorials/](Samples/tutorials/) — descriptive, progressive DEAL.JS learning examples.
- [practicals/](Samples/practicals/) — larger, realistic DEAL.JS workflows that can be adapted to real projects.

Start with [01 - Getting Started](Samples/tutorials/01-Getting-Started.djs), then try [02 - Browser and Fetch](Samples/tutorials/02-Browser-and-Fetch.djs). For a complete scraping workflow, see [Web Scraper](Samples/practicals/Web-Scraper.djs).

## 10. Convert JavaScript values to .NET collections

When a JavaScript array is passed to a .NET method that expects `string[]` or `IEnumerable<string>`, convert it explicitly. ClearScript does not treat every JavaScript array as an already-constructed CLR collection during overload resolution.

Expose ClearScript's `ExtendedHostFunctions` instance as `host` before executing DEAL modules:

```csharp
using Microsoft.ClearScript;
engine.AddHostObject("host", new ExtendedHostFunctions());
```

Then load `deal/make` and use its converters:

```deal
Use deal/core;
Use deal/make;

Consider lines is Make.arrayObject(["first", "second"], "System.String");
application.Engine.Resource.SetFileLines(path, lines, null);

// A List<string> also implements IEnumerable<string>.
Consider list is Make.listObject(["third", "fourth"], "System.String");
application.Engine.Resource.SetFileLines(path, list, null);
```

The object-conversion module also provides `stringArrayObject`, `byteArrayObject`, `objectObject` / `expandoObject`, `dictionaryObject`, `hashSetObject`, `queueObject`, `stackObject`, `linkedListObject`, `sortedSetObject`, `arrayListObject`, `hashTableObject`, `typeObject`, and `convertObject`. The `File` and `Local` modules use these conversions internally when writing file lines and byte contents. See [Tutorial 04](Samples/tutorials/04-CLR-Object-Interop.djs) for a complete example and [the runtime smoke test](Samples/tests/runtime/CLR-Object-Interop-Smoke-Test.js) for the actual .NET type and file-provider checks.

