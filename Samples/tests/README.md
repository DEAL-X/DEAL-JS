# Compiler and runtime tests

- `inputs/` contains DEAL/JavaScript stress-test source files.
- `expected/` contains the expected JavaScript output for each stress test.
- `results/` contains captured compiler result snapshots.
- `runtime/` contains executable smoke tests for browser drivers, the browser object model, native web integration, and JavaScript-to-CLR collection interoperability.
- `fixtures/` contains local files used by runtime tests.

## Runtime smoke tests

- `Browser-Driver-Smoke-Test.js` — run separately with ChromeDriver, EdgeDriver, and FirefoxDriver. It covers tab creation, navigation, activation, closing, and current-tab resolution without relying on an external website.
- `Browser-Object-Model-Smoke-Test.js` — exercises browser object proxies and Chromium-specific optional file-picker checks.
- `Native-Web-Smoke-Test.js` — exercises Fetch, local-file requests, browser navigation, abort handling, and request-body lifecycle.
- `CLR-Object-Interop-Smoke-Test.js` — requires ClearScript `ExtendedHostFunctions` exposed as global `host`, plus the `deal/make` module. It verifies that `Make.arrayObject` produces `System.String[]`, that list and enumerable converters produce generic .NET collections, that dictionaries and sets preserve their values, and that `ResourceProvider.SetFileLines` accepts both the CLR array and `IEnumerable<string>` implementation.

## Compiler stress tests

For each stress test, compare the generated JavaScript from `inputs/` with the corresponding file in `expected/`. The `results/` copy records output captured from the implementation when it was saved. These files are kept separate so differences are easy to inspect.
