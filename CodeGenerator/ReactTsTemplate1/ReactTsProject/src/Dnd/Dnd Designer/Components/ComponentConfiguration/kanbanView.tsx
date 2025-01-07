import React, { useState } from "react";
import { useAppDispatch } from "redux/store";
import {
  getColumnNameList,
  getGridList,
  getRefSlice,
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

const KanbanView = ({
  nconfig,
  componentId,
  customConfig,
  options,
  inputType,
  errorList,
  componentName,
}) => {
  const dispatch = useAppDispatch();
  const [flag, setflag] = useState(false);
  const changeRender = () => {
    setflag(!flag);
  };
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
            <TableCell>Select Reference source Column</TableCell>
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
                      changeRender();
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
              <TableCell>
                {customConfig[componentId][`${columnName}_ref_gv1_column`] &&
                  getRefSlice(
                    componentName,
                    customConfig[componentId][`${columnName}_ref_gv1_column`]
                  ) && (
                    <FormControl>
                      <InputLabel id={`status-ref-select-label`}>
                        Select...
                      </InputLabel>
                      <Select
                        fullWidth
                        labelId={`status-ref-select-label`}
                        defaultValue={
                          customConfig[componentId][`Status_kan_ref`]
                        }
                        onChange={(event) => {
                          customConfig[componentId][`Status_kan_ref`] =
                            event.target.value;
                          customConfig[componentId][`Status_kan_ref_slice`] =
                            getRefSlice(
                              componentName,
                              customConfig[componentId][
                                `${columnName}_ref_gv1_column`
                              ]
                            );
                        }}
                      >
                        {getColumnNameList(
                          getRefSlice(
                            componentName,
                            customConfig[componentId][
                              `${columnName}_ref_gv1_column`
                            ]
                          )
                        ).map((controlName) => (
                          <MenuItem key={controlName} value={controlName}>
                            {controlName}
                          </MenuItem>
                        ))}
                      </Select>
                    </FormControl>
                  )}
              </TableCell>
            </TableRow>
          ))}
        </TableBody>
      </Table>
    </TableContainer>
  );
};
export default KanbanView;
