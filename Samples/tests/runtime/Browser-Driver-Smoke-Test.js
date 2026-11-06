// Cross-driver smoke test for the common browser/session API.
// Run once with ChromeDriver, EdgeDriver, and FirefoxDriver selected by the .NET host.
// Requires no external website; data: URLs keep the test self-contained.

function runBrowserDriverSmokeTest(browser) {
    if (!browser)
        throw new Error("A configured DEAL browser provider is required.");

    const driverName = String(browser.Driver.Name);
    const initialTab = browser.Tab;
    const opened = browser.Open("data:text/html,%3Ctitle%3EDEAL%20driver%20smoke%3C/title%3E%3Ch1%3Edriver%20smoke%3C/h1%3E");

    if (!opened)
        throw new Error(driverName + " did not open a new tab.");
    if (String(opened.Title) !== "DEAL driver smoke")
        throw new Error(driverName + " did not expose the new page title.");

    opened.Navigate("data:text/html,%3Ctitle%3EDEAL%20driver%20navigated%3C/title%3E%3Cp%3Ecommon%20navigation%3C/p%3E");
    if (String(opened.Title) !== "DEAL driver navigated")
        throw new Error(driverName + " did not navigate the page.");

    const tabs = browser.Tabs;
    if (!tabs || typeof tabs.Count !== "number" || tabs.Count < 1)
        throw new Error(driverName + " did not return the browser tab collection.");

    if (!browser.Activate(opened))
        throw new Error(driverName + " could not activate the test tab.");
    if (!browser.Tab || browser.Tab.Id !== opened.Id)
        throw new Error(driverName + " did not update the current-tab reference after activation.");

    if (!opened.Close())
        throw new Error(driverName + " could not close the test tab.");

    const current = browser.Tab;
    if (!current)
        throw new Error(driverName + " did not resolve a current tab after closing the active tab.");

    return {
        driver: driverName,
        initialTabId: initialTab ? initialTab.Id : null,
        closedTabId: opened.Id,
        currentTabId: current.Id,
        tabCountAfterClose: browser.Tabs.Count
    };
}
