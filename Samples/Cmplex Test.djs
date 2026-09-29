grade.convertTo(a,b,c);

let gradeNormal = grade[a][b][c].trim(' ','\n','\r').toLowerCase();

const x = url + "?id=" + (index??grade[a][b][c].trim().toLowerCase());

use deal;
use scraper;
Reserve the;
// TEST DEAL
command testA = "a";
command testC = { tkey1: (v)=>v, tkey2: tval2};
testc tkey1(tESTA);

#loadDef
    IF url is not null LOAD url, "utf8", ELSE LOAD the "www.mimfa.net";

    FOR EACH url /* The url of the website */ OF list,
        LOAD url,
        THEN do
        APPEND ALL keys>table>h2 from document keys, to destination;
            log success "Loaded successfully!";
        end,
        otherwise log error "Could not load",
        anyway log message "The process is finished";

// Loading home
Function home() {
    FOR EACH url /* The url of the website */ OF list; // Previous defined list
    LOAD url, "utf8";
    THEN APPEND ALL "h2" TO destination;
}

// To get the grade description
Function getGrade(status="a", defaultText = "lesser than 16") {
    for(i = ParseFLOAT(status)??20; i<=20; i--) {
        if(status AND i % 2 === 0) {
            // Procedures
            console.info(`\${i} is even`);
        } else if(i>0) console.info(`\${i} is odd`);
        else throw "Error";
        // The other test processes
    }
	try to
		while status != null AND parseFloat(status) <= 20,
			switch(this.Grade[status].trim().replace(/\\s+/i, "").toLowerCase() + "") {
				case "a":
				case "a+":
				case "a++":
					return "between 19 - 20";
				case "b":
				case "b+":
				case "b++":
					return "between 17 - 18";
				case "c":
					return "between 16 - 17";
				default:
					return defaultText;
			}
	
	catch ex,
		console.error(ex.message);
	finally{
	}
    }
    // The other test processes
    console.info(`The number is `+i);
}