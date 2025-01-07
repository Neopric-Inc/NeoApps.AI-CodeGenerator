import { useState } from "react";
//import toast from "react-hot-toast";
import {
  Box,
  Button,
  CircularProgress,
  Dialog,
  Divider,
  TextField,
  Typography,
} from "@mui/material";
// import DateTimePicker from "@mui/lab/DateTimePicker";
import { useDispatch } from "react-redux";
// import LocalizationProvider from "@mui/lab/LocalizationProvider";
import {
  getOneDnd_ui_versions,
  updateDnd_ui_versions,
} from "services/dnd_ui_versionsService";


export const ImportGlobalConfigDialog = (props) => {
  const { id, onClose, open } = props;
  const userString = localStorage.getItem("userInfo");
  const user = JSON.parse(userString);
  const dispatch = useDispatch();
  const [load, setLoad] = useState(false);
  const [inputValue, setInputValue] = useState("");
  const [componentsImport, setComponentsImports] = useState("");
  const [uipages, setUipages] = useState("");
  const handleInputChange = (event) => {
    setInputValue(event.target.value);
  };
  const handlecomponentsChange = (event) => {
    setComponentsImports(event.target.value);
  };
  const handleuipagesChanges = (event) => {
    setUipages(event.target.value);
  };
  
  const handleSubmit = async () => {
    setLoad(true);
    //const response1 = await getOneDnd_ui_versions(2);

    const response = await getOneDnd_ui_versions(id);
   //console.log(response);
    if (response ) {
      let layout = "";
        let components = "";
        let ui_pages = "";
        if (response.data.document != null) {
            layout = response.data.document.layout;
            components = response.data.document.components;
            ui_pages = response.data.document.ui_pages;

        }
      const dnd_builder_obj = {
        layout: inputValue ? inputValue : layout,
        components: componentsImport ? componentsImport : components,
          ui_pages: uipages ? uipages : ui_pages,

        dnd_ui_type: "demo_page",
        createdBy: user?.username,
        modifiedBy: user?.username,
        isActive: 1,
        createdAt: "2012-07-26T01:25:58",
        modifiedAt: "2012-07-26T01:25:58",
      };
      const response1 = await updateDnd_ui_versions(id, dnd_builder_obj);
      if (response1 && response1.data) {
       //console.log("Updated Successfully : ", response1);
        window.location.reload();
        //await getLayout();
        setLoad(false);
        onClose();
      } else {
       //console.log("Some error occured While Adding!");
      }
     
    } else {
     //console.log("Some error occured While Fetching!");
    }
    setLoad(false);
  };

  const handleDelete = async (): Promise<void> => {
    try {
      //   await dispatch(
      //     deleteEvent({
      //       eventId: event.id!,
      //     })
      //   );
    } catch (err) {
      console.error(err);
    }
  };

  return (
    <Dialog fullWidth maxWidth="lg" onClose={onClose} open={true}>
      <>
        <Box sx={{ p: 1 }}>
          <Typography align="center" gutterBottom variant="h5">
            Import Config
          </Typography>
        </Box>
        <Box sx={{ p: 1 }}>
          <Box sx={{ mt: 2, mb: 3, pb: 3 }}>
            <Typography variant="subtitle1" sx={{ mb: 1 }}>
              Layout Configuration :
            </Typography>
            <TextField
              id="multiline-textfield"
              label="layout_configuration"
              multiline
              rows={8} // You can adjust the number of rows as needed
              variant="outlined"
              fullWidth
              onChange={handleInputChange}
            />
          </Box>
        </Box>
        <Divider />
        <Box sx={{ p: 1 }}>
          <Box sx={{ mt: 2, mb: 3, pb: 3 }}>
            <Typography variant="subtitle1" sx={{ mb: 1 }}>
              Components Configuration :
            </Typography>
            <TextField
              id="multiline-textfield"
              label="components_configuration"
              multiline
              rows={8} // You can adjust the number of rows as needed
              variant="outlined"
              fullWidth
              onChange={handlecomponentsChange}
            />
          </Box>
        </Box>
        <Divider />
        <Box sx={{ p: 1 }}>
          <Box sx={{ mt: 2, mb: 3, pb: 3 }}>
            <Typography variant="subtitle1" sx={{ mb: 1 }}>
              UI_Pages Configuration :
            </Typography>
            <TextField
              id="multiline-textfield"
              label="ui_pages_configuration"
              multiline
              rows={8} // You can adjust the number of rows as needed
              variant="outlined"
              fullWidth
              onChange={handleuipagesChanges}
            />
          </Box>
        </Box>
        <Divider />
        <Box
          sx={{
            alignItems: "center",
            display: "flex",
            p: 1,
          }}
        >
          {/* {event && (
            <IconButton onClick={(): Promise<void> => handleDelete()}>
              <TrashIcon fontSize="small" />
            </IconButton>
          )} */}
          <Box sx={{ flexGrow: 1 }} />
          <Button onClick={onClose} size="small">
            Cancel
          </Button>
          {!load ? (
            <Button
              sx={{ ml: 1 }}
              type="submit"
              onClick={handleSubmit}
              variant="contained"
              size="small"
            >
              Confirm
            </Button>
          ) : (
            <CircularProgress size={30} />
          )}
        </Box>
      </>
    </Dialog>
  );
};
