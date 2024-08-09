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
  getGridList,
} from "Dnd/Dnd Designer/Utility/constants";
// import Tooltip from "@mui/material/Tooltip";
import IconButton from "@mui/material/IconButton";
import RemoveIcon from "@mui/icons-material/Remove";
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

const GridView1 = ({
  nconfig,
  componentId,
  customConfig,
  options,
  inputType,
  errorList,
}) => {
  const dispatch = useAppDispatch();
 //console.log("grid");
  const GridList = getGridList(customConfig[componentId]["selectedView"]);
  return (
    <TableContainer
      component={Paper}
      style={{ maxWidth: "85vw", borderRadius: "15px", margin: "10px 10px" }}
    >
      <Table>
        <TableHead>
          <TableRow>
            <TableCell>Column</TableCell>
            <TableCell>Select Reference Column</TableCell>
          </TableRow>
        </TableHead>
        <TableBody>
          {GridList.map((columnName, index) => (
            <TableRow key="1">
              <TableCell>{columnName}</TableCell>
              <TableCell>
                <FormControl>
                  <InputLabel id={`${columnName}_gv1-select-label`}>
                    Select...
                  </InputLabel>
                  <Select
                    fullWidth
                    labelId={`${columnName}_gv1-select-label`}
                    defaultValue={
                      customConfig[componentId][`${columnName}_ref_gv1_column`]
                    }
                    onChange={(event) => {
                      customConfig[componentId][
                        `${columnName}_ref_gv1_column`
                      ] = event.target.value;
                    }}
                  >
                    {options.map((controlName) => (
                      <MenuItem key={controlName.name} value={controlName.name}>
                        {controlName.name}
                      </MenuItem>
                    ))}
                  </Select>
                </FormControl>
              </TableCell>
            </TableRow>
          ))}
        </TableBody>
      </Table>
    </TableContainer>
  );
};
export default GridView1;
