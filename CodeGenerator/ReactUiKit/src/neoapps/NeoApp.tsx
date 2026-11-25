import React, { useEffect, useMemo, useRef, useState } from "react";
import createCache from "@emotion/cache";
import { CacheProvider } from "@emotion/react";
import { prefixer } from "stylis";
import rtlPlugin from "stylis-plugin-rtl";
import { BrowserRouter, matchPath, Navigate, Route, Routes, useLocation } from "react-router-dom";
import { Alert, Box, CircularProgress, CssBaseline } from "@mui/material";
import { createTheme, ThemeProvider } from "@mui/material/styles";
import { AppProvider, useApp } from "./AppContext";
import { Customize } from "./admin/Customize";
import { Members } from "./admin/Members";
import { Roles } from "./admin/Roles";
import { Assistant } from "./ai/Assistant";
import { JobsPage } from "./jobs/JobsPage";
import { Dashboard } from "./Dashboard";
import { Login } from "./Login";
import { registerServiceWorker } from "./pwa";
import { SignatureDialog } from "./SignatureDialog";
import { RecordPage } from "./RecordPage";
import { ResourcePage } from "./ResourcePage";
import { Shell } from "./Shell";
import { SharedLinkPage } from "./SharedLinkPage";
import { JoinPage } from "./JoinPage";
import { extensions } from "./customExtensions";
import { api } from "./api";
import { t, initialLanguage, matchLanguage, savedLanguage, setAppI18n, setLanguage, useT } from "./i18n";

registerServiceWorker();

// Page translators (Google, Edge) rewrite text nodes React owns, and React then throws when it updates them.
// The app translates itself and opts out (translate="no"); this keeps it working if a translator runs anyway.
if (typeof Node === "function" && Node.prototype && !(Node.prototype as any).__neoappsSafe) {
  const remove = Node.prototype.removeChild;
  Node.prototype.removeChild = function <T extends Node>(this: Node, child: T): T {
    if (child.parentNode !== this) return child;
    return remove.call(this, child) as T;
  };
  const insert = Node.prototype.insertBefore;
  Node.prototype.insertBefore = function <T extends Node>(this: Node, node: T, ref: Node | null): T {
    if (ref && ref.parentNode !== this) return node;
    return insert.call(this, node, ref) as T;
  };
  (Node.prototype as any).__neoappsSafe = true;
}

const caches = {
  ltr: createCache({ key: "mui", prepend: true }),
  rtl: createCache({ key: "muirtl", prepend: true, stylisPlugins: [prefixer, rtlPlugin] }),
};

const Themed: React.FC<{ children?: React.ReactNode }> = ({ children }) => {
  const { config } = useApp();
  const { rtl, language } = useT();
  const theme = useMemo(() => createTheme({
    direction: rtl ? "rtl" : "ltr",
    palette: {
      primary: { main: config.theme?.primary || "#3b5bdb" },
      secondary: { main: config.theme?.secondary || "#0ca678" },
      background: { default: "#f8f9fb" },
    },
    shape: { borderRadius: 8 },
    typography: { fontFamily: '"Inter", "Segoe UI", "Noto Sans", Roboto, Helvetica, Arial, sans-serif', button: { textTransform: "none", fontWeight: 600 } },
    components: {
      MuiButton: { defaultProps: { disableElevation: true } },
      // texts inside MUI components, in the user's language
      MuiTablePagination: { defaultProps: {
        labelRowsPerPage: t("Rows per page:"),
        labelDisplayedRows: ({ from, to, count }: any) => count !== -1 ? t("{from}–{to} of {count}", { from, to, count }) : t("{from}–{to} of more than {to}", { from, to }),
        getItemAriaLabel: (type: string) => (type === "next" ? t("Go to next page") : type === "previous" ? t("Go to previous page") : type === "first" ? t("Go to first page") : t("Go to last page")),
      } },
      MuiAutocomplete: { defaultProps: { noOptionsText: t("No options"), clearText: t("Clear"), openText: t("Open"), closeText: t("Close"), loadingText: t("Loading…") } },
    },
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }), [config.theme?.primary, config.theme?.secondary, rtl, language]);
  return <CacheProvider value={rtl ? caches.rtl : caches.ltr}><ThemeProvider theme={theme}><CssBaseline />{children}</ThemeProvider></CacheProvider>;
};

