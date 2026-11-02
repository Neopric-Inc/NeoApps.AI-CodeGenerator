import React, { useMemo } from "react";
import { BrowserRouter, Navigate, Route, Routes } from "react-router-dom";
import { Alert, Box, CircularProgress, CssBaseline } from "@mui/material";
import { createTheme, ThemeProvider } from "@mui/material/styles";
import { AppProvider, useApp } from "./AppContext";
import { Customize } from "./admin/Customize";
import { Members } from "./admin/Members";
import { Roles } from "./admin/Roles";
import { Assistant } from "./ai/Assistant";
import { Dashboard } from "./Dashboard";
import { Login } from "./Login";
import { RecordPage } from "./RecordPage";
import { ResourcePage } from "./ResourcePage";
import { Shell } from "./Shell";
import { extensions } from "../custom";

const Themed: React.FC = ({ children }) => {
  const { config } = useApp();
  const theme = useMemo(() => createTheme({
    palette: {
      primary: { main: config.theme?.primary || "#3b5bdb" },
      secondary: { main: config.theme?.secondary || "#0ca678" },
      background: { default: "#f8f9fb" },
    },
    shape: { borderRadius: 8 },
    typography: { fontFamily: '"Inter", "Segoe UI", Roboto, Helvetica, Arial, sans-serif', button: { textTransform: "none", fontWeight: 600 } },
    components: { MuiButton: { defaultProps: { disableElevation: true } } },
  }), [config.theme?.primary, config.theme?.secondary]);
  return <ThemeProvider theme={theme}><CssBaseline />{children}</ThemeProvider>;
};

const AdminOnly: React.FC<{ element: JSX.Element }> = ({ element }) => {
  const { me } = useApp();
  return me?.is_admin ? element : <Alert severity="info">Administrators only.</Alert>;
};

const Routed: React.FC = () => {
  const { loading, me, error } = useApp();
  if (loading) return <Box sx={{ minHeight: "100vh", display: "grid", placeItems: "center" }}><CircularProgress /></Box>;
  if (!me) return <Login />;
  return (
    <>
      {error && <Alert severity="error">{error}</Alert>}
      <Routes>
        <Route element={<Shell />}>
          <Route path="/" element={<Dashboard />} />
          <Route path="/dashboard" element={<Navigate to="/" replace />} />
          <Route path="/assistant" element={<Assistant />} />
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
      <BrowserRouter>
        <Routed />
      </BrowserRouter>
    </Themed>
  </AppProvider>
);

export default NeoApp;
