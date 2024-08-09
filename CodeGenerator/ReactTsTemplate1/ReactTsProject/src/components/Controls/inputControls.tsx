




import {
  Box,
  Container,
  Grid,
  Drawer,
  Dialog,
  DialogContent,
  Card,
  List,
  Typography,
  ListItem,
  Autocomplete,
  TextField,
  Divider,
  FormControlLabel,
  FormControl,
  FormLabel,
  RadioGroup,
  Radio,
  Checkbox,
  FormGroup,
  Slider,
  Switch,
  Rating,
} from "@mui/material";
import StarIcon from "@mui/icons-material/Star";
import dayjs from "dayjs";
import { DateTimePicker } from "@mui/x-date-pickers/DateTimePicker";
import { DemoContainer, DemoItem } from "@mui/x-date-pickers/internals/demo";
import { functionTOmap } from "Dnd/Dnd Designer/Utility/constants";
import React, { useEffect, useState } from "react";
import { getOneDnd_ui_versions } from "services/dnd_ui_versionsService";
//import "./style.css";
import "../../Dnd/Dnd Preview/style.css"
import ApiIcon from "@mui/icons-material/Api";
// import { SyncFusion_Component_List } from "Dnd/Dnd Designer/Utility/constants";
// import { MUI_Component_List } from "Dnd/Dnd Designer/Utility/constants";
//import { PreviewSidebar } from "./Sidebar";
//import { DndProvider } from "react-dnd";
import { HTML5Backend } from "react-dnd-html5-backend";
import { useNavigate } from "react-router";
import IconButton from "@mui/material/IconButton";
import CloseIcon from "@mui/icons-material/Close";
import countries, { top100Films } from "./countries";
import { TimePicker } from "@mui/x-date-pickers/TimePicker";
import { DatePicker } from "@mui/x-date-pickers";
import { LocalizationProvider, DesktopTimePicker } from "@mui/x-date-pickers";
import { AdapterDayjs } from "@mui/x-date-pickers/AdapterDayjs";





