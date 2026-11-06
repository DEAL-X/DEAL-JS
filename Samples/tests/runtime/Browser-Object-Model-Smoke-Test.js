// Run this sample in the ClearScript host after loading the DEAL web library.
// The test opens two tabs so closing the current tab can be checked against the
// tab Chromium selects next. File picker checks require user activation.

async function browserObjectModelSmokeTest() {
    const anchor = browser.Open("about:blank");
    const opened = browser.Open("https://example.com/");

    if (!opened || !opened.Id)
        throw new Error("browser.open() did not return a BrowserTab.");

    // Browser properties return native values synchronously.
    const title = opened.Window.Document.Title;
    const href = opened.Window.Location.Href;
    const width = opened.Window.InnerWidth;
    const online = opened.Window.Navigator.Online;

    if (typeof title !== "string" || !title.toLowerCase().includes("example"))
        throw new Error("document.title did not return a synchronous string.");
    if (typeof href !== "string" || !href.includes("example.com"))
        throw new Error("location.href did not return a synchronous string.");
    if (typeof width !== "number")
        throw new Error("window.innerWidth did not return a number.");
    if (typeof online !== "boolean")
        throw new Error("navigator.onLine did not return a boolean.");

    // Methods that communicate with Chromium remain awaitable.
    opened.Window.Storage.Local.SetItem("deal.browser.smoke", "ok");
    const stored = opened.Window.Storage.Local.GetItem("deal.browser.smoke");
    if (stored !== "ok")
        throw new Error("The localStorage proxy did not round-trip a value.");

    const tabs = browser.Tabs.All;
    if (!Array.isArray(tabs) && typeof tabs.Count !== "number")
        throw new Error("browser.tabs.all did not return a synchronous collection.");

    const fetched = browser.Tabs.Get(opened.Id);
    if (fetched.Id !== opened.Id)
        throw new Error("browser.tabs.get() returned the wrong tab.");

    browser.Activate(fetched);
    if (browser.Tab.Id !== fetched.Id)
        throw new Error("browser.tab did not follow browser.activate().");

    // load() replaces the document; navigate() adds a history entry.
    browser.Tab.Load("https://example.com/");
    browser.Tab.Navigate("https://example.org/");
    browser.Tab.Back();

    // Closing the current tab should make the newly selected tab current.
    browser.Tab.Close();
    if (!browser.Tab || browser.Tab.Id !== anchor.Id)
        throw new Error("browser.tab did not refer to the tab selected after closing the current tab.");

    const version = await browser.Runtime.GetVersionAsync();
    if (!version)
        throw new Error("Browser runtime version information was not returned.");

    browser.Close();
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
// const selected = browser.Tab.Open(JSON.stringify({ multiple: true }));
// const fileInfo = selected[0].GetFile();
// const saveHandle = browser.Tab.Save(JSON.stringify({ suggestedName: "deal-output.txt" }));
// saveHandle.WriteText("Created through the native File System Access API.");
