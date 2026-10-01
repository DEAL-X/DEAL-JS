use scraper;
Reserve the;

command testA = "a";
command testC = { tkey1: (v)=>v, tkey2: tval2};
command testD(val = " ") {
return TESTc tkey1(tESTA+val);
}
#loadDef
IF url is not null, LOAD url, "utf8", ELSE LOAD the "www.mimfa.net";

#scrapeMap
FOR EACH url /* The url of the website */ OF list,
LOAD url,
THEN begin
    APPEND ALL keys>table>h2 from document keys, to destination;
    log success "Loaded successfully!";
end,
otherwise log error "Could not load",
anyway log message "The process is finished";
