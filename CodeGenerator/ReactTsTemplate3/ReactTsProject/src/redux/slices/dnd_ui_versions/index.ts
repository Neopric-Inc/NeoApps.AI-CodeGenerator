import { createSlice, PayloadAction } from "@reduxjs/toolkit";

export interface IDnd_ui_versions {
components:string,
createdAt:Date,
createdBy:string,
dnd_ui_type:string,
dnd_ui_version_id:number,
isActive:number,
layout:string,
modifiedAt:Date,
modifiedBy:string,
ui_pages:string,
}

interface IDnd_ui_versionsData {
    list?: Array<IDnd_ui_versions>;
    pageNo: number;
    pageSize: number;
    searchKey?: string;
    totalCount?: number;
    message?: string;
}

const initialState: IDnd_ui_versionsData = {
    pageNo: 1,
    pageSize: 20,
    searchKey: '',
    list: [],
    totalCount: 0,
    message: '',
};

const dnd_ui_versionsSlice = createSlice({
    name: "dnd_ui_versions",
    initialState,
    reducers: {
        setDnd_ui_versionsList: (state, _action: PayloadAction<IDnd_ui_versionsData>) => {
            state.list = _action.payload.list;
            state.pageNo = _action.payload.pageNo;
            state.pageSize = _action.payload.pageSize;
            state.totalCount = _action.payload.totalCount;
        },
        resetDnd_ui_versionsToInit: (state) => {
            state = initialState;
        },
        setDnd_ui_versionsMessage: (state, _action: PayloadAction<string>) => {
            state.message = _action.payload;
        },
    },
});

export const { setDnd_ui_versionsList, resetDnd_ui_versionsToInit, setDnd_ui_versionsMessage } = dnd_ui_versionsSlice.actions;

export default dnd_ui_versionsSlice.reducer;
