import React, { useEffect, useState } from "react";
import { useFormik } from "formik";
import { useNavigate } from "react-router";
import { useSelector } from "react-redux";
import { RootState } from "redux/reducers";
import { useAppDispatch } from "redux/store";
import { setJWTToken } from "redux/actions";
import { setError } from "redux/slices/auth";
import { tokenAPICALL } from "services/authService";
import * as yup from "yup";
import {
    Box,
    Container,
    Card,
    Typography,
    TextField,
    Button,
    Alert,
    Divider,
    Checkbox,
    CircularProgress,
} from "@mui/material";

export interface ITokenResp {
    jwtToken: any,
    expiryDate: any,
    user: any,
}

const Login: React.FC = () => {
    const navigate = useNavigate();
    const dispatch = useAppDispatch();
    const [load, setLoad] = useState(false);
    const rData = useSelector((state: RootState) => state.authToken);
    const formik = useFormik({
        initialValues: {
            username: "",
            password: "",
        },
        onSubmit: async (values) => {
            setLoad(true);
            const response: ITokenResp = await tokenAPICALL(values.username, values.password);

            if (response) {
                const user = JSON.stringify(response.user);
                localStorage.setItem("userInfo", user);
                dispatch(setJWTToken(response));
                navigate("/", { replace: true });
            } else {
                dispatch(setError("Invalid Credentials"));
            }
            setLoad(false);
        },
        validationSchema: yup.object({
            username: yup.string().trim().required("Username is required"),
            password: yup.string().trim().required("Password is required"),
        }),
    });

    useEffect(() => {
        if (rData.errorMessage) {
            const timeoutId = setTimeout(() => {
                dispatch(setError(""));
            }, 5000);
            return () => clearTimeout(timeoutId);
        }
    }, [rData.errorMessage, dispatch]);

    return (
        <Box>
            <Box>
                <Typography variant="h5">Login | Welcome back!</Typography>
            </Box>
            <Box
                component="main"
                sx={{
                    backgroundColor: "background.default",
                    display: "flex",
                    flexDirection: "column",
                    alignItems: "center",
                    justifyContent: "center",
                    minHeight: "100vh",
                }}
            >
                <Box className="col-lg-6 d-none d-lg-block bg-login-image"></Box>
                <Container
                    maxWidth="sm"
                    sx={{
                        py: {
                            xs: 2,
                            md: 4,
                        },
                    }}
                >
                    <Card elevation={16} sx={{ p: 4 }}>
                        <Typography variant="h6" sx={{ mb: 2 }}>
                            Welcome Back!
                        </Typography>
                        <form onSubmit={formik.handleSubmit}>
                            <TextField
                                name="username"
                                value={formik.values.username}
                                onChange={formik.handleChange}
                                onBlur={formik.handleBlur}
                                error={!!formik.touched.username && !!formik.errors.username}
                                helperText={formik.touched.username && formik.errors.username}
                                label="Username"
                                variant="outlined"
                                fullWidth
                                sx={{ mb: 2 }}
                            />
                            <TextField
                                name="password"
                                type="password"
                                value={formik.values.password}
                                onChange={formik.handleChange}
                                onBlur={formik.handleBlur}
                                error={!!formik.touched.password && !!formik.errors.password}
                                helperText={formik.touched.password && formik.errors.password}
                                label="Password"
                                variant="outlined"
                                fullWidth
                                sx={{ mb: 2 }}
                            />
                            {!load ? <Button
                                variant="contained"
                                color="primary"
                                type="submit"
                                fullWidth
                            >
                                Login
                            </Button> :
                                <CircularProgress size={32} />
                            }
                        </form>
                        {rData.errorMessage && (
                            <Alert
                                severity="error"
                                onClose={() => dispatch(setError(""))}
                                sx={{ mt: 2 }}
                            >
                                {rData.errorMessage}
                            </Alert>
                        )}
                        <Box sx={{ display: "flex", alignItems: "center", mt: 2 }}>
                            <Checkbox />
                            <Typography variant="body2">Remember Me</Typography>
                        </Box>
                        <Divider sx={{ my: 3 }} />
                    </Card>
                </Container>
                <Box sx={{ p: 2, textAlign: "center" }}>
                    <Typography variant="body2" color="textSecondary">
                        &copy; Neopric Inc.(NeoApps.AI) {new Date().getFullYear().toString()}
                    </Typography>
                </Box>
            </Box>
        </Box>
    );
};

export default Login;
