import { combineReducers } from "redux";

import template from "redux/slices/template";
import authToken from "redux/slices/auth";
const rootReducer = combineReducers({ template, authToken });

export type RootState = ReturnType<typeof rootReducer>;

export default rootReducer;
