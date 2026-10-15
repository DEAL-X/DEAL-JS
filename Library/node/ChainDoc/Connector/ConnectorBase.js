"use strict";

/** Connector contract: exists(path), readLines(path), writeLines(path, iterable), appendLines(path, iterable), readText(path), writeText(path,text). */
class MemoryConnector {
  constructor(initial = {}) { this.files = new Map(Object.entries(initial)); }
  exists(path) { return this.files.has(String(path)); }
  *readLines(path) {
    const text = this.files.get(String(path));
    if (text == null) return;
    const lines = String(text).split(/\r\n|\n|\r/);
    if (lines.length && lines[lines.length - 1] === "") lines.pop();
    yield* lines;
  }
  writeLines(path, lines) { this.files.set(String(path), [...lines].join("\n")); }
  appendLines(path, lines) {
    const old = this.files.get(String(path));
    const added = [...lines].join("\n");
    this.files.set(String(path), old == null || old === "" ? added : added === "" ? old : `${old}\n${added}`);
  }
  readText(path) { return this.files.get(String(path)) ?? ""; }
  writeText(path, text) { this.files.set(String(path), String(text)); }
}

/** Node.js adapter. Streaming reads are lazy; writing uses a temporary file + rename for safer replacement. */
class NodeFileConnector {
  constructor(fs = require("node:fs"), pathModule = require("node:path")) {
    this.fs = fs; this.path = pathModule;
  }
  exists(path) { try { return this.fs.statSync(path).isFile(); } catch { return false; } }
  *readLines(path) {
    // readline is asynchronous; this sync iterator intentionally avoids loading the whole file.
    const { StringDecoder } = require("node:string_decoder");
    const decoder = new StringDecoder("utf8");
    const fd = this.fs.openSync(path, "r");
    const chunk = Buffer.allocUnsafe(64 * 1024);
    let carry = "", bytesRead;
    try {
      while ((bytesRead = this.fs.readSync(fd, chunk, 0, chunk.length, null)) > 0) {
        carry += decoder.write(chunk.subarray(0, bytesRead));
        let start = 0, match;
        const re = /\r\n|\n|\r/g;
        while ((match = re.exec(carry)) !== null) {
          yield carry.slice(start, match.index);
          start = re.lastIndex;
        }
        carry = carry.slice(start);
      }
      carry += decoder.end();
      if (carry.length) yield carry;
    } finally { this.fs.closeSync(fd); }
  }
  readText(path) { return this.fs.readFileSync(path, "utf8"); }
  writeText(path, text) {
    const temp = `${path}.tmp-${process.pid}-${Date.now()}`;
    try { this.fs.writeFileSync(temp, String(text), "utf8"); this.fs.renameSync(temp, path); }
    catch (e) { try { this.fs.unlinkSync(temp); } catch {} throw e; }
  }
  writeLines(path, lines) {
    const temp = `${path}.tmp-${process.pid}-${Date.now()}`;
    const fd = this.fs.openSync(temp, "w");
    let first = true;
    try {
      for (const line of lines) {
        if (!first) this.fs.writeSync(fd, "\n");
        this.fs.writeSync(fd, String(line ?? ""));
        first = false;
      }
      this.fs.closeSync(fd);
      this.fs.renameSync(temp, path);
    } catch (e) {
      try { this.fs.closeSync(fd); } catch {}
      try { this.fs.unlinkSync(temp); } catch {}
      throw e;
    }
  }
  appendLines(path, lines) {
    const fd = this.fs.openSync(path, "a");
    let first = this.fs.fstatSync(fd).size === 0;
    try {
      for (const line of lines) {
        if (!first) this.fs.writeSync(fd, "\n");
        this.fs.writeSync(fd, String(line ?? ""));
        first = false;
      }
    } finally { this.fs.closeSync(fd); }
  }
}
module.exports = { MemoryConnector, NodeFileConnector };
