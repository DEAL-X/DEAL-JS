# DEAL.JS practical examples

These examples are complete, adaptable workflows rather than isolated syntax demonstrations. They use DEAL source as the primary implementation language and load only the modules required by each workflow.

- [File](File-1.djs) — append structured values to a temporary file and read the result.
- [Web Scraping 1](Web-Scraping-1.djs) — visit multiple pages in a reused browser tab and collect titles and headings.
- [Web Scraping 2](Web-Scraping-2.djs) — extract search-result fields, follow pagination, and filter duplicate records.
- [Website Collecting](Website-Collecting.djs) — asynchronously retrieve JSON resources, handle HTTP failures, and collect structured responses.
- [Research Catalog Processing](Research-Catalog-Processing.djs) — normalize records, validate required fields and numeric ranges, reject duplicate identifiers, attach provenance metadata, and persist accepted and rejected JSONL outputs separately.

## Host requirements

Browser examples require the .NET host to initialize the browser provider and load `deal/web`. The selected browser driver is supplied by the host; Chrome is the default, with Edge and Firefox available through their respective drivers.

Examples that use CLR collection converters require ClearScript `ExtendedHostFunctions` to be exposed as global `host` and the `deal/make` module to be loaded. The converter layer is in `deal/make/object.djs`; it creates actual CLR arrays and collection instances for .NET method parameters.
