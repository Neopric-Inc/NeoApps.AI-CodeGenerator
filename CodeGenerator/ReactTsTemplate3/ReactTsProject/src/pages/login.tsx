// // import React, { useEffect } from "react";
// // import { useNavigate } from "react-router";
// // import { useFormik } from 'formik';
// // import * as yup from 'yup';
// // import { useAppDispatch } from "redux/store";
// // import { tokenAPICALL } from "services/authService";
// // import { setJWTToken } from "redux/actions";
// // import { setError } from "redux/slices/auth";
// // import { Alert, Button, Form } from "react-bootstrap";
// // import { useSelector } from "react-redux";
// // import { RootState } from "redux/reducers";
// // import { Constant } from "template/Constant";
// // const Login: React.FC = () => {
// //     const navigate = useNavigate();
// //     const dispatch = useAppDispatch();
// //     const rData = useSelector((state: RootState) => state.authToken);
// //     const formik = useFormik({
// //         initialValues: {
// //             username: '',
// //             password: ''
// //         },
// //         onSubmit: async (values) => {
// //             const response = await tokenAPICALL(values.username, values.password);
// //             if (response) {
// //                 dispatch(setJWTToken(response));
// //                 navigate('/welcome', { replace: true });
// //             } else {
// //                 dispatch(setError("Invalid Credentials"))
// //             }
// //         },
// //         validationSchema: yup.object({
// //             username: yup.string().trim().required('Username is required'),
// //             password: yup.string().trim().required('Password is required'),
// //         }),
// //     });

// //     useEffect(() => {
// //         document.body.classList.toggle('bg-gradient-primary', true);
// //     }, [])
// //     return (

// //         <div className="container">
// //             <div className="row justify-content-center">
// //                 <div className="col-xl-10 col-lg-12 col-md-9">
// //                     <div className="card o-hidden border-0 shadow-lg my-5">
// //                         <div className="card-body p-0">
// //                             <div className="row">
// //                                 <div className="col-lg-6 d-none d-lg-block bg-login-image"></div>
// //                                 <div className="col-lg-6">
// //                                     <div className="p-5">
// //                                         <div className="text-center">
// //                                             <h1 className="h4 text-gray-900 mb-4">Welcome Back!</h1>
// //                                         </div>
// //                                         <Form className="user" onSubmit={formik.handleSubmit}>
// //                                             <Form.Group>
// //                                                 <label className="form-control-label">Username</label>
// //                                                 <Form.Control type="text" name="username" className="form-control-user" value={formik.values.username}
// //                                                     onChange={formik.handleChange}
// //                                                     onBlur={formik.handleBlur}
// //                                                     isInvalid={!!formik.touched.username && !!formik.errors.username}
// //                                                     isValid={!!formik.touched.username && !formik.errors.username}
// //                                                 ></Form.Control>
// //                                                 {formik.errors.username && (
// //                                                     <Form.Control.Feedback type="invalid">
// //                                                         {formik.errors.username}
// //                                                     </Form.Control.Feedback>
// //                                                 )}
// //                                             </Form.Group>
// //                                             <Form.Group>
// //                                                 <label className="form-control-label">Password</label>
// //                                                 <Form.Control type="password" name="password" className="form-control-user" value={formik.values.password}
// //                                                     onChange={formik.handleChange}
// //                                                     onBlur={formik.handleBlur}
// //                                                     isInvalid={!!formik.touched.password && !!formik.errors.password}
// //                                                     isValid={!!formik.touched.password && !formik.errors.password}
// //                                                 ></Form.Control>
// //                                                 {formik.errors.password && (
// //                                                     <Form.Control.Feedback type="invalid">
// //                                                         {formik.errors.password}
// //                                                     </Form.Control.Feedback>
// //                                                 )}
// //                                             </Form.Group>
// //                                             <div className="form-group">
// //                                                 <div className="custom-control custom-checkbox small">
// //                                                     <input type="checkbox" className="custom-control-input" id="customCheck" />
// //                                                     <label className="custom-control-label">Remember
// //                                                         Me</label>
// //                                                 </div>
// //                                             </div>
// //                                             <Button type="submit" className="btn-user btn-block" variant="primary">Login</Button>

