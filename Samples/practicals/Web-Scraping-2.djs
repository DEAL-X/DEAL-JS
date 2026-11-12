// HOW TO USE:
// Use Samples/practicals/Web-Scraping-2.djs;

Log subject "The process is started";

Consider the query constant to be "DSL";
Consider the normalFile constant to be the file (temporary file "normal.csv");
Consider the finalFile constant to be the file (temporary file "final.csv");

Load http://google.com/search?q=${query};

# ScrapeSERP
	Begin,
		Consider n be 0;
		For each item of (all div[data-rpos] ), begin:
			normalFile append the collect
				* query as QUERY,
				* (make normal (one a>h3 from item)) as title,
				* (make normal (one div[data-sncf="1"] from item)) as description,
				* (make normal ((one a:has(h3) from item).href)) as url;
			Log message `The record ${++n} is fetched...`;
		End;
		Const nextButton = one a#pnnext;
		If nextButton is not null, load http://google.com${nextButton.href}, then ScrapeSERP;
		Else begin,
			For each item of normalFile fields, If:
				* The item description is not empty
				* And (item description length > 20 or !item description match /\b\d{4}—\s*$/)
				* And !finalRecords find ({ title: item title }),
				finalRecords append the item;
			Log subject "The process is ended";
		End;
	End;