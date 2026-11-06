# Compiler and runtime tests

- `inputs/` contains the DEAL/JavaScript stress-test source files.
- `expected/` contains the expected JavaScript output for each stress test.
- `results/` contains the captured compiler result snapshots.
- `runtime/` contains executable smoke tests for the common browser/session API, the browser object model, and native web integration.
- The browser API exposes `BrowserTabs` as a collection of `BrowserTab` handles; tab enumeration does not create a second metadata model.
- `runtime/Browser-Driver-Smoke-Test.js` must be run separately with ChromeDriver, EdgeDriver, and FirefoxDriver selected by the .NET host. It covers tab creation, navigation, activation, closing, and current-tab resolution without relying on an external website.
- `runtime/Browser-Object-Model-Smoke-Test.js` includes Chromium-specific optional file-picker checks; those checks are not part of the common cross-driver test.
- `fixtures/` contains local files used by runtime tests.

For each stress test, compare the generated JavaScript from `inputs/` with the corresponding file in `expected/`. The `results/` copy records the output captured from the implementation at the time it was saved. These files are kept separate so differences are easy to inspect.