/**
 * Picks the language before anything shows: the signed-in user's saved choice, else this browser's, else the
 * browser languages, else the app default (ui.config "i18n.default"). Children re-mount when the language changes.
 */
const Language: React.FC<{ children?: React.ReactNode }> = ({ children }) => {
  const { config, me } = useApp();
  const { language } = useT();
  const [ready, setReady] = useState(false);
  const i18n = (config as any).i18n;
  const i18nKey = JSON.stringify(i18n ?? null);
  const synced = useRef<number | null>(null);
  useEffect(() => {
    setAppI18n(i18n);
    const mine = matchLanguage(me?.language);
    setLanguage(mine || initialLanguage(), !!mine).finally(() => setReady(true));
    // a choice made before signing in (on the sign-in page) becomes the user's saved language
    if (me && !me.language && savedLanguage() && synced.current !== me.user_id) {
      synced.current = me.user_id;
      api("PUT", "Access/me/language", { language: savedLanguage() }).catch(() => undefined);
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [i18nKey, me?.user_id, me?.language]);
  if (!ready) return null;
  return <React.Fragment key={language}>{children}</React.Fragment>;
};

const AdminOnly: React.FC<{ element: React.JSX.Element }> = ({ element }) => {
  const { me } = useApp();
  return me?.is_admin ? element : <Alert severity="info">{t("Administrators only.")}</Alert>;
};

const Routed: React.FC = () => {
  const { loading, me, error } = useApp();
  const location = useLocation();
  // pages for visitors without sign-in: shown as they are, no sign-in, no app shell
  // share links (like Google Forms): open without the app shell; "members only" links ask for sign-in on the page
  if (matchPath({ path: "/f/:token", end: true }, location.pathname))
    return <Routes><Route path="/f/:token" element={<SharedLinkPage />} /></Routes>;
  // invite links: choose a username and password, join with the invite's role
  if (matchPath({ path: "/join/:token", end: true }, location.pathname))
    return <Routes><Route path="/join/:token" element={<JoinPage />} /></Routes>;
  const publicRoutes = extensions.publicRoutes || [];
  if (publicRoutes.some((r) => matchPath({ path: r.path, end: true }, location.pathname)))
    return <Routes>{publicRoutes.map((r) => <Route key={r.path} path={r.path} element={r.element} />)}</Routes>;
  if (loading) return <Box sx={{ minHeight: "100vh", display: "grid", placeItems: "center" }}><CircularProgress /></Box>;
  if (!me) return <Login />;
  return (
    <>
      {error && <Alert severity="error">{error}</Alert>}
      <SignatureDialog />
      <Routes>
        <Route element={<Shell />}>
          <Route path="/" element={<Dashboard />} />
          <Route path="/dashboard" element={<Navigate to="/" replace />} />
          <Route path="/assistant" element={<Assistant />} />
          <Route path="/jobs" element={<JobsPage />} />
          {(extensions.routes || []).map((r) => <Route key={r.path} path={r.path} element={r.element} />)}
          <Route path="/r/:table" element={<ResourcePage />} />
          <Route path="/r/:table/:id" element={<RecordPage />} />
          <Route path="/admin/members" element={<AdminOnly element={<Members />} />} />
          <Route path="/admin/roles" element={<AdminOnly element={<Roles />} />} />
          <Route path="/admin/customize" element={<AdminOnly element={<Customize />} />} />
          <Route path="*" element={<Navigate to="/" replace />} />
        </Route>
      </Routes>
    </>
  );
};

/** Root of the generated app UI (schema-driven, configured by ui.config.json and the in-app Customize screen). */
export const NeoApp: React.FC = () => (
  <AppProvider>
    <Themed>
      <Language>
        <BrowserRouter>
          <Routed />
        </BrowserRouter>
      </Language>
    </Themed>
  </AppProvider>
);

export default NeoApp;
