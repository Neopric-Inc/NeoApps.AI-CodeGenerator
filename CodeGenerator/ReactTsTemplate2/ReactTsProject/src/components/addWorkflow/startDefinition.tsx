// here we are generating parameters after connection string or body based on specific http request
// function createQueryFromParams(properties, reqType, query) {

// (baseUrl, pathAfterBaseUrl, reqType, query) => 
// {
//     let response, content;
//     let bodyArgs = {};
//     // looping through query object that we make earlier using function "createQueryFromParams()"
//     for (let index = 0; index < query.length; index++) {
//         const element = query[index];
//         let keys = Object.keys(element);
//         // making adjustments for query string for like 'Baseurl?pages=1&items=100'
//         if (element["query"]) {
//             if (index == 0)
//                 baseUrl += pathAfterBaseUrl + '?';
//             if (element.query) {
//                 baseUrl += keys[0] + "=" + element[keys[0]];
//             }
//             if (index != query.length - 1)
//                 baseUrl += "&";
//         }
//         // making adjustments for query string for like 'Baseurl/{productID}' to 'Baseurl/1'
//         else if (element["path"]) {
//             // seperating path like /v1/api/product/getProductById to [v1,api,product,{getProductById}]
//             let fields = pathAfterBaseUrl.split('/');
//             // iterate through each field in previous list
//             for (let indexi = 0; indexi < fields.length; indexi++) {
//                 const ele = fields[indexi];
//                 // which field has both like '{getProductById}' replace it by our input and make query string
//                 if (ele.includes('{') && ele.includes('}')) {
//                     let ele1 = ele.replace('{', '');
//                     let ele2 = ele1.replace('}', '');
//                     // match extracted query string ID with former query structure
//                     for (let indexj = 0; indexj < query.length; indexj++) {
//                         const el = query[indexj];
//                         let keysj = Object.keys(el);
//                         if (keysj[0].toUpperCase() === ele2.toUpperCase()) {
//                             pathAfterBaseUrl = pathAfterBaseUrl.replace(ele2, element[keys[0]])
//                         };
//                     }
//                 }
//             }
//             pathAfterBaseUrl = pathAfterBaseUrl.replace('{', '');
//             pathAfterBaseUrl = pathAfterBaseUrl.replace('}', '');
//             baseUrl += pathAfterBaseUrl;
//         }
//         // we dont have to change URL while passing body
//         else if (element["body"]) {
//             if (index == 0)
//                 baseUrl += pathAfterBaseUrl;
//             let keys = Object.keys(element);
//             bodyArgs[keys[0]] = element[keys[0]];
//         }
//     }
//     console.log("Base URL : ", baseUrl);
//     console.log("New Body Value : ", bodyArgs);

//     // Making actual request
//     if (reqType.toUpperCase() === "GET" || reqType.toUpperCase() === "HEAD") {
//         response = await fetch(baseUrl, {
//             method: reqType.toUpperCase(), // *GET, POST, PUT, DELETE, etc.
//             mode: 'cors', // no-cors, *cors, same-origin
//             headers: {
//                 'Authorization': BearerToken,
//                 'Content-Type': 'application/json',
//                 'access-control-allow-origin': '*',
//                 'www-authenticate': 'Bearer',
//             }
//         });
//         // let response = await getAllWorkflow(1, 100);
//         // console.log("Response :- ", response.data);
//         content = await response.json();
//         // console.log("GET content : ", content);

//     } else if (reqType.toUpperCase() === "POST" || reqType.toUpperCase() === "PUT" || reqType.toUpperCase() === "PATCH" || reqType.toUpperCase() === "DELETE") {
//         response = await fetch(baseUrl, {
//             method: reqType.toUpperCase(),
//             mode: 'cors', // no-cors, *cors, same-origin
//             headers: {
//                 'Authorization': BearerToken,
//                 'Content-Type': 'application/json',
//                 'access-control-allow-origin': '*',
//                 'www-authenticate': 'Bearer',
//             },
//             body: JSON.stringify(bodyArgs),
//         });
//         content = await response.json();
//         // addBackend_Stacks(bodyArgs).then((response) => {
//         //     console.log("Response :- ", response);
//         // })
//         // console.log(response);

