/*
   ============================================================
   MiMFa DEAL.JS Compiler Stress Test
   ============================================================
   This file intentionally contains many DEAL.JS structures.
   It should remain semantically correct after compilation.
 */

// ------------------------------------------------------------
// Use libraries
// ------------------------------------------------------------
// Used 5 libraries from "deal/core"
// Used "deal/data/local.djs" library
// Used "deal/data/resource" library
// Could not find the "deal/data/notExist" library
// Used 14 libraries from "deal"
// Used 4 libraries from "scraper"
// ------------------------------------------------------------
// Reserve and noise words
// ------------------------------------------------------------
// Reserved "the" as a noise
// a noise word
/* Reserved "every" as the "for" */
// ------------------------------------------------------------
// Commands
// ------------------------------------------------------------
var testA = "A";// Definition command
const testB = "B";// Delegation command
var testC = {
    tkey1: (v) => v,
    tkey2: testB
};
function testD(val = " ") {
    return testC.tkey1(testA + val);
}
(loadDef = (() => (url != null  ? load(url,"utf8") : load("www.mimfa.net"))))();
var testE = 4;
list.insert(teste,"www.mimfa.net");// the testE next word will convert to string
var testF = 5;
list.insert(testF,"www.deal.mimfa.net");
// the testF next word will convert to string
// ------------------------------------------------------------
// Try / Catch / Finally blocks
// ------------------------------------------------------------
try {
    start(browser);
    
    for (const item in objs) {
        try {
            window.console.log(item.name + " grade is:");
            
            if (item.score >= 19) window.console.log("A");
            
            else if (item.score >= 17) window.console.log("B");
            
            else if (item.score < 10) window.console.log("Not graded");
            
            else window.console.log("C");
        } 
        catch (msg) {
            log.error(msg);
        }
    }
} 
catch (msg) {
    log.error("A problem is occured!n" + msg);
} 
finally {
    log.subject("The scoring finished!");
}
// ------------------------------------------------------------
// Operations
// ------------------------------------------------------------
grade.convertTo(a++ + 1 * 2, ++b - 1, c++ * 2, 2 * 5 >> 5);
let gradeNormal = grade[--a][--b][c--].trim(" ", "n", "r").toLowerCase();
// ------------------------------------------------------------
// Markdown like Code
// ------------------------------------------------------------
(FillList = (() => {
    (ReadFile = (() => {
        MyFile = resource.file(open());
        Log.subject("Process started!");
        
        if (MyFile != null) {
            Txt = MyFile.content;
            successful = true;
        }  else {
            Txt = "";
            successful = false;
        }
    }))();
    
    
    (UseFile = (() => {
        try {
            list = Txt.split("n");
            
            if (list.length < 1) {
                ReadFile();
                UseFile();
            }  else log.success("Process finished successfully!");
        } 
        catch (err) {
            Log.error("Could not finish the process because: " + err);
        }
    }))();
}))();
// ------------------------------------------------------------
// Complex Code
// ------------------------------------------------------------
(scrapeMap = (() => {
    try {
        open(browser).then((data) => {
            handle(data);
            
            for (const url/* The url of the website */ of list)load(url).then((data) => {
                handle(data);
                append(all("keys>table>h2",window.document.keys().toArray()),destination);
                log.success(`The ${url} is laded successfully`);
            }).catch((data) => {
                handle(data);
                log.error(`Could not load ${url}`);
            }).finally((data) => {
                handle(data);
                log.message(`The ${url} process is finished!`);
            });
        })
        log.subject(`The process of all ${list.length} urls are finished!`);
    } 
    catch (msg) {
        log.error(msg);
    } 
    finally {
        log.subject("The process finished!");
    }
}))();