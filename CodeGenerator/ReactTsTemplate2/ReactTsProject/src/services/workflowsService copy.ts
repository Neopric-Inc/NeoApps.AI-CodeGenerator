import { APIService } from "services";
// var Converter = require('api-spec-converter');

export const getWorkflows = async (pageNo, pageSize, search) => {
    let res;
    if (search.length === 0) {
        res = await getAllWorkflows(pageNo, pageSize);
    }
    else {
        try {
            res = await searchWorkflows(search, pageNo, pageSize);
        } catch (err) {
            return { records: [], totalCount: 0 }
        }
    }
    if (res && res.data && res.data.document) {
        return res.data.document;
    } else {
        return { records: [], totalCount: 0 }
    }

}


export const getAllWorkflows = (page, itemsPerPage) => {
    return APIService.api().get(`/workflows?page=${page}&itemsPerPage=${itemsPerPage}`)
}
export const getOneWorkflows = (id) => {
    return APIService.api().get(`/workflows/${id}`)
}
export const searchWorkflows = (key, page, itemsPerPage) => {
    return APIService.api().get(`/workflows/search?searchKey=${key}&page=${page}&itemsPerPage=${itemsPerPage}`)
}
export const addWorkflows = (data) => {
    return APIService.api().post(`/workflows/`, data)
}
export const updateWorkflows = (id, data) => {
    return APIService.api().put(`/workflows/${id}`, data)
}
export const deleteWorkflows = (key) => {
    return APIService.api().delete(`/workflows/${key}`)
}

