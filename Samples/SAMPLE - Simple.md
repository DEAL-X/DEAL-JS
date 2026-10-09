# A Complex Sample
Consider the following example:
```ts
/*
	This is just a tutorial sample and will not work.
*/
use scraper;

Let source be resource openfile;
Let destination be resource savefile;

# DailyChack:
For each merchandise of file (source),
	* Try to fetch "api.market.com/search" with merchandise, then:
		* If fetched,
			* For each item of response json(), append destination, item;
		* Else load "www.market.com/search/merchandise=" + merchandise, then:
			* If loaded, do
				* For each item of all ("div table>tbody>tr"), append destination, item; 
				* Click any button#next-page;
				* End;
			* Else log warning, `Could not load the page for the ${merchandise}!`;
	* Catch log error `Could not fetch data completely for the ${merchandise}!`;
```
This compiles to the following JavaScript code:
```js
/*
    This is just a tutorial sample and will not work.
*/
// Used 18 libraries from "scraper"

let source = resource.openfile;
let destination = resource.savefile;

(DailyChack = (() => {
    for (const merchandise of file(source)) try {
			fetch("api.market.com/search", merchandise).then((data) => {
				handle(data);
				
				if (fetched) 
					for (const item of response.json())
						append(destination,item);
				else load("www.market.com/search/merchandise=" + merchandise).then((data) => {
					handle(data);
					
					if (loaded) {
						for (const item of all("div table>tbody>tr"))
							append(destination,item);
						click(any("button#next-page"));
					}  else log.warning(`Could not load the page for the ${merchandise}!`);
				})
			})
		} 
		catch {
			log.error(`Could not fetch data completely for the ${merchandise}!`);
		}
}))();
```