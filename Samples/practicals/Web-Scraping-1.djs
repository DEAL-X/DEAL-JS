// Practical: collect page titles and headings from a list of websites.
// The browser object must be initialized by the host before this file runs.
// HOW TO USE:
// Use Samples/practicals/Web-Scraping-1.djs;

Use deal/core;
Use deal/web;

Consider urls be [
    "https://example.com/",
    "https://example.org/"
];
Consider results be [];

// Open one tab, then reuse it for each address.
Consider tab be browser.open("about:blank");
For each url of urls,
    Try,
        browser.tab.navigate(url);
        Consider title be browser.tab.window.document.title;
        Consider headings be browser.tab.window.document.querySelectorAll("h1, h2, h3");
        Append results, { url: browser.tab.url, title: title, headings: headings };
    Catch problem,
        Log warning "Could not scrape " + url + ": " + problem;

Print results;
