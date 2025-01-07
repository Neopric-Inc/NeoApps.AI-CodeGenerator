import React, {  } from "react";
import { useAppDispatch } from "redux/store";
import {
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
