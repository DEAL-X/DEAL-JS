# DEAL.JS sample collection

The sample collection is organized by purpose:

- [tests/](tests/) — compiler stress inputs, expected JavaScript, actual result snapshots, and runtime smoke tests, including the cross-driver browser-session check.
- [tutorials/](tutorials/) — progressive, commented DEAL.JS examples for learning the language.
- [practicals/](practicals/) — realistic DEAL.JS workflows that can be adapted to applications.

Recommended path: [Getting started](tutorials/01-Getting-Started.djs) → [Browser and Fetch](tutorials/02-Browser-and-Fetch.djs) → [Web scraping](practicals/Web-Scraping-1.djs).

The browser collection is exposed directly as `browser.Tabs`, a `BrowserTabs` collection of `BrowserTab` handles; no separate tab-info objects are used.

The expected and result JavaScript files in `tests/` are compiler test artifacts. They are intentionally JavaScript because they are compared with the compiler's generated target output; they are not intended as the primary language examples.

## JavaScript and .NET interop

The deal/make/object.djs module contains explicit converters for ClearScript host boundaries. The host exposes ExtendedHostFunctions as host; see the root README and quick-start guide (../SAMPLE.md) for setup.

- Make.arrayObject — CLR arrays such as string[].
- Make.listObject — generic List<T>.
- Make.iteratorObject / Make.enumerableObject — materialized List<T> instances assignable to IEnumerable<T>.
- Make.dictionaryObject — Dictionary<TKey, TValue>, including nested JavaScript objects.
- Make.hashSetObject, Make.queueObject, and Make.stackObject — common generic collections.
- Make.bytesObject — CLR byte[] from strings, buffers, or numeric collections.

See the .NET interop tutorial (tutorials/04-DotNet-Interop.djs), the file-export practical (practicals/Clr-Interop-File-Export.djs), and the runtime smoke test (tests/runtime/Clr-Interop-Smoke-Test.js).
