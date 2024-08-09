// import React, { useEffect } from "react";
// const Register: React.FC = () => {
//   useEffect(() => {
//     document.body.classList.toggle("bg-gradient-primary", true);
//   }, []);
//   return (
//     <div className="container">
//       <div className="card o-hidden border-0 shadow-lg my-5">
//         <div className="card-body p-0">
//           <div className="row">
//             <div className="col-lg-5 d-none d-lg-block bg-register-image"></div>
//             <div className="col-lg-7">
//               <div className="p-5">
//                 <div className="text-center">
//                   <h1 className="h4 text-gray-900 mb-4">Create an Account!</h1>
//                 </div>
//                 <form className="user">
//                   <div className="form-group row">
//                     <div className="col-sm-6 mb-3 mb-sm-0">
//                       <input
//                         type="text"
//                         className="form-control form-control-user"
//                         id="exampleFirstName"
//                         placeholder="First Name"
//                       />
//                     </div>
//                     <div className="col-sm-6">
//                       <input
//                         type="text"
//                         className="form-control form-control-user"
//                         id="exampleLastName"
//                         placeholder="Last Name"
//                       />
//                     </div>
//                   </div>
//                   <div className="form-group">
//                     <input
//                       type="email"
//                       className="form-control form-control-user"
//                       id="exampleInputEmail"
//                       placeholder="Email Address"
//                     />
//                   </div>
//                   <div className="form-group row">
//                     <div className="col-sm-6 mb-3 mb-sm-0">
//                       <input
//                         type="password"
//                         className="form-control form-control-user"
//                         id="exampleInputPassword"
//                         placeholder="Password"
//                       />
//                     </div>
//                     <div className="col-sm-6">
//                       <input
//                         type="password"
//                         className="form-control form-control-user"
//                         id="exampleRepeatPassword"
//                         placeholder="Repeat Password"
//                       />
//                     </div>
//                   </div>
//                   <a
//                     href="login.html"
//                     className="btn btn-primary btn-user btn-block"
//                   >
//                     Register Account
//                   </a>
//                   <hr />
//                   <a
//                     href="index.html"
//                     className="btn btn-google btn-user btn-block"
//                   >
//                     <i className="fab fa-google fa-fw"></i> Register with Google
//                   </a>
//                   <a
//                     href="index.html"
//                     className="btn btn-facebook btn-user btn-block"
//                   >
//                     <i className="fab fa-facebook-f fa-fw"></i> Register with
//                     Facebook
//                   </a>
//                 </form>
//                 <hr />
//                 <div className="text-center">
//                   <a className="small" href="forgot-password.html">
//                     Forgot Password?
//                   </a>
//                 </div>
//                 <div className="text-center">
//                   <a className="small" href="login.html">
//                     Already have an account? Login!
//                   </a>
//                 </div>
//               </div>
//             </div>
//           </div>
//         </div>
//       </div>
//     </div>
//   );
// };

// export default Register;

// import React, { useEffect } from "react";
// import {
//   Box,
//   Container,
//   TextField,
//   Button,
//   Typography,
//   Grid,
// } from "@mui/material";

