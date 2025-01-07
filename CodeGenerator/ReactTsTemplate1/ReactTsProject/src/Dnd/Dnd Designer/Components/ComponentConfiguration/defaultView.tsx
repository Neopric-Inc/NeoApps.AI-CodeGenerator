import React, { useState, useEffect } from "react";
import { useAppDispatch } from "redux/store";
import { useSelector } from "react-redux";
import { RootState } from "redux/reducers";
import {
  ErrorControlList,
  getColumnNameList,
  displayControlList,
} from "Dnd/Dnd Designer/Utility/constants";
// import Tooltip from "@mui/material/Tooltip";
import {
  Checkbox,
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
import {
  getS3bucket,
} from "services/s3bucketService";
import {
  setS3bucketList,
  setS3bucketMessage,
  resetS3bucketToInit,
} from "redux/actions";
import { Constant } from "template/Constant";

const DefaultView = ({
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
    options.forEach((option) => {
      customConfig[componentId][`${option.name}_new_name`] = option.name;
      customConfig[componentId][`${option.name}_form_new_name`] = option.name;
      // customConfig[componentId][`${option.name}_visible`] = true;
    });
    customConfig[componentId][`search_button_visible1`] = true;
    customConfig[componentId][`add_new_button_visible1`] = true;
    customConfig[componentId][`edit_button_visible1`] = true;
    customConfig[componentId][`delete_button_visible1`] = true;
    customConfig[componentId][`add_new_button_new_name1`] = "ADD NEW";
  } else {
    customConfig[componentId] = nconfig[componentId];
  }
  let refcol = [];
  options.forEach((option, index) => {
    if (option.fkey) {
      refcol[index] = getColumnNameList(option.slice);
    } else {
      refcol[index] = null;
    }
  });
  // {
  //   list: [
  //     { bucket_id: 1, bucket_name: "main", bucket_url: "sdfdfdfdfdfdfdf" },
  //   ],
  // };
  const [value, setValue] = useState("");
  const [flag, setflag] = useState(false);
  const changeRender = () => {
    setflag(!flag);
  };
  const getData = (page, pageSize, searchKey) => {
    getS3bucket(page, pageSize, searchKey).then((response) => {
      if (response && response.records) {
        dispatch(
          setS3bucketList({
            pageNo: page,
            pageSize: pageSize,
            list: response.records,
            totalCount: response.total_count,
            searchKey: searchKey,
          })
        );
      } else {
        dispatch(setS3bucketMessage("No Record Found"));
      }
    });
  };
  const s3bucketData1 = useSelector((state: RootState) => state.s3bucket);
  useEffect(() => {
    if (
      s3bucketData1 &&
      s3bucketData1.list &&
      s3bucketData1.list.length === 0
    ) {
      dispatch(resetS3bucketToInit());
      getData(Constant.defaultPageNumber, Constant.defaultPageSize, "");
    }
  }, [s3bucketData1.list.length]);
  const urlList = s3bucketData1.list.filter(
    (dict, index, self) =>
      self.findIndex((d) => d.bucket_url === dict.bucket_url) === index
  );
 //console.log(urlList);
  const [s3bucketData, sets3bucketData] = useState(s3bucketData1);

  const filterBucketName = (eurl) => {
    const filteredData = s3bucketData1.list.filter(
      (e) => e.bucket_url === eurl
    );
    sets3bucketData({ ...s3bucketData1, list: filteredData });
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
            <TableCell>Enter Column New Name</TableCell>
            <TableCell>Enter Form Field New Name</TableCell>
            <TableCell>Is Visible</TableCell>
            <TableCell>Reference data source</TableCell>
            <TableCell>Input Control</TableCell>
            <TableCell>Display Control</TableCell>
            <TableCell>Bucket Url</TableCell>
            <TableCell>Bucket Name</TableCell>
            <TableCell>Error Control</TableCell>
            <TableCell>Error Message</TableCell>
          </TableRow>
        </TableHead>
        <TableBody>
          {options.map((columnName, index) => (
            <TableRow key={columnName.name}>
              <TableCell>{columnName.name}</TableCell>
              <TableCell>
                <TextField
                  defaultValue={
                    customConfig[componentId][`${columnName.name}_new_name`]
                  }
                  onChange={(event) =>
                    (customConfig[componentId][`${columnName.name}_new_name`] =
                      event.target.value)
                  }
                />
              </TableCell>
              <TableCell>
                <TextField
                  defaultValue={
                    customConfig[componentId][
                      `${columnName.name}_form_new_name`
                    ]
                  }
                  onChange={(event) =>
                    (customConfig[componentId][
                      `${columnName.name}_form_new_name`
                    ] = event.target.value)
                  }
                />
              </TableCell>
              <TableCell>
                <Checkbox
                  defaultChecked={
                    customConfig[componentId][`${columnName.name}_visible`]
                  }
                  onChange={(event) =>
                    (customConfig[componentId][`${columnName.name}_visible`] =
                      event.target.checked)
                  }
                />
              </TableCell>
              <TableCell>
                {columnName.fkey && (
                  <FormControl>
                    <InputLabel id={`${columnName.name}-select-label`}>
                      Select...
                    </InputLabel>
                    <Select
                      fullWidth
                      labelId={`${columnName.name}-select-label`}
                      defaultValue={
                        customConfig[componentId][`${columnName.name}_ref`]
                      }
                      onChange={(event) => {
                        customConfig[componentId][`${columnName.name}_ref`] =
                          event.target.value;
                        customConfig[componentId][
                          `${columnName.name}_ref_slice`
                        ] = columnName.slice;
                      }}
                    >
                      {refcol[index].map((controlName) => (
                        <MenuItem key={controlName} value={controlName}>
                          {controlName}
                        </MenuItem>
                      ))}
                    </Select>
                  </FormControl>
                )}
              </TableCell>
              <TableCell>
                <FormControl>
                  <InputLabel id={`${columnName.name}-input-control-label`}>
                    Select input control
                  </InputLabel>
                  <Select
                    fullWidth
                    labelId={`${columnName.name}-input-control-label`}
                    defaultValue={
                      customConfig[componentId][`${columnName.name}_control`]
                    }
                    onChange={(e) => {
                      customConfig[componentId][`${columnName.name}_control`] =
                        e.target.value;
                      changeRender();
                      return customConfig;
                    }}
                  >
                    <MenuItem value="">Select input control</MenuItem>
                    {columnName.icontrol.map((controlName) => (
                      <MenuItem key={controlName} value={controlName}>
                        {controlName}
                      </MenuItem>
                    ))}
                  </Select>
                </FormControl>
              </TableCell>
              <TableCell>
                {customConfig[componentId][`${columnName.name}_control`] !==
                  undefined &&
                  (customConfig[componentId][`${columnName.name}_control`] ===
                    "file" ||
                    customConfig[componentId][`${columnName.name}_control`] ===
                      "url") && (
                    <FormControl>
                      <InputLabel id={`${columnName.name}-select-label`}>
                        Select...
                      </InputLabel>
                      <Select
                        fullWidth
                        labelId={`${columnName.name}-select-label`}
                        defaultValue={
                          customConfig[componentId][
                            `${columnName.name}_display_control`
                          ]
                        }
                        onChange={(event) => {
                          customConfig[componentId][
                            `${columnName.name}_display_control`
                          ] = event.target.value;
                          return customConfig;
                        }}
                      >
                        {displayControlList[
                          customConfig[componentId][
                            `${columnName.name}_control`
                          ]
                        ].map((controlName) => (
                          <MenuItem key={controlName} value={controlName}>
                            {controlName}
                          </MenuItem>
                        ))}
                      </Select>
                    </FormControl>
                  )}
              </TableCell>
              <TableCell>
                {customConfig[componentId][`${columnName.name}_control`] !==
                  undefined &&
                  customConfig[componentId][`${columnName.name}_control`] ===
                    "file" && (
                    <FormControl>
                      <InputLabel id={`${columnName.name}-select-label`}>
                        Select...
                      </InputLabel>
                      <Select
                        fullWidth
                        labelId={`${columnName.name}-select-label`}
                        defaultValue={
                          customConfig[componentId][
                            `${columnName.name}_bucket_url`
                          ]
                        }
                        onChange={(event) => {
                          customConfig[componentId][
                            `${columnName.name}_bucket_url`
                          ] = event.target.value;
                          filterBucketName(event.target.value);
                          return customConfig;
                        }}
                      >
                        {urlList.map((controlName) => (
                          <MenuItem
                            key={controlName.bucket_id}
                            value={controlName.bucket_url}
                          >
                            {controlName.bucket_url}
                          </MenuItem>
                        ))}
                      </Select>
                    </FormControl>
                  )}
              </TableCell>
              <TableCell>
                {customConfig[componentId][`${columnName.name}_control`] !==
                  undefined &&
                  customConfig[componentId][`${columnName.name}_control`] ===
                    "file" && (
                    <FormControl>
                      <InputLabel id={`${columnName.name}-select-label`}>
                        Select...
                      </InputLabel>
                      <Select
                        fullWidth
                        labelId={`${columnName.name}-select-label`}
                        defaultValue={
                          customConfig[componentId][
                            `${columnName.name}_bucket_name`
                          ]
                        }
                        onChange={(event) => {
                          customConfig[componentId][
                            `${columnName.name}_bucket_name`
                          ] = event.target.value;
                        }}
                      >
                        {s3bucketData.list.map((controlName) => (
                          <MenuItem
                            key={controlName.bucket_name + "1"}
                            value={controlName.bucket_id}
                          >
                            {controlName.bucket_name}
                          </MenuItem>
                        ))}
                      </Select>
                    </FormControl>
                  )}
              </TableCell>
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
          <TableRow key="add new button">
            <TableCell> Add New Button Option</TableCell>
            <TableCell>
              <TextField
                defaultValue={
                  customConfig[componentId][`add_new_button_new_name1`]
                }
                onChange={(event) =>
                  (customConfig[componentId][`add_new_button_new_name1`] =
                    event.target.value)
                }
              />
            </TableCell>
            <TableCell>
              <Checkbox
                defaultChecked={
                  customConfig[componentId][`add_new_button_visible1`]
                }
                onChange={(event) =>
                  (customConfig[componentId][`add_new_button_visible1`] =
                    event.target.checked)
                }
              />
            </TableCell>
          </TableRow>
          <TableRow key="search button">
            <TableCell> Search Option</TableCell>
            <TableCell></TableCell>
            <TableCell>
              <Checkbox
                defaultChecked={
                  customConfig[componentId][`search_button_visible1`]
                }
                onChange={(event) =>
                  (customConfig[componentId][`search_button_visible1`] =
                    event.target.checked)
                }
              />
            </TableCell>
          </TableRow>
          <TableRow key="edit button">
            <TableCell> Edit Button Option</TableCell>
            <TableCell></TableCell>
            <TableCell>
              <Checkbox
                defaultChecked={
                  customConfig[componentId][`edit_button_visible1`]
                }
                onChange={(event) =>
                  (customConfig[componentId][`edit_button_visible1`] =
                    event.target.checked)
                }
              />
            </TableCell>
          </TableRow>
          <TableRow key="delete button">
            <TableCell> Delete Button Option</TableCell>
            <TableCell></TableCell>
            <TableCell>
              <Checkbox
                defaultChecked={
                  customConfig[componentId][`delete_button_visible1`]
                }
                onChange={(event) =>
                  (customConfig[componentId][`delete_button_visible1`] =
                    event.target.checked)
                }
              />
            </TableCell>
          </TableRow>
        </TableBody>
      </Table>
    </TableContainer>
  );
};
export default DefaultView;
