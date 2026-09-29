 # A Tuorials Example
 This is just a tutorial scenario and will not work.
 
 You can find more examples and their parsed JS versions [here](Samples/).

```js
	/*
		* This is just a tutorial scenario and will not work
	*/

	USE scraper;			// Will attach whole of a core library to the JS engine
	USE deal\data\resource;	// Will attach a special part of a core library to the JS engine
	/*
		* You can add a core or third-party library to the JS engine using the USE command.
		* If there is no spaces between the address, you can write that without any quotes.
		* This library will be parsed and attached to the JS engine before execution.
		* The finding package procedure in this command will check one of the three steps:
			1. If the `deal\data\resource.djs` file exists, it will be parsed and attached to the JS engine.
			2. If the `deal\data\resource.js` file exists, it will be attached directly to the JS engine.
			3. If the `deal\data\resource` directory exists, it will USE all files or directories in that.
	*/
	USE "D:\My Libs\Text\Normalization";	// Will attach a third-party library using an absolute addressto the JS engine
	USE .\MyLib;						// Will attach a third-party library using a relative address to the JS engine

	CONST Source = OPENFILE();																	// Using a command of the `deal\io\resource` library, which will show an open file dialog
	CONST @Destination = SAVEFILE("results", "Comma Separated Values Files (*.csv)|*.csv");		// Using a command of the `deal\io\resource` library, which will show a save file dialog
	/*
		* All of the JS statements are accessible in a case-insensitive mode.
		* All of your defined names are case-sensitive.
			* If the name begins with `@` (delegation command sign), subsequent characters will be case-insensitive.
	*/

	const retries = parseInt(prompt("How many retry do you want to do, If it can not fetched?", 2));
	var days = 100;
	/*
		* You can write pure JS everywhere...
		* You can use a block of pure JS codes between curly-brackets { Pure JS Codes }
	*/

	#dailyChack	// Define a label to can do it again
	/*
		* All labelled procedures will parse as a function, that will execute immediately too:
		`
			(dailyChack = () => {
				// No returner procedure
			})()
		`
	*/

		FOR EACH row OF FILE(Source) rows,	// A human-readable version of a loop
		/*
			* A FILE object will have multiple types of return:
				* text:		// You can get or set the string of text content in the file
				* bytes:		// You can get or set the array of bytes in the file
				* lines:		// You can get or set the array of line strings in the file
				* rows:		// You can get or set the array of row objects in the file
				* warps:	// You can get or set the array of vertical line (concatenated cols) strings in the file
				* cols:		// You can get or set the array of column objects in the file
				* cells:		// You can get or set a flat array of all cells in the file
		*/
		BEGIN
			#checkItem
			DO
				LET trying BE 0;
				TRY	TO // Start a try block, you can write TRY instead.
					FETCH "api.market.com/search", { // To make a pure JS object, here you can not write DEAL
						merchandise:row[5],	// The column index 5, you can use DEAL Selectors in the brackets too
						type: "json"
					}
					THEN // If the fetching promise finished
						IF FETCHED	// If the data was received successfully
							FOR EACH item OF RESPONSE.json()	// Access to the RESPONSE object received in this workspace
								APPEND destination,		// Store new line, row or object on the selected database or file
									SELECT	// Collect all the cells of an object into a new object
										"api.market.com" AS Reference,
										*,
										item Main_Price - item Discount AS Price, 	// If there is not a function named the identifier you use, you can chain its properties or functions without using dot('.') too
										(IF item Count IS 0 "absent" ELSE item.Count) AS Numbers
										FROM item;
						ELSE
							LOAD "www.market.com/search/merchandise="+row[5]
							THEN	// If the loading promise finished
								IF LOADED DO	// If the website document is loaded completely
									FOR EACH item OF ALL "div table>tbody>tr",
										APPEND 	// Store new row or line on the database
											COLLECT	// Collect all the cells in an object, exactly like { name:value, ... }
												"www.market.com" AS Reference,
												NORMALIZE(CONCAT(item[".company-title>*"], " ")) AS Company_Name,	// To normalize, all elements innerTexts of selected children of item using DEAL Selector (in the brackets)
												(NORMALIZE FIRST SPLIT CONCAT(item[".product-title>*"], " "), /\s+co\s*$/gi) AS Product_Name,		// You can use multiple commands sequentially
												URLDECODE item["img.product-image"].src AS Image,
												NUMBER item[7] AS Main_Price,
												NUMBER(item[":last-child"]) * Main_Price AS Discount,		// You can switch between multiple types of calling commands to solve ambiguities...
												Main_Price - Discount AS Price,							// You are also able to use the previously named parameters
												(IF item["td.numbers"] IS 0, "absent", ELSE item["td.numbers"]) AS Numbers,
												/*
													* To make your code more human-readable, you can use the following statements too:
														BE					// Instead of =
														IS					// Instead of ==
														IS NOT				// Instead of !=
														EQUALS				// Instead of ===
														NOT EQUALS			// Instead of !==
														AND					// Instead of &&
														OR					// Instead of ||
												*/
												To destination; 	// Some commands like APPEND and PREPEND accept to send unordered arguments too
									CLICK ANY button#next-page;	// There are able to use css selectors without quotes wrapping
									/*
										* There are multiple predefined commands to interact with your browser
											CLICK		// To click on one or all selected elements
											HOVER		// To make a mouse hover on one or all selected elements
											SCROLL		// To scroll on a selected element or specified location
									*/
								END;	// Each DO or DOING command needs to indication the ending use an END command
								ELSE LOG WARNING, `Could not load the page for the ${row[5]} merchandise!`;
				CATCH DO		// A simple catch block `catch { }` without needing to handle the exception variable
					LET allow BE trying++ > retries;
					IF allow, DO
						LOG WARNING,`Could not fetch data completely for the ${row[5]} merchandise!`;
						checkItem;
					END;
					ELSE LOG ERROR `Could not fetch data completely for the ${row[5]} merchandise!`;
				END;
			END;
		END;
	IF --days > 0,
		WAIT (24 * 60 * 60 * 10000)
		AND	// You can concatenate two procedures or commands using AND/OR commands for more clarity
		DOING TRY dailyChack CATCH problem LOG ERROR problem; END()	// To make a callable block then call that
	ELSE STOP BROWSER;	// To close the current BROWSER of this workspace
	/*
		* You can STOP some variables of the workspace too, using:
			STOP browser			// To close the inputted browser
			STOP application			// To close the inputted application
			STOP window			// To close the inputted window
			STOP tab				// To close the inputted tab
			STOP document			// To stop loading of the inputted document
			STOP frame				// To stop loading of the inputted iframe
	*/
```