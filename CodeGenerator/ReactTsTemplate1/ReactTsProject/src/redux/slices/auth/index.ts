import { createSlice, PayloadAction } from "@reduxjs/toolkit";

interface IAuthToken {
    isAuthenticated?:boolean,
    jwtToken: string;
    expiryDate: any;
    errorMessage?:string;
}

const initialState: IAuthToken = {
    jwtToken: '',
    expiryDate: undefined,
    errorMessage:'',
    isAuthenticated:false
};

const authSlice = createSlice({
    name: "auth",
    initialState,
    reducers: {
        setJWTToken: (state, _action: PayloadAction<IAuthToken>) => {
            state.jwtToken = _action.payload.jwtToken;
            state.expiryDate = _action.payload.expiryDate;
            localStorage.setItem('token', state.jwtToken);
            localStorage.setItem('expire', state.expiryDate);
            state.isAuthenticated = true;
           
        },
        removeJWTToken: (state) => {
            localStorage.clear();
            state.jwtToken = '';
            state.expiryDate=undefined;
            state.isAuthenticated = false;
        },
        setError: (state, _action: PayloadAction<string>) => {
            state.errorMessage = _action.payload;
        },
    },
});

export const { setJWTToken, removeJWTToken,setError } = authSlice.actions;

export default authSlice.reducer;
