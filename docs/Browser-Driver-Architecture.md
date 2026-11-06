# Browser and Fetch Architecture

## Goals

- Keep browser orchestration independent from browser vendors and wire protocols.
- Keep Chromium-specific behavior inside Chromium drivers and DevTools protocol adapters.
- Support Chrome and Edge through the shared Chromium implementation.
- Support Firefox through WebDriver BiDi, not through a fake CDP compatibility layer.
- Keep Fetch independent from browser automation.
- Preserve the synchronous convenience API and its explicitly asynchronous counterparts.
- Keep each public type in its own file and order members consistently.

## Target layout

```text
Net/
├── Fetch/
│   ├── Transfer.cs
│   ├── FetchRequest.cs
│   └── ...
└── Browsers/
    ├── Browser.cs
    ├── Drivers/
    │   ├── BrowserDriver.cs
    │   ├── ChromiumDriver.cs
    │   ├── ChromeDriver.cs
    │   ├── EdgeDriver.cs
    │   └── FirefoxDriver.cs
    ├── Protocols/
    │   ├── Common/
    │   │   └── IBrowserCommandSession.cs
    │   ├── DevTools/
    │   │   ├── DevToolsConnection.cs
    │   │   ├── DevToolsPageSession.cs
    │   │   └── IDevToolsPageCommandSession.cs
    │   └── WebDriverBiDi/
    │       ├── BiDiConnection.cs
    │       └── FirefoxPageSession.cs
    ├── Sessions/
    │   ├── BrowserSession.cs
    │   ├── BrowserPageSession.cs
    │   ├── ChromiumBrowserSession.cs
    │   └── FirefoxBrowserSession.cs
    ├── Objects/
    │   ├── BrowserTab.cs
    │   ├── BrowserTabs.cs
    │   ├── BrowserPage.cs
    │   └── Files/
    └── Services/
```

```text
DEAL Engine
    |
    +-- Fetch.Transfer --> Fetch transport implementation
    |
    +-- Browsers.Browser --> BrowserSession
                               |
                               +--> BrowserDriver
                                       |
                                       +--> ChromiumDriver
                                       |      +--> ChromeDriver
                                       |      +--> EdgeDriver
                                       |      +--> DevTools protocol
                                       |
                                       +--> FirefoxDriver
                                              +--> WebDriver BiDi protocol
```

The provider, tab objects, page objects, and common services must not create processes, inspect executable paths, open WebSockets, or issue vendor protocol commands. Those responsibilities belong to the selected driver and its protocol-specific session/adapter.

## Driver and session contracts

### BrowserDriver

The abstract driver owns browser lifecycle and creates a `BrowserSession`. It exposes common metadata and capabilities, but does not expose CDP or BiDi command names to the provider.

Its public synchronous methods are convenience wrappers placed immediately above their asynchronous counterparts. The asynchronous methods are the implementation source of truth.

### BrowserSession and BrowserPageSession

`BrowserSession` represents one application-level browser window/session. It owns tab discovery, current-tab resolution, tab creation, activation, and closing. Protocol-specific implementations may derive from this common session abstraction.

`BrowserPageSession` represents one tab and provides the common operations used by browser objects: navigation, history, JavaScript evaluation, URL/title/document state, and disposal. Protocol-specific implementations may derive from it.

The public `BrowserPage` and `BrowserTab` objects are facades over these session abstractions. They do not know whether the implementation is CDP or BiDi. `BrowserTabs` is the collection of `BrowserTab` handles itself; there is no parallel `BrowserTabInfo` DTO. A tab handle stores its identity and resolves its `BrowserPage` lazily, so listing tabs does not require eagerly creating a page session for every tab.

### Protocol adapters

Protocol adapters translate their native protocol into the common session contracts.

- DevTools adapter: CDP command transport, target discovery, browser-level commands, remote object handles, and CDP events.
- WebDriver BiDi adapter: BiDi command transport, browsing-context lifecycle, script evaluation, navigation, and BiDi events.

Do not emulate every CDP command on Firefox. Common browser operations belong in the common contracts. Vendor-specific functionality is represented by optional capability interfaces.

## Browser-specific drivers

