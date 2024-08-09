import { APIService } from "services";

export const getDnd_Builders = async (pageNo, pageSize, search) => {
    let res;
    if (search.length === 0) {
        res = await getAllDnd_Builders(pageNo, pageSize);
    }
    else {
        try {
            res = await searchDnd_Builders(search, pageNo, pageSize);
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

export const getAllDnd_Builders = (page, itemsPerPage) => {
    return APIService.api().get(`/Pages?page=${page}&itemsPerPage=${itemsPerPage}`)
}
export const getOneDnd_Builder = (id) => {
    return APIService.api().get(`/Pages/${id}`)
}
export const searchDnd_Builders = (key, page, itemsPerPage) => {
    return APIService.api().get(`/Pages/search?searchKey=${key}&page=${page}&itemsPerPage=${itemsPerPage}`)
}
export const addDnd_Builders = (data) => {
    return APIService.api().post(`/Pages`, data)
}
export const updateDnd_Builders = (id, data) => {
    return APIService.api().put(`/Pages/${id}`, data)
}
export const deleteDnd_Builders = (key) => {
    return APIService.api().delete(`/Pages/${key}`)
}
