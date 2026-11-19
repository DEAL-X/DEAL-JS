use deal/core;
use deal/make/logical;

// The web package facade loads its shared Fetch types and the public browser,
// navigation, Fetch, request, and download APIs.
use deal/web/.js;
use deal/web/browser;
use deal/web/fetch;
use deal/web/go;
use deal/web/load;
use deal/web/send.js;
use deal/web/download.js;

reserve #Download:identifier as "Download";
reserve #Send:identifier as "Send";
