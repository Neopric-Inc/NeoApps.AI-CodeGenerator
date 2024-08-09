import { Navigate, useNavigate } from "react-router";

import { StateMachine, StateMachineSteps } from "./stateMachine"
import { Designer } from "sequential-workflow-designer";
import task_logo from "./icons/icon-task.svg";
import create_logo from "./icons/icon-create.svg";
import read_logo from "./icons/icon-read.svg";
import update_logo from "./icons/icon-update.svg";
import delete_logo from "./icons/icon-delete.svg";
import filter_logo from "./icons/icon-filter.svg";
import if_logo from "./icons/icon-if.svg";
import loop_logo from "./icons/icon-loop.svg";
import save_logo from "./icons/icon-save.svg";
import text_logo from "./icons/icon-text.svg";
// import startDeffromJSON from "./startDefinition";
import startDeffromJSON from "./startDefinition.json"
// import { addWorkflow, getAllWorkflow, getOneWorkflow, getWorkflow } from "services/workflowsService";
import "styles/workflow_style.css";
import { addWorkflows, getOneWorkflows } from "services/workflowsService";
import { updateWorkflows } from "services/workflowsService";
// import { addWorkflows_Projects } from "services/workflows_projectsService";

// let BaseApi_Url = process.env.REACT_APP_API_SWAGGER_BASE_URL;
const token = localStorage.getItem('token');
let BearerToken = "Bearer " + token;
// def Contains the all Model Schema Info which will be use for POST & PUT requests
let def;
// designer is for our Workflow Designer Window
let designer;

export class Steps {
    static createThirdPartyAPIStep(type, name, inputAPI, inputAPIBody, outputFormat) {
        return StateMachineSteps.createTaskStep(name, type, {
            inputAPI: inputAPI,
            inputAPIBody: inputAPIBody,
            outputFormat: outputFormat
        })
    }

    static createDBStep(type, name, operation) {
        return StateMachineSteps.createTaskStep(name, type, {
            op: operation
        });
    }

    // PathStep is our custom step creation for all API operations
    static createPathStep(type, name, path, tooltype, pathQuery, requestType) {
        return StateMachineSteps.createTaskStep(name, type, {
            op: path,
            toolType: tooltype,
            queryPath: pathQuery,
            reqType: requestType
        });
    }

    // add , sub , mul , div
    static createMathStep(type, name, varName, val) {
        return StateMachineSteps.createTaskStep(name, type, {
            var: varName,
            val
        });
    }

    // simple text for like start & end
    static createTextStep(message) {
        return StateMachineSteps.createTaskStep(message, 'text', {
            text: message
        });
    }

    // if , else step
    static createIfStep(varName, val, name, trueSteps, falseSteps, operation) {
        return StateMachineSteps.createIfStep(name, {
            op: operation,
            var: varName,
            val
        }, trueSteps, falseSteps);
    }

    // looping step
    static createLoopStep(varName, val, name, steps = "") {
        return StateMachineSteps.createLoopStep(name, {
            var: varName,
            val
        }, steps);
    }

}

function createVariableIfNeeded(varName, data) {
    if (typeof data[varName] === 'undefined') {
        data[varName] = 0;
    }
}

// Element is actual element in which we want to modify the query
// response is the content_query which we got from previous steps
// This will be useful while forward data from previous steps
async function createQueryFromParams(element, response) {
}

