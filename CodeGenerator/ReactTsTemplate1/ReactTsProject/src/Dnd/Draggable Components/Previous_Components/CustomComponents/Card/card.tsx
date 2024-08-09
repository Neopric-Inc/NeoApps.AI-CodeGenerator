import * as React from "react";
import * as ReactDOM from "react-dom";
import "./style.css";
export const Card = (props) => {
    let config = props.config;
    if (config === undefined)
        config = {};

   //console.log("props from Card:- ", props)
    let header, content;
    config.header ? header = config.header : header = "Header";
    config.content ? content = config.content : content = "Lorem ipsum dolor sit amet consectetur adipisicing elit. Ipsam doloribus ea ipsa corporis sint cumque corrupti accusantium voluptas. Labore dicta ab, magni similique cumque quo possimus quae harum qui officiis.";
    return (
        <div className="card">
            <div className="e-card" id="basic">
                <div className="e-card-header">
                    <div className="e-card-header-caption">
                        <div className="e-card-title">{header}</div>
                    </div>
                </div>
                <div className="e-card-content">
                    {content}
                </div>
            </div>
        </div>
    );
}
