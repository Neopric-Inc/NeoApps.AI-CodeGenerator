import { APIService } from "services"

export const uploadImageService = (data) => {
    return APIService.api().post(`/files/uploadimage.php`, data);
}
export const uploadFileService = (data) => {
    return APIService.api().post(`/files/uploadfile.php`, data);
}