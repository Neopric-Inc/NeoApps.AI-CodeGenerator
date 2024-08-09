import React from "react";
import { enableRipple } from '@syncfusion/ej2-base';
import { ButtonComponent } from '@syncfusion/ej2-react-buttons';
import Box from '@mui/material/Box';
enableRipple(true);

// Click Event.
function btnClick() {
    // window.open("https://www.google.com");
}

const CustomButton = (props) => {
    let config = props.config;
    if (config === undefined)
        config = {};

    ////console.log("props from Button:- ", props)
    return (
        // <ButtonComponent>Click Here</ButtonComponent>
        <Box>
            <ButtonComponent type={config["type"] ? config["type"] : "Button"} cssClass={config["cssClass"] ? config["cssClass"] : "e-primary auto-size"} onClick={btnClick} style={{ width: "auto" }}>{config["innerContent"] ? config["innerContent"] : "Button"}</ButtonComponent>
        </Box>
    )
}

export default CustomButton;

// cssClass
// e-primary(Pink)	=   Used to represent a primary action.
// e-success(Green)	=   Used to represent a positive action.
// e-info(Blue)	    =   Used to represent an informative action.
// e-warning(Orange)	=   Used to represent an action with caution.
// e-danger(Red)	    =   Used to represent a negative action.
// e-link(simpleLink)	    =   Changes the appearance of the Button like a hyperlink.
// e-flat
// e-outline
// e-round

// type
// Button	=   Defines a click Button.
// Submit	=   This Button submits the form data to the server.
// Reset	=   This Button resets all the controls of the form elements to their initial values.