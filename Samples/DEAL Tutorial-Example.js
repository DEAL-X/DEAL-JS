/*
	* This is just a tutorial scenario and will not work
*/

// Will attach a core library to the JS engine
// Will attach a third-party library to the JS engine

const source = OPENFILE();																// Using a command of the "deal\io\file" library, which will show an open file dialog
const destination = SAVEFILE("results", "Comma Separated Values Files (*.csv)|*.csv");		// Using a command of the "deal\io\file" library, which will show a save file dialog

const retries = parseInt(prompt("How many retry do you want to do, If it can not fetched?", 2));
var days = 100;

(dailyChack = (() => {	// Define a label to can do it again
	for (const row of FILE(source).rows) {	// A human-readable version of a loop
		(checkItem = (() => {
			let trying = 0;
			try {	// Start a try block
				FETCH("api.market.com/search", {
					merchandise: ONE(5, row),	// The column index 5, you can use DEAL Selectors in the brackets too
					type: "json"
				})
					.then((data) => { // If the fetching promise finished
						WORKSPACE(data);
						if (FETCHED)	// If the data was received successfully
							for (const item of RESPONSE.json())	// Access to the RESPONSE object received in this workspace
								APPEND(destination,	// Store new line, row or object on the selected database or file
									SELECT(		// Collect all the cells of an object into a new object
										{
											Reference: __$__Reference__$__ = "api.market.com",
											...item,
											Price: __$__Price__$__ = item.Main_Price - item.Discount,
											Numbers: __$__Numbers__$__ = (item.Count == 0 ? "absent" : Count)
										},
										item)
								);
						else
							LOAD("www.market.com/search/merchandise=" + ONE(5, row))
								.then((value) => {	// If the loading promise finished
									WORKSPACE(value);
									if (LOADED) {	// If the website document is loaded completely
										for (const item of ALL("div table>tbody>tr"))
											APPEND(destination, 	// Store new row or line on the database
												COLLECT({	// Collect all the cells in an object, exactly like { name:value, ... }
													Reference: __$__Reference__$__ = "www.market.com",
													Company_Name: __$__Company_Name__$__ = NORMALIZE(CONCAT(ONE(".company-title>*", item), " ")),	// To normalize, all elements innerTexts of selected children of item using DEAL Selector (in the brackets)
													Product_Name: __$__Product_Name__$__ = (NORMALIZE(FIRST(SPLIT(CONCAT(ONE(".product-title>*", item), " "), /\s+co\s*$/gi)))),		// You can use multiple commands sequentially
													Image: __$__Image__$__ = URLDECODE(ONE("img.product-image", item).src),
													Main_Price: __$__Main_Price__$__ = NUMBER(ONE(7, item)),
													Discount: NUMBER(ONE(":last-child", item)) * __$__Main_Price__$__,		// You can switch between multiple types of calling commands to solve ambiguities...
													Price: __$__Price__$__ = __$__Main_Price__$__ - __$__Discount__$__,							// You are also able to use the previously named parameters
													Numbers: __$__Numbers__$__ = (ONE("td.numbers", item) == 0 ? "absent" : ONE("td.numbers", item))
												})
											);
										CLICK(ON("button#next-page"));
									}
									else LOG(WARNING(`Could not load the page for the ${ONE(5, row)} merchandise!`));
								});
					});
			} catch {		// A simple catch block `catch { }` without needing to handle the exception variable
				let allow = trying++ > retries;
				LOG((allow ? WARNING : ERROR)(`Could not fetch data completely for the ${ONE(5, row)} merchandise!`));
				if (allow) checkItem();
			}
		}))();
	}
}))();
if (--days > 0)
	WAIT(24 * 60 * 60 * 10000)
		&&	// You can concatenate two procedures or commands using AND/OR commands for more clarity
		(() => { try { dailyChack(); } catch (problem) { LOG(ERROR(problem)); } })();	// To make a callable block then call that
else STOP(BROWSER);	// To close the current BROWSER of this workspace