//         // console.log("Base Url :" + baseUrl);
//     } else {
//         console.log("Invalid Request Type.");
//     }
// }
// console.log("query From Function : ", query);
// return query;
// }

// let params = properties['op'];
// let queryPath = properties['queryPath'];
// console.log(queryPath);
// // query has all variable details from input for generating proper request
// let query = [];
// for (let index = 0; index < params.length; index++) {
//     // inType is which Type of input is there for our request like for post->body , get->connectionQuery
//     let inType = properties['op'][index].in;
//     // mostly for post and patch
//     if (inType === 'body') {
//         // getting the schema info
//         let schemaType = properties['op'][index].schema["$ref"].replace("#/definitions/", "");
//         // looping through def to match schema for our request
//         for (let defindex = 0; defindex < def.length; defindex++) {
//             let element = def[defindex];
//             // if keyName is match with schema type
//             if (Object.keys(element)[0] === schemaType) {
//                 let def_properties = def[defindex][schemaType].properties; // Objects
//                 let keys = Object.keys(def_properties);
//                 // for all fields in matched schema use value from input
//                 for (let innerIndex = 0; innerIndex < keys.length; innerIndex++) {
//                     let innerKeys = Object.keys(properties['op'][index].value);
//                     let obj = {};
//                     for (let i = 0; i < innerKeys.length; i++) {
//                         const innerKeyName = innerKeys[i];
//                         if (keys[innerIndex] === innerKeyName)
//                             obj[keys[innerIndex]] = properties['op'][index].value[innerKeyName];
//                     }
//                     obj["body"] = true;
//                     query.push(obj);
//                 }
//             }
//         }
//     }
//     // Mostly for put , patch , getById
//     else if (inType == "path") {
//         let inputDataType = params[index].type;
//         let name = params[index].name;
//         let obj = {};
//         obj[name] = properties['op'][index].value;
//         obj["path"] = true;
//         query.push(obj);
//     }
//     // for getAll
//     else {
//         let inputDataType = params[index].type;
//         let name = params[index].name;
//         let obj = {};
//         obj[name] = properties['op'][index].value;
//         obj["query"] = true;
//         query.push(obj);
//     }
// }


// async function onDemoRunClicked(designer) {
//     console.log("Designer from Run : ", designer);

//     if (designer.isReadonly()) {
//         return;
//     }
//     designer.setIsReadonly(true);
//     // console.log("Designer from Run Clicked : ", designer);

//     const definition = designer.getDefinition();
//     // as name suggest sequence of steps from our flow
//     let sequence = definition.sequence;
//     console.log("sequence from Run Clicked : ", sequence);
//     let resStorage = [];
//     // content of out previous step
//     let previousReqContent;
//     let previousReq;
//     let resultofStep;
//     for (let index = 0; index < sequence.length; index++) {
//         // console.log("Previous Request Content : ", previousReqContent);
//         const element = sequence[index];
//         // console.log(element);
//         let queryPath: string = element["properties"]["queryPath"];
//         let reqType: string = element["properties"]["reqType"];
//         // let query = createQueryFromParams(element["properties"]);
//         let query = element["properties"]["query"];
//         for (let content_index = 0; content_index < sequence.length; content_index++) {
//             const content_element = sequence[content_index];
//             // console.log("content_element : ", content_element, " for Element :- ", element);
//             let content_query;
//             if (content_element["properties"]["sendContent"] === element["name"]) {
//                 // console.log("resStorage is : ", resStorage, " , content_index is : ", content_index);
//                 // let contentQueryFromParams = createQueryFromParams(content_element, resStorage, content_index)
//                 // Object.assign(obj, { a: 4, d: 5 });
//                 content_query = content_element["properties"]["query"];
//                 // console.log("Content Query for : ", content_element["name"], " is :- ", content_query);
//                 for (let copyIndCnt = 0; copyIndCnt < content_query.length; copyIndCnt++) {
//                     const copyEleCnt = content_query[copyIndCnt];
//                     const copyEleCntKey = Object.keys(copyEleCnt)[0];
//                     for (let queryCopyInd = 0; queryCopyInd < query.length; queryCopyInd++) {
//                         const queryCopyCnt = query[queryCopyInd];
//                         const queryCopyCntKey = Object.keys(queryCopyCnt)[0];
//                         if (copyEleCntKey === queryCopyCntKey) {
//                             query[queryCopyInd][queryCopyCntKey] = content_query[copyIndCnt][copyEleCntKey];
//                         }
//                     }
//                 }
//             }
//         }
//         console.log("Query Path :", queryPath, ", reqType : ", reqType, " , Query  : ", query);
//         // making actual request and using promise to store data
//         previousReq = generatePathandResponse(BaseApi_Url, queryPath, reqType, query).then((response) => {
//             // console.log("Reponse in Demo :- ", response);
//             return response;
//         });

