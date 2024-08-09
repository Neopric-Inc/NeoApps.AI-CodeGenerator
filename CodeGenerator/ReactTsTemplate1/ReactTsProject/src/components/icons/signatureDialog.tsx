import React, { useRef, useState } from "react";
import Dialog from "@mui/material/Dialog";
import DialogTitle from "@mui/material/DialogTitle";
import DialogContent from "@mui/material/DialogContent";
import Button from "@mui/material/Button";
import SignatureCanvas from "react-signature-canvas";
import Snackbar from "@mui/material/Snackbar";
import MuiAlert from "@mui/material/Alert";
import { CircularProgress } from "@mui/material";

const SignatureDialog = ({
    open,
    onClose,
    setFieldValue,
    value,
    config,
    handleFileupload,
}) => {
    const signatureRef = useRef(null);
    const [snackbarOpen, setSnackbarOpen] = useState(false);
    const [alertMessage, setAlertMessage] = useState("");
    const [isLoading, setIsLoading] = useState("");
    const handleSnackbarClose = (
        event: React.SyntheticEvent<Element, Event>,
        reason?: string
    ) => {
        if (reason === "clickaway") {
            return;
        }
        setSnackbarOpen(false);
    };
    const clearSignature = () => {
        signatureRef.current.clear();
    };

    const getSignatureImage = async () => {
        setIsLoading("true");
        if (signatureRef.current.isEmpty()) {
            setAlertMessage("Signature is empty");
            setIsLoading("");
            setSnackbarOpen(true);
        } else {
            const signatureImageURL = signatureRef.current
                .getTrimmedCanvas()
                .toDataURL("image/png");
            console.log("Signature Image URL:", signatureImageURL);

            // Convert the data URL to a Blob
            const base64Data = signatureImageURL.replace(
                /^data:image\/\w+;base64,/,
                ""
            );
            const byteCharacters = atob(base64Data);
            const byteNumbers = new Array(byteCharacters.length);
            for (let i = 0; i < byteCharacters.length; i++) {
                byteNumbers[i] = byteCharacters.charCodeAt(i);
            }
            const byteArray = new Uint8Array(byteNumbers);
            const blob = new Blob([byteArray], { type: "image/png" });

            // Create a File object
            const fileName = value + Date.now().toString() + ".png";
            const Sfile = new File([blob], fileName, { type: "image/png" });

            // Upload the signature image using the handleFileupload function
            const response = await handleFileupload(
                {
                    files: [Sfile],
                },
                setFieldValue, // You need to pass setFieldValue from your form component
                value // Replace with the field name you want to set the value for
            );

            if (response) {
                // You can handle success or failure here, e.g., show a message
                if (response === "File upload failed") {
                    setAlertMessage("Signature upload failed");
                    setIsLoading("");
                    setSnackbarOpen(true);
                } else {
                    setAlertMessage("Signature uploaded successfully");
                    setSnackbarOpen(true);
                    setIsLoading("");
                    onClose(); // Close the dialog after uploading the signature.
                }
            }
        }
    };

    return (
        <>
            <Dialog open={open} onClose={onClose}>
                <DialogTitle>Sign Here</DialogTitle>
                <DialogContent>
                    <SignatureCanvas
                        ref={signatureRef}
                        penColor="black"
                        canvasProps={{ width: 400, height: 200, className: "sigCanvas" }}
                    />
                    <div style={{ marginTop: "16px" }}>
                        <Button
                            onClick={clearSignature}
                            variant="outlined"
                            color="secondary"
                        >
                            Clear
                        </Button>
                        <Button
                            onClick={getSignatureImage}
                            variant="contained"
                            color="primary"
                            style={{ marginLeft: "8px" }}
                        >
                            {isLoading ? (
                                <CircularProgress
                                    size={24}
                                    color="inherit"
                                />
                            ) : (
                                "Save Signature"
                            )}
                        </Button>
                    </div>
                </DialogContent>
            </Dialog>
            <Snackbar
                open={snackbarOpen}
                autoHideDuration={6000}
                onClose={handleSnackbarClose}
            >
                <MuiAlert
                    severity="error"
                    elevation={6}
                    variant="filled"
                    onClose={handleSnackbarClose}
                >
                    {alertMessage}
                </MuiAlert>
            </Snackbar>
        </>
    );
};

export default SignatureDialog;
