import React, { useState, useEffect } from "react";
import { useAppDispatch } from "redux/store";
import { useSelector } from "react-redux";
import { RootState } from "redux/reducers";
import {
    getColumnNameList,
    getGridListData,
    getType,
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
import moment from "moment";
import { getS3bucket_folders } from "services/s3bucket_foldersService";

const FieldConfig = ({
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
            customConfig[componentId][`${option.name}_isHidden`] = false;
            //console.log(customConfig[componentId]);
        });
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
    console.log(nconfig);
    console.log(customConfig);
    const [textFieldValue, setTextFieldValue] = useState("");
    const [dateFieldValue, setDateFieldValue] = useState(
        moment(Date.now()).format("YYYY-MM-DD")
    );
    const [dateTimeFieldValue, setDateTimeFieldValue] = useState(
        moment(Date.now()).format("YYYY-MM-DD hh:mm:ss")
    );
    const [value, setValue] = useState("");
    const [flag, setflag] = useState(false);
    const changeRender = () => {
        setflag(!flag);
    };
    const [s3bucketData, sets3bucketData] = useState([]);
    const getData = (page, pageSize, searchKey) => {
        getS3bucket(page, pageSize, searchKey).then((response) => {
            if (response && response.records) {
                sets3bucketData(response.records);
                dispatch(
                    setS3bucketList({
                        pageNo: page,
                        pageSize: pageSize,
                        list: response.records,
                        totalCount: response.total_count,
                        searchKey: searchKey,
                    })
                );
                //customConfig[componentId][`${columnName.name}_bucket_url`] = response.records[0]?.bucket_url;
            } else {
                dispatch(setS3bucketMessage("No Record Found"));
            }
        });
    };
    const [s3BucketFolder, sets3BucketFolder] = useState([]);
    const getS3BucketFolderData = (page, pageSize, searchKey) => {
        getS3bucket_folders(page, pageSize, searchKey).then((response) => {
            if (response && response.records) {
                sets3BucketFolder(response.records);
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
    useEffect(() => {
        getData(Constant.defaultPageNumber, Constant.defaultPageSize, "");
    }, []);
    useEffect(() => {
        getS3BucketFolderData(
            Constant.defaultPageNumber,
            Constant.defaultPageSize,
            ""
        );
    }, []);
   
    const handleChange = (event) => {
        setValue(event.target.value);
        ////console.log("e.target.name :", event.target.name);
        customConfig[componentId][inputType] = event.target.value;
    };

    const handleInputChange = (value, columnName) => {
        setTextFieldValue(value);
        customConfig[componentId][`${columnName.name}_defaultValue`] = value;
        changeRender();
    };

    const handleDateChange = (e, columnName) => {
        const selectedDate = e.target.value;
        setDateFieldValue(selectedDate);

        customConfig[componentId][`${columnName.name}_defaultValue`] = moment(
            selectedDate,
            "YYYY-MM-DD"
        ); // Store the date in ISO format

        changeRender();
    };
    const handleDateTimeChange = (e, columnName) => {
        const selectedDate = e.target.value;
        setDateTimeFieldValue(selectedDate);

        customConfig[componentId][`${columnName.name}_defaultValue`] = moment(
            selectedDate,
            "YYYY-MM-DD hh:mm:ss"
        ); // Store the date in ISO format

        changeRender();
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
                        <TableCell>Hidden?</TableCell>
                        <TableCell>DefaultValue</TableCell>
                        <TableCell>Grid List Column</TableCell>
                        <TableCell>In New Line?</TableCell>
                        <TableCell>Bucket Url</TableCell>
                        <TableCell>Bucket Name</TableCell>
                        <TableCell>Bucket Folder</TableCell>
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
                                            customConfig[componentId][
                                                `${columnName.name}_control`
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
                                    <Checkbox
                                        defaultChecked={
                                            customConfig[componentId][`${columnName.name}_isHidden`]
                                        }
                                        onChange={(event) => {
                                            customConfig[componentId][
                                                `${columnName.name}_isHidden`
                                            ] = event.target.checked;
                                            changeRender();
                                            return customConfig;
                                        }}
                                    />
                                </FormControl>
                            </TableCell>
                            <TableCell>
                                <FormControl>
                                    {}
                                    {(() => {
                                        switch (getType(columnName.type)) {
                                            case "text":
                                                return (
                                                    <TextField
                                                        label={`Enter ${columnName.name}`}
                                                        variant="outlined"
                                                        value={
                                                            customConfig[componentId][
                                                            `${columnName.name}_defaultValue`
                                                            ]
                                                        }
                                                        onChange={(e) =>
                                                            handleInputChange(e.target.value, columnName)
                                                        }
                                                        disabled={
                                                            !customConfig[componentId][
                                                            `${columnName.name}_isHidden`
                                                            ]
                                                        }
                                                    />
                                                );

                                            case "datetime":
                                                const columnDefaultValue =
                                                    customConfig[componentId][
                                                    `${columnName.name}_defaultValue`
                                                    ];
                                                return (
                                                    <TextField
                                                        type="datetime-local"
                                                        name={`${columnName.name}-default-value`}
                                                        id={`${columnName.name}-default-value`}
                                                        className="form-control"
                                                        value={
                                                            columnDefaultValue
                                                                ? moment(columnDefaultValue).format(
                                                                    "YYYY-MM-DD hh:mm:ss"
                                                                )
                                                                : ""
                                                        }
                                                        onChange={(e) => {
                                                            customConfig[componentId][
                                                                `${columnName.name}_defaultValue`
                                                            ] = moment(e.target.value).format(
                                                                "YYYY-MM-DD hh:mm:ss"
                                                            );
                                                            changeRender();
                                                            return customConfig;
                                                        }}
                                                        disabled={
                                                            !customConfig[componentId][
                                                            `${columnName.name}_isHidden`
                                                            ]
                                                        }
                                                    //onBlur={handleBlur}
                                                    />
                                                );
                                            case "date":
                                                const defaultValue =
                                                    customConfig[componentId][
                                                    `${columnName.name}_defaultValue`
                                                    ];
                                                return (
                                                    <TextField
                                                        type={"date"}
                                                        name={`${columnName.name}-default-value`}
                                                        key={index}
                                                        id={`${columnName.name}-default-value`}
                                                        className="form-control"
                                                        value={
                                                            defaultValue
                                                                ? moment(defaultValue).format("YYYY-MM-DD")
                                                                : ""
                                                        }
                                                        onChange={(e) => {
                                                            customConfig[componentId][
                                                                `${columnName.name}_defaultValue`
                                                            ] = moment(e.target.value, "YYYY-MM-DD");
                                                            changeRender();
                                                            return customConfig;
                                                        }}
                                                        disabled={
                                                            !customConfig[componentId][
                                                            `${columnName.name}_isHidden`
                                                            ]
                                                        }
                                                    //onBlur={handleBlur}
                                                    />
                                                );
                                            // Add more cases for other input control types

                                            default:
                                                return null;
                                        }
                                    })()}
                                </FormControl>
                            </TableCell>
                            <TableCell>
                                <FormControl>
                                    <InputLabel id={`${columnName.name}-grid-control-label`}>
                                        Select Grid Span
                                    </InputLabel>
                                    <Select
                                        fullWidth
                                        labelId={`${columnName.name}-grid-control-label`}
                                        defaultValue={
                                            customConfig[componentId][
                                            `${columnName.name}_grid_control`
                                            ]
                                        }
                                        onChange={(e) => {
                                            customConfig[componentId][
                                                `${columnName.name}_grid_control`
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
                                    <Checkbox
                                        defaultChecked={
                                            customConfig[componentId][
                                            `${columnName.name}_isNewline`
                                            ]
                                        }
                                        onChange={(event) =>
                                        (customConfig[componentId][
                                            `${columnName.name}_isNewline`
                                        ] = event.target.checked)
                                        }
                                    />
                                </FormControl>
                            </TableCell>
                            <TableCell>
                                {customConfig[componentId][`${columnName.name}_control`] !==
                                    undefined &&
                                    (customConfig[componentId][`${columnName.name}_control`] ===
                                        "file" ||
                                        customConfig[componentId][
                                        `${columnName.name}_control`
                                        ] === "signature") && (
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
                                                }}
                                            >
                                                {s3bucketData.map((controlName) => (
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
                                    (customConfig[componentId][`${columnName.name}_control`] ===
                                        "file" ||
                                        customConfig[componentId][
                                        `${columnName.name}_control`
                                        ] === "signature") && (
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
                                                {s3bucketData.map((controlName) => (
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
                                {customConfig[componentId][`${columnName.name}_control`] !==
                                    undefined &&
                                    (customConfig[componentId][`${columnName.name}_control`] ===
                                        "file" ||
                                        customConfig[componentId][
                                        `${columnName.name}_control`
                                        ] === "signature") && (
                                        <FormControl>
                                            <InputLabel id={`${columnName.name}-select-label`}>
                                                Select...
                                            </InputLabel>
                                            <Select
                                                fullWidth
                                                labelId={`${columnName.name}-select-label`}
                                                defaultValue={
                                                    customConfig[componentId][
                                                    `${columnName.name}_bucket_folder`]
                                                    // .lastIndexOf("_") !== -1
                                                    //   ? customConfig[componentId][
                                                    //       `${columnName.name}_bucket_folder`
                                                    //     ].substring(
                                                    //       0,
                                                    //       customConfig[componentId][
                                                    //         `${columnName.name}_bucket_folder`
                                                    //       ].lastIndexOf("_")
                                                    //     )
                                                    //   : customConfig[componentId][
                                                    //       `${columnName.name}_bucket_folder`
                                                    //     ]
                                                }
                                                onChange={(event) => {
                                                    customConfig[componentId][
                                                        `${columnName.name}_bucket_folder`
                                                    ] = event.target.value;
                                                    //filterFolderName(event.target.value);
                                                    //return customConfig;
                                                }}
                                            >
                                                {s3BucketFolder.map((controlName) => (
                                                    <MenuItem
                                                        key={controlName.folder_id + "1"}
                                                        value={controlName.folder_name}
                                                    >
                                                        {controlName.folder_name &&
                                                            controlName.folder_name.lastIndexOf("_") !== -1
                                                            ? controlName.folder_name.substring(
                                                                0,
                                                                controlName.folder_name.lastIndexOf("_")
                                                            )
                                                            : controlName.folder_name}
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

// In your component render:

export default FieldConfig;
