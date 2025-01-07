import React from "react";
import Snackbar from "@mui/material/Snackbar";
import MuiAlert, { AlertProps } from "@mui/material/Alert";
function Alert(props: AlertProps) {
  return <MuiAlert elevation={6} variant="filled" {...props} />;
}

interface SnackbarProps {
  open: boolean;
  message: string;
  type: string;
  onClose: (
    event: React.SyntheticEvent | React.MouseEvent,
    reason?: string
  ) => void;
}

const CustomizedSnackbars: React.FC<SnackbarProps> = ({
  open,
  message,
  type,
  onClose,
}) => {
  return (
    <Snackbar
      open={open}
      autoHideDuration={3000}
      onClose={onClose}
      anchorOrigin={{ vertical: "top", horizontal: "center" }}
    >
      {type === "success" ? (
        <MuiAlert
          severity={"success"}
          elevation={6}
          variant="filled"
          onClose={onClose}
        >
          {message}
        </MuiAlert>
      ) : type === "error" ? (
        <MuiAlert
          severity={"error"}
          elevation={6}
          variant="filled"
          onClose={onClose}
        >
          {message}
        </MuiAlert>
      ) : (
        <MuiAlert
          severity={"info"}
          elevation={6}
          variant="filled"
          onClose={onClose}
        >
          {message}
        </MuiAlert>
      )}
    </Snackbar>
  );
};

export default CustomizedSnackbars;
