// Run this sample in the ClearScript host after loading the DEAL web library.
// File picker checks are intentionally separate because they require browser
// permission and user activation.

async function browserObjectModelSmokeTest() {
    const opened = await browser.open("https://example.com/");
    if (!opened || !opened.Id)
        throw new Error("browser.open() did not return a BrowserTab.");

    const title = await opened.window.document.title;
    if (!title || title.toLowerCase().indexOf("example") < 0)
        throw new Error("The remote document title was not read correctly.");

    const href = await opened.window.location.href;
    if (href.indexOf("example.com") < 0)
        throw new Error("The remote location was not read correctly.");

    await opened.window.storage.local.setItem("deal.browser.smoke", "ok");
    const stored = await opened.window.storage.local.getItem("deal.browser.smoke");
    if (stored !== "ok")
        throw new Error("The localStorage proxy did not round-trip a value.");

    const tabs = await browser.tabs.all;
    if (!tabs.some(tab => tab.Id === opened.Id))
        throw new Error("The opened tab was not found in browser.tabs.all.");

    const fetched = await browser.tabs.get(opened.Id);
    if (fetched.Id !== opened.Id)
        throw new Error("browser.tabs.get() returned the wrong tab.");

    await browser.activate(fetched);
    if (browser.tab.Id !== fetched.Id)
        throw new Error("browser.tab did not follow browser.activate().");

    const version = await browser.runtime.GetVersionAsync();
    if (!version || !version.product)
        throw new Error("Browser runtime version information was not returned.");

    await browser.close(opened);
    return {
        passed: true,
        title: title,
        href: href,
        tabCountBeforeClose: tabs.length,
        browserVersion: version.product
    };
}

// Optional manual checks. These invoke Chromium's native pickers.
// const selected = await browser.tab.open(JSON.stringify({ multiple: true }));
// const fileInfo = await selected[0].getFile();
// const saveHandle = await browser.tab.save(JSON.stringify({ suggestedName: "deal-output.txt" }));
// await saveHandle.writeText("Created through the native File System Access API.");
