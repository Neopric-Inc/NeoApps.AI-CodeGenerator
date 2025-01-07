import React, { useRef, useState, useEffect } from "react";
import { useDrag } from "react-dnd";
import { COMPONENT, functionTOmap } from "../Utility/constants";
import { Configurations } from "../Utility/constants";
import FilterForm from "./FilterRowsCRUD/FilterForm";
import { useAppDispatch } from "redux/store";
import { IoIosColorPalette } from "react-icons/io";
import { SketchPicker } from "react-color";
import SwipeableDrawer from "@mui/material/SwipeableDrawer";
import TabNavigation from "./Tabs";
// import Tooltip from "@mui/material/Tooltip";
import IconButton from "@mui/material/IconButton";
import RemoveIcon from "@mui/icons-material/Remove";
import DeleteTwoToneIcon from "@mui/icons-material/DeleteTwoTone";
import { ImportConfigDialog } from "./ImportConfigDialog";
import {
  Checkbox,
  Table,
  Button,
  TableBody,
  Radio,
  RadioGroup,
  FormControlLabel,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  TextField,
  Paper,
  FormControl,
  InputLabel,
  Select,
  MenuItem,
  Divider,
  Grid,
  Typography,
  Popover,
  Box,
  Tooltip,
} from "@mui/material";
import { Help } from "@mui/icons-material";
import FieldConfig from "./ComponentConfiguration/FieldsConfiguration";
import SubFieldConfig from "./ComponentConfiguration/subFieldConfiguration";
import DisplayConfig from "./ComponentConfiguration/DisplayConfiguration";
import ErrorConfig from "./ComponentConfiguration/ErrorConfiguration";
import {
  ChartSelector,
} from "./ComponentConfiguration/ChartConfiguration";
import { ColorConfig } from "./ComponentConfiguration/ChartConfiguration";
import FontAwesomeIconPicker from "template/FontAwesomeIconPicker";
let customConfig = {};
//Dev2
export const Popup = (props) => {
  const dispatch = useAppDispatch();
  ////console.log("props.componentId : ", props.componentId)
  let componentId = props.componentId;
  let NavComponents = props.LayoutList;
  //console.log(NavComponents);
  let componentName = props.componentName;
  let nconfig = props.nconfig;
  //   const [config, setConfig] = useState({});
  let inputFieldsJSX1 = [];
  let inputFieldsJSX11 = [];
  let inputFieldsJSX12 = [];
  let inputFieldsJSX13 = [];
  let inputFieldsJSX14 = [];
  let inputFieldsJSX15 = [];
  let inputFieldsJSX21 = [];
  let inputFieldsJSX22 = [];
  let inputFieldsJSX23 = [];
  let inputFieldsJSX24 = [];
  let inputFieldsJSX3 = [];
  let inputFieldsJSX4 = [];
  let inputFieldsJSX5 = [];
  let inputFieldsJSX51 = [];
  let inputFieldsJSXChart1 = [];
  let inputFieldsJSXChart11 = [];
  let inputFieldsJSXChart12 = [];

  const ShowInputFields = (props) => {
    const inputType = props.inputType;
    const options = props.options;
    const [value, setValue] = useState("");

    if (nconfig !== null && nconfig !== undefined && value === "") {
      setValue(nconfig[componentId][inputType]);
    }
    const handleChange = (event) => {
      setValue(event.target.value);
      ////console.log("e.target.name :", event.target.name);
      customConfig[componentId][inputType] = event.target.value;
    };
    return (
     
      //UI change inheading textfield
      <TableContainer>
        <Table>
          <TableBody>
            <TableRow>
              <TableCell sx={{ minWidth: "200px" }}>
                <Typography variant="body1" sx={{ textAlign: "left" }}>
                  {`${inputType.split(/(?=[A-Z])/).join(" ")}`}
                </Typography>
              </TableCell>
              <TableCell sx={{ minWidth: "220px" }}>
                <FormControl fullWidth>
                  <InputLabel
                    id={`${inputType}-label`}
                    sx={{ textAlign: "center" }}
                  >
                    {inputType.split(/(?=[A-Z])/).join(" ")}
                  </InputLabel>
                  <Select
                    labelId={`${inputType}-label`}
                    id={inputType}
                    value={value}
                    onChange={handleChange}
                    sx={{ textAlign: "center" }}
                  >
                    {options.map((option) => (
                      <MenuItem key={option} value={option}>
                        {option}
                      </MenuItem>
                    ))}
                  </Select>
                </FormControl>
              </TableCell>
            </TableRow>
          </TableBody>
        </Table>
      </TableContainer>
    );
  };
  const ShowInputColorFields = (props) => {
    const inputType = props.inputType;
    const options = props.options;
    const [value, setValue] = useState("");
    const [anchorEl, setAnchorEl] = React.useState(null);

    

    const handleClose = () => {
      setAnchorEl(null);
    };

    const isOpen = Boolean(anchorEl);
    const handleDs = (event) => {
      setAnchorEl(event.currentTarget);
    };
    if (nconfig !== null && nconfig !== undefined && value === "") {
      setValue(nconfig[componentId][inputType]);
    }

    const handleChange = (event) => {
      setValue(event.hex);
      // setValue(event.target.value);
      ////console.log("e.target.name :", event.target.name);
      customConfig[componentId][inputType] = event.hex;
    };

    return (
      <div style={{ textAlign: "center" }}>
        <Typography variant="body1">
          {`${inputType.split(/(?=[A-Z])/).join(" ")} `}
          <IconButton onClick={handleDs}>
            <IoIosColorPalette />
          </IconButton>
        </Typography>
        <Popover
          open={isOpen}
          anchorEl={anchorEl}
          onClose={handleClose}
          anchorOrigin={{
            vertical: "bottom",
            horizontal: "center",
          }}
          transformOrigin={{
            vertical: "top",
            horizontal: "center",
          }}
        >
          <div>
            <SketchPicker color={value} onChange={handleChange} />
          </div>
        </Popover>
        <Divider sx={{ my: 2 }} />
      </div>
    );
  };

  const ShowTextInputFields = (props) => {
    const { inputType } = props;
    const [inputValue, setInputValue] = useState("");

    useEffect(() => {
      if (nconfig !== null && nconfig !== undefined && inputValue === "") {
        setInputValue(nconfig[componentId][inputType]);
      }
    }, [nconfig, componentId, inputType, inputValue]);

    const handleChange = (event) => {
      setInputValue(event.target.value);
      customConfig[componentId][inputType] = event.target.value;
    };

    return (
      <TableContainer>
        <Table>
          <TableBody>
            <TableRow>
              <TableCell sx={{ minWidth: "200px" }}>
                <Typography variant="body1" sx={{ textAlign: "left" }}>
                  {`${inputType.split(/(?=[A-Z])/).join(" ")}`}
                </Typography>
              </TableCell>
              <TableCell>
                <TextField
                  value={inputValue}
                  onChange={handleChange}
                  variant="outlined"
                  label={`${inputType.split(/(?=[A-Z])/).join(" ")}`}
                  sx={{ textAlign: "center" }}
                />
              </TableCell>
            </TableRow>
          </TableBody>
        </Table>
      </TableContainer>
     
    );
  };
  interface Condition {
    columnName: string;
    columnCondition: string;
    columnValue: string;
  }
  const ShowFilterFormInputs = (props) => {
    const inputType = props.inputType;
    const options = props.options;
    const conditions = props.columnConditions;
    const handleSubmit = (output: Condition[]) => {
      let result: any[] = [];

      output.forEach((condition) => {
        // Assign values to the new object and push it to the result array
        if (
          condition.columnName &&
          condition.columnCondition &&
          condition.columnValue
        ) {
          result.push({
            columnName: condition.columnName,
            columnCondition: Number(condition.columnCondition),
            columnValue: eval(condition.columnValue).toString(),
          });
        }
      });

      // Log the result array
      //console.log("Result:", result);
      //console.log("Result String:", JSON.stringify(result));
      customConfig[componentId][`filtercondition`] = result;
    };

    return (
      <div>
        <FilterForm
          columnNames={options}
          columnCondition={conditions}
          componentName={componentName}
          onSubmit={handleSubmit}
          selectedConditons={nconfig[componentId][`filtercondition`]}
        ></FilterForm>
      </div>
    );
  };

  const ShowGroupInputFields1 = (props) => {
    const inputType = props.inputType;
    const options = props.options;
    //console.log(props);
    return (
      <FieldConfig
        nconfig={nconfig}
        componentId={componentId}
        customConfig={customConfig}
        options={options}
        inputType={inputType}
        errorList={props.errorList}
      />
    );
  };
  const ShowGroupInputsubFields1 = (props) => {
    const inputType = props.inputType;
    const options = props.options;
    //console.log(props);
    return (
      <SubFieldConfig
        nconfig={nconfig}
        componentId={componentId}
        customConfig={customConfig}
        options={options}
        inputType={inputType}
        errorList={props.errorList}
        groups={props.groups}
      />
    );
  };
  const ShowGroupInputFields2 = (props) => {
    const inputType = props.inputType;
    const options = props.options;
    //console.log(props);
    return (
      <DisplayConfig
        nconfig={nconfig}
        componentId={componentId}
        customConfig={customConfig}
        options={options}
        inputType={inputType}
        componentName={componentName}
        errorList={props.errorList}
      />
    );
  };
  const ShowGroupInputFields3 = (props) => {
    const inputType = props.inputType;
    const options = props.options;
    //console.log(props);
    return (
      <ErrorConfig
        nconfig={nconfig}
        componentId={componentId}
        customConfig={customConfig}
        options={options}
        inputType={inputType}
        errorList={props.errorList}
      />
    );
  };
  //Dev1
  //For mode change
  const [render, setRender] = useState(false);
  const ShowGroupInputFields4 = (props) => {
    const inputType = props.inputType;
    const options = props.options;
    const [flag, setflag] = useState(false);
    const changeRender = () => {
      setflag(!flag);
    };
    const [open, setOpen] = useState(false);

    const handleOpen = async () => {
      setOpen(true);
    };

    const handleClose = (): void => {
      //setProceed(true);

      setOpen(false);
    };
    const exportConfig = () => {
      const jsonConfig = JSON.stringify(nconfig[componentId], null, 2);

      //console.log(jsonConfig);

      const blob = new Blob([jsonConfig], { type: "application/json" });

      const url = URL.createObjectURL(blob);

      const a = document.createElement("a");

      a.href = url;

      a.download = "config.json";

      a.click();

      URL.revokeObjectURL(url);
    };
    if (
      customConfig[componentId][`mode`] === undefined ||
      customConfig[componentId][`mode`] === null
    ) {
      customConfig[componentId][`mode`] = "view mode";
      customConfig[componentId][`ui_mode`] = "view mode";
    }
    const [selectedValue, setSelectedValue] = useState(
      customConfig[componentId][`mode`]
    );
    const [selectedValue1, setSelectedValue1] = useState(
      customConfig[componentId][`ui_mode`]
    );
    const handleChange = (event) => {
      setSelectedValue(event.target.value);
      customConfig[componentId][`mode`] = event.target.value;
      changeRender();
    };
    const handleUIChange = (event) => {
      setSelectedValue1(event.target.value);
      customConfig[componentId][`ui_mode`] = event.target.value;
      // changeRender();
    };
    return (
      <Box className="p-1 mb-1">
        <FormControl component="fieldset" sx={{ paddingLeft: "20px" }}>
          <Typography variant="h6">Select a mode:</Typography>
          <RadioGroup
            aria-label="options"
            name="options"
            value={selectedValue}
            onChange={handleChange}
          >
            <FormControlLabel
              value="view mode"
              control={<Radio />}
              label="view mode"
            />
            <FormControlLabel
              value="create mode"
              control={<Radio />}
              label="create mode"
            />
          </RadioGroup>
        </FormControl>
        <FormControl component="fieldset" sx={{ paddingLeft: "20px" }}>
          <Typography variant="h6">Select a UI mode:</Typography>
          <RadioGroup
            aria-label="options"
            name="options"
            value={selectedValue1}
            onChange={handleUIChange}
          >
            <FormControlLabel
              value="view mode"
              control={<Radio />}
              label="view mode"
            />
            <FormControlLabel
              value="Edit mode"
              control={<Radio />}
              label="Edit mode"
            />
          </RadioGroup>
        </FormControl>
        <Box sx={{ flexDirection: "row" }}>
          <Box sx={{ p: 1 }}>
            <Button
              size="small"
              variant="contained"
              color="primary"
              onClick={() => exportConfig()} // Add your export function here
            >
              Export Config
            </Button>
          </Box>
          <Box sx={{ px: 1 }}>
            <Button
              size="small"
              variant="contained"
              color="secondary"
              onClick={() => handleOpen()} // Add your import function here
            >
              Import Config
            </Button>
          </Box>
        </Box>
        {open && (
          <ImportConfigDialog
            open={open}
            onClose={handleClose}
            nconfig={nconfig}
            componentId={componentId}
            customConfig={customConfig}
          />
        )}
      </Box>
    );
  };
  

  const ShowNavInputFields = (props) => {
    const inputType = props.inputType;
    const options = props.options;
    const navType = props.navList;
    //customConfig[componentId][`icon`] = "";
    const [selectedIcon, setSelectedIcon] = useState<string | null>(null);
    const [textAnchorEl, setTextanchorEl] = React.useState(null);
    const handleTextClose = () => {
      setTextanchorEl(null);
    };

    const isTextOpen = Boolean(textAnchorEl);
    const [buttonAnchorEl, setButtonanchorEl] = React.useState(null);
    const handleButtonClose = () => {
      setButtonanchorEl(null);
    };

    const isButtonOpen = Boolean(buttonAnchorEl);
    const handleIconSelect = (icon: string) => {
      customConfig[componentId][`icon`] = icon;
      setSelectedIcon(icon);
    };
    //console.log(props);
    if (customConfig[componentId][`nav_count`] === undefined)
      customConfig[componentId][`nav_count`] = 0;
    const [value, setValue] = useState("");
    const [rows, setRows] = useState(customConfig[componentId][`nav_count`]);
    const [flag, setflag] = useState(false);
    const changeRender = () => {
      setflag(!flag);
    };

    const [navPages, setNavPages] = useState(() => {
      // initialize navPages from customConfig
      let initialNavPages = [];
      for (let i = 0; i < rows; i++) {
        switch (customConfig[componentId][`nav_b_type`]) {
          case "page redirect":
            initialNavPages[i] = NavComponents["sidbarLinks"].flatMap((np) => {
              return np.submenu;
            });
            break;
          case "popup":
            initialNavPages[i] = NavComponents["popupLinks"];
            break;
          case "slide in drawer":
            initialNavPages[i] = NavComponents["drawerLinks"];
            break;
          case "components Pages":
            initialNavPages[i] = NavComponents["componentLinks"];
            break;
          default:
            initialNavPages[i] = null;
            break;
        }
      }
      return initialNavPages;
    });

    const addNavButton = () => {
      customConfig[componentId][`nav_count`] =
        customConfig[componentId][`nav_count`] + 1;
      setRows(rows + 1);
    };
    const reduceNavButton = () => {
      if (customConfig[componentId][`nav_count`] > 0) {
        customConfig[componentId][`nav_count`] =
          customConfig[componentId][`nav_count`] - 1;
        let newNavPages = [...navPages];
        newNavPages.pop();
        setNavPages(newNavPages);
        setRows(rows - 1);
      }
    };
    const handleNavSelction = (navType, rowIndex) => {
      setNavPages((prevNavPages) => {
        let newNavPages = [...prevNavPages]; // clone the current state

        switch (navType) {
          case "page redirect":
            newNavPages[rowIndex] = NavComponents["sidbarLinks"].flatMap(
              (np) => {
                return np.submenu;
              }
            );
            break;
          case "popup":
            newNavPages[rowIndex] = NavComponents["popupLinks"];
            break;
          case "slide in drawer":
            newNavPages[rowIndex] = NavComponents["drawerLinks"];
            break;
          case "components Pages":
            newNavPages[rowIndex] = NavComponents["componentLinks"];
            break;
          default:
            newNavPages[rowIndex] = null;
            break;
        }

        return newNavPages; // return the updated state
      });
    };

    const [button,setButton] = useState("");
    const [text,setText] = useState("");
    const handleChange = (event) => {
      setValue(event.target.value);
      ////console.log("e.target.name :", event.target.name);
      customConfig[componentId][inputType] = event.target.value;
    };

    const handleButtonColorChange = (color) => {
      // Handle button color change here
      console.log("Button color:", color);
    };

    const handleTextColorChange = (color) => {
      // Handle text color change here
      console.log("Text color:", color);
    };
    const handleButtonDs = (event) => {
      setButtonanchorEl(event.currentTarget);
    };
    const handleTextDs = (event) => {
      setTextanchorEl(event.currentTarget);
    };
    const defaultText = "#121828";
    const [iconNames, setIconNames] = useState([]);
    
    const [config,setConfig] = useState([]);
    const handleMultipleChange = (event, index) => {
      const selectedValues = event.target.value; // Array of selected values
      customConfig[componentId][`nav_${index}_column`] = selectedValues;
      setConfig((prevConfig) => ({
        ...prevConfig,
        [componentId]: {
          ...prevConfig[componentId],
          [`nav_${index}_column`]: selectedValues,
        },
      }));
    };
    const handleIconChange = (value: string, index: number) => {
      const updatedIconNames = [...iconNames];
      updatedIconNames[index] = value;
      setIconNames(updatedIconNames);
      customConfig[componentId][`nav_${index}_icon`] = value;
      //onChange(customConfig);
    };
    console.log(customConfig);
    return (
      <TableContainer
        component={Paper}
        style={{ maxWidth: "85vw", borderRadius: "15px", margin: "10px" }}
      >
        <Table>
          <TableHead>
            <TableRow>
              <TableCell>Column</TableCell>
              <TableCell>Navigation Type</TableCell>
              <TableCell>Available pages</TableCell>
              <TableCell>Pass the column value</TableCell>
              <TableCell>Icon Picker</TableCell>
              <TableCell>Button Label</TableCell>
              <TableCell>Button Color</TableCell>
              <TableCell>Text Color</TableCell>
            </TableRow>
          </TableHead>
          <TableBody>
            {Array.from({ length: rows }, (_, i) => (
              <TableRow key={i}>
                <TableCell>
                  <FormControl fullWidth>
                    <InputLabel id={`nav-${i}-col-select-label`}>
                      Select...
                    </InputLabel>
                    <Select
                      labelId={`${i}-col-select-label`}
                      value={customConfig[componentId][`nav_${i}_column`]}
                      onChange={(event) => {
                        customConfig[componentId][`nav_${i}_column`] =
                          event.target.value;
                      }}
                    >
                      {options.map((controlName) => (
                        <MenuItem key={controlName} value={controlName}>
                          {controlName}
                        </MenuItem>
                      ))}
                            </Select>
                            {/*<Select*/}
                            {/*    labelId={`${i}-col-select-label`}*/}
                            {/*    multiple*/}
                            {/*    value={*/}
                            {/*        config[componentId]*/}
                            {/*            ? config[componentId][`nav_${i}_column`] || []*/}
                            {/*            : []*/}
                            {/*    }*/}
                            {/*    onChange={(event) => handleMultipleChange(event, i)}*/}
                            {/*>*/}
                            {/*    {options.map((controlName) => (*/}
                            {/*        <MenuItem key={controlName} value={controlName}>*/}
                            {/*            {controlName}*/}
                            {/*        </MenuItem>*/}
                            {/*    ))}*/}
                            {/*</Select>*/}
                  </FormControl>
                </TableCell>
                <TableCell>
                  <FormControl fullWidth>
                    <InputLabel id={`nav-${i}-type-select-label`}>
                      Select...
                    </InputLabel>
                    <Select
                      labelId={`nav-${i}-type-select-label`}
                      value={customConfig[componentId][`nav_${i}_type`]}
                      onChange={(event) => {
                        customConfig[componentId][`nav_${i}_type`] =
                          event.target.value;
                        handleNavSelction(
                          customConfig[componentId][`nav_${i}_type`],
                          i
                        );
                      }}
                    >
                      {navType.map((controlName) => (
                        <MenuItem key={controlName} value={controlName}>
                          {controlName}
                        </MenuItem>
                      ))}
                    </Select>
                  </FormControl>
                </TableCell>
                <TableCell>
                  {navPages[i] && (
                    <FormControl fullWidth>
                      <InputLabel id={`nav-${i}-select-label`}>
                        Select...
                      </InputLabel>
                      <Select
                        labelId={`${i}-select-label`}
                        value={customConfig[componentId][`nav_${i}_page`]}
                        onChange={(event) => {
                          customConfig[componentId][`nav_${i}_page`] =
                            event.target.value;
                        }}
                      >
                        {navPages[i].map((controlName) => (
                          <MenuItem
                            key={controlName.name}
                            value={controlName.id}
                          >
                            {controlName.name}
                          </MenuItem>
                        ))}
                      </Select>
                    </FormControl>
                  )}
                </TableCell>
                <TableCell>
                  <Checkbox
                    checked={customConfig[componentId][`nav_${i}_pass_value`]}
                    onChange={(event) => {
                      customConfig[componentId][`nav_${i}_pass_value`] =
                        event.target.checked;
                    }}
                  />
                </TableCell>
                <TableCell>
                  <FontAwesomeIconPicker
                    value={customConfig[componentId][`nav_${i}_icon`]}
                    index={i}
                    onChange={handleIconChange}
                  />
                  {/* <IconPicker
                    onIconSelect={handleIconSelect}
                    value={customConfig[componentId][`nav_${i}_icon`]}
                  /> */}
                </TableCell>
                <TableCell>
                  <TextField
                    defaultValue={
                      customConfig[componentId][`nav_${i}_button_name`]
                    }
                    onChange={(event) =>
                      (customConfig[componentId][`nav_${i}_button_name`] =
                        event.target.value)
                    }
                  />
                </TableCell>
                <TableCell>
                  <div>
                    <Typography variant="body1">
                      <IconButton onClick={handleButtonDs}>
                        <IoIosColorPalette />
                      </IconButton>
                    </Typography>
                    <Popover
                      open={isButtonOpen}
                      anchorEl={buttonAnchorEl}
                      onClose={handleButtonClose}
                      anchorOrigin={{
                        vertical: "bottom",
                        horizontal: "center",
                      }}
                      transformOrigin={{
                        vertical: "top",
                        horizontal: "center",
                      }}
                    >
                      <div>
                        <SketchPicker
                          color={
                            customConfig[componentId][
                              `nav_${i}_button_color`
                            ] || "#5048E5"
                          }
                          onChange={(color) => {
                            customConfig[componentId][`nav_${i}_button_color`] =
                              color.hex;
                            setButton(color.hex);
                          }}
                        />
                      </div>
                    </Popover>
                  </div>
                </TableCell>
                <TableCell>
                  <div>
                    <Typography variant="body1">
                      <IconButton onClick={handleTextDs}>
                        <IoIosColorPalette />
                      </IconButton>
                    </Typography>
                    <Popover
                      open={isTextOpen}
                      anchorEl={textAnchorEl}
                      onClose={handleTextClose}
                      anchorOrigin={{
                        vertical: "bottom",
                        horizontal: "center",
                      }}
                      transformOrigin={{
                        vertical: "top",
                        horizontal: "center",
                      }}
                    >
                      <div>
                        <SketchPicker
                          color={
                            customConfig[componentId][`nav_${i}_text_color`] ||
                            defaultText
                          }
                          onChange={(color) => {
                            customConfig[componentId][`nav_${i}_text_color`] =
                              color.hex;
                            setText(color.hex);
                          }}
                        />
                      </div>
                    </Popover>
                  </div>
                </TableCell>
              </TableRow>
            ))}
            <TableRow>
              <Grid item xs={12}>
                <Button
                  variant="contained"
                  color="primary"
                  onClick={addNavButton}
                  style={{
                    margin: "10px",
                    fontSize: "0.8rem",
                  }}
                >
                  Add BUTTON
                </Button>
                <IconButton
                  style={{
                    margin: "10px",
                    backgroundColor: "red",
                    color: "white",
                  }}
                  onClick={reduceNavButton}
                >
                  <RemoveIcon style={{ fontSize: 20 }} />
                </IconButton>
              </Grid>
            </TableRow>
          </TableBody>
        </Table>
      </TableContainer>
    );
  };

  

  const ShowNavInputFields1 = (props) => {
    const inputType = props.inputType;
    const navType = props.navList;
    //console.log(props);
    const [value, setValue] = useState("");
    const [flag, setflag] = useState(false);
    const changeRender = () => {
      setflag(!flag);
    };
    const [navPages, setNavPages] = useState([]);
    const handleNavSelction = (navType) => {
      setNavPages((prevNavPages) => {
        let newNavPages = [...prevNavPages]; // clone the current state

        switch (navType) {
          case "page redirect":
            newNavPages = NavComponents["sidbarLinks"].flatMap((np) => {
              return np.submenu;
            });
            break;
          case "popup":
            newNavPages = NavComponents["popupLinks"];
            break;
          case "slide in drawer":
            newNavPages = NavComponents["drawerLinks"];
            break;
          case "components Pages":
            newNavPages = NavComponents["componentLinks"];
            break;
          default:
            newNavPages = null;
            break;
        }

        return newNavPages; // return the updated state
      });
    };

    const handleChange = (event) => {
      setValue(event.target.value);
      ////console.log("e.target.name :", event.target.name);
      customConfig[componentId][inputType] = event.target.value;
    };
    return (
      <TableContainer
        component={Paper}
        style={{
          maxWidth: "85vw",
          borderRadius: "15px",
          margin: "10px 10px",
        }}
      >
        <Table>
          <TableHead>
            <TableRow>
              <TableCell>Navigation Type</TableCell>
              <TableCell>Available pages</TableCell>
              <TableCell>Button Label</TableCell>
            </TableRow>
          </TableHead>
          <TableBody>
            <TableRow key={"i"}>
              <TableCell>
                <FormControl>
                  <InputLabel id={`nav-type-select-label`}>
                    Select...
                  </InputLabel>
                  <Select
                    fullWidth
                    labelId={`nav-b-type-select-label`}
                    defaultValue={customConfig[componentId][`nav_type`]}
                    onChange={(event) => {
                      customConfig[componentId][`nav_type`] =
                        event.target.value;
                      handleNavSelction(customConfig[componentId][`nav_type`]);
                    }}
                  >
                    {navType.map((controlName) => (
                      <MenuItem key={controlName} value={controlName}>
                        {controlName}
                      </MenuItem>
                    ))}
                  </Select>
                </FormControl>
              </TableCell>
              <TableCell>
                {navPages && (
                  <FormControl>
                    <InputLabel id={`nav-b-select-label`}>Select...</InputLabel>
                    <Select
                      fullWidth
                      labelId={`b-select-label`}
                      defaultValue={customConfig[componentId][`nav_b_page`]}
                      onChange={(event) => {
                        customConfig[componentId][`nav_b_page`] =
                          event.target.value;
                      }}
                    >
                      {navPages.map((controlName) => (
                        <MenuItem key={controlName.name} value={controlName.id}>
                          {controlName.name}
                        </MenuItem>
                      ))}
                    </Select>
                  </FormControl>
                )}
              </TableCell>
              <TableCell>
                <TextField
                  defaultValue={customConfig[componentId][`nav_button_name`]}
                  onChange={(event) =>
                    (customConfig[componentId][`nav_button_name`] =
                      event.target.value)
                  }
                />
              </TableCell>
            </TableRow>
          </TableBody>
        </Table>
      </TableContainer>
    );
  };

  const ShowNavInputFields2 = (props) => {
    const inputType = props.inputType;
    const navType = props.navList;
    if (customConfig[componentId][`tab_nav_count`] === undefined)
      customConfig[componentId][`tab_nav_count`] = 0;
    const [value, setValue] = useState("");
    const [rows, setRows] = useState(
      customConfig[componentId][`tab_nav_count`]
    );
    const [flag, setflag] = useState(false);
    const changeRender = () => {
      setflag(!flag);
    };

    const [navPages, setNavPages] = useState(() => {
      // initialize navPages from customConfig
      let initialNavPages = [];
      for (let i = 0; i < rows; i++) {
        switch (customConfig[componentId][`nav_b_type`]) {
          case "page redirect":
            initialNavPages[i] = NavComponents["sidbarLinks"].flatMap((np) => {
              return np.submenu;
            });
            break;
          case "popup":
            initialNavPages[i] = NavComponents["popupLinks"];
            break;
          case "slide in drawer":
            initialNavPages[i] = NavComponents["drawerLinks"];
            break;
          case "components Pages":
            initialNavPages[i] = NavComponents["componentLinks"];
            break;
          default:
            initialNavPages[i] = null;
            break;
        }
      }
      return initialNavPages;
    });

    const addNavButton = () => {
      customConfig[componentId][`tab_nav_count`] =
        customConfig[componentId][`tab_nav_count`] + 1;
      setRows(rows + 1);
    };
    const reduceNavButton = () => {
      if (customConfig[componentId][`tab_nav_count`] > 0) {
        customConfig[componentId][`tab_nav_count`] =
          customConfig[componentId][`tab_nav_count`] - 1;
        let newNavPages = [...navPages];
        newNavPages.pop();
        setNavPages(newNavPages);
        setRows(rows - 1);
      }
    };
    const handleNavSelction = (navType, rowIndex) => {
      setNavPages((prevNavPages) => {
        let newNavPages = [...prevNavPages]; // clone the current state

        switch (navType) {
          case "page redirect":
            newNavPages[rowIndex] = NavComponents["sidbarLinks"].flatMap(
              (np) => {
                return np.submenu;
              }
            );
            break;
          case "popup":
            newNavPages[rowIndex] = NavComponents["popupLinks"];
            break;
          case "slide in drawer":
            newNavPages[rowIndex] = NavComponents["drawerLinks"];
            break;
          case "components Pages":
            newNavPages[rowIndex] = NavComponents["componentLinks"];
            break;
          default:
            newNavPages[rowIndex] = null;
            break;
        }

        return newNavPages; // return the updated state
      });
    };

    const handleChange = (event) => {
      setValue(event.target.value);
      ////console.log("e.target.name :", event.target.name);
      customConfig[componentId][inputType] = event.target.value;
    };
    return (
      <TableContainer
        component={Paper}
        style={{
          maxWidth: "85vw",
          borderRadius: "15px",
          margin: "10px 10px",
        }}
      >
        <Table>
          <TableHead>
            <TableRow>
              <TableCell>Navigation Type</TableCell>
              <TableCell>Available pages</TableCell>
              <TableCell>Button Label</TableCell>
            </TableRow>
          </TableHead>
          <TableBody>
            {Array.from({ length: rows }, (_, i) => (
              <TableRow key={i}>
                <TableCell>
                  <FormControl>
                    <InputLabel id={`tab_nav-${i}-type-select-label`}>
                      Select...
                    </InputLabel>
                    <Select
                      fullWidth
                      labelId={`tab_nav-${i}-type-select-label`}
                      defaultValue={
                        customConfig[componentId][`tab_nav_${i}_type`]
                      }
                      onChange={(event) => {
                        customConfig[componentId][`tab_nav_${i}_type`] =
                          event.target.value;
                        handleNavSelction(
                          customConfig[componentId][`tab_nav_${i}_type`],
                          i
                        );
                      }}
                    >
                      {navType.map((controlName) => (
                        <MenuItem key={controlName} value={controlName}>
                          {controlName}
                        </MenuItem>
                      ))}
                    </Select>
                  </FormControl>
                </TableCell>
                <TableCell>
                  {navPages[i] && (
                    <FormControl>
                      <InputLabel id={`tab_nav-${i}-select-label`}>
                        Select...
                      </InputLabel>
                      <Select
                        fullWidth
                        labelId={`${i}-select-label`}
                        defaultValue={
                          customConfig[componentId][`tab_nav_${i}_page`]
                        }
                        onChange={(event) => {
                          customConfig[componentId][`tab_nav_${i}_page`] =
                            event.target.value;
                        }}
                      >
                        {navPages[i].map((controlName) => (
                          <MenuItem
                            key={controlName.name}
                            value={controlName.id}
                          >
                            {controlName.name}
                          </MenuItem>
                        ))}
                      </Select>
                    </FormControl>
                  )}
                </TableCell>
                <TableCell>
                  <TextField
                    defaultValue={
                      customConfig[componentId][`tab_nav_${i}_button_name`]
                    }
                    onChange={(event) =>
                      (customConfig[componentId][`tab_nav_${i}_button_name`] =
                        event.target.value)
                    }
                  />
                </TableCell>
              </TableRow>
            ))}
            <TableRow>
              <TableCell colSpan={5}>
                <Grid container justifyContent="space-between">
                  <Grid item>
                    <Button
                      variant="contained"
                      color="primary"
                      onClick={addNavButton}
                      style={{ fontSize: "0.8rem" }}
                    >
                      Add BUTTON
                    </Button>
                  </Grid>
                  <Grid item>
                    <IconButton color="error" onClick={reduceNavButton}>
                      <Tooltip title="Remove Row">
                        <DeleteTwoToneIcon />
                      </Tooltip>
                    </IconButton>
                  </Grid>
                </Grid>
              </TableCell>
            </TableRow>
          </TableBody>
        </Table>
      </TableContainer>
    );
  };

  const handleSubmit = (event) => {
    event.preventDefault();
    ////console.log("Submitted Configuration: ", config);
    //console.log("Configurations :- ", customConfig);
    props.handleConfigurationChange(customConfig);
    props.handleClose();
  };

  const setInputUsingConfig = () => {
    {
      /* controllerKey = Button , Checkbox , Input , List , etc  */
    }
    {
      /* controllerValue = content of that controllerKey , like for that Button => type , cssClass , onClick etc... */
    }
    //console.log(Configurations);
    Object.entries(Configurations).forEach(
      ([controllerKey, controllerValue]) => {
        if (controllerKey === props.componentType) {
          // controllerOptionsKey => type , cssClass , onClick for Button
          // controllerOptionsValue => list of options of drop down like object which has a {input-type ,options}
          Object.entries(controllerValue).forEach(
            ([controllerOptionsKey, controllerOptionsValue]) => {
              //const inputType = controllerOptionsValue["iaccumulatedJSXElementsChart11"];
              const inputType = controllerOptionsValue["input-type"];
              //console.log(inputType);
              //console.log(controllerOptionsKey);
              //console.log(controllerOptionsValue);
              //Dev1Chart
              if (inputType === "chart") {
                const optionList = controllerOptionsValue["options"];
                //console.log("hello");
                // inputFieldsJSXChart1.push(
                //   <VisibleConfig
                //     nconfig={nconfig}
                //     componentId={componentId}
                //     customConfig={customConfig}
                //     options={optionList}
                //   />
                // );
                inputFieldsJSXChart1.push(
                  <ChartSelector
                    nconfig={nconfig}
                    componentId={componentId}
                    customConfig={customConfig}
                    options={optionList}
                  />
                );
              }
              if (inputType === "color-selection") {
                const optionList = controllerOptionsValue["options"];
                inputFieldsJSXChart11.push(
                  <ColorConfig
                    nconfig={nconfig}
                    componentId={componentId}
                    customConfig={customConfig}
                    options={optionList}
                  />
                );
              }
              if (inputType === "list") {
                const optionList = controllerOptionsValue["options"];
                inputFieldsJSX1.push(
                  <ShowInputFields
                    inputType={controllerOptionsKey}
                    options={optionList}
                    npConfig={customConfig}
                  />
                );
              }
              if (inputType === "heading-color") {
                const optionList = controllerOptionsValue["options"];
                inputFieldsJSX11.push(
                  <ShowInputColorFields
                    inputType={controllerOptionsKey}
                    options={optionList}
                    npConfig={customConfig}
                  />
                );
              }
              if (inputType === "table-head-color") {
                const optionList = controllerOptionsValue["options"];
                inputFieldsJSX12.push(
                  <ShowInputColorFields
                    inputType={controllerOptionsKey}
                    options={optionList}
                    npConfig={customConfig}
                  />
                );
              }
              if (inputType === "table-color") {
                const optionList = controllerOptionsValue["options"];
                inputFieldsJSX13.push(
                  <ShowInputColorFields
                    inputType={controllerOptionsKey}
                    options={optionList}
                    npConfig={customConfig}
                  />
                );
              }
              if (inputType === "global-configuration") {
                const optionList = controllerOptionsValue["options"];
                inputFieldsJSX15.push(
                  <ShowInputColorFields
                    inputType={controllerOptionsKey}
                    options={optionList}
                    npConfig={customConfig}
                  />
                );
              }
              if (inputType === "row-color") {
                const optionList = controllerOptionsValue["options"];
                inputFieldsJSX14.push(
                  <ShowInputColorFields
                    inputType={controllerOptionsKey}
                    options={optionList}
                    npConfig={customConfig}
                  />
                );
              }

              if (inputType === "group") {
                const columnList = controllerOptionsValue["columns-list"];
                const errorList = controllerOptionsValue["error-control-list"];
                inputFieldsJSX21.push(
                  <ShowGroupInputFields1
                    inputType={controllerOptionsKey}
                    options={columnList}
                    npConfig={customConfig}
                    errorList={errorList}
                  />
                );
                inputFieldsJSX22.push(
                  <ShowGroupInputFields2
                    inputType={controllerOptionsKey}
                    options={columnList}
                    npConfig={customConfig}
                    errorList={errorList}
                  />
                );
                inputFieldsJSX23.push(
                  <ShowGroupInputFields3
                    inputType={controllerOptionsKey}
                    options={columnList}
                    npConfig={customConfig}
                    errorList={errorList}
                  />
                );
                inputFieldsJSX24.push(
                  <ShowGroupInputFields4
                    inputType={controllerOptionsKey}
                    options={columnList}
                    npConfig={customConfig}
                    errorList={errorList}
                  />
                );
              }
              if (inputType == "subGroup") {
                const subGroup = controllerOptionsValue["subGroupColumns"];
                Object.keys(subGroup).map((groups) => {
                  //console.log(subGroup[groups]);
                  const columnList = subGroup[groups]["columns-list"];
                  const errorList = subGroup[groups]["error-control-list"];
                  inputFieldsJSX5.push(
                    <Box className="p-1 mb-1" sx={{ textAlign: "center" }}>
                      <Typography variant="h6" component="div">
                        Configuration: {groups}
                        <Box component="small" className="p-2"></Box>
                        <Tooltip
                          title="Configure sub Group Field"
                          placement="right"
                        >
                          <Help
                            id="help-21"
                            style={{ fontSize: 15, marginLeft: "8px" }}
                          />
                        </Tooltip>
                      </Typography>
                    </Box>
                  );
                  inputFieldsJSX5.push(
                    <ShowGroupInputsubFields1
                      inputType={controllerOptionsKey}
                      options={columnList}
                      npConfig={customConfig}
                      errorList={errorList}
                      groups={groups}
                    />
                  );
                  inputFieldsJSX51.push(
                    <Box className="p-1 mb-1" sx={{ textAlign: "center" }}>
                      <Typography variant="h6" component="div">
                        Configuration: Validation Control({groups})
                        <Box component="small" className="p-2"></Box>
                        <Tooltip
                          title="Configure sub Group Field's validation"
                          placement="right"
                        >
                          <Help
                            id="help-31"
                            style={{ fontSize: 15, marginLeft: "8px" }}
                          />
                        </Tooltip>
                      </Typography>
                    </Box>
                  );
                  inputFieldsJSX51.push(
                    <ShowGroupInputFields3
                      inputType={controllerOptionsKey}
                      options={columnList}
                      npConfig={customConfig}
                      errorList={errorList}
                    />
                  );
                });
              }
              if (inputType === "filter-form") {
                const columnList = controllerOptionsValue["columns-list"];
                const columnConditionsList =
                  controllerOptionsValue["column-condition"];
                inputFieldsJSX3.push(
                  <ShowFilterFormInputs
                    inputType={controllerOptionsKey}
                    options={columnList}
                    columnConditions={columnConditionsList}
                  />
                );
              } else if (inputType === "text") {
                inputFieldsJSX1.push(
                  <ShowTextInputFields
                    npConfig={customConfig}
                    inputType={controllerOptionsKey}
                  />
                );
              } else if (inputType === "nav") {
                const columnList = controllerOptionsValue["columns-list"];
                const navList = controllerOptionsValue["nav-list"];
                inputFieldsJSX4.push(
                  <ShowNavInputFields
                    inputType={controllerOptionsKey}
                    options={columnList}
                    npConfig={customConfig}
                    navList={navList}
                  />
                );
              } else if (inputType === "button-nav") {
                const navList = controllerOptionsValue["nav-list"];
                inputFieldsJSX4.push(
                  <ShowNavInputFields1
                    inputType={controllerOptionsKey}
                    npConfig={customConfig}
                    navList={navList}
                  />
                );
              } else if (inputType === "tab-nav") {
                const navList = controllerOptionsValue["nav-list"];
                inputFieldsJSX4.push(
                  <ShowNavInputFields2
                    inputType={controllerOptionsKey}
                    npConfig={customConfig}
                    navList={navList}
                  />
                );
              } else {
                //console.log("Nothing! but this : ", controllerOptionsValue);
              }
            }
          );
        }
      }
    );
  };

  let accumulatedJSXElements1 = null;
  let accumulatedJSXElements11 = null;
  let accumulatedJSXElements12 = null;
  let accumulatedJSXElements13 = null;
  let accumulatedJSXElements14 = null;
  let accumulatedJSXElements15 = null;
  let accumulatedJSXElements21 = null;
  let accumulatedJSXElements22 = null;
  let accumulatedJSXElements23 = null;
  let accumulatedJSXElements24 = null;
  let accumulatedJSXElements3 = null;
  let accumulatedJSXElements4 = null;
  let accumulatedJSXElements5 = null;
  let accumulatedJSXElements51 = null;
  let accumulatedJSXElementsChart1 = null;
  let accumulatedJSXElementsChart11 = null;
  let accumulatedJSXElementsChart12 = null;
  //console.log(accumulatedJSXElementsChart11);
  const [selectedTab, setSelectedTab] = useState("Mode");
  const handleTabChange = (tabName: string) => {
    setSelectedTab(tabName);
  };
  return (
    //     import {
    //   Button,
    //   Col,
    //   Container,
    //   Row,
    //   FormControl,
    //   InputLabel,
    //   MenuItem,
    //   Select,
    //   TextField
    // } from '@mui/material';

    <SwipeableDrawer
      anchor="right"
      open={true}
      onClose={props.handleClose}
      onOpen={props.handleClose}
    >
      {/* <div className="config-popup-box">
      <div className="config-box">
        <span className="config-close-icon" onClick={props.handleClose}>
          &#x2718;
        </span> */}
      {setInputUsingConfig()}
      {(() => {
        accumulatedJSXElements1 = inputFieldsJSX1.map((element, index) => (
          <div key={index}>{element}</div>
        ));
        accumulatedJSXElements11 = inputFieldsJSX11.map((element, index) => (
          <div key={index}>{element}</div>
        ));
        accumulatedJSXElements12 = inputFieldsJSX12.map((element, index) => (
          <div key={index}>{element}</div>
        ));
        accumulatedJSXElements13 = inputFieldsJSX13.map((element, index) => (
          <div key={index}>{element}</div>
        ));
        accumulatedJSXElements14 = inputFieldsJSX14.map((element, index) => (
          <div key={index}>{element}</div>
        ));
        accumulatedJSXElements15 = inputFieldsJSX15.map((element, index) => (
          <div key={index}>{element}</div>
        ));
        accumulatedJSXElements21 = inputFieldsJSX21.map((element, index) => (
          <div key={index}>{element}</div>
        ));
        accumulatedJSXElements22 = inputFieldsJSX22.map((element, index) => (
          <div key={index}>{element}</div>
        ));
        accumulatedJSXElements23 = inputFieldsJSX23.map((element, index) => (
          <div key={index}>{element}</div>
        ));
        accumulatedJSXElements24 = inputFieldsJSX24.map((element, index) => (
          <div key={index}>{element}</div>
        ));
        accumulatedJSXElements3 = inputFieldsJSX3.map((element, index) => (
          <div key={index}>{element}</div>
        ));
        accumulatedJSXElements4 = inputFieldsJSX4.map((element, index) => (
          <div key={index}>{element}</div>
        ));
        accumulatedJSXElements5 = inputFieldsJSX5.map((element, index) => (
          <div key={index}>{element}</div>
        ));
        accumulatedJSXElements51 = inputFieldsJSX51.map((element, index) => (
          <div key={index}>{element}</div>
        ));
        accumulatedJSXElementsChart1 = inputFieldsJSXChart1.map(
          (element, index) => <div key={index}>{element}</div>
        );
        accumulatedJSXElementsChart11 = inputFieldsJSXChart11.map(
          (element, index) => <div key={index}>{element}</div>
        );
      })()}
      <div className="configuration-popup">
        <TabNavigation
          selectedTab={selectedTab}
          onTabChange={handleTabChange}
          customConfig={customConfig}
          componentId={componentId}
        />
        {selectedTab === "General" && inputFieldsJSX1.length !== 0 && (
          <>
            <Box className="p-1 mb-1" sx={{ textAlign: "center" }}>
              <Typography variant="h6" component="div">
                Configuration: Charts
                <Box component="small" className="p-2"></Box>
                <Tooltip title="Chart Heading Configuration" placement="right">
                  <Help
                    id="help-41"
                    style={{ fontSize: 15, marginLeft: "8px" }}
                  />
                </Tooltip>
              </Typography>
            </Box>
            <div>{accumulatedJSXElements1}</div>
          </>
        )}
        {selectedTab === "General" && inputFieldsJSXChart1.length !== 0 && (
          <>
            <Box className="p-1 mb-1" sx={{ textAlign: "center" }}>
              <Typography variant="h6" component="div">
                Configuration: Charts
                <Box component="small" className="p-2"></Box>
                <Tooltip title="Chart Configuration" placement="right">
                  <Help
                    id="help-41"
                    style={{ fontSize: 15, marginLeft: "8px" }}
                  />
                </Tooltip>
              </Typography>
            </Box>
            <div>{accumulatedJSXElementsChart1}</div>
          </>
        )}

        {selectedTab === "General" && inputFieldsJSX11.length !== 0 && (
          <>
            <Box className="p-1 mb-1" sx={{ textAlign: "center" }}>
              <Typography variant="h6" component="div">
                Configuration: Heading Color
                <Box component="small" className="p-2"></Box>
                <Tooltip title="configure heading color" placement="right">
                  <Help
                    id="help-1"
                    style={{ fontSize: 15, marginLeft: "8px" }}
                  />
                </Tooltip>
              </Typography>
            </Box>
            <div>{accumulatedJSXElements11}</div>
          </>
        )}
        {selectedTab === "General" && inputFieldsJSX12.length !== 0 && (
          <>
            <Box className="p-1 mb-1" sx={{ textAlign: "center" }}>
              <Typography variant="h6" component="div">
                Configuration : Table Heading Color
                <Box component="small" className="p-2"></Box>
                <Tooltip
                  title="configure table heading color"
                  placement="right"
                >
                  <Help
                    id="help-2"
                    style={{ fontSize: 15, marginLeft: "8px" }}
                  />
                </Tooltip>
              </Typography>
            </Box>
            <div>{accumulatedJSXElements12}</div>
          </>
        )}
        {selectedTab === "General" && inputFieldsJSX13.length !== 0 && (
          <>
            <Box className="p-1 mb-1" sx={{ textAlign: "center" }}>
              <Typography variant="h6" component="div">
                Configuration : Table Color
                <Box component="small" className="p-2"></Box>
                <Tooltip title="configure table color" placement="right">
                  <Help
                    id="help-3"
                    style={{ fontSize: 15, marginLeft: "8px" }}
                  />
                </Tooltip>
              </Typography>
            </Box>
            <div>{accumulatedJSXElements13}</div>
          </>
        )}
        {selectedTab === "General" && inputFieldsJSX14.length !== 0 && (
          <>
            <Box className="p-1 mb-1" sx={{ textAlign: "center" }}>
              <Typography variant="h6" component="div">
                Configuration : Table Properties Color
                <Box component="small" className="p-2"></Box>
                <Tooltip
                  title="configure table properties color"
                  placement="right"
                >
                  <Help
                    id="help-4"
                    style={{ fontSize: 15, marginLeft: "8px" }}
                  />
                </Tooltip>
              </Typography>
            </Box>
            <div>{accumulatedJSXElements14}</div>
          </>
        )}
        {inputFieldsJSX15.length !== 0 && (
          <>
            <Box className="p-1 mb-1" sx={{ textAlign: "center" }}>
              <Typography variant="h6" component="div">
                Configuration : Global Color
                <Box component="small" className="p-2"></Box>
                <Tooltip title="configure global color" placement="right">
                  <Help
                    id="help-42"
                    style={{ fontSize: 15, marginLeft: "8px" }}
                  />
                </Tooltip>
              </Typography>
            </Box>
            <div>{accumulatedJSXElements15}</div>
          </>
        )}
        {selectedTab === "Navigation" && inputFieldsJSX4.length !== 0 && (
          <>
            <Box className="p-1 mb-1" sx={{ textAlign: "center" }}>
              <Typography variant="h6" component="div">
                Configuration : Navigations
                <Box component="small" className="p-2"></Box>
                <Tooltip title="configure column" placement="right">
                  <Help
                    id="help-52"
                    style={{ fontSize: 15, marginLeft: "8px" }}
                  />
                </Tooltip>
              </Typography>
            </Box>
            <div>{accumulatedJSXElements4}</div>
          </>
        )}
        {selectedTab === "Create" &&
          customConfig[componentId][`mode`] === "create mode" &&
          inputFieldsJSX21.length !== 0 && (
            <>
              <Box className="p-1 mb-1" sx={{ textAlign: "center" }}>
                <Typography variant="h6" component="div">
                  Configuration : Create Mode
                  <Box component="small" className="p-2"></Box>
                  <Tooltip title="configure column" placement="right">
                    <Help
                      id="help-53"
                      style={{ fontSize: 15, marginLeft: "8px" }}
                    />
                  </Tooltip>
                </Typography>
              </Box>
              <div>{accumulatedJSXElements21}</div>
            </>
          )}
        {selectedTab === "Create" &&
          customConfig[componentId][`mode`] === "create mode" &&
          inputFieldsJSX5.length !== 0 && (
            <>
              <div>{accumulatedJSXElements5}</div>
            </>
          )}
        {selectedTab === "View" &&
          customConfig[componentId][`mode`] === "view mode" &&
          inputFieldsJSX22.length !== 0 && (
            <>
              <Box className="p-1 mb-1" sx={{ textAlign: "center" }}>
                <Typography variant="h6" component="div">
                  Configuration : View Control
                  <Box component="small" className="p-2"></Box>
                  <Tooltip title="configure column" placement="right">
                    <Help
                      id="help-54"
                      style={{ fontSize: 15, marginLeft: "8px" }}
                    />
                  </Tooltip>
                </Typography>
              </Box>
              <div>{accumulatedJSXElements22}</div>
            </>
          )}
        {selectedTab === "Validation" &&
          customConfig[componentId][`mode`] === "create mode" &&
          inputFieldsJSX23.length !== 0 && (
            <>
              <Box className="p-1 mb-1" sx={{ textAlign: "center" }}>
                <Typography variant="h6" component="div">
                  Configuration : Validation Control
                  <Box component="small" className="p-2"></Box>
                  <Tooltip title="configure column" placement="right">
                    <Help
                      id="help-56"
                      style={{ fontSize: 15, marginLeft: "8px" }}
                    />
                  </Tooltip>
                </Typography>
              </Box>
              <div>{accumulatedJSXElements23}</div>
            </>
          )}
        {selectedTab === "Validation" &&
          customConfig[componentId][`mode`] === "create mode" &&
          inputFieldsJSX51.length !== 0 && (
            <>
              <div>{accumulatedJSXElements51}</div>
            </>
          )}
        {selectedTab === "Mode" && inputFieldsJSX24.length !== 0 && (
          <>
            <Box className="p-1 mb-1" sx={{ textAlign: "center" }}>
              {/* <Typography variant="h6" component="div">
                Configuration : Validation Control
                <Box component="small" className="p-2"></Box>
                <Tooltip title="configure column" placement="right">
                  <Help id="help-5" />
                </Tooltip>
              </Typography> */}
            </Box>
            <div>{accumulatedJSXElements24}</div>
          </>
        )}
        {selectedTab === "Filter" && inputFieldsJSX3.length !== 0 && (
          <>
            <Box className="p-1 mb-1" sx={{ textAlign: "center" }}>
              <Typography variant="h6" component="div">
                Configuration : Filter
                <Box component="small" className="p-2"></Box>
                <Tooltip title="configure filter" placement="right">
                  <Help
                    id="help-6"
                    style={{ fontSize: 15, marginLeft: "8px" }}
                  />
                </Tooltip>
              </Typography>
            </Box>
            <div>{accumulatedJSXElements3}</div>
          </>
        )}
        {selectedTab === "Save&Exit" && (
          <Box className="p-2 mb-2" sx={{ textAlign: "center" }}>
            <Button
              variant="contained"
              className="btn btn-primary"
              onClick={handleSubmit}
            >
              Save & Exit
            </Button>
          </Box>
        )}
      </div>
      {/* /</div> */}
      {/* {console.log("inputFieldsJSX :", inputFieldsJSX)} */}
      {/* </div> */}
    </SwipeableDrawer>

  );
};

