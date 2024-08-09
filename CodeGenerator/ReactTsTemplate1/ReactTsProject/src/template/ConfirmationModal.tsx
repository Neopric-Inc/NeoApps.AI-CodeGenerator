import React, { FC, ReactNode } from "react";
import {
  Dialog,
  DialogTitle,
  DialogContent,
  DialogActions,
  Button,
} from "@mui/material";

type Props = {
  show: boolean;
  title: ReactNode;
  body: ReactNode;
  buttonPositive: ReactNode;
  buttonNegative: ReactNode;
  disableButton?: boolean;
  onPositive: () => void;
  onNegative: () => void;
};

const ConfirmationModal: FC<Props> = ({
  show,
  title,
  body,
  buttonPositive,
  buttonNegative,
  onPositive,
  onNegative,
  disableButton,
}) => {
  return (
    <Dialog open={show} onClose={() => onNegative()} fullWidth maxWidth="sm">
      <DialogTitle>{title}</DialogTitle>
      <DialogContent>{body}</DialogContent>
      <DialogActions sx={{ mt: -2, justifyContent: "space-between" }}>
        <Button
          size="small"
          onClick={() => onNegative()}
          color="secondary"
          sx={{ ml: 2 }}
          variant="outlined"
          disabled={disableButton}
        >
          {buttonNegative}
        </Button>
        <Button
          size="small"
          onClick={() => onPositive()}
          color="error"
          variant="contained"
          disabled={disableButton}
        >
          {buttonPositive}
        </Button>
      </DialogActions>
    </Dialog>
  );
};

export default ConfirmationModal;
