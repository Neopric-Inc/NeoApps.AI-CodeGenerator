import type { FC } from "react";
import { useMemo, useState } from "react";
import PropTypes from "prop-types";
import dayjs from "dayjs";
//import toast from "react-hot-toast";
import { addMinutes } from "date-fns";
import * as Yup from "yup";
import { useFormik } from "formik";
import ApiIcon from "@mui/icons-material/Api";
import {
  Box,
  Button,
  CircularProgress,
  Dialog,
  Divider,
  FormControlLabel,
  FormHelperText,
  Grid,
  IconButton,
  MenuItem,
  Select,
  Switch,
  TextField,
  Typography,
} from "@mui/material";
// import DateTimePicker from "@mui/lab/DateTimePicker";
import { Trash as TrashIcon } from "components/icons/trash";
import { useDispatch } from "react-redux";
// import LocalizationProvider from "@mui/lab/LocalizationProvider";
import { LocalizationProvider } from "@mui/x-date-pickers/LocalizationProvider";
import AdapterDateFns from "@mui/lab/AdapterDateFns";
import { DateTimePicker } from "@mui/x-date-pickers/DateTimePicker";
import { AdapterDayjs } from "@mui/x-date-pickers/AdapterDayjs";
import ReactQuill from "react-quill";
import { DatePicker } from "@mui/x-date-pickers";
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
  //   async function getLayout() {
  //    //console.log("Get Layout is Clicked.... ", id);
  //     // we are using id which is passed from index.tsx as a prop
  //     const response1 = await getOneDnd_ui_versions(2);
  //    //console.log(response1);
  //     const response = await getOneDnd_ui_versions(id);
  //     if (response && response.data.document !== null) {
  //      //console.log("Fetched Successfully");
  //       const dnd_builder_data = response["data"]["document"];
  //       const dnd_builder_layout = JSON.parse(dnd_builder_data["layout"]);
  //       let dnd_builder_layoutList = dnd_builder_layout["layoutList"];
  //       let dnd_builder_pages = JSON.parse(dnd_builder_data["ui_pages"]);
  //      //console.log(dnd_builder_layout);
  //       let dnd_builder_components = JSON.parse(dnd_builder_data["components"]);
  //       ////console.log("layoutList : ", dnd_builder_layoutList, "pages : ", dnd_builder_pages);
  //       let formatted_components = {};
  //       ////console.log("Raw Components Data : ", dnd_builder_components)
  //       Object.keys(dnd_builder_components).forEach((key) => {
  //         const component_id = dnd_builder_components[key]["id"];
  //         // let temp_component = { id: dnd_builder_components[key]['id'], type: SIDEBAR_ITEM, component: { type: dnd_builder_components[key]['type'], content: dnd_builder_components[key]['content'], icon: ApiIcon } };
  //         // let temp_component = { id: dnd_builder_components[key]['id'], type: dnd_builder_components[key]['type'], content: mapNametoComponent[dnd_builder_components[key]['content']], icon: ApiIcon }
  //         let temp_config = {};
  //         temp_config[component_id] = dnd_builder_components[key]["config"];
  //         // dnd_builder_components[key]['config'] ? temp_config = dnd_builder_components[key]['config'] : temp_config = {};
  //         setConfigurations((prevState) => ({ ...prevState, ...temp_config }));
  //         let temp_component = {
  //           id: dnd_builder_components[key]["id"],
  //           type: dnd_builder_components[key]["type"],
  //           content: functionTOmap(
  //             dnd_builder_components[key]["content"],
  //             temp_config
  //           ),
  //           icon: ApiIcon,
  //         };
  //         // dnd_builder_components[key]['icon_name'] !== undefined ? temp_component["component"]["icon"] = '<' + dnd_builder_components[key]['icon_name'] + " className='dnd sidebarIcon' />" : temp_component["component"]["icon"] = temp_component["component"]["icon"];
  //         formatted_components[component_id] = temp_component;
  //         ////console.log("mapNametoComponent : ", dnd_builder_components[key], mapNametoComponent[dnd_builder_components[key]['content']])
  //       });
  //       ////console.log("formatted components : ", formatted_components)
  //       // setLayout(dnd_builder_layout);
  //       // setComponents(formatted_components);

  //       ////console.log("layoutList : ", dnd_builder_layoutList, "pages : ", dnd_builder_pages, "components : ", formatted_components);
  //       ////console.log("layout from getLayout : ", dnd_builder_layoutList[0])
  //       setActiveLink(null);
  //       setLayoutList(dnd_builder_layoutList);
  //       setSidebarLinks(dnd_builder_pages["sidbarLinks"]);
  //       setPopupLinks(dnd_builder_pages["popupLinks"]);
  //       setDrawerLinks(dnd_builder_pages["drawerLinks"]);
  //       setComponentLinks(dnd_builder_pages["componentLinks"]);
  //      //console.log(dnd_builder_pages);
  //       ////console.log("errorneous layout ", dnd_builder_layoutList[0]);
  //       // (dnd_builder_layoutList[0] === undefined || dnd_builder_layoutList[0] === null) ? setLayout([]) : setLayout(dnd_builder_layoutList[0]);
  //       setComponents(formatted_components);
  //       setLayout([]);
  //     } else {
  //      //console.log("Some error occured While Fetching!");
  //     }
  //   }
  //  //console.log(nconfig);
  //  //console.log(componentId);
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
      //  //console.log("Fetched Successfully");
      //   const dnd_builder_data = response["data"]["document"];
      //   const dnd_builder_layout = JSON.parse(dnd_builder_data["layout"]);
      //   let dnd_builder_layoutList = dnd_builder_layout["layoutList"];
      //   let dnd_builder_pages = JSON.parse(dnd_builder_data["ui_pages"]);
      //  //console.log(dnd_builder_layout);
      //   let dnd_builder_components = JSON.parse(dnd_builder_data["components"]);
      //   ////console.log("layoutList : ", dnd_builder_layoutList, "pages : ", dnd_builder_pages);
      //   let formatted_components = {};
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