async function onDemoRunClicked(definition, BaseApi_Url) {
    let sequence = definition.sequence;
    // console.log("sequence from Run Clicked : ", sequence);
    // resStorage is useFul for Storing all return Data after executing Workflow
    let resStorage = [];
    let previousReqContent;
    let previousReq;
    let resultofStep;
    for (let index = 0; index < sequence.length; index++) {
        const element = sequence[index];
        if (element.type === "Third_Party_API") {
            let requestOptions;
            if (element["properties"]['reqType'] === "GET" || element["properties"]['reqType'] === "DELETE") {
                requestOptions = {
                    method: element["properties"]['reqType'],
                    'access-control-allow-origin': '*',
                };
            }
            else if (element["properties"]['reqType'] === "POST" || element["properties"]['reqType'] === "PUT") {
                requestOptions = {
                    method: element["properties"]['reqType'],
                    'access-control-allow-origin': '*',
                    body: element["properties"]["inputAPIBody"]
                };
            }
            console.log("requestOptions : ", requestOptions);
            const response = await fetch(element["properties"]["inputAPI"], requestOptions);
            const result = await response.json();
            resStorage.push(result);
        }
        else {
            let queryPath: string = element["properties"]["queryPath"];
            let reqType: string = element["properties"]["reqType"];
            // let query = createQueryFromParams(element["properties"]);
            let query = element["properties"]["query"];
            // console.log("Query Path :", queryPath, ", reqType : ", reqType, " , Query  : ", query);
            // making actual request and using promise to store data
            previousReq = generatePathandResponse(BaseApi_Url, queryPath, reqType, query).then((response) => {
                return response;
            });
            // Store Particular Step Result
            let obj = {};
            await previousReq.then(content => {
                if (reqType.toUpperCase() === "GET" || reqType.toUpperCase() === "HEAD") {
                    resultofStep = content["document"];
                    obj["result"] = resultofStep;
                    obj["methodName"] = element["name"];
                    obj["resultValue"] = resultofStep;
                    obj["reqType"] = reqType;
                    obj["base_queryPath"] = element["properties"]["queryPath"];

                    for (let sendContent_index = index + 1; sendContent_index < sequence.length; sendContent_index++) {
                        const send_contentElement = sequence[sendContent_index];
                        if (send_contentElement["name"] === element["properties"]["sendContent"]) {
                            let send_contentQuery = send_contentElement["properties"]["query"];
                            for (let send_contentQuery_index = 0; send_contentQuery_index < send_contentQuery.length; send_contentQuery_index++) {
                                let send_contentQuery_obj = send_contentQuery[send_contentQuery_index];
                                // console.log("send_contentQuery_obj : ", Object.keys(send_contentQuery_obj)[0], " Value : ", send_contentQuery_obj[Object.keys(send_contentQuery_obj)[0]]);
                                if (send_contentQuery_obj !== null && resultofStep !== null) {
                                    // console.log("inside send_contentQuery_obj !== null && resultofStep !== null");

                                    // if (send_contentQuery_obj[Object.keys(send_contentQuery_obj)[0]]) {

                                    // console.log("send_contentQuery_obj[Object.keys(send_contentQuery_obj)[0]] : ", send_contentQuery_obj[Object.keys(send_contentQuery_obj)[0]]);
                                    // console.log("resultofStep :", resultofStep);
                                    // console.log("resultofStep[Object.keys(send_contentQuery_obj)[0]] :", resultofStep[Object.keys(send_contentQuery_obj)[0]]);
                                    if (resultofStep[Object.keys(send_contentQuery_obj)[0]]) {
                                        if (Object.keys(send_contentQuery_obj)[0] === "createdAt" || Object.keys(send_contentQuery_obj)[0] === "modifiedAt") {
                                            let date = new Date();
                                            let formattedDate = `${date.getFullYear()}-${(date.getMonth() + 1).toString().padStart(2, "0")}-${date.getDate().toString().padStart(2, "0")}T${date.getHours().toString().padStart(2, "0")}:${date.getMinutes().toString().padStart(2, "0")}:${date.getSeconds().toString().padStart(2, "0")}`;
                                            send_contentQuery_obj[Object.keys(send_contentQuery_obj)[0]] = formattedDate;
                                            console.log("date from send_contentElement['name'] : ", formattedDate);
                                            console.log(element["name"], " => ", send_contentElement["name"], "{", Object.keys(send_contentQuery_obj)[0], " : ", resultofStep[Object.keys(send_contentQuery_obj)[0]], "}");
                                        }
                                        else {
                                            send_contentQuery_obj[Object.keys(send_contentQuery_obj)[0]] = resultofStep[Object.keys(send_contentQuery_obj)[0]];
                                            console.log(element["name"], " => ", send_contentElement["name"], "{", Object.keys(send_contentQuery_obj)[0], " : ", resultofStep[Object.keys(send_contentQuery_obj)[0]], "}");
                                        }
                                    }
                                    // }
                                }
                                else {
                                    console.log("send_contentQuery_obj OR resultofStep is NULL.")
                                }
                            }
                            // send_contentElement["properties"]["query"] = resultofStep;
                            console.log("from :", element, " send_contentElement : ", send_contentElement);
                        }
                    }
                }
                else if (reqType.toUpperCase() === "POST" || reqType.toUpperCase() === "PUT" || reqType.toUpperCase() === "PATCH" || reqType.toUpperCase() === "DELETE") {
                    // resultofStep = content["document"];
                    resultofStep = content["document"];
                    obj["methodName"] = element["name"];
                    obj["result"] = resultofStep;
                    obj["reqType"] = reqType;
                    obj["base_queryPath"] = element["properties"]["queryPath"];

                    // const newQuery = query.map(obj => {
                    //     const keys = Object.keys(obj);
                    //     const firstKey = keys[0];
                    //     return { [firstKey]: obj[firstKey] };
                    // });

                    for (let sendContent_index = index + 1; sendContent_index < sequence.length; sendContent_index++) {
                        const send_contentElement = sequence[sendContent_index];
                        if (send_contentElement["name"] === element["properties"]["sendContent"]) {
                            let send_contentQuery = send_contentElement["properties"]["query"];
                            for (let send_contentQuery_index = 0; send_contentQuery_index < send_contentQuery.length; send_contentQuery_index++) {
                                let send_contentQuery_obj = send_contentQuery[send_contentQuery_index];
                                // console.log("send_contentQuery_obj : ", Object.keys(send_contentQuery_obj)[0], " Value : ", send_contentQuery_obj[Object.keys(send_contentQuery_obj)[0]]);
                                if (send_contentQuery_obj !== null && resultofStep !== null) {
                                    // console.log("inside send_contentQuery_obj !== null && resultofStep !== null");

                                    // if (send_contentQuery_obj[Object.keys(send_contentQuery_obj)[0]] && resultofStep[Object.keys(send_contentQuery_obj)[0]]) {

                                    // console.log("send_contentQuery_obj[Object.keys(send_contentQuery_obj)[0]] : ", send_contentQuery_obj[Object.keys(send_contentQuery_obj)[0]]);
                                    // console.log("resultofStep :", resultofStep);
                                    // console.log("resultofStep[Object.keys(send_contentQuery_obj)[0]] :", resultofStep[Object.keys(send_contentQuery_obj)[0]]);
                                    if (resultofStep[Object.keys(send_contentQuery_obj)[0]]) {
                                        if (Object.keys(send_contentQuery_obj)[0] === "createdAt" || Object.keys(send_contentQuery_obj)[0] === "modifiedAt") {
                                            let date = new Date();
                                            let formattedDate = `${date.getFullYear()}-${(date.getMonth() + 1).toString().padStart(2, "0")}-${date.getDate().toString().padStart(2, "0")}T${date.getHours().toString().padStart(2, "0")}:${date.getMinutes().toString().padStart(2, "0")}:${date.getSeconds().toString().padStart(2, "0")}`;
                                            send_contentQuery_obj[Object.keys(send_contentQuery_obj)[0]] = formattedDate;
                                            console.log("date from send_contentElement['name'] : ", formattedDate);
                                            console.log(element["name"], " => ", send_contentElement["name"], "{", Object.keys(send_contentQuery_obj)[0], " : ", resultofStep[Object.keys(send_contentQuery_obj)[0]], "}");
                                        }
                                        else {
                                            send_contentQuery_obj[Object.keys(send_contentQuery_obj)[0]] = resultofStep[Object.keys(send_contentQuery_obj)[0]];
                                            console.log(element["name"], " => ", send_contentElement["name"], "{", Object.keys(send_contentQuery_obj)[0], " : ", resultofStep[Object.keys(send_contentQuery_obj)[0]], "}");
                                        }
                                    }

                                    // }
                                }
                                else {
                                    console.log("send_contentQuery_obj OR resultofStep is NULL.")
                                }
                            }
                            // send_contentElement["properties"]["query"] = resultofStep;
                            console.log("from :", element, " send_contentElement : ", send_contentElement);
                        }
                    }
                    // obj["result"] = newQuery;
                    // console.log(`Before Object.keys(obj["result"][0])[0] : `, Object.values(obj["result"][0])[0]);
                    // obj["result"][0][Object.keys(obj["result"][0])[0]] = resultofStep;
                    // console.log(`After Object.keys(obj["result"][0])[0] resultofStep : `, resultofStep);

                    // if (resultofStep == null) {
                    //     console.log(element["name"], "resultofStep is Null");
                    // }
                    // else {
                    //     console.log(element["name"], "resultofStep is NOT Null");
                    // }


                }
                previousReqContent = content;
            });
            resStorage.push(obj);
        }
    }
    console.log("final Combine result : ", resStorage);
}


