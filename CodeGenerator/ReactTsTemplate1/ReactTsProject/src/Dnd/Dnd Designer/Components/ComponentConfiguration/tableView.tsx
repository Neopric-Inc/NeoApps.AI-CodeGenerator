import React, {  } from "react";
import { useAppDispatch } from "redux/store";
import {
  getColumnNameList,
  getGridList,
} from "Dnd/Dnd Designer/Utility/constants";
// import Tooltip from "@mui/material/Tooltip";
import {
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
} from "@mui/material";

const TableView = ({
  nconfig,
  componentId,
  customConfig,
  options,
  inputType,
  errorList,
}) => {
  const dispatch = useAppDispatch();
 //console.log("grid");
  //const GridList = getGridList(customConfig[componentId]["selectedView"]);
  //Dev1
  const GridList = getGridList(customConfig[componentId]["selectedView"]);
  const ColumnList = getColumnNameList(
    customConfig[componentId]["componentInfo"]["type"]
  );
  // ColumnList.map((option,index) => {
  //   customConfig[componentId][`${option}_visible`] = true;
  // })
  //here call getColumnlist
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
          {ColumnList.map((columnName, index) => (
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
                    {GridList.map((controlName) => (
                      <MenuItem key={controlName} value={controlName}>
                        {controlName}
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
export default TableView;
