# DEAL.JS sample collection

The sample collection is organized by purpose:

- [tests/](tests/) — compiler stress inputs, expected JavaScript, actual result snapshots, and runtime smoke tests, including the cross-driver browser-session check.
- [tutorials/](tutorials/) — progressive, commented DEAL.JS examples for learning the language.
- [practicals/](practicals/) — realistic DEAL.JS workflows that can be adapted to applications.

Recommended path: [Getting started](tutorials/01-Getting-Started.djs) → [Browser and Fetch](tutorials/02-Browser-and-Fetch.djs) → [Web scraper](practicals/Web-Scraper.djs).

The browser collection is exposed directly as `browser.Tabs`, a `BrowserTabs` collection of `BrowserTab` handles; no separate tab-info objects are used.

The expected and result JavaScript files in `tests/` are compiler test artifacts. They are intentionally JavaScript because they are compared with the compiler's generated target output; they are not intended as the primary language examples.
