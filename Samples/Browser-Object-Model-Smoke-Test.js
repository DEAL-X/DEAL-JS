// Run this sample in the ClearScript host after loading the DEAL web library.
// The test opens two tabs so closing the current tab can be checked against the
// tab Chromium selects next. File picker checks require user activation.

async function browserObjectModelSmokeTest() {
    const anchor = await browser.open("about:blank");
    const opened = await browser.open("https://example.com/");

    if (!opened || !opened.Id)
        throw new Error("browser.open() did not return a BrowserTab.");

    // Browser properties return native values synchronously.
    const title = opened.window.document.title;
    const href = opened.window.location.href;
    const width = opened.window.innerWidth;
    const online = opened.window.navigator.onLine;

    if (typeof title !== "string" || !title.toLowerCase().includes("example"))
        throw new Error("document.title did not return a synchronous string.");
    if (typeof href !== "string" || !href.includes("example.com"))
        throw new Error("location.href did not return a synchronous string.");
    if (typeof width !== "number")
        throw new Error("window.innerWidth did not return a number.");
    if (typeof online !== "boolean")
        throw new Error("navigator.onLine did not return a boolean.");

    // Methods that communicate with Chromium remain awaitable.
    await opened.window.storage.local.setItem("deal.browser.smoke", "ok");
    const stored = await opened.window.storage.local.getItem("deal.browser.smoke");
    if (stored !== "ok")
        throw new Error("The localStorage proxy did not round-trip a value.");

    const tabs = browser.tabs.all;
    if (!Array.isArray(tabs) && typeof tabs.Count !== "number")
        throw new Error("browser.tabs.all did not return a synchronous collection.");

    const fetched = await browser.tabs.get(opened.Id);
    if (fetched.Id !== opened.Id)
        throw new Error("browser.tabs.get() returned the wrong tab.");

    await browser.activate(fetched);
    if (browser.tab.Id !== fetched.Id)
        throw new Error("browser.tab did not follow browser.activate().");

    // load() replaces the document; navigate() adds a history entry.
    await browser.tab.load("https://example.com/");
    await browser.tab.navigate("https://example.org/");
    await browser.tab.back();

    // Closing the current tab should make the newly selected tab current.
    await browser.tab.close();
    if (!browser.tab || browser.tab.Id !== anchor.Id)
        throw new Error("browser.tab did not refer to the tab selected after closing the current tab.");

    const version = await browser.runtime.GetVersionAsync();
    if (!version)
        throw new Error("Browser runtime version information was not returned.");

    browser.close();
    return {
        passed: true,
        title: title,
        href: href,
        width: width,
        online: online,
        tabCountBeforeClose: tabs.Count || tabs.length,
        browserVersion: version.ToString()
    };
}

// Optional manual checks. These invoke Chromium's native file pickers.
// const selected = await browser.tab.open(JSON.stringify({ multiple: true }));
// const fileInfo = await selected[0].getFile();
// const saveHandle = await browser.tab.save(JSON.stringify({ suggestedName: "deal-output.txt" }));
// await saveHandle.writeText("Created through the native File System Access API.");
