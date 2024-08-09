import { Designer } from "sequential-workflow-designer";
import { getAllWorkflows, getapi, getDef } from "services/workflowsService";
import logo from "../icons/icon-task.svg";
import 'sequential-workflow-designer/dist/index.umd.js'
import 'sequential-workflow-designer/css/designer.css';
import 'sequential-workflow-designer/css/designer-light.css';
import 'sequential-workflow-designer/css/designer-dark.css';
import "styles/workflow_style.css";
import { useEffect, useRef, useState } from "react";
import main from "./utility";
import { useParams } from "react-router-dom";


export const AddWorkflow: React.FC = (props: any) => {
    const { tableName, action, workflowId } = useParams();

    // const { id: idValue } = useParams();
    // Importing Variables from configuration File
    // let BaseApi_Url = process.env.REACT_APP_API_SWAGGER_BASE_URL;
    // const api_url = BaseApi_Url + "/swagger/v1/swagger.json";
    // def contains all the model schema
    var def;
    // result is all the data from swagger.json in structured form
    var result;
    var groupsFromResult = [];
    const designerRef = useRef(null);
    let designer;
    const api_url = "not created"
    const finalUrl = api_url.replace("v1/api", "swagger/v1/swagger.json");
    console.log("finalUrl : ", finalUrl);
    const new_url = api_url.replace("/v1/api", "");
    // console.log("base_url : " , new_url);
    let base_url = finalUrl;
    // let protocolRegex = /^((https?:\/\/)|([^\/]+))[^\/]+/i;
    // let match = base_url.match(protocolRegex);
    // if (match) {
    //     base_url = match[0];
    // }
    // console.log(base_url);
    // console.log("base_url : ", base_url);
    // Syntex for Promise & Resolve (store pathList into the result only after we successfully fetch our data)
    getapi(base_url).then((pathList) => {
        result = pathList;
        // console.log("API Result from Main : ", result);
        return getDef(base_url);
    }).then((response) => {
        def = response;
        // console.log("Models from Main : ", def);
        main(designerRef.current, groupsFromResult, result, def, workflowId, tableName, action, new_url);
        // console.log(designerRef.current);
    });






    // useEffect(() => {
    //     // console.log(designer);
    // }, []);
    function closeWindow() {
        window.close();
    }

    return (
        <>
            {console.log("Data from param : ", tableName, action, workflowId)}
            <div id="navbar">
                <div id="left-buttons">
                    <button id="exporttoDB">Save 🔼</button>
                    <button id="importtoDB">Import ⬇️</button>
                    <button id="updatetoDB">Update</button>
                    <button id="close" onClick={closeWindow}>Close ❌</button>
                </div>
                <div id="crud-operation">

                </div>
                <div id="right-buttons">
                    <input type="file" id="jsonFileInput" ></input>
                    <button id="run">Run 🚀</button>
                    <button id="download">Download 🔽</button>
                </div>
            </div>
            <div id="designer" ref={designerRef}></div>
        </>
    )
}

