/*
 * ============================================================
 * MiMFa DEAL.JS compiler stress test
 * ============================================================
 * This file intentionally contains many DEAL.JS structures.
 * It should remain semantically correct after compilation.
 */
// ------------------------------------------------------------
// Use libraries
// ------------------------------------------------------------
Use deal/core;
Use deal/data/local.djs;
Use deal/data/resource;
Use deal/data/notExist;
Use deal;
Use scraper;

// ------------------------------------------------------------
// Reserve and noise words
// ------------------------------------------------------------
Reserve the; // a noise word
Reserve every = "for";

// ------------------------------------------------------------
// Definitions
// ------------------------------------------------------------
Consider myConstant constant is procedure;
Consider myVariable variable is equal procedure;
Consider let letVariable be procedure;
Consider directVariable equals procedure;

// ------------------------------------------------------------
// Commands
// ------------------------------------------------------------
Command testA = "A"; // Definition command
const #testB = "B"; // Delegation command
Command testC = { tkey1: (v)=>v, tkey2: TESTB};
Command testD(val = " ") {  // Function command
    return TESTc tkey1(tESTA+val);
}
# loadDef If url is not null, await browser.tab.load(url), Else await browser.tab.load("www.mimfa.net"); // Action command

var #testE:int:string = 4;
list insert testE, www.mimfa.net; // the testE next word will convert to string
var #testF::string = 5;
list insert TestF, www.deal.mimfa.net; // the testF next word will convert to string

Command normalized (value = "") is:
    * make string (value)
    * normalize ("nfkc")
    * replace (/\s+/gu   to the " ")
    * trim it

Consider title const is equal:
    Normalized "  alpha   study ";

// ------------------------------------------------------------
// Try / Catch / Finally blocks
// ------------------------------------------------------------
Try to
    start browser;
    For each item in objs, 
        Begin Try:
                console log item.name + " grade is:";
                If item.score equals greater than 19,
                    console log "A";
                Else if item.score equals greater than 17,
                    console log "B";
                Else if item.score is lesser than 10,
                    console log "Not graded";
                Else console log "C";
            Catch msg, Log error msg;
        End;
Catch msg, Log error "A problem is occured!\n" + msg;
Finally Log subject "The scoring finished!";

// ------------------------------------------------------------
// Operations
// ------------------------------------------------------------
grade.convertTo(a++ + 1 *2, ++b - 1, c++ * 2, 2*5 >> 5);
let gradeNormal = grade[--a][--b][c--].trim(" ", "n", "r").toLowerCase();

// ------------------------------------------------------------
// Markdown like Code
// ------------------------------------------------------------
# FillList
Begin,

## ReadFile
    Begin,
        * MyFile = resource file open();
        * Log subject "Process started!";
        * If **MyFile** not be null, begin:
            - The #Txt be myFile content;
            - The successful be true;
            - end;
        * Else, begin:
            - The txt be empty;
            - The successful be false;
            - end;
    End;

## UseFile
    Try,
        * The list = txt split "\n";
        * If list length be lesser than 1, do:
            + ReadFile;
            + UseFile;
            + End;
        * Else* Log success "Process finished successfully!";
    Catch err,
        * Log error "Could not finish the process because: " + err;

End;

// ------------------------------------------------------------
// Complex Code
// ------------------------------------------------------------
Consider VAR promiseTest will be 18 ** 18, then* Log success its, otherwise* Log error data;

# scrapeMap
    Try to open browser, then
        Every url /* The URL of the website */ of list,
            * await browser.tab.load(url),
            * then begin
                * Append all keys>table>h2 from document keys, to destination;
                ** Log success `The ${url} loaded successfully`;
                * End,
            * otherwise* Log error `Could not load ${url}`,
            * anyway log message `The ${url} process is finished!`;
        Log subject `The process of all ${list.length} urls are finished!`;
    Catch msg, Log error msg;
    Finally Log subject "The process finished!";
