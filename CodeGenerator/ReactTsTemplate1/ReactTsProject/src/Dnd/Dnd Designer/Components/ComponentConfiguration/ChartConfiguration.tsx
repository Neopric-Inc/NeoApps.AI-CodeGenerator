import React, { useState, useEffect } from "react";
import { useAppDispatch } from "redux/store";
// import Tooltip from "@mui/material/Tooltip";
import IconButton from "@mui/material/IconButton";
import { SketchPicker } from "react-color";
import { IoIosColorPalette } from "react-icons/io";
import {
  Checkbox,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  Paper,
  FormControl,
  InputLabel,
  Select,
  MenuItem,
  Typography,
  Popover,
} from "@mui/material";
import { getAllChartData } from "services/flowService";

const dataset1 = null;

export const ChartSelector = ({
  nconfig,
  componentId,
  customConfig,
  options,
}) => {
  const [apidata, setApidata] = useState({});
  const [chartOptions, setChartOptions] = useState([]);
  useEffect(() => {
    const getData = async () => {
      const resp = (await getAllChartData()).data;
     //console.log(resp);
      if (resp != null) {
       //console.log(resp);
        setApidata(resp);
        setChartOptions(Object.keys(resp));
      }
    };
    getData();
  }, []);

  const [selectedChart, setSelectedChart] = useState(
    customConfig[componentId]?.chart || ""
  );
  const [selectedValue, setSelectedValue] = useState(
    customConfig[componentId]?.chartURL || ""
  );

  // Update state values when nconfig changes
  useEffect(() => {
    if (nconfig === null || nconfig === undefined) {
      // setSelectedChart("barchart");
      // setSelectedValue("flows/hello1");
    } else {
      setSelectedChart(nconfig[componentId]?.chart || "");
      setSelectedValue(nconfig[componentId]?.chartURL || "");
    }
  }, [nconfig, componentId]);

  const handleChartChange = (chart) => {
   //console.log(chart);
    setSelectedChart(chart);
    setSelectedValue("");
    // Update customConfig when chart changes
    customConfig[componentId] = {
      ...customConfig[componentId],
      chart: chart,
      chartURL: "", // Reset chartURL when chart changes
    };
  };

  const handleValueChange = (value) => {
    setSelectedValue(value);
    // Update customConfig when value changes
    customConfig[componentId] = {
      ...customConfig[componentId],
      chartURL: value,
    };
  };

  return (
    <div>
      <FormControl fullWidth>
        <InputLabel>Select Chart</InputLabel>
        <Select
          value={selectedChart}
          onChange={(e) => handleChartChange(e.target.value)}
        >
          <MenuItem value="">Select Chart</MenuItem>
          {chartOptions?.map((chart) => (
            <MenuItem key={chart} value={chart}>
              {chart}
            </MenuItem>
          ))}
        </Select>
      </FormControl>

      <FormControl fullWidth style={{ marginTop: "16px" }}>
        <InputLabel>Select Value</InputLabel>
        <Select
          value={selectedValue}
          onChange={(e) => handleValueChange(e.target.value)}
          disabled={!selectedChart}
        >
          <MenuItem value="" disabled={!selectedChart}>
            Select Value
          </MenuItem>
          {selectedChart &&
            apidata[selectedChart]?.map((value) => (
              <MenuItem key={value} value={value}>
                {value}
              </MenuItem>
            ))}
        </Select>
      </FormControl>
    </div>
  );
};

const VisibleConfig = ({ nconfig, componentId, customConfig, options }) => {
  const dispatch = useAppDispatch();
  // Dev2//console.log(options.length);
  if (nconfig === null || nconfig === undefined) {
    // options.map(
    //   (option) => (customConfig[componentId][`${option}_visible`] = true)
    // );
  } else {
    customConfig[componentId] = nconfig[componentId];
  }
  return (
    <TableContainer
      component={Paper}
      style={{ maxWidth: "85vw", borderRadius: "15px", margin: "10px 10px" }}
    >
      <Table>
        <TableHead>
          <TableRow>
            <TableCell>Column</TableCell>
            <TableCell>Visible</TableCell>
          </TableRow>
        </TableHead>
        <TableBody>
          {options.map((columnName, index) => (
            <TableRow key={columnName + index}>
              <TableCell>{columnName}</TableCell>
              <TableCell>
                <Checkbox
                  defaultChecked={
                    customConfig[componentId][`${columnName}_visible`]
                  }
                  onChange={(event) =>
                    (customConfig[componentId][`${columnName}_visible`] =
                      event.target.checked)
                  }
                />
              </TableCell>
            </TableRow>
          ))}
        </TableBody>
      </Table>
    </TableContainer>
  );
};
export default VisibleConfig;

export const ColorConfig = ({
  nconfig,
  componentId,
  customConfig,
  options,
}) => {
  const [anchorEl, setAnchorEl] = React.useState(null);
  const [currentColumn, setCurrentColumn] = React.useState(null);
  if (nconfig !== null && nconfig !== undefined)
    customConfig[componentId] = nconfig[componentId];

  const handleDs = (event, columnName) => {
    setCurrentColumn(columnName);
    setAnchorEl(event.currentTarget);
  };
  const [flag, setflag] = useState(false);
  const handleClose = () => {
    setAnchorEl(null);
    setCurrentColumn(null);
  };

  const isOpen = Boolean(anchorEl);

  const handleChange = (event) => {
    setflag(!flag);
    customConfig[componentId][`${currentColumn}_color`] = event.hex;
  };

  return (
    <TableContainer
      component={Paper}
      style={{ maxWidth: "85vw", borderRadius: "15px", margin: "10px 10px" }}
    >
      <Table>
        <TableHead>
          <TableRow>
            <TableCell>Column</TableCell>
            <TableCell>Color Selection</TableCell>
          </TableRow>
        </TableHead>
        <TableBody>
          {options.map((columnName, index) => (
            <TableRow key={columnName + index}>
              <TableCell>
                {`${columnName.split(/(?=[A-Z])/).join(" ")} `}
              </TableCell>
              <TableCell>
                <Typography variant="body1">
                  <IconButton onClick={(e) => handleDs(e, columnName)}>
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
                    <SketchPicker
                      color={
                        customConfig[componentId][`${currentColumn}_color`]
                      }
                      onChange={handleChange}
                    />
                  </div>
                </Popover>
              </TableCell>
            </TableRow>
          ))}
        </TableBody>
      </Table>
    </TableContainer>
  );
};
