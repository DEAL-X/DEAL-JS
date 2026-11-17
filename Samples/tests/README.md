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
- `CLR-Object-Interop-Smoke-Test.js` — requires ClearScript `ExtendedHostFunctions` exposed as global `host`, plus the `deal/make` module. It verifies that `Make.arrayObject` and `Make.stringArrayObject` produce `System.String[]`, `Make.byteArrayObject` produces `System.Byte[]`, that list and enumerable converters produce generic .NET collections, that dictionaries, dynamic objects, and sets preserve their values, and that `ResourceProvider.SetFileLines` and `SetFileContents` accept the converted CLR arrays.

## Compiler stress tests

For each stress test, compare the generated JavaScript from `inputs/` with the corresponding file in `expected/`. The `results/` copy records output captured from the implementation when it was saved. These files are kept separate so differences are easy to inspect.

- `inputs/Hierarchical-Keyword-Spelling.djs` — focused regression fixture for `make StRing`, `make .StRing`, and `make.StRing`. Each spelling is compiled independently by the automated regression runner and must produce exactly `Make.string;`. The three corresponding lines in `expected/Hierarchical-Keyword-Spelling.expected.js` are the individual output assertions.

Run the compiler regression tests from the repository root:

```powershell
dotnet run --project Samples/tests/CompilerRegressionTests.csproj --configuration Release
```

The runner loads `deal/make/string`, compiles each spelling in a fresh engine, verifies that the dependency was loaded, and compares the generated statement with its corresponding expected line.