//         let obj = {};

//         // waiting for responce to come
//         await previousReq.then(content => {
//             // console.log("Content from PR : ", previousReqContent);
//             if (reqType.toUpperCase() === "GET" || reqType.toUpperCase() === "HEAD") {
//                 // console.log("get Request");
//                 resultofStep = content["document"];
//                 obj["methodName"] = element["name"];
//                 obj["resultValue"] = resultofStep;
//                 obj["reqType"] = reqType;
//                 obj["base_queryPath"] = element["properties"]["queryPath"];
//                 // resStorage.push(element["name"] : pre)
//             }
//             else if (reqType.toUpperCase() === "POST" || reqType.toUpperCase() === "PUT" || reqType.toUpperCase() === "PATCH" || reqType.toUpperCase() === "DELETE") {
//                 resultofStep = content["document"];
//                 obj["methodName"] = element["name"];
//                 obj["resultID"] = resultofStep;
//                 obj["reqType"] = reqType;
//                 obj["base_queryPath"] = element["properties"]["queryPath"];
//                 // console.log("Not get Request");
//             }
//             previousReqContent = content;
//         });

//         // console.log("resultofStep Req : ", resultofStep);

//         resStorage.push(obj);
//         console.log("resStorage after ", index, " step is : ", resStorage);

//     }
//     designer.setIsReadonly(false);
//     console.log("final Combine result : ", resStorage);

// }


export default { "sequence": [] };