// const Register: React.FC = () => {
//   useEffect(() => {
//     document.body.classList.toggle("bg-gradient-primary", true);
//   }, []);
//   return (
//     <Container maxWidth="md">
//       <Box
//         sx={{
//           my: 5,
//           bgcolor: "background.default",
//           p: 4,
//           display: "flex",
//           flexDirection: "column",
//           borderRadius: 2,
//         }}
//       >
//         <Grid container spacing={2}>
//           <Grid item xs={12} md={6}>
//             <Typography variant="h5" align="center">
//               Create an Account!
//             </Typography>
//           </Grid>
//           <Grid item xs={12} md={6}>
//             <img
//               src="path/to/bg-register-image"
//               alt="Register Image"
//               style={{ width: "100%", height: "auto" }}
//             />
//           </Grid>
//         </Grid>
//         <Box component="form" noValidate sx={{ mt: 2 }}>
//           <Grid container spacing={2}>
//             <Grid item xs={12} sm={6}>
//               <TextField
//                 autoComplete="fname"
//                 name="firstName"
//                 required
//                 fullWidth
//                 id="firstName"
//                 label="First Name"
//                 autoFocus
//               />
//             </Grid>
//             <Grid item xs={12} sm={6}>
//               <TextField
//                 required
//                 fullWidth
//                 id="lastName"
//                 label="Last Name"
//                 name="lastName"
//                 autoComplete="lname"
//               />
//             </Grid>
//             <Grid item xs={12}>
//               <TextField
//                 required
//                 fullWidth
//                 id="email"
//                 label="Email Address"
//                 name="email"
//                 autoComplete="email"
//               />
//             </Grid>
//             <Grid item xs={12} sm={6}>
//               <TextField
//                 required
//                 fullWidth
//                 name="password"
//                 label="Password"
//                 type="password"
//                 id="password"
//                 autoComplete="new-password"
//               />
//             </Grid>
//             <Grid item xs={12} sm={6}>
//               <TextField
//                 required
//                 fullWidth
//                 name="confirmPassword"
//                 label="Confirm Password"
//                 type="password"
//                 id="confirmPassword"
//                 autoComplete="new-password"
//               />
//             </Grid>
//           </Grid>
//           <Button
//             type="submit"
//             fullWidth
//             variant="contained"
//             color="primary"
//             sx={{ mt: 3, mb: 2 }}
//           >
//             Register Account
//           </Button>
//           <Button
//             fullWidth
//             variant="outlined"
//             color="secondary"
//             sx={{ mt: 2, mb: 2 }}
//           >
//             Register with Google
//           </Button>
//           <Button
//             fullWidth
//             variant="outlined"
//             color="primary"
//             sx={{ mt: 2, mb: 2 }}
//           >
//             Register with Facebook
//           </Button>
//         </Box>
//         <Box mt={2}>
//           <Typography variant="body2" color="text.secondary" align="center">
//             <a href="forgot-password.html">Forgot password?</a>
//           </Typography>
//           <Typography variant="body2" color="text.secondary" align="center">
//             <a href="login.html">Already have an account? Login!</a>
//           </Typography>
//         </Box>
//       </Box>
//     </Container>
//   );
// };

// export default Register;

import React, { useEffect } from "react";
import {
  Container,
  Grid,
  TextField,
  Button,
  Typography,
  Card,
  CardContent,
  Box,
} from "@mui/material";
import GoogleIcon from "@mui/icons-material/Google";
import FacebookIcon from "@mui/icons-material/Facebook";

const Register: React.FC = () => {
  useEffect(() => {
    document.body.classList.toggle("bg-gradient-primary", true);
  }, []);

  return (
    <Container maxWidth="md" sx={{ mt: 8 }}>
      <Card>
        <CardContent
          sx={{
            display: "flex",
            flexDirection: "column",
            alignItems: "center",
          }}
        >
          <Typography variant="h5" align="center" gutterBottom>
            Create an Account!
          </Typography>
          <form noValidate>
            <Grid container spacing={2}>
              <Grid item xs={12} sm={6}>
                <TextField
                  required
                  fullWidth
                  id="firstName"
                  label="First Name"
                  autoFocus
                />
              </Grid>
              <Grid item xs={12} sm={6}>
                <TextField required fullWidth id="lastName" label="Last Name" />
              </Grid>
              <Grid item xs={12}>
                <TextField
                  required
                  fullWidth
                  id="email"
                  label="Email Address"
                  type="email"
                />
              </Grid>
              <Grid item xs={12} sm={6}>
                <TextField
                  required
                  fullWidth
                  id="password"
                  label="Password"
                  type="password"
                />
              </Grid>
              <Grid item xs={12} sm={6}>
                <TextField
                  required
                  fullWidth
                  id="confirmPassword"
                  label="Repeat Password"
                  type="password"
                />
              </Grid>
            </Grid>
            <Button
              type="submit"
              fullWidth
              variant="contained"
              color="primary"
              sx={{ mt: 3, mb: 2 }}
            >
              Register Account
            </Button>
            <Button
              fullWidth
              variant="outlined"
              startIcon={<GoogleIcon />}
              sx={{ mt: 2 }}
            >
              <a href="index.html"> Register with Google</a>
            </Button>
            <Button
              fullWidth
              variant="outlined"
              startIcon={<FacebookIcon />}
              sx={{ mt: 2 }}
            >
              <a href="index.html">Register with Facebook</a>
            </Button>
          </form>
          <Box sx={{ mt: 3 }}>
            <Typography variant="body2" align="center">
              <a href="forgot-password">Forgot Password?</a>
            </Typography>
            <Typography variant="body2" align="center">
              <a href="/">Already have an account? Login!</a>
            </Typography>
          </Box>
        </CardContent>
      </Card>
    </Container>
  );
};

export default Register;
