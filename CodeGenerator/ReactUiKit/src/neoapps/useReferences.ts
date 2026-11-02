import { useEffect, useState } from "react";
import { listRecords } from "./api";
import { useApp } from "./AppContext";
import { ResolvedTable } from "./types";

export interface RefOption { id: any; label: string }

const cache: Record<string, Promise<RefOption[]>> = {};

/** Loads id -> display label for every table referenced by `table` (cached per session). */
export function useReferences(table?: ResolvedTable, reloadKey = 0) {
  const { tables, can } = useApp();
  const [refs, setRefs] = useState<Record<string, RefOption[]>>({});

  useEffect(() => {
    if (!table) return;
    let alive = true;
    const targets = Array.from(new Set(Object.values(table.fields).map((f) => f.column.references?.table).filter(Boolean))) as string[];
    Promise.all(
      targets.map(async (t) => {
        const target = tables[t];
        if (!target || !can(t, "read")) return [t, []] as const;
        const key = `${t}:${reloadKey}`;
        if (!cache[key]) {
          cache[key] = listRecords(t, 1, 1000).then((p) =>
            p.records.map((r) => ({
              id: r[target.primaryKey],
              label: String(r[target.displayField] ?? `#${r[target.primaryKey]}`),
            }))
          ).catch(() => []);
        }
        return [t, await cache[key]] as const;
      })
    ).then((pairs) => alive && setRefs(Object.fromEntries(pairs.map(([k, v]) => [k, v as RefOption[]]))));
    return () => {
      alive = false;
    };
  }, [table, tables, can, reloadKey]);

  const labelFor = (fieldName: string, id: any): string | undefined => {
    const refTable = table?.fields[fieldName]?.column.references?.table;
    if (!refTable || id === null || id === undefined) return undefined;
    const hit = (refs[refTable] || []).find((o) => String(o.id) === String(id));
    return hit ? hit.label : `#${id}`;
  };

  return { refs, labelFor };
}

export const invalidateReferences = (table: string) => {
  Object.keys(cache).filter((k) => k.startsWith(table + ":")).forEach((k) => delete cache[k]);
};
