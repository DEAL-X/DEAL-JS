// Practical: collect page titles and headings from a list of websites.
// The browser object must be initialized by the host before this file runs.

Use deal/core;
Use deal/web;

Consider urls is [
    "https://example.com/",
    "https://example.org/"
];
Consider results is [];

// Open one tab, then reuse it for each address.
Consider tab is browser.open("about:blank");
For each url of urls,
    Try,
        browser.tab.navigate(url);
        Consider title is browser.tab.window.document.title;
        Consider headings is browser.tab.window.document.querySelectorAll("h1, h2, h3");
        Append results, { url: browser.tab.url, title: title, headings: headings };
    Catch problem,
        Log warning "Could not scrape " + url + ": " + problem;

Print results;
