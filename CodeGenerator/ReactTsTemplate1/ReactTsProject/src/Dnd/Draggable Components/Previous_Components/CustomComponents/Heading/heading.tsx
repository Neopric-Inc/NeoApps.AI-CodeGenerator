import React from "react";
const Heading = (props) => {
    let config = props.config;
    if (config === undefined)
        config = {};

   //console.log("props from Header:- ", props)
    let innerContent, backgroundColor, color, fontFamily;
    config.innerContent ? innerContent = config.innerContent : innerContent = "Heading";
    config.backgroundColor ? backgroundColor = config.backgroundColor : backgroundColor = "white";
    config.color ? color = config.color : color = "black";
    config.fontFamily ? fontFamily = config.fontFamily : fontFamily = "Arial";

    const renderHeader = (header, content) => {
        switch (header) {
            case 'h1':
                return <h1>{content}</h1>;
            case 'h2':
                return <h2>{content}</h2>;
            case 'h3':
                return <h3>{content}</h3>;
            case 'h4':
                return <h4>{content}</h4>;
            case 'h5':
                return <h5>{content}</h5>;
            case 'h6':
                return <h6>{content}</h6>;
            default:
                return <h2>{content}</h2>;
        }
    }
    return (
        <div style={{ backgroundColor: backgroundColor, color: color, fontFamily: fontFamily, padding: "1px", borderRadius: "1px", textAlign: "center" }}>
            {renderHeader(config.headerSize, innerContent)}
        </div>
    )
}

export default Heading;