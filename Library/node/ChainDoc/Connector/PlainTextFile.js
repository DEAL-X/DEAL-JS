"use strict";
const { NodeFileConnector } = require("./ConnectorBase");
/** Node.js plain-text connector; the shared document core depends on this contract, not Node APIs. */
class PlainTextFile extends NodeFileConnector {
  constructor(options = {}) { super(options.fs, options.pathModule); }
}
module.exports = { PlainTextFile };