// title for right side configuration Bar
function appendTitle(parent, text) {
    const title = document.createElement('h4');
    title.innerHTML = "<h5 id='methodTitle'>" + text + "</h5>";
    parent.appendChild(title);
}


// actual logic for generating respose by making actual path from taking input from configuration bar
var generatePathandResponse = (baseUrl, pathAfterBaseUrl, reqType, query) => {
    return new Promise(async (resolve, reject) => {
        let response, content;
        let bodyArgs = {};
        // looping through query object that we make earlier using function "createQueryFromParams()"
        for (let index = 0; index < query.length; index++) {
            const element = query[index];
            let keys = Object.keys(element);
            // making adjustments for query string for like 'Baseurl?pages=1&items=100'
            if (element["query"]) {
                if (index == 0)
                    baseUrl += pathAfterBaseUrl + '?';
                if (element.query) {
                    baseUrl += keys[0] + "=" + element[keys[0]];
                }
                if (index != query.length - 1)
                    baseUrl += "&";
            }
            // making adjustments for query string for like 'Baseurl/{productID}' to 'Baseurl/1'
            else if (element["path"]) {
                // seperating path like /v1/api/product/getProductById to [v1,api,product,{getProductById}]
                let fields = pathAfterBaseUrl.split('/');
                // iterate through each field in previous list
                for (let indexi = 0; indexi < fields.length; indexi++) {
                    const ele = fields[indexi];
                    // which field has both like '{getProductById}' replace it by our input and make query string
                    if (ele.includes('{') && ele.includes('}')) {
                        let ele1 = ele.replace('{', '');
                        let ele2 = ele1.replace('}', '');
                        // match extracted query string ID with former query structure
                        for (let indexj = 0; indexj < query.length; indexj++) {
                            const el = query[indexj];
                            let keysj = Object.keys(el);
                            if (keysj[0].toUpperCase() === ele2.toUpperCase()) {
                                pathAfterBaseUrl = pathAfterBaseUrl.replace(ele2, element[keys[0]])
                            };
                        }
                    }
                }
                pathAfterBaseUrl = pathAfterBaseUrl.replace('{', '');
                pathAfterBaseUrl = pathAfterBaseUrl.replace('}', '');
                baseUrl += pathAfterBaseUrl;
            }
            // we dont have to change URL while passing body
            else if (element["body"]) {
                if (index == 0)
                    baseUrl += pathAfterBaseUrl;
                let keys = Object.keys(element);
                bodyArgs[keys[0]] = element[keys[0]];
            }
        }
        // Making actual request
        if (reqType.toUpperCase() === "GET" || reqType.toUpperCase() === "HEAD") {
            response = await fetch(baseUrl, {
                method: reqType.toUpperCase(), // *GET, POST, PUT, DELETE, etc.
                mode: 'cors', // no-cors, *cors, same-origin
                headers: {
                    'Authorization': BearerToken,
                    'Content-Type': 'application/json',
                    'access-control-allow-origin': '*',
                    'www-authenticate': 'Bearer',
                }
            });
            content = await response.json();

        } else if (reqType.toUpperCase() === "POST" || reqType.toUpperCase() === "PUT" || reqType.toUpperCase() === "PATCH" || reqType.toUpperCase() === "DELETE") {
            response = await fetch(baseUrl, {
                method: reqType.toUpperCase(),
                mode: 'cors', // no-cors, *cors, same-origin
                headers: {
                    'Authorization': BearerToken,
                    'Content-Type': 'application/json',
                    'access-control-allow-origin': '*',
                    'www-authenticate': 'Bearer',
                },
                body: JSON.stringify(bodyArgs),
            });
            content = await response.json();
        } else {
            console.log("Invalid Request Type.");
        }
        resolve(content);
    })
}