// ======================= Only 3 Steps =============================
// export default {
//     "properties": {
//         "speed": 300
//     },
//     "sequence": [
//         {
//             "id": "b478216f6a9a3335c080dfd4b246ec4c",
//             "componentType": "task",
//             "type": "create",
//             "name": "Tenants_Post",
//             "properties": {
//                 "op": [
//                     {
//                         "name": "model",
//                         "in": "body",
//                         "required": true,
//                         "schema": {
//                             "$ref": "#/definitions/TenantsModel"
//                         },
//                         "x-nullable": false,
//                         "value": [
//                             {
//                                 "tenant_id": 4,
//                                 "body": true
//                             },
//                             {
//                                 "tenant_name": "JB",
//                                 "body": true
//                             },
//                             {
//                                 "tenant_contact_info": "Jeelesh is demo tenant.",
//                                 "body": true
//                             }
//                         ]
//                     }
//                 ],
//                 "toolType": "DBOperation",
//                 "queryPath": "/v1/api/Tenants",
//                 "reqType": "post"
//             }
//         },
//         {
//             "id": "7f9e9f37d58750ea772dfe7fb3bb5a9d",
//             "componentType": "task",
//             "type": "create",
//             "name": "Environments_Post",
//             "properties": {
//                 "op": [
//                     {
//                         "name": "model",
//                         "in": "body",
//                         "required": true,
//                         "schema": {
//                             "$ref": "#/definitions/EnvironmentsModel"
//                         },
//                         "x-nullable": false,
//                         "value": [
//                             {
//                                 "environment_id": 5,
//                                 "body": true
//                             },
//                             {
//                                 "environment_name": "Jeelesh_env_5",
//                                 "body": true
//                             }
//                         ]
//                     }
//                 ],
//                 "toolType": "DBOperation",
//                 "queryPath": "/v1/api/Environments",
//                 "reqType": "post"
//             }
//         },
//         {
//             "id": "acdb6731146d6438fb3186490cc8b77c",
//             "componentType": "task",
//             "type": "create",
//             "name": "Projects_Post",
//             "properties": {
//                 "op": [
//                     {
//                         "name": "model",
//                         "in": "body",
//                         "required": true,
//                         "schema": {
//                             "$ref": "#/definitions/ProjectsModel"
//                         },
//                         "x-nullable": false,
//                         "value": [
//                             {
//                                 "project_id": 1,
//                                 "body": true
//                             },
//                             {
//                                 "project_name": "frontendGenerator",
//                                 "body": true
//                             },
//                             {
//                                 "tenant_id": 1,
//                                 "body": true
//                             },
//                             {
//                                 "environment_id": 1,
//                                 "body": true
//                             },
//                             {
//                                 "createdBy": "Jeelesh Darji 1",
//                                 "body": true
//                             },
//                             {
//                                 "modifiedBy": "Jeelesh Darji 2",
//                                 "body": true
//                             },
//                             {
//                                 "createdAt": "2022-01-01T00:00:00.000Z",
//                                 "body": true
//                             },
//                             {
//                                 "modifiedAt": "2022-01-01T00:00:00.000Z",
//                                 "body": true
//                             },
//                             {
//                                 "isActive": 1,
//                                 "body": true
//                             }
//                         ]
//                     }
//                 ],
//                 "toolType": "DBOperation",
//                 "queryPath": "/v1/api/Projects",
//                 "reqType": "post"
//             }
//         }
//     ]
// }

