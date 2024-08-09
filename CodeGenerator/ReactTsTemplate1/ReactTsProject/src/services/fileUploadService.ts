import { APIService } from "services";

export const uploadFileService = async (data) => {
  const config = {
    headers: {
      "Content-Type": "multipart/form-data",
    },
  };
  try {
   //console.log(data);
    const response = await APIService.api().post(`/FileUpload/`, data, config);
   //console.log(response);
    return response;
  } catch (error) {
    console.error("API call failed:", error);
    throw error; // Rethrow the error or handle it accordingly
  }
};

export const uploadImageService = (data) => {
  return APIService.api().post(`/files/uploadimage.php`, data);
};
export const GetDataUrl = async (data) => {
  return await APIService.api().post(`/FileUpload/geturl`, data);
};