// for taking input from right side configuration bar (its bit lengthy because we are inserting HTML to js (but it will shrink when we use react .jsx))
async function takeInput(parent, properties, step) {
    // console.log("step : ", step);
    let params = properties['op'];
    let queryPath = properties['queryPath'];
    let reqType = properties['reqType'];
    let allSelectValues: any;
    let allSelectValuesContent;
    let query = [];
    let field;
    let fieldInRequire;
    var responseFromPUT;
    if (step["type"] === "create" || step["type"] === "read" || step["type"] === "update" || step["type"] === "delete") {

        // for each input field , this will work similar to previous 'generatePathandResponse()' function
        for (let index = 0; index < params.length; index++) {
            let inType = properties['op'][index].in;
            // for schema values from model
            if (inType === 'body') {
                // console.log("inside Body");
                let schemaType = properties['op'][index].schema["$ref"].replace("#/definitions/", "");
                for (let defindex = 0; defindex < def.length; defindex++) {
                    let element = def[defindex];
                    // upper for loop is for matching the schemaType from all those model in def
                    if (Object.keys(element)[0] === schemaType) {
                        let required = def[defindex][schemaType].required; // Array
                        let def_properties = def[defindex][schemaType].properties; // Objects
                        let keys = Object.keys(def_properties);
                        field = document.createElement('p');
                        parent.appendChild(field);
                        // check if required attribute is true or not & insert label & input field
                        for (let innerIndex = 0; innerIndex < keys.length; innerIndex++) {
                            if (keys[innerIndex] === "createdBy" || keys[innerIndex] === "modifiedBy" || keys[innerIndex] === "createdAt" || keys[innerIndex] === "modifiedAt" || def_properties[keys[innerIndex]].type === 'date-time') {
                                let obj = {};
                                if (def_properties[keys[innerIndex]].type === 'date-time' || keys[innerIndex] === "createdAt" || keys[innerIndex] === "modifiedAt") {
                                    let date = new Date();
                                    let formattedDate = `${date.getFullYear()}-${(date.getMonth() + 1).toString().padStart(2, "0")}-${date.getDate().toString().padStart(2, "0")}T${date.getHours().toString().padStart(2, "0")}:${date.getMinutes().toString().padStart(2, "0")}:${date.getSeconds().toString().padStart(2, "0")}`;
                                    obj[keys[innerIndex]] = formattedDate;
                                }
                                else {
                                    obj[keys[innerIndex]] = "Jeelesh Darji";
                                }
                                obj["body"] = true;
                                query.push(obj);
                            }
                            else {
                                if (required.includes(keys[innerIndex])) {
                                    field.innerHTML = `<label>*</label> <input type="text" required>`
                                } else
                                    field.innerHTML = `<label></label> <input type="text">`;
                                field.querySelector('label').innerText += keys[innerIndex];

                                // Input Variable Type
                                let inputDataType = def_properties[keys[innerIndex]].type;
                                const input = field.querySelector('input');
                                if (!(properties['op'][index].value == undefined || properties['op'][index].value == null)) {
                                    // console.log("inside if.");
                                    for (let i = 0; i < properties['op'][index].value.length; i++) {
                                        const innerKeyName = Object.keys(properties['op'][index].value[i])[0];
                                        if (keys[innerIndex] === innerKeyName) {
                                            input.value = (properties['op'][index].value[i][innerKeyName] ? properties['op'][index].value[i][innerKeyName] : "");
                                        }
                                    }
                                }
                                let obj = {};
                                let parsedIntInput;
                                // to track change in values
                                field.addEventListener('input', () => {
                                    parsedIntInput = parseInt(input.value);
                                    // simple input validation (it can be improved further)
                                    if (inputDataType == 'integer' && !isNaN(parsedIntInput) && input.value == parsedIntInput) {
                                        input.value = parseInt(input.value);
                                        // console.log("input.value : ", input.value, " type : ", typeof parseInt(input.value));
                                        obj[keys[innerIndex]] = parseInt(input.value);
                                    } else if (inputDataType == 'string') {
                                        obj[keys[innerIndex]] = input.value;
                                    } else
                                        alert("Please Enter Integer Value.")
                                });
                                if (inputDataType == 'integer') {
                                    obj[keys[innerIndex]] = parseInt(input.value);
                                }
                                else {
                                    obj[keys[innerIndex]] = input.value;
                                }
                                obj["body"] = true;
                                // console.log("Obj after Query : ", obj);
                                query.push(obj);
                                field = document.createElement('p');
                                parent.appendChild(field);
                            }
                        }
                    }
                }
                properties['op'][index].value = query;
            }
            // for query string input params like /{productID}/{OrderId}
            else if (inType == "path") {
                let inputDataType = params[index].type;
                let keys = Object.keys(properties);
                field = document.createElement('p');
                parent.appendChild(field);
                field.innerHTML = `<label></label> <input type="text">`;
                field.querySelector('label').innerText = params[index].name;
                const input = field.querySelector('input');
                let name = params[index].name;
                // console.log("params[index] : ", params[index]);
                // console.log("for PATH properties['op'][index].value :- ", properties['op'][index].value);
                let obj = {};
                if (Array.isArray(properties['op'][index].value)) {
                    for (let innerIndexforPath = 0; innerIndexforPath < properties['op'][index].value.length; innerIndexforPath++) {
                        const element = properties['op'][index].value[innerIndexforPath];
                        if (element[name] != undefined && element["path"] == true) {
                            input.value = element[name];
                            obj[name] = input.value;
                            break;
                        } else {
                            input.value = params[index].default ? params[index].default : "";
                            obj[name] = input.value;
                            break;
                        }
                    }
                } else {
                    input.value = (properties['op'][index].value ? properties['op'][index].value : (params[index].default ? params[index].default : ""));
                }
                // it is essential for persisting the values if we mistekanly change the form
                let parsedIntInput;
                field.addEventListener('input', () => {
                    obj[name] = input.value;
                });
                obj[name] = input.value;
                obj["path"] = true;
                query.push(obj);
                properties['op'][index].value = query;
                // console.log("Obj for PATH , ", obj);
                field = document.createElement('p');
                parent.appendChild(field);
            }
            // for simple query like 'url?page=10&item=100' 
            else {
                let inputDataType = params[index].type;
                let inType = properties['op'][index].in;
                field = document.createElement('p');
                parent.appendChild(field);
                // field name & input field
                if (inputDataType == 'string' && params[index].format == 'date-time') {
                    field.innerHTML = `<label></label> <input type="date">`;
                } else {
                    field.innerHTML = `<label></label> <input type="text">`;

                }
                // setting up field name 
                field.querySelector('label').innerText = params[index].name;
                let name = params[index].name;
                const input = field.querySelector('input');
                input.value = (properties['op'][index].value ? properties['op'][index].value : (params[index].default ? params[index].default : ""));
                let obj = {};
                let parsedIntInput;
                field.addEventListener('input', () => {
                    parsedIntInput = parseInt(input.value);
                    obj[name] = input.value;

                    if (inputDataType == 'integer' && !isNaN(parsedIntInput) && input.value == parsedIntInput) {
                        input.value = parsedIntInput;
                        properties['op'][index].value = parsedIntInput;
                        console.log("Name : ", name, "properties['op'][index].value is : ", properties['op'][index].value);
                    } else if (inputDataType == 'string') {
                        properties['op'][index].value = input.value;
                        console.log("Name : ", name, "properties['op'][index].value is : ", properties['op'][index].value);
                    } else
                        alert("Please Enter Integer Value.")
                });
                obj[name] = input.value;
                properties['op'][index].value = input.value;
                obj["query"] = true;
                query.push(obj);
            }
        }

        // for Selecting at Which Step you want to forward your Output
        let selectfield = document.createElement('p');
        parent.appendChild(selectfield);
        selectfield.innerHTML = `<label></label> <select id='stepName'></select>`;
        selectfield.querySelector('label').innerText = "At Which Step pass the data?";
        const definition = designer.getDefinition();
        const sequence = definition["sequence"];
        const select = selectfield.querySelector("select");
        let selectTextContent = "";
        // flag is useful for render only below element than current
        let flag = false;
        for (let index = 0; index < sequence.length; index++) {
            const element = sequence[index];
            if (step["name"] === element["name"]) { flag = true; }
            if (flag === true) {
                if (step["name"] === element["name"] || properties["sendContent"] === element["name"]) {
                    selectTextContent += `<option value="${element["name"]}" selected>${element["name"]}</option>`;
                }
                else {
                    selectTextContent += `<option value="${element["name"]}">${element["name"]}</option>`;
                }
            }
        }
        selectfield.querySelector("select").innerHTML = selectTextContent;
        select.addEventListener("change", () => {
            properties["sendContent"] = select.value;
        })
        properties["sendContent"] = select.value;
        properties["query"] = query;
        // console.log("Properties from Takeinput : ", properties);
        const spaceLine = document.createElement('div');
        parent.appendChild(spaceLine);
        spaceLine.innerHTML = `<hr>`;

    }
    // console.log("sequence after input : ", sequence);
}

