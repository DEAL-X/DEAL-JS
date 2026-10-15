"use strict";
const { FilterItem, TableChangeMode } = require("./FilterItem");
class FilterCollection extends Array {
  addOrReplace(filter) {
    if (!(filter instanceof FilterItem)) throw new TypeError("Expected FilterItem.");
    const last = this[this.length - 1];
    if (last && last.From === filter.From && last.To === filter.To && last.Type === filter.Type && filter.Type === TableChangeMode.Modify) this[this.length - 1] = filter;
    else this.push(filter);
    this.sort((a, b) => a.From - b.From || a.ID - b.ID);
    return this;
  }
  *filter(lines) {
    const filters = [...this].sort((a, b) => a.From - b.From || a.ID - b.ID);
    let index = 0, filterIndex = 0;
    for (const line of lines) {
      while (filterIndex < filters.length && filters[filterIndex].To >= 0 && filters[filterIndex].To < index) filterIndex++;
      const active = filters.filter(f => f.From <= index && (f.To < 0 || f.To >= index));
      let result = line, emit = true;
      for (const f of active) {
        if (f.Type === TableChangeMode.Delete) { emit = false; result = null; }
        else if (f.Type === TableChangeMode.Modify) result = f.getItem(index);
      }
      if (emit) yield result;
      index++;
    }
  }
}
module.exports = { FilterCollection };
