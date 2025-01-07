import React, { useState } from "react";

import {
    Container,
    Grid,
    TextField,
    Button,
    FormControl,
    Select,
    MenuItem,
    InputLabel,
} from "@mui/material";
import { RootState } from "redux/reducers";
import { useSelector } from "react-redux";
import FilterModal from "./FilterModal";
export interface FilterModel {
    columnName: string;
    columnCondition: string;
    columnValue: string;
}

interface FilterFormProps {
    columnNames: string[];
    columnCondition: string[];
    onSubmit: (output: FilterModel[]) => void;
    selectedConditons;
    componentName;
}

const FilterForm: React.FC<FilterFormProps> = ({
    columnNames,
    columnCondition,
    onSubmit,
    selectedConditons,
    componentName,
}) => {
    const [conditions, setConditions] = useState<FilterModel[]>([]);

    const [selectedStack, setSelectedStack] = useState<number>(-1);
    const [selectedColumn, setSelectedColumn] = useState("");
    const [selectedOperator, setSelectedOperator] = useState(">=");
    const [inputValue, setInputValue] = useState("");
    const [showModal, setShowModal] = useState<number | null>(null);

    //Dev2
    React.useEffect(() => {
        if (selectedConditons !== undefined && selectedConditons !== null) {
            setConditions(eval(selectedConditons));
        }
    }, [selectedConditons, setConditions]);

    const rData = useSelector((state: RootState) => state[componentName]);
    const handleModalClose = () => {
        setShowModal(null);
    };

    const handleModalSubmit = (index: number, output: string) => {
        // Update the input value with the filter string from the modal for the specific condition
        updateCondition(index, "columnValue", output);
        onSubmit(conditions);
        applyFilter();
        handleSubmit();
        setShowModal(null);
    };
    const handleModelDelete = (index) => {
        const newc = conditions.filter((con, idx) => index !== idx);
        console.log("Condition removed");
        console.log(newc);
        setConditions(newc);
        onSubmit(newc);
    };
    const addCondition = () => {
        setConditions((prevConditions) => [
            ...prevConditions,
            {
                columnName: "",
                columnCondition: "1",
                columnValue: "",
            },
        ]);
    };

    const updateCondition = (
        index: number,
        key: keyof FilterModel,
        value: string
    ) => {
        const newConditions = [...conditions];
        newConditions[index][key] = value;
        setConditions(newConditions);
    };

    const applyFilter = () => {
        if (selectedStack !== -1 && selectedColumn && inputValue) {
            const filteredStacks = rData.list.filter((stack) =>
                eval(`stack.${selectedColumn} ${selectedOperator} ${inputValue}`)
            );

            const filterString = `rData.list.filter(stack => stack.${selectedColumn} ${selectedOperator} ${inputValue})`;
            updateCondition(selectedStack, "columnValue", filterString);
        }
    };
    const evaluateFilter = (filterString: string) => {
        try {
            const result = eval(filterString);
            return JSON.stringify(result);
        } catch (error) {
            return filterString;
        }
    };

    const handleSubmit = () => {
        onSubmit(conditions);
        // Execute the filter string and log the result
        if (conditions.length > 0 && conditions[0].columnValue) {
            const executedResult = eval(conditions[0].columnValue);
            console.log('Executed value:', executedResult);
        }
    };

    return (
        <Container>
            {conditions.map((condition, index) => (
                <React.Fragment key={index}>
                    <Grid
                        container
                        spacing={{ xs: 2, md: 3 }}
                        columns={{ xs: 4, sm: 8, md: 12 }}
                        key={index}
                    >
                        <Grid item xs={12} md={2}>
                            <FormControl fullWidth>
                                <InputLabel>Select column name</InputLabel>
                                <Select
                                    value={condition.columnName}
                                    onChange={(e) =>
                                        updateCondition(
                                            index,
                                            "columnName",
                                            e.target.value as string
                                        )
                                    }
                                >
                                    <MenuItem value="">
                                        <em>None</em>
                                    </MenuItem>
                                    {columnNames.map((name) => (
                                        <MenuItem key={name} value={name}>
                                            {name}
                                        </MenuItem>
                                    ))}
                                </Select>
                            </FormControl>
                        </Grid>
                        <Grid item xs={12} md={2}>
                            <FormControl fullWidth>
                                <InputLabel>Select column condition</InputLabel>
                                <Select
                                    fullWidth
                                    value={condition.columnCondition}
                                    onChange={(e) =>
                                        updateCondition(
                                            index,
                                            "columnCondition",
                                            e.target.value as string
                                        )
                                    }
                                >
                                    {columnCondition.map((logic, index) => (
                                        <MenuItem
                                            key={index.toString() + logic}
                                            value={(index + 1).toString()}
                                        >
                                            {logic}
                                        </MenuItem>
                                    ))}
                                </Select>
                            </FormControl>
                        </Grid>
                        <Grid item xs={12} md={2}>
                            <TextField
                                fullWidth
                                disabled
                                type="text"
                                placeholder="Value"
                                value={evaluateFilter(condition.columnValue)}
                            />
                        </Grid>
                        <Grid item xs={12} md={2}>
                            <TextField
                                fullWidth
                                disabled
                                type="text"
                                placeholder="Evaluated Value"
                                value={evaluateFilter(condition.columnValue)}
                            />
                        </Grid>
                        <Grid item xs={12} md={2}>
                            <Button
                                variant="contained"
                                color="primary"
                                onClick={() => setShowModal(index)}
                            >
                                Select Value
                            </Button>
                        </Grid>
                        <Grid item xs={12} md={2}>
                            <Button
                                variant="contained"
                                color="secondary"
                                onClick={() => handleModelDelete(index)}
                            >
                                Remove
                            </Button>
                        </Grid>
                    </Grid>
                    <FilterModal
                        show={showModal === index}
                        componentName={componentName}
                        handleClose={handleModalClose}
                        onSubmit={(output) => handleModalSubmit(index, output)}
                    />
                </React.Fragment>
            ))}
            <Grid
                container
                item
                spacing={{ xs: 2, md: 3 }}
                columns={{ xs: 4, sm: 8, md: 12 }}
            >
                <Grid item xs={12}>
                    <Button variant="contained" color="primary" onClick={addCondition}>
                        Add Condition
                    </Button>
                </Grid>
            </Grid>
        </Container>
    );
};

export default FilterForm;
export { };
