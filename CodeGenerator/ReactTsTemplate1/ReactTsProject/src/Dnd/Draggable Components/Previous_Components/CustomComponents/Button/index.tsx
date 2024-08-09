import React from "react";
import { Button } from "@mui/material";

export default function CustomButton(props) {
  const temp = () => {
   //console.log(props.config);
    props.openLink(props.config[`nav_b_page`]);
  };
  return (
    <Button variant="contained" onClick={temp}>
      {props.config.nav_button_name ? props.config.nav_button_name : "Button Name"}
    </Button>
  );
}