export const getapi = (api_url) => {
    var allPaths = []
    return new Promise(async (resolve, reject) => {
        var pathList = [];
        // fetching data & allowing cors request
        const response = await fetch(api_url, {
            mode: 'cors',
            headers: {
                'Content-Type': 'application/json',
                'access-control-allow-origin': '*'
            }
        });
        var data = await response.json();

        // Converter.convert({
        //     from: 'openapi_3',
        //     to: 'swagger_2',
        //     source: api_url,
        // }).then(function (converted) {
        //     console.log("converted.stringify() : ", converted.stringify());
        // });
        for (let path = 0; path < Object.keys(data["paths"]).length; path++) {
            let pathUrl = Object.keys(data["paths"])[path];
            let pathStruct;
            for (let key in data["paths"][Object.keys(data["paths"])[path]]) {
                // pathObj has a all the details for specific API endPoint like parameters , request type , etc
                let pathObj = data["paths"][Object.keys(data["paths"])[path]][key];
                // we have to generate name for each API request
                // 1.if at the end Multiple and Filter , 
                // 2.if at the end /search then Backend_stacks_Search , 
                // 3. if '/v1/api/Backend_stacks/{backend_stack_id}' in that if put , delete then simply put , delete if get then getbyid
                // 4. if 'v1/api/backend_stacks' get , post then get and post only

                // pathStruct has a format in which we are storing our swagger.json result
                let operationId: string;
                const regex = /{([^}]+)}$/;
                if (pathObj.parameters)
                    pathObj.parameters = pathObj.parameters.map((param) => ({
                        type: param.schema.type,
                        name: param.name,
                        in: param.in,
                        format: param.schema.format,
                        default: param.schema.default,
                        "x-nullable": false,
                    }))

                if (key === "get") {
                    if (pathUrl.endsWith('/search')) {
                        pathStruct = {
                            "tag": (pathObj.tags)[0],
                            "reqType": key,
                            "operationId": `${(pathObj.tags)[0]}_Search`,
                            "parameters": pathObj.parameters,
                            "toolType": "DBOperation",
                            "queryPath": pathUrl,
                        };
                    }
                    else if (regex.exec(pathUrl)) {
                        pathStruct = {
                            "tag": (pathObj.tags)[0],
                            "reqType": key,
                            "operationId": `${(pathObj.tags)[0]}_GetById`,
                            "parameters": pathObj.parameters,
                            "toolType": "DBOperation",
                            "queryPath": pathUrl,
                        };
                    }
                    else {
                        pathStruct = {
                            "tag": (pathObj.tags)[0],
                            "reqType": key,
                            "operationId": `${(pathObj.tags)[0]}_${key.charAt(0).toUpperCase()}${key.slice(1)}`,
                            "parameters": pathObj.parameters,
                            "toolType": "DBOperation",
                            "queryPath": pathUrl,
                        };
                    }
                }
                else if (key === 'post') {

                    // we have to add requestBody Model into parameter inself
                    // we have to add Filter Condition also
                    if (pathUrl.endsWith('/filter')) {
                        let parameter;
                        if (pathObj.requestBody) {
                            parameter = {
                                name: "model",
                                in: "body",
                                required: true,
                                schema: pathObj['requestBody']['content']['application/json']['schema']['items'],
                                "x-nullable": false,
                            }
                        }
                        pathObj.parameters = [];
                        pathObj.parameters.push(parameter);
                        pathStruct = {
                            "tag": (pathObj.tags)[0],
                            "reqType": key,
                            "operationId": `${(pathObj.tags)[0]}_Filter`,
                            "parameters": pathObj.parameters,
                            "toolType": "DBOperation",
                            "queryPath": pathUrl,
                        };
                    }
                    else {
                        let parameter;
                        if (pathObj.requestBody) {
                            parameter = {
                                name: "model",
                                in: "body",
                                required: true,
                                schema: pathObj['requestBody']['content']['application/json']['schema'],
                                "x-nullable": false,
                            }
                        }
                        pathObj.parameters = [];
                        pathObj.parameters.push(parameter);
                        pathStruct = {
                            "tag": (pathObj.tags)[0],
                            "reqType": key,
                            "operationId": `${(pathObj.tags)[0]}_${key.charAt(0).toUpperCase()}${key.slice(1)}`,
                            "parameters": pathObj.parameters,
                            "toolType": "DBOperation",
                            "queryPath": pathUrl,
                        };
                    }
                }
                else if (key === "put") {

                    // we have to add requestBody Model into parameter inself
                    if (regex.exec(pathUrl)) {
                        let parameter;
                        if (pathObj.requestBody) {
                            parameter = {
                                name: "model",
                                in: "body",
                                required: true,
                                schema: pathObj['requestBody']['content']['application/json']['schema'],
                                "x-nullable": false,
                            }
                        }
                        pathObj.parameters = [];
                        pathObj.parameters.push(parameter);
                        pathStruct = {
                            "tag": (pathObj.tags)[0],
                            "reqType": key,
                            "operationId": `${(pathObj.tags)[0]}_${key.charAt(0).toUpperCase()}${key.slice(1)}`,
                            "parameters": pathObj.parameters,
                            "toolType": "DBOperation",
                            "queryPath": pathUrl,
                        };
                    }
                    pathStruct = {
                        "tag": (pathObj.tags)[0],
                        "reqType": key,
                        "operationId": `${(pathObj.tags)[0]}_${key.charAt(0).toUpperCase()}${key.slice(1)}`,
                        "parameters": pathObj.parameters,
                        "toolType": "DBOperation",
                        "queryPath": pathUrl,
                    };
                }
                // for key === delete
                else {
                    // if delete multiple then we have to add deleteMutliple model
                    if (pathUrl.endsWith('/Multiple')) {
                        let parameter;
                        if (pathObj.requestBody) {
                            parameter = {
                                name: "model",
                                in: "body",
                                required: true,
                                schema: pathObj['requestBody']['content']['application/json']['schema']['items'],
                                "x-nullable": false,
                            }
                        }
                        pathObj.parameters = [];
                        pathObj.parameters.push(parameter);
                        pathStruct = {
                            "tag": (pathObj.tags)[0],
                            "reqType": key,
                            "operationId": `${(pathObj.tags)[0]}_${key.charAt(0).toUpperCase()}${key.slice(1)}Multiple`,
                            "parameters": pathObj.parameters,
                            "toolType": "DBOperation",
                            "queryPath": pathUrl,
                        };
                    }
                    // else if (regex.exec(pathUrl)) {}
                    else {
                        pathStruct = {
                            "tag": (pathObj.tags)[0],
                            "reqType": key,
                            "operationId": `${(pathObj.tags)[0]}_${key.charAt(0).toUpperCase()}${key.slice(1)}`,
                            "parameters": pathObj.parameters,
                            "toolType": "DBOperation",
                            "queryPath": pathUrl,
                        };
                    }
                }
                allPaths.push(pathStruct)
            }
        }
        var pathContoller = new Set();
        allPaths.forEach(element => {
            pathContoller.add(element.tag)
        });
        pathContoller.forEach((pathctrl: any) => {
            let temp = [];
            // add matching path & push into our result
            allPaths.forEach(element => {
                if (element.tag == pathctrl)
                    temp.push(element);
            });
            let res = {};
            res[pathctrl] = temp;
            pathList.push(res);
        });
        // resolving promise & returning the final output
        resolve(pathList);
    })
};

// do same work as a getApi function but store model schema details in specific format
export const getDef = (api_url) => {
    return new Promise(async (resolve, reject) => {
        const response = await fetch(api_url, {
            mode: 'cors',
            headers: {
                'Content-Type': 'application/json',
                'access-control-allow-origin': '*'
            }
        });
        var data = await response.json();
        var defList = [];

        for (let defIndex = 0; defIndex < Object.keys(data["components"]["schemas"]).length; defIndex++) {
            let defUrl = Object.keys(data["components"]["schemas"])[defIndex];
            let defStruct = {};
            let defObj = data["components"]["schemas"][Object.keys(data["components"]["schemas"])[defIndex]];
            let required = defObj["required"];
            let properties = defObj["properties"];
            defStruct["required"] = required;
            defStruct["properties"] = properties;
            let resultantDefObj = {};
            resultantDefObj[defUrl] = defStruct;
            defList.push(resultantDefObj);
        }
        resolve(defList);
    })
}