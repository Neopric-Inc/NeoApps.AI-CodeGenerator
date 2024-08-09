import axios from "axios"
export class APIService {

    static apiCustomHeader(headers: any) {
        return axios.create({
            baseURL: process.env.REACT_APP_API_BASE_URL,
            headers: headers,
        })
    }
    static api() {
        const jwtToken = localStorage.getItem("token");
        return axios.create({
            baseURL: process.env.REACT_APP_API_BASE_URL,
            headers: {
                'Access-Control-Allow-Origin': '*',
                'Content-Type': 'application/json',
                'Authorization': 'Bearer ' + jwtToken
            }
        })
    }
    static apiNoAuth() {
        return axios.create({
            baseURL: process.env.REACT_APP_API_BASE_URL,
            headers: {
                'Access-Control-Allow-Origin': '*',
                'Content-Type': 'application/json',
            }
        })
    }
}