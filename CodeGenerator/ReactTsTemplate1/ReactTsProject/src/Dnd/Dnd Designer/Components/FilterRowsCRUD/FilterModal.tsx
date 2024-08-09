import React, { useState } from "react";
import { Row, Col, Form } from "react-bootstrap";
import { RootState } from "redux/reducers";
import { useSelector } from "react-redux";
import styles from "./SlideDrawer.module.css";
import { MdHelp } from "react-icons/md";
import "react-tooltip/dist/react-tooltip.css";
// import { Tooltip } from "react-tooltip";
import { IEngagementtypeiData } from "redux/slices/engagementtype";
import { IContentstatusiData } from "redux/slices/contentstatus";
import { ISocial_media_usersiData } from "redux/slices/social_media_users";
import { ISocialmediaaccountsiData } from "redux/slices/socialmediaaccounts";
import { IEngagementiData } from "redux/slices/engagement";
import { IAnalyticsiData } from "redux/slices/analytics";
import { IInfluenceriData } from "redux/slices/influencer";
import { ICollaborationiData } from "redux/slices/collaboration";
import { IS3bucketiData } from "redux/slices/s3bucket";
import { IContentiData } from "redux/slices/content";

import Tooltip from "@mui/material/Tooltip";
import IconButton from "@mui/material/IconButton";
import {
  Typography,
  Table,
  TableBody,
  Button,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  Drawer,
  Grid,
  FormControl,
  InputLabel,
  Select,
  MenuItem,
  TextField,
  Box,
} from "@mui/material";

interface FilterModalProps {
  show: boolean;
  handleClose: () => void;
  onSubmit: (output: string) => void;
  componentName;
}

interface FilterRow {
  selectedColumn: string;
  selectedOperator: string;
  inputValue: string;
  logicalOperator: string;
}

