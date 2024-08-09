import React, { useRef, useState, useEffect } from "react";
import { useDrag } from "react-dnd";
import { useAppDispatch } from "redux/store";
import { useSelector } from "react-redux";
import { RootState } from "redux/reducers";
import { MdHelp } from "react-icons/md";
import { Tooltip as ReactTooltip } from "react-tooltip";
import { Row, Col, Form } from "react-bootstrap";
import {
  ErrorControlList,
  getColumnNameList,
  displayControlList,
} from "Dnd/Dnd Designer/Utility/constants";
// import Tooltip from "@mui/material/Tooltip";
import IconButton from "@mui/material/IconButton";
import RemoveIcon from "@mui/icons-material/Remove";
import { SketchPicker } from "react-color";
import { IoIosColorPalette } from "react-icons/io";
import {
  Checkbox,
  Table,
  Button,
  TableBody,
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
import {
  getS3bucket,
  filterS3bucketWithColumns,
} from "services/s3bucketService";
import {
  setS3bucketList,
  setS3bucketMessage,
  resetS3bucketToInit,
} from "redux/actions";
import { Constant } from "template/Constant";
import { getAllChartData } from "services/flowService";

const dataset1 = null;
const dataset = {
  Simple_PieChart: [
    "/flows/Simple_PieChart/hello1",
    "/flows/Simple_PieChart/hello2",
    "/flows/Simple_PieChart/hello3",
  ],
  Simple_BarChart: [
    "/flows/Simple_BarChart/data1",
    "/flows/Simple_BarChart/data2",
  ],
  Simple_LineChart: [
    "/flows/Simple_LineChart/line1",
    "/flows/Simple_LineChart/line2",
    "/flows/Simple_LineChart/line3",
  ],
  Shadow_LineChart: [
    "/flows/Shadow_LineChart/line1",
    "/flows/Shadow_LineChart/line2",
    "/flows/Shadow_LineChart/line3",
  ],
  Simple_BarChart2: [
    "/flows/Simple_BarChart2/data1",
    "/flows/Simple_BarChart2/data2",
    "/flows/Simple_BarChart2/data3",
  ],
  Complex_LineChart: [
    "/flows/Complex_LineChart/line1",
    "/flows/Complex_LineChart/line2",
    "/flows/Complex_LineChart/line3",
  ],
  Complex_PieChart: [
    "/flows/Complex_PieChart/hello1",
    "/flows/Complex_PieChart/hello2",
    "/flows/Complex_PieChart/hello3",
  ],
  Straight_LineChart: [
    "/flows/Straight_LineChart/line1",
    "/flows/Straight_LineChart/line2",
    "/flows/Straight_LineChart/line3",
  ],
  Simple_PieChart2: [
    "/flows/Simple_PieChart2/hello1",
    "/flows/Simple_PieChart2/hello2",
    "/flows/Simple_PieChart2/hello3",
  ],
  Horizontal_BarChart: [
    "/flows/Horizontal_BarChart/data1",
    "/flows/Horizontal_BarChart/data2",
  ],
  Multi_BarChart: [
    "/flows/Multi_BarChart/data1",
    "/flows/Multi_BarChart/data2",
  ],

  // Add more flows for other charts as needed
};
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

 //console.log(apidata);
 //console.log(chartOptions);
  // Initialize state values based on default values or customConfig
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