- `ChromiumDriver` contains the shared Chromium process, profile, executable discovery hooks, and debugging-endpoint setup. `ChromiumBrowserSession` owns CDP target discovery and browser-window tab lifecycle.
- `ChromeDriver` supplies Chrome executable discovery and defaults.
- `EdgeDriver` supplies Edge executable discovery and defaults while inheriting Chromium behavior.
- `FirefoxDriver` owns geckodriver startup and WebDriver session creation. `FirefoxBrowserSession` owns BiDi browsing-context lifecycle and `FirefoxPageSession` implements page operations. It does not inherit from ChromiumDriver.

The `Browser` provider accepts a `BrowserDriver` instance. Its existing constructor remains source-compatible and selects `ChromeDriver` by default. Callers can inject `ChromeDriver`, `EdgeDriver`, `FirefoxDriver`, or a custom driver explicitly.

## Capabilities

Common contracts must stay small. Features that are not universal are optional capabilities, for example:

- download behavior
- browser permission overrides
- browser command-line/runtime information
- native file-system picker remote handles
- browser-specific profile and context management

Browser-level CDP services use the optional `IBrowserCommandSession` capability. Page-level raw commands are exposed only through the optional `IDevToolsPageCommandSession` capability; the common `BrowserPageSession` contract contains no CDP command surface. A service checks for the relevant capability and throws a clear `NotSupportedException` when the selected backend does not implement it; it must never silently send a Chromium command through Firefox.

## Fetch separation

Fetch is a peer subsystem under `Net/Fetch`, not a browser driver feature. `Transfer` owns HTTP/file transport, cookies, request cancellation, and Fetch request/response conversion. It may use the shared address router, but it must not depend on Browser, BrowserDriver, CDP, or BiDi.

Browser navigation may use the same router for URL resolution. Sharing address resolution does not create a dependency between Fetch and Browsers.

## Member ordering

Every class follows this order:

1. Static properties and fields
2. Instance properties and fields
3. Constructors, finalizers, and disposal/lifecycle members
4. Instance methods
5. Static methods

For each asynchronous public or internal operation, provide the synchronous convenience counterpart immediately above it unless blocking would be unsafe or the operation is inherently asynchronous (for example, event streams). Prefer optional/default parameter values over multiple overloads when the overloads differ only by omitted arguments; keep the sync and async pair aligned with the same defaults. The synchronous counterpart delegates to the async implementation with `GetAwaiter().GetResult()`; it must not duplicate the operation logic.

## Migration sequence

1. [x] Introduce the common driver, session, and optional capability contracts in the accepted folder structure.
2. [x] Move the DevTools transport and Chromium page operations behind the DevTools protocol/page-session implementation.
3. [x] Make `Browser` and `BrowserPage` facades delegate through `BrowserDriver`, `BrowserSession`, and `BrowserPageSession`.
4. [x] Add `ChromeDriver` and `EdgeDriver` as `ChromiumDriver` specializations.
5. [x] Add `FirefoxDriver`, the BiDi transport, and Firefox browser/page sessions for common operations.
6. [x] Move Fetch transport to `Net/Fetch` and update engine wiring/namespaces.
7. [x] Audit the migrated Browser and Fetch classes for file separation, namespace/folder alignment, member ordering, synchronous/async pairs, and synchronous properties.
8. [x] Update README.md, SAMPLE.md, sample directory guides, and add a cross-driver browser smoke test. The smoke test still needs to be executed against real Chrome, Edge, and Firefox installations.
9. [x] Replace `BrowserTabInfo` with lazy `BrowserTab` handles and consolidate duplicate timeout overloads.

## Implementation status

The core architecture and source migration are now present on the feature branch. The common browser provider no longer owns Chromium target management or a DevTools connection; it delegates to a selected driver and session. Chromium and Firefox have separate session implementations, and Fetch is a peer subsystem.

The Windows GitHub Actions build passes on the current branch snapshot, verifying compilation. The cross-driver runtime smoke test has been added at `Samples/tests/runtime/Browser-Driver-Smoke-Test.js`, but a real-browser run against Chrome, Edge, and Firefox is still required; compilation alone does not establish protocol/runtime compatibility.

## Compatibility policy

The current default browser behavior and public DEAL-facing browser object model should remain compatible unless a documented bug fix is required. Keep the existing synchronous methods and properties synchronous. Keep explicitly named `...Async` APIs awaitable. Do not make browser properties return Tasks.

The migration is not complete until the project builds and the browser smoke tests run against Chrome, Edge, and Firefox. A successful compile alone does not establish BiDi runtime compatibility.