const FilterModal: React.FC<FilterModalProps> = ({
  show,
  handleClose,
  onSubmit,
  componentName,
}) => {
  const engagementtypeData = useSelector(
    (state: RootState) => state.engagementtype
  );
  const contentstatusData = useSelector(
    (state: RootState) => state.contentstatus
  );
  const social_media_usersData = useSelector(
    (state: RootState) => state.social_media_users
  );
  const socialmediaaccountsData = useSelector(
    (state: RootState) => state.socialmediaaccounts
  );
  const engagementData = useSelector((state: RootState) => state.engagement);
  const analyticsData = useSelector((state: RootState) => state.analytics);
  const influencerData = useSelector((state: RootState) => state.influencer);
  const collaborationData = useSelector(
    (state: RootState) => state.collaboration
  );
  const s3bucketData = useSelector((state: RootState) => state.s3bucket);
  const contentData = useSelector((state: RootState) => state.content);

  const rData = useSelector((state: RootState) => state[componentName]);
  const [selectedObject, setSelectedObject] = useState("");
  const [filteredData, setFilteredData] = useState<any[]>([]);
  const [combinedData, setCombinedData] = useState<any[]>([]);

  const [filterRows, setFilterRows] = useState<FilterRow[]>([
    {
      selectedColumn: "",
      selectedOperator: ">=",
      inputValue: "",
      logicalOperator: "&&",
    },
  ]);

  function getPropertiesFromObject(obj: object): string[] {
    return Object.keys(obj);
  }
  const [slices, setSlices] = useState<{ [key: string]: string[] }>({
    engagementtype: getPropertiesFromObject(IEngagementtypeiData),
    contentstatus: getPropertiesFromObject(IContentstatusiData),
    social_media_users: getPropertiesFromObject(ISocial_media_usersiData),
    socialmediaaccounts: getPropertiesFromObject(ISocialmediaaccountsiData),
    engagement: getPropertiesFromObject(IEngagementiData),
    analytics: getPropertiesFromObject(IAnalyticsiData),
    influencer: getPropertiesFromObject(IInfluenceriData),
    collaboration: getPropertiesFromObject(ICollaborationiData),
    s3bucket: getPropertiesFromObject(IS3bucketiData),
    content: getPropertiesFromObject(IContentiData),
  });
  const updateFilterRow = (
    index: number,
    key: keyof FilterRow,
    value: string
  ) => {
    const newFilterRows = [...filterRows];
    newFilterRows[index][key] = value;
    setFilterRows(newFilterRows);
  };

  const [selectedSlice, setSelectedSlice] = useState("");
  const applyFilterOnSliceSelection = () => {
    if (!selectedReduxSlice) return;

    const filtered = selectedReduxSlice.list.filter((stack: any) => {
      return filterRows.every((row) => {
        if (!row.selectedColumn || !row.inputValue) return true;

        const value = parseFloat(row.inputValue);
        const stackValue = parseFloat(stack[row.selectedColumn]);

        switch (row.selectedOperator) {
          case ">=":
            return stackValue >= value;
          case "<=":
            return stackValue <= value;
          case "==":
            return stackValue === value;
          case "!=":
            return stackValue !== value;
          default:
            return true;
        }
      });
    });

   //console.log(filtered);
    setFilteredData(filtered);
  };

  const handleSliceSelection = (selectedSlice: string) => {
    setSelectedSlice(selectedSlice);
   //console.log(selectedSlice);
    switch (selectedSlice) {
      case "engagementtype":
        setSelectedReduxSlice(engagementtypeData);
        break;
      case "contentstatus":
        setSelectedReduxSlice(contentstatusData);
        break;
      case "social_media_users":
        setSelectedReduxSlice(social_media_usersData);
        break;
      case "socialmediaaccounts":
        setSelectedReduxSlice(socialmediaaccountsData);
        break;
      case "engagement":
        setSelectedReduxSlice(engagementData);
        break;
      case "analytics":
        setSelectedReduxSlice(analyticsData);
        break;
      case "influencer":
        setSelectedReduxSlice(influencerData);
        break;
      case "collaboration":
        setSelectedReduxSlice(collaborationData);
        break;
      case "s3bucket":
        setSelectedReduxSlice(s3bucketData);
        break;
      case "content":
        setSelectedReduxSlice(contentData);
        break;
      default:
        setSelectedReduxSlice(null);
    }
  };
  React.useEffect(() => {
    applyFilterOnSliceSelection();
  }, [selectedSlice, filterRows]);

  //Dev2
  React.useEffect(() => {
    // setCombinedData((prevData) => [...prevData, ...filteredData]);
    setCombinedData(filteredData);
  }, [filteredData]);

  const [selectedReduxSlice, setSelectedReduxSlice] = useState<any>(null);
  const handleDropdownChange = (e: any) => {
   //console.log("handleDropdownChange called");
    const selectedSliceName = e.target.value;
    setSelectedObject(selectedSliceName);

    switch (selectedSliceName) {
      case "engagementtype":
        setSelectedReduxSlice(engagementtypeData);
        break;
      case "contentstatus":
        setSelectedReduxSlice(contentstatusData);
        break;
      case "social_media_users":
        setSelectedReduxSlice(social_media_usersData);
        break;
      case "socialmediaaccounts":
        setSelectedReduxSlice(socialmediaaccountsData);
        break;
      case "engagement":
        setSelectedReduxSlice(engagementData);
        break;
      case "analytics":
        setSelectedReduxSlice(analyticsData);
        break;
      case "influencer":
        setSelectedReduxSlice(influencerData);
        break;
      case "collaboration":
        setSelectedReduxSlice(collaborationData);
        break;
      case "s3bucket":
        setSelectedReduxSlice(s3bucketData);
        break;
      case "content":
        setSelectedReduxSlice(contentData);
        break;
      default:
        setSelectedReduxSlice(null);
    }
  };

  const handleApplyFilter = () => {
    if (!selectedReduxSlice) return;
   //console.log("Apply filter clicked", filterRows);
    let filterString = `selectedReduxSlice.list.find(stack => `;
    filterRows.forEach((row, index) => {
      filterString += `stack.${row.selectedColumn} ${row.selectedOperator} ${row.inputValue}`;
      if (index < filterRows.length - 1) {
        filterString += ` ${row.logicalOperator} `;
      }
    });
    filterString += `).${selectedObject}`;
   //console.log("The selected slice is:", selectedSlice);
   //console.log(eval(filterString));
    // //console.log(selectedReduxSlice.list);

    onSubmit(JSON.stringify(eval(filterString)));
    // Update the filteredData state

    // Filter the selectedReduxSlice data based on the filterRows conditions
  };
  const renderSelectedData = () => {
    if (combinedData.length === 0) {
      return <div>No data to display.</div>;
    }

    return (
      <TableContainer>
        <Table>
          <TableHead>
            <TableRow>
              {slices[selectedSlice].map((colName) => (
                <TableCell key={colName}>{colName}</TableCell>
              ))}
            </TableRow>
          </TableHead>
          <TableBody>
            {combinedData.map((row, index) => (
              <TableRow key={index}>
                {slices[selectedSlice].map((colName) => (
                  <TableCell key={colName}>{row[colName]}</TableCell>
                ))}
              </TableRow>
            ))}
          </TableBody>
        </Table>
      </TableContainer>
    );
  };

  return (
    <>
      {show && (
        <div className={styles.drawerOverlay} onClick={handleClose}>
          <div
            className={`${styles.drawerContent} ${
              show ? styles.open : styles.closed
            }`}
            onClick={(e) => e.stopPropagation()}
          >
            <Grid container spacing={2}>
              <Grid item xs={12}>
                <Typography variant="h3">
                  Data Source Filter{" "}
                  <Tooltip title="Configure Data Filter" placement="right">
                    <IconButton size="small">
                      <MdHelp id="filter-help-4" />
                    </IconButton>
                  </Tooltip>
                </Typography>
              </Grid>
              <Grid item xs={12}>
                {filterRows.map((row, index) => (
                  <Grid container spacing={2} key={index}>
                    <Grid item xs={3}>
                      <FormControl fullWidth>
                        <InputLabel>Select Data Source</InputLabel>
                        <Select
                          value={selectedSlice}
                          onChange={(e) => handleSliceSelection(e.target.value)}
                        >
                          <MenuItem value="">Select Data Source</MenuItem>
                          {Object.keys(slices).map((sliceName) => (
                            <MenuItem key={sliceName} value={sliceName}>
                              {sliceName}
                            </MenuItem>
                          ))}
                        </Select>
                      </FormControl>
                    </Grid>
                    <Grid item xs={3}>
                      <FormControl fullWidth>
                        <InputLabel>Select column</InputLabel>
                        <Select
                          value={row.selectedColumn}
                          onChange={(e) =>
                            updateFilterRow(
                              index,
                              "selectedColumn",
                              e.target.value
                            )
                          }
                        >
                          <MenuItem value="">Select column</MenuItem>
                          {selectedSlice &&
                            slices[selectedSlice].map((property) => (
                              <MenuItem key={property} value={property}>
                                {property}
                              </MenuItem>
                            ))}
                        </Select>
                      </FormControl>
                    </Grid>
                    <Grid item xs={3}>
                      <FormControl fullWidth>
                        <InputLabel>Select operator</InputLabel>
                        <Select
                          value={row.selectedOperator}
                          onChange={(e) =>
                            updateFilterRow(
                              index,
                              "selectedOperator",
                              e.target.value
                            )
                          }
                        >
                          <MenuItem value=">=">{">="}</MenuItem>
                          <MenuItem value="<=">{"<="}</MenuItem>
                          <MenuItem value="==">{"=="}</MenuItem>
                          <MenuItem value="!=">{"!="}</MenuItem>
                        </Select>
                      </FormControl>
                    </Grid>
                    <Grid item xs={3}>
                      {index > 0 && (
                        <FormControl fullWidth>
                          <InputLabel>Select logical operator</InputLabel>
                          <Select
                            value={row.logicalOperator}
                            onChange={(e) =>
                              updateFilterRow(
                                index,
                                "logicalOperator",
                                e.target.value
                              )
                            }
                          >
                            <MenuItem value="&&">{"&&"}</MenuItem>
                            <MenuItem value="||">{"||"}</MenuItem>
                          </Select>
                        </FormControl>
                      )}
                      <TextField
                        fullWidth
                        label="Enter value"
                        value={row.inputValue}
                        onChange={(e) =>
                          updateFilterRow(index, "inputValue", e.target.value)
                        }
                      />
                    </Grid>
                  </Grid>
                ))}
              </Grid>

              <Grid item xs={12}>
                <FormControl fullWidth>
                  <InputLabel>
                    Select the column you want to see the value.
                  </InputLabel>
                  <Select
                    value={selectedObject}
                    onChange={(e) => setSelectedObject(e.target.value)}
                  >
                    <MenuItem value="">
                      Select the column you want to see the value.
                    </MenuItem>
                    {selectedSlice &&
                      slices[selectedSlice].map((property) => (
                        <MenuItem key={property} value={property}>
                          {property}
                        </MenuItem>
                      ))}
                  </Select>
                </FormControl>
              </Grid>
              <Grid item xs={6}>
                <Grid container spacing={2}>
                  <Grid item xs={3}>
                    <Button variant="contained" onClick={handleApplyFilter}>
                      Apply Filter
                    </Button>
                  </Grid>
                  <Grid item xs={3}>
                    <Button variant="contained" onClick={handleClose}>
                      Close
                    </Button>
                  </Grid>
                </Grid>
              </Grid>
              <Grid item xs={12}>
                <Typography variant="h4">Selected Data Source</Typography>
                {renderSelectedData()}
              </Grid>
            </Grid>
          </div>
        </div>
      )}
    </>
  );
};

export default FilterModal;
