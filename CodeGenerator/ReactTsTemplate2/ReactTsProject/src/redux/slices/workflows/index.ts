import { createSlice, PayloadAction } from "@reduxjs/toolkit";

export interface IWorkflows {
createdAt:Date,
createdBy:string,
isActive:number,
modifiedAt:Date,
modifiedBy:string,
steps:string,
triggerpoint:string,
workflow_description:string,
workflow_id:number,
workflow_name:string,
}

interface IWorkflowsData {
    list?: Array<IWorkflows>;
    pageNo: number;
    pageSize: number;
    searchKey?: string;
    totalCount?: number;
    message?: string;
}

const initialState: IWorkflowsData = {
    pageNo: 1,
    pageSize: 20,
    searchKey: '',
    list: [],
    totalCount: 0,
    message: '',
};

const workflowsSlice = createSlice({
    name: "workflows",
    initialState,
    reducers: {
        setWorkflowsList: (state, _action: PayloadAction<IWorkflowsData>) => {
            state.list = _action.payload.list;
            state.pageNo = _action.payload.pageNo;
            state.pageSize = _action.payload.pageSize;
            state.totalCount = _action.payload.totalCount;
        },
        resetWorkflowsToInit: (state) => {
            state = initialState;
        },
        setWorkflowsMessage: (state, _action: PayloadAction<string>) => {
            state.message = _action.payload;
        },
    },
});

export const { setWorkflowsList, resetWorkflowsToInit, setWorkflowsMessage } = workflowsSlice.actions;

export default workflowsSlice.reducer;
