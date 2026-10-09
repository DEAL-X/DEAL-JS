# A Complex Sample
Consider the following example:
```ts
/*
	This is just a tutorial sample and will not work.
*/
use scraper;

source = resource openfile();
destination = resource savefile("results", "comma separated values files (*.csv)|*.csv");

days = 100;

# DailyChack:
For each row of file(source) rows, begin:
	* Let merchandise be row[5];
	* Try to fetch "api.market.com/search" with merchandise, then:
		* If fetched,
			* For each item of response json(),
				* Append destination, select
						"api.market.com" as reference,
						*,
						item main_price - item discount as price,
						(if item count is 0 "absent" else item.count) as numbers
					From item;
		* Else load "www.market.com/search/merchandise=" + merchandise, then:
			* If loaded, do
				* For each item of all "div table>tbody>tr",
					* Append destination, collect
							"www.market.com" as reference,
							normalize(concat(item[".company-title>*"], " ")) as company_name,
							(normalize first split concat(item[".product-title>*"], " "), /\s+co\s*$/gi) as product_name,
							urldecode item["img.product-image"].src as image,
							number item[7] as main_price,
							number(item[":last-child"]) * main_price as discount,
							main_price - discount as price,
							(if item["td.numbers"] is 0, "absent", else item["td.numbers"]) as numbers; 
				* Click any button#next-page;
				End;
			* Else log warning, `Could not load the page for the ${merchandise} merchandise!`;
	* Catch log error `Could not fetch data completely for the ${merchandise} merchandise!`;
	* End;

If --days > 0, do:
	* Wait (24 * 60 * 60 * 10000);
	* Try DailyChack, catch problem, log error problem;
	* End;
Else stop browser;
```