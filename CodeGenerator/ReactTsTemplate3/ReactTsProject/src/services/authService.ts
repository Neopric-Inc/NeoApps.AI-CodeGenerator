
import { APIService } from "services";
import { ITokenResp } from "pages/login";

export const tokenAPICALL = async (user, pass) => {
    const response = await APIService.apiNoAuth().post(`/token/generate.php`, {
        "username": user,
        "password": pass
    }, {
        headers: {
            'Content-Type': 'application/json; charset=UTF-8'
        }
    })

    if (response && response.status === 200) {
        const data = response.data;
        const now = new Date().getTime();
        const expiryDate = now + data.document.validTo;
        const userData = data.document.user;
        const resp: ITokenResp = {
            jwtToken: data.document.accessToken,
            expiryDate: expiryDate,
            user: userData,
        };
        return resp;
    } else {
        return null;
    }
}