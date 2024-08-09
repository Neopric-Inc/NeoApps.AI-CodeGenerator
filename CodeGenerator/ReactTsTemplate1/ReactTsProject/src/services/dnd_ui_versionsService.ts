import { APIService } from "services";

export const getDnd_ui_versions = async (pageNo,pageSize,search) => {
    let res;
    if(search.length===0) {
        res = await getAllDnd_ui_versions(pageNo,pageSize);
    }
    else{
        try {
            res = await searchDnd_ui_versions(search,pageNo,pageSize);
        } catch(err) {
             return { records:[], totalCount:0 }
        }
    }
    if(res && res.data && res.data.document){
    return res.data.document;
    }else{
    return { records:[], totalCount:0 }
    }
    
}

export const getAllDnd_ui_versions = (pageno,pagesize) => {
return APIService.api().get(`/dnd_ui_versions?pageno=${pageno}&pagesize=${pagesize}`)
}
export const getOneDnd_ui_versions = (id) => {
return APIService.api().get(`/dnd_ui_versions/${id}`)
}
export const searchDnd_ui_versions = (key,pageno,pagesize) => {
return APIService.api().get(`/dnd_ui_versions/search?searchKey=${key}&pageno=${pageno}&pagesize=${pagesize}`)
}
export const addDnd_ui_versions = (data) => {
return APIService.api().post(`/dnd_ui_versions/`,data)
}
export const updateDnd_ui_versions = (id1,data) => {
return APIService.api().put(`/dnd_ui_versions/${id1}/`,data)
}
export const deleteDnd_ui_versions = (dnd_ui_version_id) => {
return APIService.api().delete(`/dnd_ui_versions/${dnd_ui_version_id}/`)
}
