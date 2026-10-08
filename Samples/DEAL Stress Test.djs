/*
 * ============================================================
 * MiMFa DEAL.JS Compiler Stress Test
 * ============================================================
 * This file intentionally contains many DEAL.JS structures.
 * It should remain semantically correct after compilation.
 */
// ------------------------------------------------------------
// Use libraries
// ------------------------------------------------------------
use deal/core;
use deal/data/local.djs;
use deal/data/resource;
use deal/data/notExist;
use deal;
use scraper;

// ------------------------------------------------------------
// Reserve and noise words
// ------------------------------------------------------------
Reserve the; // a noise word
Reserve every = "for";

// ------------------------------------------------------------
// Commands
// ------------------------------------------------------------
command testA = "A"; // Definition command
const #testB = "B"; // Delegation command
command testC = { tkey1: (v)=>v, tkey2: TESTB};
command testD(val = " ") {  // Function command
    return TESTc tkey1(tESTA+val);
}
# loadDef IF url is not null, LOAD url, "utf8", ELSE LOAD the "www.mimfa.net"; // Action Command

// ------------------------------------------------------------
// Try / Catch / Finally blocks
// ------------------------------------------------------------
try to
    start browser;
    For each item in objs, 
        Begin try:
                console log item.name + " grade is:";
                if item.score equals greater than 19,
                    console log "A";
                else if item.score equals greater than 17,
                    console log "B";
                else if item.score is lesser than 10,
                    console log "Not graded";
                else console log "C";
            catch msg, log error msg;
        End;
catch msg, log error "A problem is occured!\n" + msg;
finally log subject "The scoring finished!";

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
            - The #Text be myFile content;
            - The successful be true;
            - end;
        * Else, begin:
            - The text be empty;
            - The successful be false;
            - end;
    End;

## UseFile
    Try,
        * The list = text split "\n";
        * If list length be lesser than 1, do:
            + ReadFile;
            + UseFile;
            + End;
        * Else log success "Process finished successfully!";
    Catch err,
        * Log error "Could not finish the process because: " + err;

End;

// ------------------------------------------------------------
// Complex Code
// ------------------------------------------------------------
# scrapeMap
    try to open browser, then
        EVERY url /* The url of the website */ OF list,
            * LOAD url,
            * THEN begin
                * APPEND ALL keys>table>h2 from document keys, to destination;
                * log success `The ${url} is laded successfully`;
                * End,
            * otherwise log error `Could not load ${url}`,
            * anyway log message `The ${url} process is finished!`;
        log subject `The process of all ${list.length} urls are finished!`;
    catch msg, log error msg;
    finally log subject "The process finished!";
