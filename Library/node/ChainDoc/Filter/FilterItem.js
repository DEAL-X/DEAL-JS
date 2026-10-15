"use strict";
const TableChangeMode = Object.freeze({ Insert: "Insert", Modify: "Modify", Delete: "Delete" });
class FilterItem {
  constructor(type, from, to = from, items = [], id = 0) {
    if (!Object.values(TableChangeMode).includes(type)) throw new TypeError("Unknown table change mode.");
    if (!Number.isSafeInteger(from) || from < 0) throw new RangeError("from must be a non-negative safe integer.");
    if (!Number.isSafeInteger(to) && to >= 0) throw new RangeError("to must be a safe integer or negative sentinel.");
    this.Type = type; this.From = from; this.To = to; this.Items = [...(items || [])]; this.ID = id;
  }
  get IsCollection() { return this.From < this.To; }
  get ChangeNumber() { const unit = this.Type === TableChangeMode.Insert ? 1 : this.Type === TableChangeMode.Delete ? -1 : 0; if (this.To < 0) return unit * (this.Items.length || Number.MAX_SAFE_INTEGER); return unit * (Math.abs(this.To - this.From) + 1); }
  getItem(index) { if (index < this.From) return null; const offset = index - this.From; if (offset < this.Items.length) return this.Items[offset]; return this.Items.length ? this.Items[0] : null; }
}
module.exports = { FilterItem, TableChangeMode };