// //                                         </Form>
// //                                         <hr />
// //                                         {rData.errorMessage ?
// //                                             <Alert variant={Constant.defaultAlertVarient} className="alert-dismissible fade">{rData.errorMessage}
// //                                                 <Button type="button" className="close" data-dismiss="alert" aria-label="Close" onClick={() => dispatch(setError(''))}>
// //                                                     <span aria-hidden="true">&times;</span>
// //                                                 </Button>
// //                                             </Alert> : null}

// //                                         <div className="copyright text-center my-auto">
// //                                             <span>Copyright &copy; Onlinemeet 2022</span>
// //                                         </div>
// //                                     </div>
// //                                 </div>
// //                             </div>
// //                         </div>
// //                     </div>

// //                 </div>

// //             </div>

// //         </div>
// //     );
// // };

// // export default Login;

// import React, { useEffect } from "react";
// import { useNavigate } from "react-router";
// import { useFormik } from "formik";
// import * as yup from "yup";
// import { useAppDispatch } from "redux/store";
// import { tokenAPICALL } from "services/authService";
// import { setJWTToken } from "redux/actions";
// import { setError } from "redux/slices/auth";
// import { useSelector } from "react-redux";
// import { RootState } from "redux/reducers";
// import { Constant } from "template/Constant";
// import {
//   Box,
//   TextField,
//   Button,
//   Alert,
//   Typography,
//   Checkbox,
//   FormHelperText,
// } from "@mui/material";
// import CloseIcon from "@mui/icons-material/Close";
// import IconButton from "@mui/material/IconButton";

// const Login: React.FC = () => {
//   const navigate = useNavigate();
//   const dispatch = useAppDispatch();
//   const rData = useSelector((state: RootState) => state.authToken);
//   const formik = useFormik({
//     initialValues: {
//       username: "",
//       password: "",
//     },
//     onSubmit: async (values) => {
//       const response = await tokenAPICALL(values.username, values.password);
//       if (response) {
//         dispatch(setJWTToken(response));
//         navigate("/welcome", { replace: true });
//       } else {
//         dispatch(setError("Invalid Credentials"));
//       }
//     },
//     validationSchema: yup.object({
//       username: yup.string().trim().required("Username is required"),
//       password: yup.string().trim().required("Password is required"),
//     }),
//   });

//   useEffect(() => {
//     document.body.classList.toggle("bg-gradient-primary", true);
//   }, []);

//   return (
//     <Box className="container">
//       <Box className="row justify-content-center">
//         <Box className="col-xl-10 col-lg-12 col-md-9">
//           <Box className="card o-hidden border-0 shadow-lg my-5">
//             <Box className="card-body p-0">
//               <Box className="row">
//                 <Box className="col-lg-6 d-none d-lg-block bg-login-image"></Box>
//                 <Box className="col-lg-6">
//                   <Box className="p-5">
//                     <Box className="text-center">
//                       <Typography variant="h4">Welcome Back!</Typography>
//                     </Box>
//                     <Box component="form" onSubmit={formik.handleSubmit}>
//                       <Box>
//                         <TextField
//                           label="Username"
//                           type="text"
//                           name="username"
//                           value={formik.values.username}
//                           onChange={formik.handleChange}
//                           onBlur={formik.handleBlur}
//                           error={
//                             !!formik.touched.username &&
//                             !!formik.errors.username
//                           }
//                         />
//                         {formik.errors.username && (
//                           <FormHelperText error>
//                             {formik.errors.username}
//                           </FormHelperText>
//                         )}
//                       </Box>
//                       <Box>
//                         <TextField
//                           label="Password"
//                           type="password"
//                           name="password"
//                           value={formik.values.password}
//                           onChange={formik.handleChange}
//                           onBlur={formik.handleBlur}
//                           error={
//                             !!formik.touched.password &&
//                             !!formik.errors.password
//                           }
//                         />
//                         {formik.errors.password && (
//                           <FormHelperText error>
//                             {formik.errors.password}
//                           </FormHelperText>
//                         )}
//                       </Box>
//                       <Box>
//                         <Checkbox />
//                         <label>Remember Me</label>
//                       </Box>
//                       <Button type="submit" variant="contained" color="primary">
//                         Login
//                       </Button>
//                     </Box>
//                     <hr />
//                     {rData.errorMessage ? (
//                       <Alert severity="error">
//                         {rData.errorMessage}
//                         <IconButton
//                           size="small"
//                           onClick={() => dispatch(setError(""))}
//                         >
//                           <CloseIcon fontSize="inherit" />
//                         </IconButton>
//                       </Alert>
//                     ) : null}
//                     <Box className="copyright text-center my-auto">
//                       <Typography>Copyright &copy; Onlinemeet 2023</Typography>
//                     </Box>
//                   </Box>
//                 </Box>
//               </Box>
//             </Box>
//           </Box>
//         </Box>
//       </Box>
//     </Box>
//   );
// };

