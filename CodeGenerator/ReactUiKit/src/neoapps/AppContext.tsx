import React, { createContext, useCallback, useContext, useEffect, useMemo, useState } from "react";
import { api } from "./api";
import { aiStatus, AiStatus } from "./ai/aiApi";
import { effectiveSaved, mergeConfig, resolveAll, resolveMenu } from "./config";
import { AppSchema, Me, MenuGroup, Permission, ResolvedTable, UiConfig } from "./types";

interface AppState {
  loading: boolean;
  error?: string;
  schema: AppSchema;
  /** Config from public/ui.config.json (generator / Claude) merged with the admin-saved copy. */
  config: UiConfig;
  baseConfig: UiConfig;
  tables: Record<string, ResolvedTable>;
  menu: MenuGroup[];
  me: Me | null;
  /** AI assistant status for this user; null while loading or when the API has no AI module. */
  ai: AiStatus | null;
  can: (table: string, action: keyof Permission) => boolean;
  canOpen: (table: string) => boolean;
  /** Whether this user may edit/delete this record: the role permission plus the table's lifecycle locks
   *  (frozen states for everyone, "editableIn" states per role). The API enforces the same rules. */
  canChangeRecord: (table: string, record: Record<string, any> | null | undefined, action: "update" | "delete") => boolean;
  refreshSession: () => Promise<void>;
  saveConfig: (cfg: UiConfig) => Promise<void>;
  previewConfig: (cfg: UiConfig | null) => void;
  logout: () => void;
}

const Ctx = createContext<AppState>(null as any);
export const useApp = () => useContext(Ctx);

async function fetchJson(path: string) {
  try {
    const res = await fetch(`${(import.meta.env.BASE_URL || "/").replace(/\/$/, "")}${path}`, { cache: "no-store" });
    return res.ok ? await res.json() : null;
  } catch {
    return null;
  }
}

export const AppProvider: React.FC<{ children?: React.ReactNode }> = ({ children }) => {
  const [schema, setSchema] = useState<AppSchema>({ app: "", tables: [] });
  const [fileConfig, setFileConfig] = useState<UiConfig>({});
  const [serverConfig, setServerConfig] = useState<UiConfig | null>(null);
  const [preview, setPreview] = useState<UiConfig | null>(null);
  const [me, setMe] = useState<Me | null>(null);
  const [ai, setAi] = useState<AiStatus | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string>();

  useEffect(() => {
    (async () => {
      const [s, c] = await Promise.all([fetchJson("/schema.json"), fetchJson("/ui.config.json")]);
      if (!s) setError("schema.json not found. Regenerate the frontend with the NeoApps generator.");
      setSchema(s || { app: "", tables: [] });
      setFileConfig(c || {});
    })();
  }, []);

  const refreshSession = useCallback(async () => {
    try {
      const [meRes, cfgRes] = await Promise.all([api<any>("GET", "Access/me"), api<any>("GET", "Access/ui-config").catch(() => null)]);
      setMe(meRes?.document || null);
      setServerConfig(cfgRes?.document || null);
      aiStatus().then(setAi).catch(() => setAi({ enabled: false, configured: false, model: "", assistantName: "Assistant", documentTables: [], suggestions: [], workflows: [] }));
    } catch {
      setMe(null); // no session cookie (or it expired): show the sign-in page
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    refreshSession();
  }, [refreshSession]);

  const baseConfig = useMemo(() => mergeConfig(fileConfig, effectiveSaved(fileConfig, serverConfig)), [fileConfig, serverConfig]);
  const config = useMemo(() => (preview ? mergeConfig(preview) : baseConfig), [baseConfig, preview]);
  const tables = useMemo(() => {
    const all = resolveAll(schema, config);
    // columns hidden from this role by the API are left out of lists, forms and detail pages
    for (const [t, cols] of Object.entries(me?.hidden_fields || {})) {
      const r = all[t];
      if (!r || !cols.length) continue;
      const hide = new Set(cols);
      all[t] = {
        ...r,
        listColumns: r.listColumns.filter((c) => !hide.has(c)),
        formFields: r.formFields.filter((c) => !hide.has(c)),
        displayField: hide.has(r.displayField) ? r.primaryKey : r.displayField,
        fields: Object.fromEntries(Object.entries(r.fields).filter(([c]) => !hide.has(c))),
      };
    }
    return all;
  }, [schema, config, me]);

  const can = useCallback(
    (table: string, action: keyof Permission) => {
      if (!me) return false;
      if (me.is_admin) return true;
      const p = me.permissions[table.toLowerCase()];
      return !!p && !!p[action];
    },
    [me]
  );

  const menu = useMemo(() => {
    const groups = resolveMenu(tables, config);
    // optional per-role sidebar: config.menuByRole = { "Client": ["documents"] } shows only those tables for that role
    // (access itself is still governed by role permissions; this only declutters the menu)
    const only = me?.role_name ? (config as any).menuByRole?.[me.role_name] as string[] | undefined : undefined;
    return groups.map((g) => ({ ...g, items: g.items.filter((i) => can(i, "read") && (!only || only.includes(i))) })).filter((g) => g.items.length);
  }, [tables, config, can, me]);

  /** Table pages a role may open: with config.menuByRole, only its menu tables, their child tables and config.openByRole (typing a URL doesn't bypass the menu). */
  const canOpen = useCallback(
    (table: string) => {
      if (!can(table, "read")) return false;
      if (me?.is_admin) return true;
      const only = me?.role_name ? ((config as any).menuByRole?.[me.role_name] as string[] | undefined) : undefined;
      if (!only) return true;
      // config.openByRole = { "Client": ["documents"] }: pages a role may open from custom screens without a menu entry
      const extra = ((config as any).openByRole?.[me!.role_name] as string[] | undefined) || [];
      return only.includes(table) || extra.includes(table) || only.some((t) => tables[t]?.children?.includes(table));
    },
    [can, me, config, tables]
  );

  const canChangeRecord = useCallback(
    (table: string, record: Record<string, any> | null | undefined, action: "update" | "delete") => {
      if (!can(table, action)) return false;
      const lc = (config as any).lifecycles?.[table] as { column?: string; frozen?: string[]; editableIn?: Record<string, string[]> } | undefined;
      if (!lc || !record) return true;
      const state = String(record[lc.column || "status"] ?? "");
      if (action === "update" && (lc.frozen || []).some((s) => s.toLowerCase() === state.toLowerCase())) return false;
      const allowed = me?.role_name ? lc.editableIn?.[me.role_name] : undefined;
      if (allowed && !allowed.includes("*") && !allowed.some((s) => s.toLowerCase() === state.toLowerCase())) return false;
      return true;
    },
    [can, config, me]
  );

  const saveConfig = useCallback(async (cfg: UiConfig) => {
    // remember which published config this was saved against, so a later publish still shows (effectiveSaved)
    const saved = { ...cfg, _base: fileConfig } as UiConfig;
    await api("PUT", "Access/ui-config", saved);
    setServerConfig(saved);
    setPreview(null);
  }, [fileConfig]);

  const logout = useCallback(() => {
    api("POST", "Access/logout").catch(() => undefined); // end the session on the server too
    setMe(null);
  }, []);

  const value: AppState = {
    loading, error, schema, config, baseConfig, tables, menu, me, ai, can, canOpen, canChangeRecord, refreshSession, saveConfig,
    previewConfig: setPreview, logout,
  };
  return <Ctx.Provider value={value}>{children}</Ctx.Provider>;
};
