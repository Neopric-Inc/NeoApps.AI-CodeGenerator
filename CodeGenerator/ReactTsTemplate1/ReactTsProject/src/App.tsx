import { Provider } from "react-redux";

import store from "redux/store";

import Pages from "pages";

import { RTL } from "./components/rtl";

import { ThemeProvider, responsiveFontSizes } from "@mui/material";

import { SettingsConsumer, SettingsProvider } from "context/settings-context";

import { createTheme } from "theme";

function App(): JSX.Element {
  return (
    <Provider store={store}>
      <SettingsProvider>
        <SettingsConsumer>
          {({ settings }) => (
            <ThemeProvider
              theme={createTheme({
                direction: settings.direction,

                responsiveFontSizes: settings.responsiveFontSizes,

                mode: settings.theme,
              })}
            >
              <RTL direction={settings.direction}>
                <div className="App">
                  <Pages />
                </div>
              </RTL>
            </ThemeProvider>
          )}
        </SettingsConsumer>
      </SettingsProvider>
    </Provider>
  );
}

export default App;