// what to do with right side configuration bar when click on specific type of toolbox
function appendDropDownField(parent, label, startValue, set, step: any = "") {
    const field = document.createElement('p');
    parent.appendChild(field);
    field.innerHTML = `<label></label> <select id='reqType'></select>`;
    field.querySelector('label').innerText = label;
    const select = field.querySelector("select");
    const selectTextContent = `<option value="GET" selected>get</option><option value="POST">post</option><option value="PUT">put</option><option value="DELETE">delete</option>`;
    select.innerHTML = selectTextContent;
    select.addEventListener("change", () => {
        step["properties"]["reqType"] = select.value;
    })

}

function appendTextareaField(parent, label, startValue, set, step: any = "") {
    if (label === "Body") {
        const field = document.createElement('p');
        parent.appendChild(field);
        field.innerHTML = `<label></label> <textarea>`;
        field.querySelector('label').innerText = label;
        const input = field.querySelector('textarea');
        console.log(`step["properties"]["inputAPIBody"] : ${step["properties"]["inputAPIBody"]}`);
        (step !== undefined && step["properties"]["inputAPIBody"] !== undefined) ? input.value = step["properties"]["inputAPIBody"] : input.value = startValue;
        field.addEventListener('input', () => {
            // set(input.value)
            step["properties"]["inputAPIBody"] = input.value
            console.log("step from Body : ", step);
        });
    }
}
// what to do with right side configuration bar when click on specific type of toolbox
function appendTextField(parent, label, startValue, set, step: any = "") {
    if (label === "Database") {
        const field = document.createElement('p');
        parent.appendChild(field);
        // Taking input from right congiguration bar
        takeInput(parent, step.properties, step);
    }
    else if (label === "URL") {
        const field = document.createElement('p');
        parent.appendChild(field);
        field.innerHTML = `<label></label> <input type="text">`;
        field.querySelector('label').innerText = label;
        const input = field.querySelector('input');
        (step != undefined && step["properties"]["inputAPI"] != undefined) ? input.value = step["properties"]["inputAPI"] : input.value = startValue;
        field.addEventListener('input', () => {
            // set(input.value)
            step["properties"]["inputAPI"] = input.value
            console.log("step from API : ", step);

        });
    }
    else {
        const field = document.createElement('p');
        parent.appendChild(field);
        field.innerHTML = `<label></label> <input type="text">`;
        field.querySelector('label').innerText = label;
        const input = field.querySelector('input');
        input.value = startValue;
        field.addEventListener('input', () => set(input.value));
    }
}