// ======================= All 7 Steps =============================
// export default {
//     "properties": {
//         "speed": 300
//     },
//     "sequence": [
//         {
//             "id": "b478216f6a9a3335c080dfd4b246ec4c",
//             "componentType": "task",
//             "type": "create",
//             "name": "Tenants_Post",
//             "properties": {
//                 "op": [
//                     {
//                         "name": "model",
//                         "in": "body",
//                         "required": true,
//                         "schema": {
//                             "$ref": "#/definitions/TenantsModel"
//                         },
//                         "x-nullable": false,
//                         "value": [
//                             {
//                                 "tenant_id": 1,
//                                 "body": true
//                             },
//                             {
//                                 "tenant_name": "Jeelesh",
//                                 "body": true
//                             },
//                             {
//                                 "tenant_contact_info": "Jeelesh is demo tenant.",
//                                 "body": true
//                             }
//                         ]
//                     }
//                 ],
//                 "toolType": "DBOperation",
//                 "queryPath": "/v1/api/Tenants",
//                 "reqType": "post"
//             }
//         },
//         {
//             "id": "7f9e9f37d58750ea772dfe7fb3bb5a9d",
//             "componentType": "task",
//             "type": "create",
//             "name": "Environments_Post",
//             "properties": {
//                 "op": [
//                     {
//                         "name": "model",
//                         "in": "body",
//                         "required": true,
//                         "schema": {
//                             "$ref": "#/definitions/EnvironmentsModel"
//                         },
//                         "x-nullable": false,
//                         "value": [
//                             {
//                                 "environment_id": 1,
//                                 "body": true
//                             },
//                             {
//                                 "environment_name": "Jeelesh_env_1",
//                                 "body": true
//                             }
//                         ]
//                     }
//                 ],
//                 "toolType": "DBOperation",
//                 "queryPath": "/v1/api/Environments",
//                 "reqType": "post"
//             }
//         },
//         {
//             "id": "acdb6731146d6438fb3186490cc8b77c",
//             "componentType": "task",
//             "type": "create",
//             "name": "Projects_Post",
//             "properties": {
//                 "op": [
//                     {
//                         "name": "model",
//                         "in": "body",
//                         "required": true,
//                         "schema": {
//                             "$ref": "#/definitions/ProjectsModel"
//                         },
//                         "x-nullable": false,
//                         "value": [
//                             {
//                                 "project_id": 1,
//                                 "body": true
//                             },
//                             {
//                                 "project_name": "frontendGenerator",
//                                 "body": true
//                             },
//                             {
//                                 "tenant_id": 1,
//                                 "body": true
//                             },
//                             {
//                                 "environment_id": 1,
//                                 "body": true
//                             },
//                             {
//                                 "createdBy": "Jeelesh Darji 1",
//                                 "body": true
//                             },
//                             {
//                                 "modifiedBy": "Jeelesh Darji 2",
//                                 "body": true
//                             },
//                             {
//                                 "createdAt": "2022-01-01T00:00:00.000Z",
//                                 "body": true
//                             },
//                             {
//                                 "modifiedAt": "2022-01-01T00:00:00.000Z",
//                                 "body": true
//                             },
//                             {
//                                 "isActive": 1,
//                                 "body": true
//                             }
//                         ]
//                     }
//                 ],
//                 "toolType": "DBOperation",
//                 "queryPath": "/v1/api/Projects",
//                 "reqType": "post"
//             }
//         },
//         {
//             "id": "5a34af3bfbe45e6ef31bc5af3beb7528",
//             "componentType": "task",
//             "type": "read",
//             "name": "Frontend_Stacks_GetById",
//             "properties": {
//                 "op": [
//                     {
//                         "type": "integer",
//                         "name": "frontend_stack_id",
//                         "in": "path",
//                         "required": true,
//                         "format": "int32",
//                         "x-nullable": false,
//                         "value": [
//                             {
//                                 "frontend_stack_id": "1",
//                                 "path": true
//                             }
//                         ]
//                     }
//                 ],
//                 "toolType": "DBOperation",
//                 "queryPath": "/v1/api/Frontend_Stacks/{frontend_stack_id}",
//                 "reqType": "get"
//             }
//         },
//         {
//             "id": "bcb252f8e5498c8cd9695cec5cd69f62",
//             "componentType": "task",
//             "type": "read",
//             "name": "Code_Generation_Tools_GetById",
//             "properties": {
//                 "op": [
//                     {
//                         "type": "integer",
//                         "name": "code_generation_tool_id",
//                         "in": "path",
//                         "required": true,
//                         "format": "int32",
//                         "x-nullable": false,
//                         "value": [
//                             {
//                                 "code_generation_tool_id": "1",
//                                 "path": true
//                             }
//                         ]
//                     }
//                 ],
//                 "toolType": "DBOperation",
//                 "queryPath": "/v1/api/Code_Generation_Tools/{code_generation_tool_id}",
//                 "reqType": "get"
//             }
//         },
//         {
//             "id": "fd9bcb9b19f2209878228f1377941733",
//             "componentType": "task",
//             "type": "create",
//             "name": "Project_Frontends_Post",
//             "properties": {
//                 "op": [
//                     {
//                         "name": "model",
//                         "in": "body",
//                         "required": true,
//                         "schema": {
//                             "$ref": "#/definitions/Project_FrontendsModel"
//                         },
//                         "x-nullable": false,
//                         "value": [
//                             {
//                                 "project_frontend_id": 1,
//                                 "body": true
//                             },
//                             {
//                                 "project_id": 1,
//                                 "body": true
//                             },
//                             {
//                                 "project_frontend_name": "ProjectFrontend-1",
//                                 "body": true
//                             },
//                             {
//                                 "project_frontend_status": "this is newly created project.",
//                                 "body": true
//                             },
//                             {
//                                 "frontend_stack_id": 1,
//                                 "body": true
//                             },
//                             {
//                                 "project_frontend_deployment": "ProjectFrontend-1-Deployment",
//                                 "body": true
//                             },
//                             {
//                                 "project_frontend_deployment_status": "Project is not deployed yet",
//                                 "body": true
//                             },
//                             {
//                                 "code_generation_tool": "ProjectFrontend-1-codeGenerator",
//                                 "body": true
//                             },
//                             {
//                                 "code_generation_status": "Project is not Generated yet",
//                                 "body": true
//                             },
//                             {
//                                 "code_generation_errors": "None",
//                                 "body": true
//                             },
//                             {
//                                 "code_generation_tool_id": 1,
//                                 "body": true
//                             },
//                             {
//                                 "createdBy": "Jeelesh Darji 1",
//                                 "body": true
//                             },
//                             {
//                                 "modifiedBy": "Jeelesh Darji 2",
//                                 "body": true
//                             },
//                             {
//                                 "createdAt": "2022-01-01T00:00:00.000Z",
//                                 "body": true
//                             },
//                             {
//                                 "modifiedAt": "2022-01-01T00:00:00.000Z",
//                                 "body": true
//                             },
//                             {
//                                 "isActive": 1,
//                                 "body": true
//                             }
//                         ]
//                     }
//                 ],
//                 "toolType": "DBOperation",
//                 "queryPath": "/v1/api/Project_Frontends",
//                 "reqType": "post"
//             }
//         },
//         {
//             "id": "f00ba01c013bb12e77e247120f00f0eb",
//             "componentType": "task",
//             "type": "create",
//             "name": "Project_Frontend_Builds_Post",
//             "properties": {
//                 "op": [
//                     {
//                         "name": "model",
//                         "in": "body",
//                         "required": true,
//                         "schema": {
//                             "$ref": "#/definitions/Project_Frontend_BuildsModel"
//                         },
//                         "x-nullable": false,
//                         "value": [
//                             {
//                                 "project_frontend_build_id": 1,
//                                 "body": true
//                             },
//                             {
//                                 "project_frontend_id": 1,
//                                 "body": true
//                             },
//                             {
//                                 "project_frontend_build_status": "Building in Process via Project FrontEnd Build.",
//                                 "body": true
//                             },
//                             {
//                                 "project_frontend_build_start_time": "2022-01-01T00:00:00.000Z",
//                                 "body": true
//                             },
//                             {
//                                 "project_frontend_build_end_time": "2022-01-01T00:00:00.000Z",
//                                 "body": true
//                             }
//                         ]
//                     }
//                 ],
//                 "toolType": "DBOperation",
//                 "queryPath": "/v1/api/Project_Frontend_Builds",
//                 "reqType": "post"
//             }
//         },
//         {
//             "id": "f9e84a2ed939e0147815f5950beeeeb8",
//             "componentType": "task",
//             "type": "create",
//             "name": "Project_Frontend_Deployments_Post",
//             "properties": {
//                 "op": [
//                     {
//                         "name": "model",
//                         "in": "body",
//                         "required": true,
//                         "schema": {
//                             "$ref": "#/definitions/Project_Frontend_DeploymentsModel"
//                         },
//                         "x-nullable": false,
//                         "value": [
//                             {
//                                 "project_frontend_deployment_id": 1,
//                                 "body": true
//                             },
//                             {
//                                 "project_frontend_build_id": 1,
//                                 "body": true
//                             },
//                             {
//                                 "project_frontend_deployment_status": "Deployment in Process via Project FrontEnd Build.",
//                                 "body": true
//                             },
//                             {
//                                 "project_frontend_deployment_start_time": "2022-01-01T00:00:00.000Z",
//                                 "body": true
//                             },
//                             {
//                                 "project_frontend_deployment_end_time": "2022-01-01T00:00:00.000Z",
//                                 "body": true
//                             }
//                         ]
//                     }
//                 ],
//                 "toolType": "DBOperation",
//                 "queryPath": "/v1/api/Project_Frontend_Deployments",
//                 "reqType": "post"
//             }
//         }
//     ]
// }
