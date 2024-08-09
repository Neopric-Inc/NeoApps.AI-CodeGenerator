import axios from "axios"
export class FlowsAPIService {

    static apiCustomHeader(headers: any) {
        return axios.create({
            //baseURL: `https://split17.neoapps.ai/nodered`,
            baseURL: process.env.REACT_APP_NODERED_BASE_URL,
            //baseURL: `http:/localhost:1880`,
            headers: headers,
        })
    }
    static api() {
        const jwtToken = localStorage.getItem("token");
        return axios.create({
            //baseURL: `https://split17.neoapps.ai/nodered`,
            baseURL: process.env.REACT_APP_NODERED_BASE_URL,
            //baseURL: `http://localhost:1880`,
            headers: {
                'Access-Control-Allow-Origin': '*',
                'Content-Type': 'application/json',
                'Authorization': 'Bearer ' + jwtToken
            }
        })
    }
    static apiNoAuth() {
        return axios.create({
            //baseURL: `https://split17.neoapps.ai/nodered`,
            baseURL: process.env.REACT_APP_NODERED_BASE_URL,
            //baseURL: `http://localhost:1880`,
            headers: {
                'Access-Control-Allow-Origin': '*',
                'Content-Type': 'application/json',
            }
        })
    }
}