var gep = 0;
// when we didn't choose any button GlobalMenu from Right Bar
function globalEditorProvider(definition, globalContext) {
    // console.log("GlobalContext : ", globalContext);
    const container = document.createElement('span');
    let sequence = definition.sequence;
    if (gep > 0) {
        for (let index = 0; index < sequence.length; index++) {
            let step = sequence[index];
            appendTitle(container, step["name"] + " " + step.type);
            appendTextField(container, 'Name', step.name,
                v => {
                    let newName = "<h2>New Name</h2>"
                    step.name.innerHTML = newName;
                    globalContext.notifyNameChanged();
                });
            appendTextField(container, 'Database', step.type,
                v => {
                    step.properties['op'] = v;
                    globalContext.notifyPropertiesChanged();
                }, step)
        }
    }
    globalContext.notifyPropertiesChanged();
    gep = gep + 1;
    return container;
}

// on which type of step like what to do
function stepEditorProvider(step, editorContext) {
    // console.log("step from stepEditor :- ", step);
    const container = document.createElement('div');
    appendTitle(container, step["name"] + " " + step.type);
    appendTextField(container, 'Name', step.name,
        v => {
            let newName = "<h2>New Name</h2>"
            step.name.innerHTML = newName;
            editorContext.notifyNameChanged();
        });
    if (step.properties['var'] !== undefined) {
        appendTextField(container, 'Variable', step.properties['var'],
            v => {
                step.properties['var'] = v;
                editorContext.notifyPropertiesChanged();
            });
    }
    if (step.properties['val']) {
        appendTextField(container, 'Value', step.properties['val'],
            v => {
                step.properties['val'] = parseInt(v, 10);
                editorContext.notifyPropertiesChanged();
            });
    }
    if (step.properties['text']) {
        appendTextField(container, 'Text', step.properties['text'],
            v => {
                step.properties['text'] = v;
                editorContext.notifyPropertiesChanged();
            });
    }
    if (step.properties['toolType']) {
        appendTextField(container, 'Database', step.type,
            v => {
                step.properties['op'] = v;
                editorContext.notifyPropertiesChanged();
            }, step)
    }
    if (step.type === "Third_Party_API") {
        appendTextField(container, 'URL', "http://localhost:5000/swagger/v1/swagger.json",
            v => {
                step.properties['inputAPI'] = v;
                editorContext.notifyPropertiesChanged();
            }, step);
        editorContext.notifyPropertiesChanged();
        appendDropDownField(container, 'ReqType', "",
            v => {
                step.properties['reqType'] = v;
                editorContext.notifyPropertiesChanged();
            }, step)
        appendTextareaField(container, 'Body', "",
            v => {
                step.properties['inputAPIBody'] = v;
                editorContext.notifyPropertiesChanged();
            }, step);
        editorContext.notifyPropertiesChanged();
        appendTextField(container, 'JSONSchema', "{name : '',type : '',path : ''}",
            v => {
                step.properties['outputFormat'] = v;
                editorContext.notifyPropertiesChanged();
            });
    }
    // when we choose any step this log will be trigger
    // console.log(step);
    editorContext.notifyPropertiesChanged();
    return container;
}

// this function is responsible for generating actual step when we drag it from toolBox
function createStepsFromPathResult(result, excludingAction) {
    // console.log("createStepsFromPathResult : ", result)
    let steps = [];
    for (let index = 0; index < result.length; index++) {
        const element = result[index];
        let key = Object.keys(element)[0];
        let listOfGroups = [];
        for (let keyIndex = 0; keyIndex < result[index][key].length; keyIndex++) {
            let obj = result[index][key][keyIndex];
            if (obj.operationId != excludingAction)
                // for all 4 basic CRUD operation make seperate type of request
                if (obj.reqType == 'get')
                    listOfGroups.push(Steps.createPathStep('read', obj.operationId, obj.parameters, "DBOperation", obj.queryPath, obj.reqType));
                else if (obj.reqType == 'post')
                    listOfGroups.push(Steps.createPathStep('create', obj.operationId, obj.parameters, "DBOperation", obj.queryPath, obj.reqType));
                else if (obj.reqType == 'put' || obj.reqType == 'patch')
                    listOfGroups.push(Steps.createPathStep('update', obj.operationId, obj.parameters, "DBOperation", obj.queryPath, obj.reqType));
                else if (obj.reqType == 'delete')
                    listOfGroups.push(Steps.createPathStep('delete', obj.operationId, obj.parameters, "DBOperation", obj.queryPath, obj.reqType));
        }
        let group;
        group = {
            name: key,
            steps: listOfGroups
        }
        steps.push(group);
    }
    return steps;
}

// how to set toolBox at the start-up of project
let actualSteps = [{
    name: 'Tasks',
    steps: [
        Steps.createMathStep('add', 'Add', 'x', 10),
        Steps.createMathStep('sub', 'Subtract', 'x', 10),
        Steps.createMathStep('mul', 'Multiply', 'x', 10),
        Steps.createMathStep('div', 'Divide', 'x', 10),
        Steps.createTextStep('Message!')
    ]
},
{
    name: 'Logic',
    steps: [
        Steps.createIfStep('x', 10, 'If', '', '', '>'),
        Steps.createLoopStep('index', 3, 'Loop')
    ]
},
{
    name: 'APICalls',
    steps: [
        Steps.createThirdPartyAPIStep("Third_Party_API", "API Call", "URL", "", "JSONSchema")
    ]
}
];