const style = {};

const Component = ({
  data,
  components,
  path,
  openLink,
  openTabLink,
  configurations,
  LayoutList,
  handleConfigurationChange,
}) => {
  customConfig = configurations;
  //console.log(data);

  const ref = useRef(null);
  const [{ isDragging }, drag] = useDrag({
    type: COMPONENT,
    item: { type: COMPONENT, id: data.id, path },
    collect: (monitor) => ({
      isDragging: monitor.isDragging(),
    }),
  });

  const opacity = isDragging ? 0.3 : 1;
  drag(ref);
  const Component = components[data.id];
  //console.log(Component.type);
  if (customConfig[data.id]) customConfig[data.id]["componentInfo"] = Component;
  //console.log(customConfig);

  const [clicked, setClicked] = useState(false);
  const [blurred, setBlurred] = useState(false);
  const [open, setOpen] = useState(false);
  const [state, setState] = useState(false);

  function onFocus(component) {
    setClicked(true);
    setBlurred(false);
    setOpen(true);
    ////console.log("Clicked on Component : ", component);
  }
  function onBlur(event, component) {
    if (event.relatedTarget && ref.current.contains(event.relatedTarget)) {
      ////console.log("NOT Blur on Component : ", component);
      // The focus has moved to a child element of the excluded component, so do nothing
    } else {
      // The focus has moved to an element outside the excluded component, so do something
      setBlurred(true);
      setClicked(false);
      setOpen(false);
      ////console.log("Blur on Component : ", component);
    }
    // setBlurred(true);
    // setClicked(false);
    // setOpen(false);
    ////console.log("onBlur Event :- ", event);
    ////console.log("Blur on Component : ", component);
  }

  function handleClose() {
    setOpen(false);
  }

  const toggleDrawer =
    (open: boolean) => (event: React.KeyboardEvent | React.MouseEvent) => {
      if (
        event &&
        event.type === "keydown" &&
        ((event as React.KeyboardEvent).key === "Tab" ||
          (event as React.KeyboardEvent).key === "Shift")
      ) {
        return;
      }
      setState(open);
    };

  // function onSelect(args) {
  //    //console.log("Select on Component : ", args);
  // }
  // function onMouseEnter(args) {
  //    //console.log("Mouse Enter on Component : ", args)
  // }
  return (
    <React.StrictMode>
      <div
        ref={ref}
        style={{ ...style, opacity }}
        className={
          clicked === true && blurred === false
            ? "dnd component draggable selected"
            : blurred === true && clicked === false
            ? "dnd component draggable"
            : "dnd component draggable"
        }
        // className="dnd component draggable"
        // onFocus={() => { setBlurred(false); setClicked(true) }}
      >
        {(() => {
          if (
            customConfig[data.id] === undefined ||
            customConfig[data.id] === null
          )
            customConfig[data.id] = {};
        })()}
        {open && (
          <Popup
            nconfig={customConfig}
            content={
              <>
                <h3>Configuration : {Component.type}</h3>
              </>
            }
            componentType={Component.type}
            componentId={data.id}
            LayoutList={LayoutList}
            componentName={Component.component_name}
            handleConfigurationChange={handleConfigurationChange}
            handleClose={() => {
              setOpen(!open);
              setClicked(false);
              setBlurred(true);
            }}
          />
        )}
        {/* {console.log("from Component.tsx : ", Component)} */}
        {/* <div>{data.id}</div> */}
        {/* {(Component !== undefined && Component.content !== undefined) ? ((CRUD_Component_List.includes(Component.type) || SyncFusion_Component_List.includes(Component.type) || MUI_Component_List.includes(Component.type)) ? <Component.content /> : Component.content) : <p>Can't Render Component</p>} */}
        <div
          onDoubleClick={() => onFocus(Component)}
          // onFocus={() => onFocus(Component)}
          onBlur={(event) => onBlur(event, Component)}
        >
          {/* {console.log(Component)} */}
          {functionTOmap(
            Component.type,
            customConfig[data.id],
            openLink,
            "",
            "",
            openTabLink
          )}
          {/* {mapNametoComponent[Component.content]} */}
        </div>
        {/* {console.log(Component.content)} */}
      </div>
    </React.StrictMode>
  );
};
export default Component;

{
  /* {(<Popup
    show={modalShow}
    onHide={() => { setModalShow(false) }}
/>)} */
}
