import type { FC, ReactNode } from "react";
import { useEffect, useState } from "react";
import PropTypes from "prop-types";
import {
  Card,
  CardHeader,
  Divider,
  IconButton,
  ThemeProvider,
  
} from "@mui/material";

import DarkModeRoundedIcon from "@mui/icons-material/DarkModeRounded";
import WbSunnyRoundedIcon from "@mui/icons-material/WbSunnyRounded";
import {createTheme} from '../../theme/index';

interface DemoPreviewerProps {
  element: ReactNode;
  name: string ;
}
export interface Settings {
  direction?: "ltr" | "rtl";
  responsiveFontSizes?: boolean;
  theme: "light" | "dark";
}
export const WidgetPreviewer: FC<DemoPreviewerProps> = (props) => {
  const { element, name, ...other } = props;
  
  
  let settings : Settings = {
    direction: 'ltr',
        responsiveFontSizes: true,
        theme: 'light'
  }
  // const [selectedTheme, setSelectedTheme] = useState(settings.theme);
  // const handleSwitch = (): void => {
  //   setSelectedTheme((prevSelectedTheme) => {
  
  //     return prevSelectedTheme === "light"
  //       ? setSelectedTheme("dark")
  //       : setSelectedTheme("light");
  //   });
  // };

  const theme = createTheme({
    ...settings,
    mode: "light",
  });

 

  return (
    <Card variant="outlined" sx={{ mb: 8 }} {...other}>
      <CardHeader
        // action={
        //   <IconButton onClick={handleSwitch}>
        //     {selectedTheme === "light" ? (
        //       <DarkModeRoundedIcon fontSize="small" />
        //     ) : (
        //       <WbSunnyRoundedIcon fontSize="small" />
        //     )}
        //   </IconButton>
        // }
        title={name}
      />
      <Divider />
      <ThemeProvider theme={theme}>
        {element}
      </ThemeProvider>
      {/* <>{element}</> */}
    </Card>
  );
};

WidgetPreviewer.propTypes = {
  element: PropTypes.node.isRequired,
  name: PropTypes.string.isRequired,
};
