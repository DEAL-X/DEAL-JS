// Run this sample in the ClearScript host after loading the DEAL web library.
// File picker checks are intentionally separate because they require browser
// permission and user activation.

async function browserObjectModelSmokeTest() {
    const opened = await browser.open("https://example.com/");
    if (!opened || !opened.Id)
        throw new Error("browser.open() did not return a BrowserTab.");

    // Browser object properties are synchronous, not Task-returning values.
    const title = opened.window.document.title;
    if (!title || title.toLowerCase().indexOf("example") < 0)
        throw new Error("The remote document title was not read synchronously.");

    const href = opened.window.location.href;
    if (href.indexOf("example.com") < 0)
        throw new Error("The remote location was not read synchronously.");

    await opened.window.storage.local.setItem("deal.browser.smoke", "ok");
    const stored = await opened.window.storage.local.getItem("deal.browser.smoke");
    if (stored !== "ok")
        throw new Error("The localStorage proxy did not round-trip a value.");

    const tabs = browser.tabs.all;
    let found = false;
    for (let i = 0; i < tabs.Count; i++)
        if (tabs[i].Id === opened.Id) found = true;
    if (!found)
        throw new Error("browser.tabs.all was not available synchronously or missed the opened tab.");

    const fetched = await browser.tabs.get(opened.Id);
    if (fetched.Id !== opened.Id)
        throw new Error("browser.tabs.get() returned the wrong tab.");

    await browser.activate(fetched);
    if (browser.tab.Id !== fetched.Id)
        throw new Error("browser.tab did not follow browser.activate().");

    const version = await browser.runtime.GetVersionAsync();
    if (!version || version.ToString().indexOf("product") < 0)
        throw new Error("Browser runtime version information was not returned.");

    // load() replaces the current document; navigate() adds a history entry.
    await browser.tab.load("https://example.com/");
    await browser.tab.navigate("https://example.org/");
    await browser.tab.back();
    if (browser.tab.Id !== fetched.Id)
        throw new Error("Current-tab navigation changed the current tab unexpectedly.");

    await browser.tab.close();
    const replacement = browser.tab;
    if (replacement && replacement.Id === fetched.Id)
        throw new Error("browser.tab.close() did not select a replacement tab.");

    browser.close();
    return {
        passed: true,
        title: title,
        href: href,
        tabCountBeforeClose: tabs.Count,
        browserVersion: version.ToString()
    };
}

// Optional manual checks. These invoke Chromium's native pickers.
// const selected = await browser.tab.open(JSON.stringify({ multiple: true }));
// const fileInfo = await selected[0].getFile();
// const saveHandle = await browser.tab.save(JSON.stringify({ suggestedName: "deal-output.txt" }));
// await saveHandle.writeText("Created through the native File System Access API.");
