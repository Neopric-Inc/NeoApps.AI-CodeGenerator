import React from "react";
import { Me } from "./types";

/** Hooks for app-specific screens (see src/custom/index.tsx). */
export interface Extensions {
  /**
   * Pages for visitors without sign-in (a shop, a public form), outside the app shell. They may only call
   * publicApi(...) from "../neoapps/api" (the server allows only what the "Public" role in ui.config.json grants).
   */
  publicRoutes?: { path: string; element: React.ReactElement }[];
  /** Extra pages inside the app shell. */
  routes?: { path: string; element: React.ReactElement }[];
  /** Extra items at the top of the sidebar (shown when `visible` returns true or is omitted). */
  nav?: { to: string; icon: string; label: string; visible?: (me: Me) => boolean }[];
  /** Rendered above the dashboard cards. */
  dashboardTop?: React.ComponentType;
  /** Extra buttons in a record page header. */
  recordActions?: React.ComponentType<{ table: string; record: Record<string, any>; onChanged: () => void }>;
}
