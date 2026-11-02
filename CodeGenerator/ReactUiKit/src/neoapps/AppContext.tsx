import React, { createContext, useCallback, useContext, useEffect, useMemo, useState } from "react";
import { api, getToken, setToken } from "./api";
import { mergeConfig, resolveAll, resolveMenu } from "./config";
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
  can: (table: string, action: keyof Permission) => boolean;
  refreshSession: () => Promise<void>;
  saveConfig: (cfg: UiConfig) => Promise<void>;
  previewConfig: (cfg: UiConfig | null) => void;
  logout: () => void;
}

const Ctx = createContext<AppState>(null as any);
export const useApp = () => useContext(Ctx);

async function fetchJson(path: string) {
  try {
    const res = await fetch(`${process.env.PUBLIC_URL || ""}${path}`, { cache: "no-store" });
    return res.ok ? await res.json() : null;
  } catch {
    return null;
  }
}

export const AppProvider: React.FC = ({ children }) => {
  const [schema, setSchema] = useState<AppSchema>({ app: "", tables: [] });
  const [fileConfig, setFileConfig] = useState<UiConfig>({});
  const [serverConfig, setServerConfig] = useState<UiConfig | null>(null);
  const [preview, setPreview] = useState<UiConfig | null>(null);
  const [me, setMe] = useState<Me | null>(null);
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
    if (!getToken()) {
      setMe(null);
      setLoading(false);
      return;
    }
    try {
      const [meRes, cfgRes] = await Promise.all([api<any>("GET", "Access/me"), api<any>("GET", "Access/ui-config").catch(() => null)]);
      setMe(meRes?.document || null);
      setServerConfig(cfgRes?.document || null);
    } catch {
      setToken(null);
      setMe(null);
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    refreshSession();
  }, [refreshSession]);

  const baseConfig = useMemo(() => mergeConfig(fileConfig, serverConfig), [fileConfig, serverConfig]);
  const config = useMemo(() => (preview ? mergeConfig(preview) : baseConfig), [baseConfig, preview]);
  const tables = useMemo(() => resolveAll(schema, config), [schema, config]);

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
    return groups.map((g) => ({ ...g, items: g.items.filter((i) => can(i, "read")) })).filter((g) => g.items.length);
  }, [tables, config, can]);

  const saveConfig = useCallback(async (cfg: UiConfig) => {
    await api("PUT", "Access/ui-config", cfg);
    setServerConfig(cfg);
    setPreview(null);
  }, []);

  const logout = useCallback(() => {
    setToken(null);
    setMe(null);
  }, []);

  const value: AppState = {
    loading, error, schema, config, baseConfig, tables, menu, me, can, refreshSession, saveConfig,
    previewConfig: setPreview, logout,
  };
  return <Ctx.Provider value={value}>{children}</Ctx.Provider>;
};
