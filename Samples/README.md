# DEAL.JS sample collection

The samples are organized by purpose:

- [tests/](tests/) — compiler stress inputs, expected JavaScript, captured result snapshots, and executable runtime smoke tests.
- [tutorials/](tutorials/) — progressive, commented DEAL.JS examples, from language fundamentals through browser and CLR interoperability.
- [practicals/](practicals/) — complete workflows that combine reusable modules, data processing, browser operations, and persistence.

Recommended learning path:

1. [Getting started](tutorials/01-Getting-Started.djs) — values, collections, iteration, and output.
2. [Browser and Fetch](tutorials/02-Browser-and-Fetch.djs) — browser control and asynchronous HTTP requests.
3. [Commands](tutorials/03-Commands.djs) — reusable command syntax.
4. [CLR object interoperability](tutorials/04-CLR-Object-Interop.djs) — JavaScript-to-.NET arrays and collections.

Recommended practical: [Research Catalog Processing](practicals/Research-Catalog-Processing.djs), which demonstrates normalization, validation, duplicate detection, provenance fields, and separate accepted/rejected JSONL outputs. Its five-record fixture asserts two accepted records and three rejections: one duplicate identifier, one missing price, and one out-of-range discount. The browser-oriented examples are [Web Scraping 1](practicals/Web-Scraping-1.djs), [Web Scraping 2](practicals/Web-Scraping-2.djs), and [Website Collecting](practicals/Website-Collecting.djs).

## Runtime prerequisites

The CLR interoperability examples require a ClearScript V8 host that exposes `Microsoft.ClearScript.ExtendedHostFunctions` as the global `host` object. Load `deal/make` to make the `Make.*Object` converters available. See the root README's CLR object interoperability section for the host setup and supported converter types.

The expected and result JavaScript files in `tests/` are compiler test artifacts. They are intentionally JavaScript because they are compared with generated target output; DEAL source remains the primary format for language and application examples.
