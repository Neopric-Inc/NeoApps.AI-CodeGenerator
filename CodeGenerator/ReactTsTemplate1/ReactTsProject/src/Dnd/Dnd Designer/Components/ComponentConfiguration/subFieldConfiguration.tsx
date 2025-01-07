import React, { useState, useEffect } from "react";
import { useAppDispatch } from "redux/store";
import { useSelector } from "react-redux";
import { RootState } from "redux/reducers";
import {
  getColumnNameList,
  getGridListData,
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

const SubFieldConfig = ({
  nconfig,
  componentId,
  customConfig,
  options,
  inputType,
  errorList,
  groups,
}) => {
  const dispatch = useAppDispatch();
  // Dev2//console.log(options.length);
 //console.log(nconfig[componentId]);
  customConfig[componentId] = nconfig[componentId];
 //console.log(options);
  options.forEach((option) => {
    if (
      customConfig[componentId][`${groups}_${option.name}_new_name`] ===
        undefined ||
      customConfig[componentId][`${groups}_${option.name}_new_name`] === null
    ) {
      customConfig[componentId][`${groups}_${option.name}_new_name`] =
        option.name;
      customConfig[componentId][`${groups}_${option.name}_form_new_name`] =
        option.name;
      customConfig[componentId][`${groups}_${option.name}_visible`] = true;
    }
  });
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
            <TableCell>Enter Form Field New Name</TableCell>
            <TableCell>Input Control</TableCell>
            <TableCell>Preferences</TableCell>
            <TableCell>Grid List Column</TableCell>
            <TableCell>In New Line?</TableCell>
            <TableCell>Bucket Url</TableCell>
            <TableCell>Bucket Name</TableCell>
          </TableRow>
        </TableHead>
        <TableBody>
          {options.map((columnName, index) => (
            <TableRow key={columnName.name}>
              <TableCell>{columnName.name}</TableCell>
              <TableCell>
                <TextField
                  defaultValue={
                    customConfig[componentId][
                      `${groups}_${columnName.name}_form_new_name`
                    ]
                  }
                  onChange={(event) =>
                    (customConfig[componentId][
                      `${groups}_${columnName.name}_form_new_name`
                    ] = event.target.value)
                  }
                />
              </TableCell>
              <TableCell>
                <FormControl>
                  <InputLabel
                    id={`${groups}_${columnName.name}-input-control-label`}
                  >
                    Select input control
                  </InputLabel>
                  <Select
                    fullWidth
                    labelId={`${groups}_${columnName.name}-input-control-label`}
                    defaultValue={
                      customConfig[componentId][
                        `${groups}_${columnName.name}_control`
                      ]
                    }
                    onChange={(e) => {
                      customConfig[componentId][
                        `${groups}_${columnName.name}_control`
                      ] = e.target.value;
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
                <FormControl>
                  <InputLabel
                    id={`${groups}_${columnName.name}-preference-control-label`}
                  >
                    Select Preference
                  </InputLabel>
                  <Select
                    fullWidth
                    labelId={`${groups}_${columnName.name}-preference-control-label`}
                    defaultValue={
                      customConfig[componentId][
                        `${groups}_${columnName.name}_preference`
                      ]
                    }
                    onChange={(e) => {
                      customConfig[componentId][
                        `${groups}_${columnName.name}_preference`
                      ] = e.target.value;
                      changeRender();
                      return customConfig;
                    }}
                  >
                    <MenuItem value="">Select preference</MenuItem>
                    {options.map((columnName, index) => (
                      <MenuItem key={index + 1} value={index + 1}>
                        {index + 1}
                      </MenuItem>
                    ))}
                  </Select>
                </FormControl>
              </TableCell>
              <TableCell>
                <FormControl>
                  <InputLabel
                    id={`${groups}_${columnName.name}-grid-control-label`}
                  >
                    Select Grid Span
                  </InputLabel>
                  <Select
                    fullWidth
                    labelId={`${groups}_${columnName.name}-grid-control-label`}
                    defaultValue={
                      customConfig[componentId][
                        `${groups}_${columnName.name}_grid_control`
                      ]
                    }
                    onChange={(e) => {
                      customConfig[componentId][
                        `${groups}_${columnName.name}_grid_control`
                      ] = e.target.value;
                      changeRender();
                      return customConfig;
                    }}
                  >
                    <MenuItem value="">Select Grid Span</MenuItem>
                    {getGridListData.map((gridSpan, index) => (
                      <MenuItem key={index + 1} value={gridSpan}>
                        {gridSpan}
                      </MenuItem>
                    ))}
                  </Select>
                </FormControl>
              </TableCell>
              <TableCell>
                <FormControl>
                  <InputLabel id={`${groups}_${columnName.name}-is-newLine`}>
                    inNewLine?
                  </InputLabel>
                  <Checkbox
                    defaultChecked={
                      customConfig[componentId][
                        `${groups}_${columnName.name}_isNewline`
                      ]
                    }
                    onChange={(event) =>
                      (customConfig[componentId][
                        `${groups}_${columnName.name}_isNewline`
                      ] = event.target.checked)
                    }
                  />
                </FormControl>
              </TableCell>
              <TableCell>
                {customConfig[componentId][
                  `${groups}_${columnName.name}_control`
                ] !== undefined &&
                  customConfig[componentId][
                    `${groups}_${columnName.name}_control`
                  ] === "file" && (
                    <FormControl>
                      <InputLabel
                        id={`${groups}_${columnName.name}-select-label`}
                      >
                        Select...
                      </InputLabel>
                      <Select
                        fullWidth
                        labelId={`${groups}_${columnName.name}-select-label`}
                        defaultValue={
                          customConfig[componentId][
                            `${groups}_${columnName.name}_bucket_url`
                          ]
                        }
                        onChange={(event) => {
                          customConfig[componentId][
                            `${groups}_${columnName.name}_bucket_url`
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
                {customConfig[componentId][
                  `${groups}_${columnName.name}_control`
                ] !== undefined &&
                  customConfig[componentId][
                    `${groups}_${columnName.name}_control`
                  ] === "file" && (
                    <FormControl>
                      <InputLabel
                        id={`${groups}_${columnName.name}-select-label`}
                      >
                        Select...
                      </InputLabel>
                      <Select
                        fullWidth
                        labelId={`${groups}_${columnName.name}-select-label`}
                        defaultValue={
                          customConfig[componentId][
                            `${groups}_${columnName.name}_bucket_name`
                          ]
                        }
                        onChange={(event) => {
                          customConfig[componentId][
                            `${groups}_${columnName.name}_bucket_name`
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
            </TableRow>
          ))}
        </TableBody>
      </Table>
    </TableContainer>
  );
};
export default SubFieldConfig;