// export default Login;

import React, { useEffect } from "react";
import { useFormik } from "formik";
import { useNavigate } from "react-router";
import { useSelector } from "react-redux";
import { RootState } from "redux/reducers";
import { useAppDispatch } from "redux/store";
import { setJWTToken } from "redux/actions";
import { setError } from "redux/slices/auth";
import { tokenAPICALL } from "services/authService";
import CloseIcon from "@mui/icons-material/Close";
import IconButton from "@mui/material/IconButton";
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
} from "@mui/material";

const Login: React.FC = () => {
  const navigate = useNavigate();
  const dispatch = useAppDispatch();
  const rData = useSelector((state: RootState) => state.authToken);
  const formik = useFormik({
    initialValues: {
      username: "",
      password: "",
    },
    onSubmit: async (values) => {
      const response = await tokenAPICALL(values.username, values.password);
      if (response) {
        dispatch(setJWTToken(response));
        navigate("/dashboard", { replace: true });
      } else {
        dispatch(setError("Invalid Credentials"));
      }
    },
    validationSchema: yup.object({
      username: yup.string().trim().required("Username is required"),
      password: yup.string().trim().required("Password is required"),
    }),
  });

  return (
    <>
      <Box>
        <title>Login | Welcome back!</title>
      </Box>
      <Box
        component="main"
        sx={{
          backgroundColor: "background.default",
          display: "flex",
          flexDirection: "column",
          minHeight: "100vh",
        }}
      >
        <Box className="col-lg-6 d-none d-lg-block bg-login-image"></Box>
        <Container
          maxWidth="sm"
          sx={{
            py: {
              xs: "60px",
              md: "120px",
            },
          }}
        >
          <Card elevation={16} sx={{ p: 4 }}>
            <Box
              sx={{
                alignItems: "center",
                display: "flex",
                flexDirection: "column",
                justifyContent: "center",
              }}
            >
              <Typography variant="h4">Welcome Back!</Typography>
            </Box>
            <Box
              sx={{
                flexGrow: 1,
                mt: 3,
              }}
              component="form"
              noValidate
              autoComplete="off"
              onSubmit={formik.handleSubmit}
            >
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

              <Button variant="contained" color="primary" type="submit">
                Login
              </Button>
              {rData.errorMessage ? (
                <Alert severity="error">
                  {rData.errorMessage}
                  <IconButton
                    size="small"
                    onClick={() => dispatch(setError(""))}
                  >
                    <CloseIcon fontSize="inherit" />
                  </IconButton>
                </Alert>
              ) : null}
            </Box>
            <Box
              sx={{
                flexGrow: 0.5,
                mt: 2,
              }}
            >
              <Checkbox />
              <label>Remember Me</label>
            </Box>

            <Divider sx={{ my: 3 }} />
          </Card>
        </Container>
        <Box sx={{ p: 2, textAlign: "center" }}>
          <Typography variant="body2" color="blue">
            Copyright &copy; {projectName} {new Date().getFullYear().toString()}
          </Typography>
        </Box>
      </Box>
    </>
  );
};

export default Login;
