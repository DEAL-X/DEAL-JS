"use strict";
const { ChainedDocument } = require("./ChainedDocument");

class ChainedDocumentCollection {
  constructor() { this.items = new Map(); }
  get size() { return this.items.size; }
  get count() { return this.items.size; }
  has(key) { return this.items.has(String(key)); }
  get(key) { return this.items.get(String(key)) ?? null; }
  at(index) { return [...this.items.values()][index] ?? null; }
  keys() { return this.items.keys(); }
  values() { return this.items.values(); }
  [Symbol.iterator]() { return this.items[Symbol.iterator](); }
  add(keyOrDocument, maybeDocument) {
    const key = maybeDocument === undefined ? keyOrDocument.Path : String(keyOrDocument);
    const doc = maybeDocument === undefined ? keyOrDocument : maybeDocument;
    if (!(doc instanceof ChainedDocument)) throw new TypeError("Expected a ChainedDocument.");
    if (this.items.has(key)) this.items.get(key).appendForePiece(doc);
    else this.items.set(key, doc);
    return this;
  }
  delete(key) { return this.items.delete(String(key)); }
}
module.exports = { ChainedDocumentCollection };