export const InputControls = () => {
  const [id, setId] = useState(null);
  const [activeLink, setActiveLink] = useState(0);
  const [sidbarLinks, setSidebarLinks] = useState([]);
  const [popupLinks, setPopupLinks] = useState([]);
  const [drawerLinks, setDrawerLinks] = useState([]);
  const [layout, setLayout] = useState([]);
  const [layoutList, setLayoutList] = useState({});
  const [Components, setComponents] = useState({});
  const [configurations, setConfigurations] = useState({});
  const [prevLayouts, setPrevLayouts] = useState([]);
  const [prevActiveLinks, setPrevActiveLinks] = useState([]);
  useEffect(() => {
    // Whenever activeLink changes, push the previous active link to the stack
    setPrevActiveLinks((prevActiveLinks) => [...prevActiveLinks, activeLink]);
  }, [activeLink]);
  useEffect(() => {
    // Whenever layout changes, push the previous layout to the stack
    setPrevLayouts((prevLayouts) => [...prevLayouts, layout]);
  }, [layout]);

  // Add a handler to pop the top layout from the stack and set it as the current layout
  
  const defaultProps = {
    options: top100Films,
    getOptionLabel: (option) => option.title,
  };
  const flatProps = {
    options: top100Films.map((option) => option.title),
  };
  const [value, setValue] = React.useState(null);
  const [state, setState] = React.useState({
    Facebook: true,
    Instagram: false,
    Twitter: false,
  });
  const handleChange = (event: React.ChangeEvent<HTMLInputElement>) => {
    setState({
      ...state,
      [event.target.name]: event.target.checked,
    });
  };


  const { Facebook, Instagram, Twitter } = state;
  const [sliderValue, setSliderValue] = useState(50); // Initialize with a default value

  // Function to handle slider value changes
  const handleSliderChange = (event, newValue) => {
    setSliderValue(newValue); // Update the state variable with the new value
  };

  const labels: { [index: string]: string } = {
    0.5: "Worst",
    1: "Worst+",
    1.5: "Poor",
    2: "Poor+",
    2.5: "Average",
    3: "Average+",
    3.5: "Good",
    4: "Good+",
    4.5: "Excellent",
    5: "Excellent+",
  };

  function getLabelText(value: number) {
    return `${value} Star${value !== 1 ? "s" : ""}, ${labels[value]}`;
  }
  const [rate, setRate] = React.useState<number | null>(2);
  const [hover, setHover] = React.useState(-1);
  const [selectedDateTime, setSelectedDateTime] = useState(null);

  // Step 3: Define an onChange handler to update the state variable
  const handleDateTimeChange = (newDateTime) => {
    setSelectedDateTime(newDateTime);
  };

   const [selectedDate, setSelectedDate] = useState(null);

   // Step 3: Define an onChange handler to update the state variable
   const handleDateChange = (newDate) => {
     setSelectedDate(newDate);
   };
   const [selectedTime, setSelectedTime] = useState(null);

   // Step 3: Define an onChange handler to update the state variable
   const handleTimeChange = (newTime) => {
     setSelectedTime(newTime);
   };

  const formatDateTimeForLogging = (dateTime) => {
    if (!dateTime) return "No date and time selected"; // Handle the case when no date and time are selected
    //console.log(dateTime.toLocaleString());
    return dateTime.toLocaleString(); // You can use toLocaleString() with the appropriate options for your desired format
  };
const currentDate = new Date();

  return (
    // <DndProvider backend={HTML5Backend}>
    //   {/* <Sidebar sidbarLinks={sidbarLinks} onLinkClick={() => //console.log("onClick Event Happend.")} handleLinkAdd={() => //console.log("onAdd Event Happend.")} /> */}
    //   {/* <h3><button onClick={fetchData} style={{ 'background': 'grey', 'color': 'white', justifyContent: 'center', textAlign: 'center', margin: 'auto' }}>Get Preview</button></h3> */}
    //   {/* <h3 style={{ 'background': 'grey', 'color': 'white', justifyContent: 'center', textAlign: 'center', margin: 'auto', padding: '5px', borderRadius: '10px' }}>Preview</h3> */}
    //   {/* <hr /> */}
    //   {//console.log(
    //     "Outside fetch Layout : ",
    //     layout,
    //     "components : ",
    //     Components,
    //     "configurations : ",
    //     configurations
    //   )}
    //   {/* <p>ID : {id}</p> */}
    //   {/* <button onClick={fetchData} style={{ 'background': 'gray', 'color': 'white' }}>Get Preview</button> */}
    //   {/* {//console.log("Layout : ", layout, "components : ", components)} */}
    //   <div style={{ display: "flex", flexDirection: "row" }}>
    //     <PreviewSidebar
    //       links={sidbarLinks}
    //       config={configurations}
    //       onLinkClick={onLinkClick}
    //     />
    //     {popupLinks.some((link) => link.id === activeLink) ? (
    //       <Dialog
    //         open={popupLinks.some((link) => link.id === activeLink)}
    //         onClose={handleDrawerClose}
    //         maxWidth="xl" // Set the max width of the dialog to 'md', 'sm', 'lg', 'xl', or 'false'
    //         fullWidth={true}
    //         style={{ overflow: "visible" }}
    //       >
    //         <IconButton
    //           edge="end"
    //           onClick={handleDrawerClose}
    //           aria-label="close"
    //           style={{
    //             position: "fixed",
    //             top: 13,
    //             right: 25,
    //             color: "white",
    //             borderRadius: "50%", // Set to 50% for a round shape
    //             width: "40px", // Adjust the width to make it a circle (e.g., 40px)
    //             height: "40px", // Adjust the height to match the width (e.g., 40px)
    //             overflow: "visible",
    //             backgroundColor: "blue",
    //           }}
    //         >
    //           <CloseIcon />
    //         </IconButton>
    //         <DialogContent style={{ overflow: "visible" }}>
    //           {getLayoutContent()}
    //         </DialogContent>
    //       </Dialog>
    //     ) : drawerLinks.some((link) => link.id === activeLink) ? (
    //       <Drawer
    //         anchor="right"
    //         open={drawerLinks.some((link) => link.id === activeLink)}
    //         onClose={handleDrawerClose}
    //         PaperProps={{ style: { width: "70%" } }}
    //       >
    //         {getLayoutContent()}
    //       </Drawer>
    //     ) : (
    //       <Container
    //         style={{
    //           backgroundColor:
    //             configurations["globalConfig"] !== undefined
    //               ? configurations["globalConfig"]["backgroundColor"]
    //               : "white",
    //           border: "1px solid #ddd",
    //         }}
    //         maxWidth={false}
    //       >
    //         {getLayoutContent()}
    //       </Container>
    //     )}
    //   </div>
    // </DndProvider>
    <Card>
      <List sx={{ justifyContent: "space-between" }}>
        <ListItem sx={{ justifyContent: "space-between" }}>
          <Typography>AutoComplete1</Typography>
          <Autocomplete
            id="country-select-demo"
            sx={{ width: 300 }}
            options={countries}
            autoHighlight
            getOptionLabel={(option) => option.label}
            autoComplete
            includeInputInList
            renderOption={(props, option) => (
              <Box
                component="li"
                sx={{ "& > img": { mr: 2, flexShrink: 0 } }}
                {...props}
              >
                <img
                  loading="lazy"
                  width="20"
                  src={`https://flagcdn.com/w20/${option.code.toLowerCase()}.png`}
                  srcSet={`https://flagcdn.com/w40/${option.code.toLowerCase()}.png 2x`}
                  alt=""
                />
                {option.label} ({option.code}) +{option.phone}
              </Box>
            )}
            renderInput={(params) => (
              <TextField
                sx={{ width: 300 }}
                {...params}
                label="Choose a country"
                inputProps={{
                  ...params.inputProps,
                  autoComplete: "new-password", // disable autocomplete and autofill
                }}
              />
            )}
          />
        </ListItem>
        <Divider />
        <ListItem sx={{ justifyContent: "space-between" }}>
          <Typography>AutoComplete2</Typography>
          <Autocomplete
            sx={{ width: 300 }}
            {...defaultProps}
            id="auto-complete"
            autoComplete
            includeInputInList
            renderInput={(params) => (
              <TextField {...params} label="autoComplete" variant="standard" />
            )}
          />
        </ListItem>
        <Divider />
        <ListItem sx={{ justifyContent: "space-between" }}>
          <Typography>Search Input</Typography>
          <Autocomplete
            sx={{ width: 300 }}
            freeSolo
            id="free-solo-2-demo"
            disableClearable
            options={top100Films.map((option) => option.title)}
            renderInput={(params) => (
              <TextField
                {...params}
                label="Search input"
                InputProps={{
                  ...params.InputProps,
                  type: "search",
                }}
              />
            )}
          />
        </ListItem>
        <Divider />
        <ListItem sx={{ justifyContent: "space-between" }}>
          <Typography>Text Field(Only Numbers)</Typography>
          <TextField
            id="outlined-number"
            label="Number"
            type="number"
            InputLabelProps={{
              shrink: true,
            }}
          />
        </ListItem>
        <Divider />
        <ListItem sx={{ justifyContent: "space-between" }}>
          <Typography>Text Field</Typography>
          <TextField
            required
            id="outlined-required"
            label="Required"
            defaultValue="Social Media"
          />
        </ListItem>
        <Divider />
        <ListItem sx={{ justifyContent: "space-between" }}>
          <Typography>Radio Button</Typography>
          <FormControl>
            <FormLabel id="demo-radio-buttons-group-label">
              Social Media App
            </FormLabel>
            <RadioGroup
              aria-labelledby="demo-radio-buttons-group-label"
              defaultValue="instagram"
              name="radio-buttons-group"
            >
              <FormControlLabel
                value="instagram"
                control={<Radio />}
                label="Instagram"
              />
              <FormControlLabel
                value="facebook"
                control={<Radio />}
                label="Facebook"
              />
              <FormControlLabel
                value="twitter"
                control={<Radio />}
                label="Twitter"
              />
            </RadioGroup>
          </FormControl>
        </ListItem>
        <Divider />
        <ListItem sx={{ justifyContent: "space-between" }}>
          <Typography>Text Field</Typography>
          <FormControl sx={{ m: 3 }} component="fieldset" variant="standard">
            <FormLabel component="legend">Choose Social Media</FormLabel>
            <FormGroup>
              <FormControlLabel
                control={
                  <Checkbox
                    checked={Facebook}
                    onChange={handleChange}
                    name="Facebook"
                  />
                }
                label="Facebook"
              />
              <FormControlLabel
                control={
                  <Checkbox
                    checked={Instagram}
                    onChange={handleChange}
                    name="Instagram"
                  />
                }
                label="Instagram"
              />
              <FormControlLabel
                control={
                  <Checkbox
                    checked={Twitter}
                    onChange={handleChange}
                    name="Twitter"
                  />
                }
                label="Twitter"
              />
            </FormGroup>
            {/* <FormHelperText>Be careful</FormHelperText> */}
          </FormControl>
        </ListItem>
        <Divider />
        <ListItem sx={{ justifyContent: "space-between" }}>
          <Typography>Slider</Typography>
          <Slider
            sx={{ width: 500 }}
            value={sliderValue} // Set the value of the slider from the state variable
            onChange={handleSliderChange} // Handle slider value changes
            aria-label="Default"
            valueLabelDisplay="auto"
          />
        </ListItem>
        <Divider />
        <ListItem sx={{ justifyContent: "space-between" }}>
          <Typography>Rating</Typography>
          <Rating
            name="hover-feedback"
            value={rate}
            precision={0.5}
            getLabelText={getLabelText}
            onChange={(event, newValue) => {
              setRate(newValue);
            }}
            onChangeActive={(event, newHover) => {
              setHover(newHover);
            }}
            emptyIcon={
              <StarIcon style={{ opacity: 0.55 }} fontSize="inherit" />
            }
          />
          {value !== null && (
            <Box sx={{ ml: 2 }}>{labels[hover !== -1 ? hover : rate]}</Box>
          )}
        </ListItem>
        <Divider />
        <ListItem sx={{ justifyContent: "space-between" }}>
          <Typography>Time Picker</Typography>
          <LocalizationProvider dateAdapter={AdapterDayjs}>
            {/* Step 1: Use the DateTimePicker component with an onChange handler */}
            <TimePicker
              views={["hours", "minutes", "seconds"]}
              value={selectedTime} // Set the value from state
              onChange={handleTimeChange} // Update the state variable
            />
          </LocalizationProvider>
        </ListItem>
        <Divider />
        <ListItem sx={{ justifyContent: "space-between" }}>
          <Typography>Date Picker</Typography>
          <LocalizationProvider dateAdapter={AdapterDayjs}>
            <DatePicker
              views={["year", "month", "day"]}
              defaultValue={dayjs(currentDate)}
              value={selectedDate} // Set the value from state
              onChange={handleDateChange} // Update the state variable
            />
          </LocalizationProvider>
        </ListItem>
        <Divider />
        <ListItem sx={{ justifyContent: "space-between" }}>
          <Typography>Date Time Picker</Typography>
          <DemoItem label={'"year", "day", "hours", "minutes", "seconds"'}>
            <LocalizationProvider dateAdapter={AdapterDayjs}>
              <DateTimePicker
                views={["year", "day", "hours", "minutes", "seconds"]}
                value={selectedDateTime} // Set the value from state
                onChange={handleDateTimeChange} // Update the state variable
              />
            </LocalizationProvider>
          </DemoItem>
        </ListItem>
        <Divider />
      </List>
    </Card>
  );
};