import React, { useState, useEffect } from "react";
import { useAppDispatch } from "redux/store";
import { useSelector } from "react-redux";
import { RootState } from "redux/reducers";
import GridView1 from "./gridView1";
import KanbanView from "./kanbanView";
import { DemoViews } from "components/static";
import {
    getColumnNameList,
    getViewList,
    displayControlList,
} from "Dnd/Dnd Designer/Utility/constants";
// import Tooltip from "@mui/material/Tooltip";
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
    Popover,
    Box,
    RadioGroup,
    Radio,
    FormControlLabel,
    FormLabel,
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
import { Theme } from "@mui/material/styles";
import { makeStyles } from "@mui/styles";
import TableView from "./tableView";
import { CustomGridLayoutConfig } from "../DefaultGridTest";
import { getS3bucket_folders } from "services/s3bucket_foldersService";

declare module "@mui/material/styles" {
    interface Theme {
        spacing: (factor: number) => string;
    }
}

const useStyles = makeStyles((theme: Theme) => ({
    popover: {
        pointerEvents: "none",
    },
    paper: {
        padding: theme.spacing(2),
    },
}));
const DisplayConfig = ({
    nconfig,
    componentId,
    customConfig,
    options,
    inputType,
    errorList,
    componentName,
}) => {
    const dispatch = useAppDispatch();
    const classes = useStyles();
    const [anchorEl, setAnchorEl] = useState<null | HTMLElement>(null);

    const handlePopoverOpen = (event: React.MouseEvent<HTMLElement>) => {
        setAnchorEl(event.currentTarget);
    };

    const handlePopoverClose = () => {
        setAnchorEl(null);
    };

    const open = Boolean(anchorEl);
    // Dev2//console.log(options.length);
    if (
        nconfig === null ||
        nconfig === undefined ||
        Object.keys(nconfig[componentId]).length < options.length
    ) {
        options.forEach((option) => {
            customConfig[componentId][`${option.name}_new_name`] = option.name;
            customConfig[componentId][`${option.name}_form_new_name`] = option.name;
            customConfig[componentId][`${option.name}_visible`] = true;
        });
        customConfig[componentId][`search_button_visible1`] = true;
        customConfig[componentId][`add_new_button_visible1`] = true;
        customConfig[componentId][`edit_button_visible1`] = true;
        customConfig[componentId][`delete_button_visible1`] = true;
        customConfig[componentId][`add_new_button_new_name1`] = "ADD NEW";
        customConfig[componentId][`heading1_visible1`] = true;
        customConfig[componentId][`heading2_visible1`] = true;
        customConfig[componentId][`label_value`] = "separate_label";
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
    
    const [selectedView, setselectedView] = useState("defaultView");
    if (
        nconfig[componentId][`selectedView`] !== null &&
        nconfig[componentId][`selectedView`] !== undefined &&
        nconfig[componentId][`selectedView`] !== selectedView
    ) {
        setselectedView(nconfig[componentId][`selectedView`]);
    }
    //Build your own means BYOView and other is defaultDataGridView
    const [isBuildYourOwnView, setSelectedBYOView] = useState(
        "defaultDataGridView"
    );
    const handleBYOViewChange = (event) => {
        setSelectedBYOView(event.target.value);
    };
    const [selectedColumns, setSelectedColumns] = useState(0); // Default to 10 or any sensible default
    // Initialize state with the saved value or a default
    const [selectedRows, setSelectedRows] = useState(() => {
        const savedRows =
            customConfig[componentId]?.[`BYOView_Number_of_DbTable_Rows`];
        return savedRows || ""; // Default value if not previously saved
    });
    const generateLayout = () => {
        //hide and unhide columns based on configuration
        //console.log("options", options);

        if (customConfig[componentId]["BYOViewConfig"] !== undefined) {
            setSelectedBYOView("BYOView");
            // setLayoutItems(customConfig[componentId]["BYOViewConfig"]);
            setSelectedColumns(customConfig[componentId]["BYOViewConfig"].length);
            return customConfig[componentId]["BYOViewConfig"];
        }
    };
    const [layoutItems, setLayoutItems] = useState(generateLayout); // Define your initial layout
    //  const [layoutItems, setLayoutItems] = useState(initialLayout); // Define your initial layout

    // Handler for when the number of rows selection changes
    const handleRowsChange = (event) => {
        const newValue = event.target.value;
        // Save the new value to your config object
        if (!customConfig[componentId]) customConfig[componentId] = {};
        customConfig[componentId][`BYOView_Number_of_DbTable_Rows`] = newValue;
        setSelectedRows(newValue);
    };

    // Dropdown change handler
    const handleColumnsChange = (event) => {
        const newSelectedColumns = event.target.value;
        setSelectedColumns(newSelectedColumns);
    };
    useEffect(() => {
        const generateLayout = () => {
            const layout = [];
            for (let i = 0; i < selectedColumns; i++) {
                layout.push({
                    i: i.toString(),
                    x: (i * 2) % 12,
                    y: Math.floor(i / 6),
                    w: 1,
                    h: 1,
                    minW: 1,
                    maxW: 12,
                    minH: 1,
                    maxH: 1000,
                });
            }
            return layout;
        };

        // Update layout items based on the generated layout
        setLayoutItems(generateLayout());
    }, [selectedColumns, customConfig, componentId]); // Re-run when selectedColumns changes

    const filteredColumns = [
        /* Define your columns */
    ];

    // Handle layout updates
    const handleLayoutUpdate = (updatedLayout) => {
        //console.log("Updated layout:", updatedLayout);
        customConfig[componentId]["BYOViewConfig"] = updatedLayout;
        // Here you can set the updated layout to state, or perform other actions like saving to a database
        setLayoutItems(updatedLayout);
    };
    return (
        <>
            <div style={{ padding: "20px" }}>
                <FormControl component="fieldset">
                    <FormLabel component="legend">Select View</FormLabel>
                    <RadioGroup
                        aria-label="view"
                        name="view"
                        value={isBuildYourOwnView}
                        onChange={handleBYOViewChange}
                    >
                        <FormControlLabel
                            value="defaultDataGridView"
                            control={<Radio />}
                            label="Default Data Grid View"
                        />
                        <FormControlLabel
                            value="BYOView"
                            control={<Radio />}
                            label="Build Your Own View"
                        />
                    </RadioGroup>
                </FormControl>
            </div>
            <>
                {isBuildYourOwnView === "defaultDataGridView" && (
                    <TableContainer
                        component={Paper}
                        style={{
                            maxWidth: "85vw",
                            borderRadius: "15px",
                            margin: "10px 10px",
                        }}
                    >
                        <Table>
                            <TableHead>
                                <TableRow>
                                    <TableCell>Column</TableCell>
                                    <TableCell>Enter Column New Name</TableCell>
                                    <TableCell>Is Visible</TableCell>
                                    <TableCell>Reference data source</TableCell>
                                    <TableCell>Input Control</TableCell>
                                    <TableCell>Display Control</TableCell>
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
                                                    `${columnName.name}_new_name`
                                                    ]
                                                }
                                                onChange={(event) =>
                                                (customConfig[componentId][
                                                    `${columnName.name}_new_name`
                                                ] = event.target.value)
                                                }
                                            />
                                        </TableCell>

                                        <TableCell>
                                            <Checkbox
                                                defaultChecked={
                                                    customConfig[componentId][
                                                    `${columnName.name}_visible`
                                                    ]
                                                }
                                                onChange={(event) =>
                                                (customConfig[componentId][
                                                    `${columnName.name}_visible`
                                                ] = event.target.checked)
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
                                                            customConfig[componentId][
                                                            `${columnName.name}_ref`
                                                            ]
                                                        }
                                                        onChange={(event) => {
                                                            customConfig[componentId][
                                                                `${columnName.name}_ref`
                                                            ] = event.target.value;
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
                                                <InputLabel
                                                    id={`${columnName.name}-input-control-label`}
                                                >
                                                    Select input control
                                                </InputLabel>
                                                <Select
                                                    fullWidth
                                                    labelId={`${columnName.name}-input-control-label`}
                                                    defaultValue={
                                                        customConfig[componentId][
                                                        `${columnName.name}_control`
                                                        ]
                                                    }
                                                    onChange={(e) => {
                                                        customConfig[componentId][
                                                            `${columnName.name}_control`
                                                        ] = e.target.value;
                                                        changeRender();
                                                        return customConfig;
                                                    }}
                                                    disabled={true}
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
                                            {customConfig[componentId][
                                                `${columnName.name}_control`
                                            ] !== undefined &&
                                                (customConfig[componentId][
                                                    `${columnName.name}_control`
                                                ] === "file" ||
                                                    customConfig[componentId][
                                                    `${columnName.name}_control`
                                                    ] === "url" ||
                                                    customConfig[componentId][
                                                    `${columnName.name}_control`
                                                    ] === "signature" ||
                                                    customConfig[componentId][
                                                    `${columnName.name}_control`
                                                    ] === "code") && (
                                                    <FormControl>
                                                        <InputLabel
                                                            id={`${columnName.name}-select-label`}
                                                        >
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
                                                                <MenuItem
                                                                    key={controlName}
                                                                    value={controlName}
                                                                >
                                                                    {controlName}
                                                                </MenuItem>
                                                            ))}
                                                        </Select>
                                                    </FormControl>
                                                )}
                                        </TableCell>
                                        <TableCell>
                                            {customConfig[componentId][
                                                `${columnName.name}_control`
                                            ] !== undefined &&
                                                (customConfig[componentId][
                                                    `${columnName.name}_control`
                                                ] === "file" ||
                                                    customConfig[componentId][
                                                    `${columnName.name}_control`
                                                    ] === "signature") && (
                                                    <FormControl>
                                                        <InputLabel
                                                            id={`${columnName.name}-select-label`}
                                                        >
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
                                            {customConfig[componentId][
                                                `${columnName.name}_control`
                                            ] !== undefined &&
                                                (customConfig[componentId][
                                                    `${columnName.name}_control`
                                                ] === "file" ||
                                                    customConfig[componentId][
                                                    `${columnName.name}_control`
                                                    ] === "signature") && (
                                                    <FormControl>
                                                        <InputLabel
                                                            id={`${columnName.name}-select-label`}
                                                        >
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
                                            (customConfig[componentId][
                                                `add_new_button_new_name1`
                                            ] = event.target.value)
                                            }
                                        />
                                    </TableCell>
                                    <TableCell>
                                        <Checkbox
                                            defaultChecked={
                                                customConfig[componentId][`add_new_button_visible1`]
                                            }
                                            onChange={(event) =>
                                            (customConfig[componentId][
                                                `add_new_button_visible1`
                                            ] = event.target.checked)
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
                                <TableRow key="heading1">
                                    <TableCell> Heading1 Visible</TableCell>
                                    <TableCell></TableCell>
                                    <TableCell>
                                        <Checkbox
                                            defaultChecked={
                                                customConfig[componentId][`heading1_visible1`]
                                            }
                                            onChange={(event) =>
                                            (customConfig[componentId][`heading1_visible1`] =
                                                event.target.checked)
                                            }
                                        />
                                    </TableCell>
                                </TableRow>
                                <TableRow key="heading2">
                                    <TableCell>Heading2 Visible</TableCell>
                                    <TableCell></TableCell>
                                    <TableCell>
                                        <Checkbox
                                            defaultChecked={
                                                customConfig[componentId][`heading2_visible1`]
                                            }
                                            onChange={(event) =>
                                            (customConfig[componentId][`heading2_visible1`] =
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
                                <TableRow>
                                    <TableCell>Form Label Choice</TableCell>
                                    <TableCell></TableCell>
                                    <TableCell>
                                        <Box
                                            sx={{
                                                display: "flex",
                                                flexDirection: "row",
                                                justifyContent: "space-between",
                                            }}
                                        >
                                            <RadioGroup
                                                value={customConfig[componentId][`label_value`]}
                                                onChange={(event) => {
                                                    customConfig[componentId][`label_value`] =
                                                        event.target.value;
                                                    changeRender();
                                                }}
                                            >
                                                <FormControlLabel
                                                    value="separate_label"
                                                    control={<Radio />}
                                                    label="Separate Label"
                                                />

                                                <FormControlLabel
                                                    value="field_label"
                                                    control={<Radio />}
                                                    label="Field Label"
                                                />
                                            </RadioGroup>
                                        </Box>
                                    </TableCell>
                                </TableRow>
                                <TableRow>
                                    <>
                                        <Box
                                            className="p-1 mb-1"
                                            sx={{ display: "flex", alignItems: "center" }}
                                        >
                                            <FormControl sx={{ marginRight: "8px" }}>
                                                <InputLabel id={`viewMode-select-label`}>
                                                    Select...
                                                </InputLabel>
                                                <Select
                                                    fullWidth
                                                    labelId={`viewMode-select-label`}
                                                    defaultValue={
                                                        customConfig[componentId][`selectedView`]
                                                    }
                                                    onChange={(event) => {
                                                        customConfig[componentId][`selectedView`] =
                                                            event.target.value;
                                                        setselectedView(event.target.value);
                                                    }}
                                                >
                                                    {getViewList(
                                                        customConfig[componentId]["componentInfo"][
                                                        "component_name"
                                                        ]
                                                    ).map((controlName) => (
                                                        <MenuItem key={controlName} value={controlName}>
                                                            {controlName}
                                                        </MenuItem>
                                                    ))}
                                                </Select>
                                            </FormControl>
                                            {selectedView !== "" &&
                                                selectedView !== "defaultView" && (
                                                    <>
                                                        <Button
                                                            variant="outlined"
                                                            onMouseEnter={handlePopoverOpen}
                                                            onMouseLeave={handlePopoverClose}
                                                        >
                                                            Preview
                                                        </Button>
                                                        <Popover
                                                            className={classes.popover}
                                                            classes={{
                                                                paper: classes.paper,
                                                            }}
                                                            open={open}
                                                            anchorEl={anchorEl}
                                                            onClose={handlePopoverClose}
                                                            anchorOrigin={{
                                                                vertical: "bottom",
                                                                horizontal: "center",
                                                            }}
                                                            transformOrigin={{
                                                                vertical: "top",
                                                                horizontal: "center",
                                                            }}
                                                        >
                                                            <DemoViews selectedView={selectedView} />
                                                        </Popover>
                                                    </>
                                                )}
                                        </Box>

                                        {(() => {
                                            switch (true) {
                                                case selectedView.startsWith("defaultView"):
                                                    return null;
                                                    break;
                                                case selectedView.startsWith("gridView"):
                                                    return (
                                                        <GridView1
                                                            nconfig={nconfig}
                                                            componentId={componentId}
                                                            customConfig={customConfig}
                                                            options={options}
                                                            inputType={inputType}
                                                            errorList={errorList}
                                                        />
                                                    );
                                                case selectedView.startsWith("tableView"):
                                                    return (
                                                        <TableView
                                                            nconfig={nconfig}
                                                            componentId={componentId}
                                                            customConfig={customConfig}
                                                            options={options}
                                                            inputType={inputType}
                                                            errorList={errorList}
                                                        />
                                                    );
                                                case selectedView.startsWith("groupList"):
                                                    return (
                                                        <GridView1
                                                            nconfig={nconfig}
                                                            componentId={componentId}
                                                            customConfig={customConfig}
                                                            options={options}
                                                            inputType={inputType}
                                                            errorList={errorList}
                                                        />
                                                    );
                                                case selectedView.startsWith("detailList"):
                                                    return (
                                                        <GridView1
                                                            nconfig={nconfig}
                                                            componentId={componentId}
                                                            customConfig={customConfig}
                                                            options={options}
                                                            inputType={inputType}
                                                            errorList={errorList}
                                                        />
                                                    );
                                                case selectedView.startsWith("Calendar"):
                                                    return (
                                                        <GridView1
                                                            nconfig={nconfig}
                                                            componentId={componentId}
                                                            customConfig={customConfig}
                                                            options={options}
                                                            inputType={inputType}
                                                            errorList={errorList}
                                                        />
                                                    );
                                                case selectedView.startsWith("Kanban"):
                                                    return (
                                                        <KanbanView
                                                            nconfig={nconfig}
                                                            componentId={componentId}
                                                            customConfig={customConfig}
                                                            options={options}
                                                            inputType={inputType}
                                                            errorList={errorList}
                                                            componentName={componentName}
                                                        />
                                                    );
                                                    break;
                                                default:
                                                    return null;
                                                    break;
                                            }
                                        })()}
                                    </>
                                </TableRow>
                            </TableBody>
                        </Table>
                    </TableContainer>
                )}
                {isBuildYourOwnView === "BYOView" && (
                    <>
                        <TableContainer
                            component={Paper}
                            style={{
                                maxWidth: "85vw",
                                borderRadius: "15px",
                                margin: "10px 10px",
                            }}
                        >
                            <Table>
                                <TableHead>
                                    <TableRow>
                                        <TableCell>Column</TableCell>
                                        <TableCell>Enter Column New Name</TableCell>
                                        <TableCell>Hide Column Label ?</TableCell>
                                        <TableCell>Seperate Out Data and Column?</TableCell>
                                        <TableCell>Is Visible</TableCell>
                                        <TableCell>Reference data source</TableCell>
                                        <TableCell>Input Control</TableCell>
                                        <TableCell>Display Control</TableCell>
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
                                                        `${columnName.name}_new_name`
                                                        ]
                                                    }
                                                    onChange={(event) =>
                                                    (customConfig[componentId][
                                                        `${columnName.name}_new_name`
                                                    ] = event.target.value)
                                                    }
                                                />
                                            </TableCell>

                                            <TableCell>
                                                <Checkbox
                                                    defaultChecked={
                                                        customConfig[componentId][
                                                        `${columnName.name}_hide_column_label`
                                                        ]
                                                    }
                                                    onChange={(event) =>
                                                    (customConfig[componentId][
                                                        `${columnName.name}_hide_column_label`
                                                    ] = event.target.checked)
                                                    }
                                                />
                                            </TableCell>
                                            <TableCell>
                                                <Checkbox
                                                    defaultChecked={
                                                        customConfig[componentId][
                                                        `${columnName.name}_is_column_data_seperate`
                                                        ]
                                                    }
                                                    onChange={(event) =>
                                                    (customConfig[componentId][
                                                        `${columnName.name}_is_column_data_seperate`
                                                    ] = event.target.checked)
                                                    }
                                                />
                                            </TableCell>

                                            <TableCell>
                                                <Checkbox
                                                    defaultChecked={
                                                        customConfig[componentId][
                                                        `${columnName.name}_visible`
                                                        ]
                                                    }
                                                    onChange={(event) =>
                                                    (customConfig[componentId][
                                                        `${columnName.name}_visible`
                                                    ] = event.target.checked)
                                                    }
                                                />
                                            </TableCell>
                                            <TableCell>
                                                {columnName.fkey && (
                                                    <FormControl>
                                                        <InputLabel
                                                            id={`${columnName.name}-select-label`}
                                                        >
                                                            Select...
                                                        </InputLabel>
                                                        <Select
                                                            fullWidth
                                                            labelId={`${columnName.name}-select-label`}
                                                            defaultValue={
                                                                customConfig[componentId][
                                                                `${columnName.name}_ref`
                                                                ]
                                                            }
                                                            onChange={(event) => {
                                                                customConfig[componentId][
                                                                    `${columnName.name}_ref`
                                                                ] = event.target.value;
                                                                customConfig[componentId][
                                                                    `${columnName.name}_ref_slice`
                                                                ] = columnName.slice;
                                                            }}
                                                        >
                                                            {refcol[index].map((controlName) => (
                                                                <MenuItem
                                                                    key={controlName}
                                                                    value={controlName}
                                                                >
                                                                    {controlName}
                                                                </MenuItem>
                                                            ))}
                                                        </Select>
                                                    </FormControl>
                                                )}
                                            </TableCell>
                                            <TableCell>
                                                <FormControl>
                                                    <InputLabel
                                                        id={`${columnName.name}-input-control-label`}
                                                    >
                                                        Select input control
                                                    </InputLabel>
                                                    <Select
                                                        fullWidth
                                                        labelId={`${columnName.name}-input-control-label`}
                                                        defaultValue={
                                                            customConfig[componentId][
                                                            `${columnName.name}_control`
                                                            ]
                                                        }
                                                        onChange={(e) => {
                                                            customConfig[componentId][
                                                                `${columnName.name}_control`
                                                            ] = e.target.value;
                                                            changeRender();
                                                            return customConfig;
                                                        }}
                                                        disabled={true}
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
                                                {customConfig[componentId][
                                                    `${columnName.name}_control`
                                                ] !== undefined &&
                                                    (customConfig[componentId][
                                                        `${columnName.name}_control`
                                                    ] === "file" ||
                                                        customConfig[componentId][
                                                        `${columnName.name}_control`
                                                        ] === "url" ||
                                                        customConfig[componentId][
                                                        `${columnName.name}_control`
                                                        ] === "signature") && (
                                                        <FormControl>
                                                            <InputLabel
                                                                id={`${columnName.name}-select-label`}
                                                            >
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
                                                                    <MenuItem
                                                                        key={controlName}
                                                                        value={controlName}
                                                                    >
                                                                        {controlName}
                                                                    </MenuItem>
                                                                ))}
                                                            </Select>
                                                        </FormControl>
                                                    )}
                                            </TableCell>
                                            <TableCell>
                                                {customConfig[componentId][
                                                    `${columnName.name}_control`
                                                ] !== undefined &&
                                                    (customConfig[componentId][
                                                        `${columnName.name}_control`
                                                    ] === "file" ||
                                                        customConfig[componentId][
                                                        `${columnName.name}_control`
                                                        ] === "signature") && (
                                                        <FormControl>
                                                            <InputLabel
                                                                id={`${columnName.name}-select-label`}
                                                            >
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
                                                {customConfig[componentId][
                                                    `${columnName.name}_control`
                                                ] !== undefined &&
                                                    (customConfig[componentId][
                                                        `${columnName.name}_control`
                                                    ] === "file" ||
                                                        customConfig[componentId][
                                                        `${columnName.name}_control`
                                                        ] === "signature") && (
                                                        <FormControl>
                                                            <InputLabel
                                                                id={`${columnName.name}-select-label`}
                                                            >
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
                                                {customConfig[componentId][
                                                    `${columnName.name}_control`
                                                ] !== undefined &&
                                                    (customConfig[componentId][
                                                        `${columnName.name}_control`
                                                    ] === "file" ||
                                                        customConfig[componentId][
                                                        `${columnName.name}_control`
                                                        ] === "signature") && (
                                                        <FormControl>
                                                            <InputLabel
                                                                id={`${columnName.name}-select-label`}
                                                            >
                                                                Select...
                                                            </InputLabel>
                                                            <Select
                                                                fullWidth
                                                                labelId={`${columnName.name}-select-label`}
                                                                defaultValue={
                                                                    customConfig[componentId][
                                                                    `${columnName.name}_bucket_folder`
                                                                    ]
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
                                                                            controlName.folder_name.lastIndexOf(
                                                                                "_"
                                                                            ) !== -1
                                                                            ? controlName.folder_name.substring(
                                                                                0,
                                                                                controlName.folder_name.lastIndexOf(
                                                                                    "_"
                                                                                )
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
                                    <TableRow key="add new button">
                                        <TableCell> Add New Button Option</TableCell>
                                        <TableCell>
                                            <TextField
                                                defaultValue={
                                                    customConfig[componentId][
                                                    `add_new_button_new_name1`
                                                    ]
                                                }
                                                onChange={(event) =>
                                                (customConfig[componentId][
                                                    `add_new_button_new_name1`
                                                ] = event.target.value)
                                                }
                                            />
                                        </TableCell>
                                        <TableCell>
                                            <Checkbox
                                                defaultChecked={
                                                    customConfig[componentId][`add_new_button_visible1`]
                                                }
                                                onChange={(event) =>
                                                (customConfig[componentId][
                                                    `add_new_button_visible1`
                                                ] = event.target.checked)
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
                                                (customConfig[componentId][
                                                    `search_button_visible1`
                                                ] = event.target.checked)
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
                                    <TableRow key="heading1">
                                        <TableCell> Heading1 Visible</TableCell>
                                        <TableCell></TableCell>
                                        <TableCell>
                                            <Checkbox
                                                defaultChecked={
                                                    customConfig[componentId][`heading1_visible1`]
                                                }
                                                onChange={(event) =>
                                                (customConfig[componentId][`heading1_visible1`] =
                                                    event.target.checked)
                                                }
                                            />
                                        </TableCell>
                                    </TableRow>
                                    <TableRow key="heading2">
                                        <TableCell>Heading2 Visible</TableCell>
                                        <TableCell></TableCell>
                                        <TableCell>
                                            <Checkbox
                                                defaultChecked={
                                                    customConfig[componentId][`heading2_visible1`]
                                                }
                                                onChange={(event) =>
                                                (customConfig[componentId][`heading2_visible1`] =
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
                                                (customConfig[componentId][
                                                    `delete_button_visible1`
                                                ] = event.target.checked)
                                                }
                                            />
                                        </TableCell>
                                    </TableRow>
                                    <TableRow>
                                        <TableCell>Form Label Choice</TableCell>
                                        <TableCell></TableCell>
                                        <TableCell>
                                            <Box
                                                sx={{
                                                    display: "flex",
                                                    flexDirection: "row",
                                                    justifyContent: "space-between",
                                                }}
                                            >
                                                <RadioGroup
                                                    value={customConfig[componentId][`label_value`]}
                                                    onChange={(event) => {
                                                        customConfig[componentId][`label_value`] =
                                                            event.target.value;
                                                        changeRender();
                                                    }}
                                                >
                                                    <FormControlLabel
                                                        value="separate_label"
                                                        control={<Radio />}
                                                        label="Separate Label"
                                                    />

                                                    <FormControlLabel
                                                        value="field_label"
                                                        control={<Radio />}
                                                        label="Field Label"
                                                    />
                                                </RadioGroup>
                                            </Box>
                                        </TableCell>
                                    </TableRow>
                                    {/* <TableRow>
                    <>
                      <Box
                        className="p-1 mb-1"
                        sx={{ display: "flex", alignItems: "center" }}
                      >
                        <FormControl sx={{ marginRight: "8px" }}>
                          <InputLabel id={`viewMode-select-label`}>
                            Select...
                          </InputLabel>
                          <Select
                            fullWidth
                            labelId={`viewMode-select-label`}
                            defaultValue={
                              customConfig[componentId][`selectedView`]
                            }
                            onChange={(event) => {
                              customConfig[componentId][`selectedView`] =
                                event.target.value;
                              setselectedView(event.target.value);
                            }}
                          >
                            {getViewList(
                              customConfig[componentId]["componentInfo"][
                                "component_name"
                              ]
                            ).map((controlName) => (
                              <MenuItem key={controlName} value={controlName}>
                                {controlName}
                              </MenuItem>
                            ))}
                          </Select>
                        </FormControl>
                        {selectedView !== "" &&
                          selectedView !== "defaultView" && (
                            <>
                              <Button
                                variant="outlined"
                                onMouseEnter={handlePopoverOpen}
                                onMouseLeave={handlePopoverClose}
                              >
                                Preview
                              </Button>
                              <Popover
                                className={classes.popover}
                                classes={{
                                  paper: classes.paper,
                                }}
                                open={open}
                                anchorEl={anchorEl}
                                onClose={handlePopoverClose}
                                anchorOrigin={{
                                  vertical: "bottom",
                                  horizontal: "center",
                                }}
                                transformOrigin={{
                                  vertical: "top",
                                  horizontal: "center",
                                }}
                              >
                                <DemoViews selectedView={selectedView} />
                              </Popover>
                            </>
                          )}
                      </Box>

                      {(() => {
                        switch (true) {
                          case selectedView.startsWith("defaultView"):
                            return null;
                            break;
                          case selectedView.startsWith("gridView"):
                            return (
                              <GridView1
                                nconfig={nconfig}
                                componentId={componentId}
                                customConfig={customConfig}
                                options={options}
                                inputType={inputType}
                                errorList={errorList}
                              />
                            );
                          case selectedView.startsWith("tableView"):
                            return (
                              <TableView
                                nconfig={nconfig}
                                componentId={componentId}
                                customConfig={customConfig}
                                options={options}
                                inputType={inputType}
                                errorList={errorList}
                              />
                            );
                          case selectedView.startsWith("groupList"):
                            return (
                              <GridView1
                                nconfig={nconfig}
                                componentId={componentId}
                                customConfig={customConfig}
                                options={options}
                                inputType={inputType}
                                errorList={errorList}
                              />
                            );
                          case selectedView.startsWith("detailList"):
                            return (
                              <GridView1
                                nconfig={nconfig}
                                componentId={componentId}
                                customConfig={customConfig}
                                options={options}
                                inputType={inputType}
                                errorList={errorList}
                              />
                            );
                          case selectedView.startsWith("Calendar"):
                            return (
                              <GridView1
                                nconfig={nconfig}
                                componentId={componentId}
                                customConfig={customConfig}
                                options={options}
                                inputType={inputType}
                                errorList={errorList}
                              />
                            );
                          case selectedView.startsWith("Kanban"):
                            return (
                              <KanbanView
                                nconfig={nconfig}
                                componentId={componentId}
                                customConfig={customConfig}
                                options={options}
                                inputType={inputType}
                                errorList={errorList}
                                componentName={componentName}
                              />
                            );
                            break;
                          default:
                            return null;
                            break;
                        }
                      })()}
                    </>
                  </TableRow> */}
                                </TableBody>
                            </Table>
                        </TableContainer>
                        <Box
                            className="p-25 mb-1"
                            sx={{ display: "flex", alignItems: "center", mb: 5 }}
                        >
                            <FormControl
                                variant="outlined"
                                size="small"
                                sx={{ width: "50%" }}
                            >
                                <InputLabel id="num-rows-label">
                                    Number of Rows you want to display
                                </InputLabel>
                                <Select
                                    labelId="grid-size-label"
                                    id="grid-size-select"
                                    value={selectedRows}
                                    onChange={handleRowsChange}
                                    label="Select Grid Size"
                                >
                                    {[
                                        "1",
                                        "2",
                                        "3",
                                        "4",
                                        "5",
                                        "6",
                                        "7",
                                        "8",
                                        "9",
                                        "10",
                                        "11",
                                        "12",
                                    ].map((option) => (
                                        <MenuItem key={option} value={option}>
                                            {option}
                                        </MenuItem>
                                    ))}
                                </Select>
                            </FormControl>
                        </Box>

                        <Box
                            className="p-10 mb-1"
                            sx={{ display: "flex", alignItems: "center" }}
                        >
                            <FormControl
                                variant="outlined"
                                size="small"
                                sx={{ width: "50%" }}
                            >
                                <InputLabel id="num-columns-label">
                                    Number of Columns you want to render on page
                                </InputLabel>
                                <Select
                                    labelId="num-columns-label"
                                    id="num-columns-select"
                                    value={selectedColumns}
                                    onChange={handleColumnsChange}
                                    label="Number of Columns you want to render on page" // Ensures the label moves out of the way when the select is opened
                                >
                                    {Array.from({ length: 1000 }, (_, i) => (
                                        <MenuItem key={i + 1} value={i + 1}>
                                            {i + 1}
                                        </MenuItem>
                                    ))}
                                </Select>
                            </FormControl>
                        </Box>
                        <CustomGridLayoutConfig
                            layoutItems={layoutItems}
                            filteredColumns={filteredColumns}
                            onLayoutUpdate={handleLayoutUpdate}
                        />
                    </>
                )}
            </>
        </>
    );
};
export default DisplayConfig;