// Downloading the JSON definition for making the actual workflow (import , export functionality)
function onDownloadClicked(event, newDefinition) {
    // Create a Blob object with the content of the file
    const content = JSON.stringify(newDefinition, undefined, 4);
    const blob = new Blob([content], {
        type: 'application/json'
    });
    // Create a URL that can be used to reference the file
    const url = URL.createObjectURL(blob);
    // Create an anchor element and set its href and download attributes
    const a = document.createElement('a');
    a.href = url;
    a.download = 'Definition.json';
    // Add the anchor element to the document and click it to initiate the download
    document.body.appendChild(a);
    a.click();
    // Clean up by revoking the object URL
    URL.revokeObjectURL(url);
}

function onUpdatebtnClicked(newDefinition, id) {
    // newDefinition["sequence"][0]
    let BodyObj = {};
    console.log("New Definition from Update : ", newDefinition);
    BodyObj["workflow_name"] = "Backend_stacks_adding";
    BodyObj["workflow_description"] = "Adding Backend Stacks into Database.";
    BodyObj["steps"] = JSON.stringify(newDefinition['sequence']);
    BodyObj["triggerpoint"] = JSON.stringify({ "trigger": newDefinition["sequence"][0] });
    BodyObj["createdBy"] = "Jeelesh Darji";
    BodyObj["modifiedBy"] = "Jeelesh Darji";

    let date = new Date();
    let formattedDate = `${date.getFullYear()}-${(date.getMonth() + 1).toString().padStart(2, "0")}-${date.getDate().toString().padStart(2, "0")}T${date.getHours().toString().padStart(2, "0")}:${date.getMinutes().toString().padStart(2, "0")}:${date.getSeconds().toString().padStart(2, "0")}`;
    // let formattedDate = "2023-02-27T17:08:26.00";
    BodyObj["createdAt"] = formattedDate;
    BodyObj["modifiedAt"] = formattedDate;
    BodyObj["isActive"] = 1;
    let response = updateWorkflows(id, BodyObj).then((res) => {
        if (res.status === 200) {
            document.getElementById("crud-operation").innerHTML = res.data.message;
        }
        console.log("res", res);
    });
}

function OnExportbtnClicked(newDefinition) {
    let BodyObj = {};
    console.log("New Definition from Export : ", newDefinition);
    BodyObj["workflow_id"] = 1;

    if (newDefinition['sequence'].length === 0) {
        BodyObj["workflow_name"] = "Empty workflow";
        BodyObj["workflow_description"] = "Empty Workflow";
    }
    else {
        let workflow_name, workflow_description, tableName, actionName;
        tableName = newDefinition['sequence'][0]['name'];
        actionName = newDefinition['sequence'][0]['type'];

        workflow_name = tableName.split(/_(?!.*_)/)[0]; // "Backend_stacks"
        tableName = workflow_name;

        if (actionName === "read")
            workflow_name += "_reading";
        else if (actionName === "create")
            workflow_name += "_adding";
        else if (actionName === "update")
            workflow_name += "_updating";
        else if (actionName === "delete")
            workflow_name += "_deleting";

        BodyObj["workflow_name"] = workflow_name;

        workflow_description = "when " + tableName + " is ";
        if (actionName === "read")
            workflow_description += "readed.";
        else if (actionName === "create")
            workflow_description += "added";
        else if (actionName === "update")
            workflow_description += "updated";
        else if (actionName === "delete")
            workflow_description += "deleted";

        BodyObj["workflow_description"] = workflow_description;
    }

    BodyObj["steps"] = JSON.stringify(newDefinition['sequence']);
    BodyObj["triggerpoint"] = JSON.stringify({ "trigger": newDefinition["sequence"][0] });
    BodyObj["createdBy"] = "Jeelesh Darji";
    BodyObj["modifiedBy"] = "Jeelesh Darji";

    // let date = new Date();
    // let formattedDate = `${date.getFullYear()}-${(date.getMonth() + 1).toString().padStart(2, "0")}-${date.getDate().toString().padStart(2, "0")}T${date.getHours().toString().padStart(2, "0")}:${date.getMinutes().toString().padStart(2, "0")}:${date.getSeconds().toString().padStart(2, "0")}.000Z`;
    let formattedDate = "2023-02-27T17:08:26.00";
    BodyObj["createdAt"] = formattedDate;
    BodyObj["modifiedAt"] = formattedDate;
    BodyObj["isActive"] = 1;
    let response = addWorkflows(BodyObj).then((res) => {
        console.log("addWorkflows : ", res);
        let workflowId = res.data.document;
        if (res.status === 200) {
            document.getElementById("crud-operation").innerHTML = res.data.message;
        }
        // addWorkflows_Projects({ "workflow_id": workflowId, "project_id": parseInt(project_id), "modifiedBy": "Jeelesh Darji", "createdBy": "Jeelesh Darji", "createdAt": formattedDate, "modifiedAt": formattedDate, "isActive": 1 }).then((res) => {
        //     console.log("addWorkflows_Projects : ", res);

        // })
    });

    // console.log("Response :- ", response);
}


async function onImportbtnClicked(id) {
    if (id != undefined) {
        let response = await getOneWorkflows(id);
        let importSequence = JSON.parse(response.data.document.steps);
        let importDefinition = {};
        importDefinition['properties'] = {
            speed: 300
        };
        importDefinition['sequence'] = importSequence;
        console.log("importDefinition : ", importDefinition);
        return importDefinition;
    }
    else {
        console.log("id not found.");
    }
}

