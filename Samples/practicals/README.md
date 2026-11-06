# DEAL.JS practical examples

The browser provider uses the driver selected by the .NET host (Chrome by default, with Edge and Firefox available). Practical DEAL source uses the common browser-object API; driver-specific optional capabilities should be checked by the host.

These examples are larger, adaptable workflows rather than isolated syntax demonstrations. Browser tab collections are exposed directly as `BrowserTabs` collections of `BrowserTab` handles; the page session for a handle is resolved only when needed. They use DEAL source as the primary implementation language and assume the host has initialized the browser provider and loaded the `deal/web` module.

- [Web scraper](Web-Scraper.djs) — visit a list of pages and collect page titles and headings.
- [Batch website collector](Batch-Website-Collector.djs) — process a URL collection and store structured results.
