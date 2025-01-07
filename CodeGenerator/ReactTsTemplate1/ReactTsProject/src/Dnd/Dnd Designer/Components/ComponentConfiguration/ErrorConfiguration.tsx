import React, { useState } from "react";
import { useAppDispatch } from "redux/store";
import {
  ErrorControlList,
} from "Dnd/Dnd Designer/Utility/constants";
// import Tooltip from "@mui/material/Tooltip";
import {
  Table,
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
} from "@mui/material";

const ErrorConfig = ({
  nconfig,
  componentId,
  customConfig,
  options,
  inputType,
  errorList,
}) => {
  const dispatch = useAppDispatch();
  // Dev2//console.log(options.length);
  if (
    nconfig === null ||
    nconfig === undefined ||
    Object.keys(nconfig[componentId]).length < options.length
  ) {
   //console.log("temp2");
  } else {
    customConfig[componentId] = nconfig[componentId];
  }
  const [value, setValue] = useState("");
  const [flag, setflag] = useState(false);
  const changeRender = () => {
    setflag(!flag);
  };

  const handleChange = (event) => {
    setValue(event.target.value);
    ////console.log("e.target.name :", event.target.name);
    customConfig[componentId][inputType] = event.target.value;
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
            <TableCell>Error Control</TableCell>
            <TableCell>Error Message</TableCell>
          </TableRow>
        </TableHead>
        <TableBody>
          {options.map((columnName, index) => (
            <TableRow key={columnName.name}>
              <TableCell>{columnName.name}</TableCell>
              <TableCell>
                {!columnName.fkey && !columnName.pkey && (
                  <FormControl>
                    <InputLabel id={`${columnName.name}-error-control-label`}>
                      Select error control
                    </InputLabel>
                    <Select
                      fullWidth
                      labelId={`${columnName.name}-error-control-label`}
                      defaultValue={
                        customConfig[componentId][
                          `${columnName.name}_error_control`
                        ]
                      }
                      onChange={(e) => {
                        customConfig[componentId][
                          `${columnName.name}_error_control`
                        ] = e.target.value;
                        changeRender();
                        return customConfig;
                      }}
                    >
                      <MenuItem value="">Select error control</MenuItem>
                      {errorList.map((errorType) => (
                        <MenuItem key={errorType} value={errorType}>
                          {errorType}
                        </MenuItem>
                      ))}
                    </Select>
                  </FormControl>
                )}
              </TableCell>
              <TableCell>
                {customConfig[componentId][
                  `${columnName.name}_error_control`
                ] &&
                  typeof ErrorControlList[
                    customConfig[componentId][
                      `${columnName.name}_error_control`
                    ]
                  ] === "string" && (
                    <TextField
                      defaultValue={
                        customConfig[componentId][
                          `${columnName.name}_error_message`
                        ]
                      }
                      onChange={(e) =>
                        (customConfig[componentId][
                          `${columnName.name}_error_message`
                        ] = e.target.value)
                      }
                    />
                  )}
                {customConfig[componentId][
                  `${columnName.name}_error_control`
                ] &&
                  typeof ErrorControlList[
                    customConfig[componentId][
                      `${columnName.name}_error_control`
                    ]
                  ] === "object" && (
                    <FormControl>
                      <InputLabel id={`${columnName.name}-error-message-label`}>
                        Select error Message
                      </InputLabel>
                      <Select
                        fullWidth
                        labelId={`${columnName.name}-error-message-label`}
                        defaultValue={
                          customConfig[componentId][
                            `${columnName.name}_error_message`
                          ]
                        }
                        onChange={(e) =>
                          (customConfig[componentId][
                            `${columnName.name}_error_message`
                          ] = e.target.value)
                        }
                      >
                        <MenuItem value="">Select error Message</MenuItem>
                        {Object.keys(
                          ErrorControlList[
                            customConfig[componentId][
                              `${columnName.name}_error_control`
                            ]
                          ]
                        ).map((errorType) => (
                          <MenuItem key={errorType} value={errorType}>
                            {
                              ErrorControlList[
                                customConfig[componentId][
                                  `${columnName.name}_error_control`
                                ]
                              ][errorType]
                            }
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
export default ErrorConfig;