export default async function main(designerFromMain, groupsFromResult, result, defFromMain, id, tableName, action, base_url) {
    console.log("id from Main :", id);
    // Passing def & designer from Main
    def = defFromMain;
    designer = designerFromMain;
    let startDefinition: any;
    // if we are creating new workflow then we need some basic starting datastructure
    if (id === undefined) {
        startDefinition = {
            properties: {
                speed: 300
            },
            sequence: []
        }
    }
    else {
        startDefinition = await onImportbtnClicked(id);
    }
    // startDeffromJSON is JSON object from startDefinition.tsx file
    // startDefinition = startDeffromJSON;
    // console.log("startDefinition from JSON data :- ", startDefinition);
    // console.log("designer from Main Before : ", designer);
    var newDefinition = startDefinition;
    var importSequence = [];
    var importDefinition: any;
    // generating all CRUD operation steps for out ToolBar
    let promise = new Promise((resolve, reject) => {
        let excludingAction = tableName;
        if (action === "deleting")
            excludingAction += "_Delete";
        else if (action === "adding")
            excludingAction += "_Post";
        else if (action === "updating")
            excludingAction += "_Put";

        groupsFromResult = createStepsFromPathResult(result, excludingAction);
        resolve(groupsFromResult);
    });

    // initial configuration (ideally it should not be in main but due JS single threaded approch it is not working)
    const configuration = {
        undoStackSize: 5,
        toolbox: {
            isHidden: false,
            // appending if-else , loop
            groups: (groupsFromResult).concat(actualSteps),
        },
        // setting-up the icons (inbuilt functionality)
        steps: {
            iconUrlProvider: (componentType, type) => {
                const supportedIcons = ['if', 'loop', 'text', 'create', 'read', 'update', 'delete'];
                const fileName = supportedIcons.includes(type) ? type : 'task';
                let logo = (fileName === 'create') ? create_logo : (fileName === 'read') ? read_logo : (fileName === 'update') ? update_logo : (fileName === 'delete') ? delete_logo : (fileName === 'filter') ? filter_logo : (fileName === 'if') ? if_logo : (fileName === 'loop') ? loop_logo : (fileName === 'save') ? save_logo : (fileName === 'text') ? text_logo : task_logo;
                return logo;
            },
            validator: (step) => {
                return Object.keys(step.properties).every(n => !!step.properties[n]);
            }
        },
        editors: {
            globalEditorProvider, // Global Main Menu
            stepEditorProvider // Menu for Each Step
        }
    };
    const placeholder = document.getElementById('designer');
    designer = Designer.create(placeholder, startDefinition, configuration);

    // when we drop any menu from toolBox it will change the definition
    designer.onDefinitionChanged.subscribe((changedDefinition) => {
        newDefinition = changedDefinition;
        designer = Designer.create(placeholder, newDefinition, configuration);
        console.log(newDefinition);
    });

    document.getElementById('run').addEventListener('click', () => {
        console.log("on Run Clicked : ", newDefinition);
        onDemoRunClicked(newDefinition, base_url);
    });
    // what to do when download button is clicked
    document.getElementById('download').addEventListener('click', function (event) {
        // The anonymous function receives the event object as an argument
        // You can pass any additional values as arguments to the anonymous function
        onDownloadClicked(event, newDefinition);
    });
    document.getElementById('importtoDB').addEventListener('click', async function (event) {
        importDefinition = await onImportbtnClicked(id);
        if (importDefinition) {
            // console.log("Import Definition from Main : ", importDefinition);
            let newPlaceholder = document.getElementById('designer');
            newPlaceholder.innerHTML = "";
            designer = await Designer.create(newPlaceholder, importDefinition, configuration);
            designer.onDefinitionChanged.subscribe((changedDefinition) => {
                newDefinition = changedDefinition;
                // console.log("New Changed : ", newDefinition);
            });
        }
        else {
            console.log("Can't Import.");
        }
    });
    document.getElementById('exporttoDB').addEventListener('click', async function (event) {
        OnExportbtnClicked(newDefinition);
    });

    document.getElementById('updatetoDB').addEventListener('click', async function (event) {
        onUpdatebtnClicked(newDefinition, id);
    })
    document.getElementById('jsonFileInput').addEventListener('change', async function (event) {
        const inputElement = document.getElementById('jsonFileInput') as HTMLInputElement;
        const file = inputElement.files[0];
        let workflowTriggerData;
        if (file) {
            const reader = new FileReader();
            reader.readAsText(file);

            reader.onload = async () => {
                const fileContents = reader.result;
                if (typeof fileContents === "string") {
                    workflowTriggerData = JSON.parse(fileContents);
                    // console.log("workflowTriggerData : ", workflowTriggerData);
                } else if (fileContents instanceof ArrayBuffer) {
                    const decoder = new TextDecoder();
                    const decodedString = decoder.decode(fileContents);
                    workflowTriggerData = JSON.parse(decodedString);
                    // console.log("workflowTriggerData : ", workflowTriggerData);
                }
                let importSequence = [];
                importSequence.push(workflowTriggerData);
                importDefinition = {};
                importDefinition['properties'] = {
                    speed: 300
                };
                importDefinition['sequence'] = importSequence;
                console.log("importDefinition from File Change: ", importDefinition);
                if (importDefinition) {
                    // console.log("Import Definition from Main : ", importDefinition);
                    let newPlaceholder = document.getElementById('designer');
                    newPlaceholder.innerHTML = "";
                    designer = await Designer.create(newPlaceholder, importDefinition, configuration);
                    designer.onDefinitionChanged.subscribe((changedDefinition) => {
                        newDefinition = changedDefinition;
                        // console.log("New Changed : ", newDefinition);
                    });
                }
                else {
                    console.log("Can't Import.");
                }
            };
        }


    });

}
