import * as React from 'react';
import Box from '@mui/material/Box';
import TextField from '@mui/material/TextField';
import { TextBoxComponent } from '@syncfusion/ej2-react-inputs';

export default function CustomTextField(props) {
    let config = props.config;
    if (config === undefined)
        config = {};
    return (
        <TextBoxComponent placeholder="Outlined" cssClass="e-outline" floatLabelType="Auto" value={config["innerContent"] ? config["innerContent"] : "Input"} width="auto" />
    )
}