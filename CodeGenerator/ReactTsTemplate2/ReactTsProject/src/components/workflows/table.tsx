// import React, { useEffect, useState } from "react";
// import DataTable from 'react-data-table-component';
// import { useSelector } from "react-redux";
// import { RootState } from "redux/reducers";
// import { useAppDispatch } from "redux/store";
// // import { resetWorkflow_BuildsToInit, resetWorkflow_RunsToInit, resetWorkflowsToInit, resetWorkflows_ProjectsToInit, setWorkflow_BuildsList, setWorkflow_BuildsMessage, setWorkflow_RunsList, setWorkflow_RunsMessage, setWorkflowsMessage, setWorkflows_ProjectsList, setWorkflows_ProjectsMessage } from "redux/actions";
// import { Button, Card, Col, Form, InputGroup } from "react-bootstrap";
// import { Constant } from "template/Constant";
// import ConfirmationModal from "template/ConfirmationModal";
// import { deleteWorkflows } from "services/workflowsService";
// // import { getOneWorkflow_Builds, getWorkflow_Builds, updateWorkflow_Builds } from "services/workflow_buildsService";
// // import { getOneWorkflow_Runs, getWorkflow_Runs, updateWorkflow_Runs } from "services/workflow_runsService";
// // import { getWorkflows_Projects } from "services/workflows_projectsService";
// // import LoadingSpinner from "template/Loading";
// type Props = {
//     hideShowForm: (action) => void;
//     handleRowEdit: (row) => void;
//     getData: (page, pageSize, searchKey) => void;
//     projectId;
// };
// export const WorkflowsTable: React.FC<Props> = ({ hideShowForm, handleRowEdit, getData, projectId }) => {
//     const dispatch = useAppDispatch();
//     const [search, setSearch] = useState('');
//     const [showDelete, setShowDelete] = useState(false);
//     const [rowData, setRowData] = useState(undefined);
//     const workflowsData = useSelector((state: RootState) => state.workflows);

//     const workflowBuildsData = useSelector((state: RootState) => state.workflow_builds);
//     const workflowRunsData = useSelector((state: RootState) => state.workflow_runs);
//     const workflowProjectsdata = useSelector((state: RootState) => state.workflows_projects);

//     const getWorkflowBuilds = (page, pageSize, searchKey) => {
//         getWorkflow_Builds(page, pageSize, searchKey).then((response) => {
//             if (response && response.records) {
//                 dispatch(setWorkflow_BuildsList({ pageNo: page, pageSize: pageSize, list: response.records, totalCount: response.total_count, searchKey: searchKey }));
//             } else {
//                 dispatch(setWorkflowsMessage("No Record Found"));
//             }
//         })
//     }

//     const getWorkflowRuns = (page, pageSize, searchKey) => {
//         getWorkflow_Runs(page, pageSize, searchKey).then((response) => {
//             if (response && response.records) {
//                 dispatch(setWorkflow_RunsList({ pageNo: page, pageSize: pageSize, list: response.records, totalCount: response.total_count, searchKey: searchKey }));
//             } else {
//                 dispatch(setWorkflow_RunsMessage("No Record Found"));
//             }
//         })
//     }

//     const getWorkflow_projects = (page, pageSize, searchKey) => {
//         getWorkflows_Projects(page, pageSize, searchKey).then((response) => {
//             const desiredRecords = response.records.filter(record => record.project_id === parseInt(projectId));
//             console.log("getWorkflows_Projects : ", response.records);
//             if (response && desiredRecords) {
//                 dispatch(setWorkflows_ProjectsList({ pageNo: page, pageSize: pageSize, list: desiredRecords, totalCount: desiredRecords.total_count, searchKey: searchKey }));
//             } else {
//                 dispatch(setWorkflows_ProjectsMessage("No Record Found"));
//             }
//         })
//     }

//     const handleSearch = () => {
//         if (search.length > 0) {
//             getData(Constant.defaultPageNumber, Constant.defaultPageSize, search);
//         }
//     }
//     // const handlePerRowsChange = async (newPerPage, page) => {
//     //     await getData(page, newPerPage, '');
//     // }
//     // const handlePageChange = (page) => {
//     //     getData(page, workflowsData.pageSize, '');
//     // };
//     // const handleRowDeleteClick = (row) => {
//     //     setRowData(row);
//     //     setShowDelete(true);
//     // }

//     const handlePerRowsChange = async (newPerPage, page) => {
//         await getData(page, newPerPage, '');
//         await getWorkflowBuilds(page, newPerPage, '');
//         await getWorkflowRuns(page, newPerPage, '');
//         await getWorkflow_projects(page, newPerPage, '');
//     }
//     const handlePageChange = (page) => {
//         getData(page, workflowsData.pageSize, '');
//         getWorkflowBuilds(page, workflowBuildsData.pageSize, '');
//         getWorkflowRuns(page, workflowRunsData.pageSize, '');
//         getWorkflow_projects(page, workflowProjectsdata.pageSize, '');
//     };
//     const handleRowDeleteClick = (row) => {
//         setRowData(row);
//         setShowDelete(true);
//     }


//     useEffect(() => {
//         // if (workflowsData && workflowsData.list && workflowsData.list.length === 0) {
//         //     dispatch(resetWorkflowsToInit());
//         //     getData(Constant.defaultPageNumber, Constant.defaultPageSize, '');
//         // }
//         if (workflowsData && workflowsData.list && workflowsData.list.length === 0) {
//             dispatch(resetWorkflowsToInit());
//             dispatch(resetWorkflow_BuildsToInit());
//             dispatch(resetWorkflow_RunsToInit());
//             dispatch(resetWorkflows_ProjectsToInit());
//             getData(Constant.defaultPageNumber, Constant.defaultPageSize, '');
//             getWorkflowBuilds(Constant.defaultPageNumber, Constant.defaultPageSize, '');
//             getWorkflowRuns(Constant.defaultPageNumber, Constant.defaultPageSize, '');
//             getWorkflow_projects(Constant.defaultPageNumber, Constant.defaultPageSize, '');
//         }
//     }, [workflowsData.list.length])
//     const handleReset = () => {
//         // setSearch('');
//         // dispatch(resetWorkflowsToInit());
//         // getData(Constant.defaultPageNumber, workflowsData.pageSize, '');
//         setSearch('');
//         dispatch(resetWorkflowsToInit());
//         dispatch(resetWorkflow_BuildsToInit());
//         dispatch(resetWorkflow_RunsToInit());
//         dispatch(resetWorkflows_ProjectsToInit());
//         getData(Constant.defaultPageNumber, workflowsData.pageSize, '');
//         getWorkflowBuilds(Constant.defaultPageNumber, workflowBuildsData.pageSize, '');
//         getWorkflowRuns(Constant.defaultPageNumber, workflowRunsData.pageSize, '');
//         getWorkflow_projects(Constant.defaultPageNumber, workflowProjectsdata.pageSize, '');

//     }
//     const handleServerDelete = async () => {
//         if (rowData) {
//             const response = await deleteWorkflows(rowData.workflow_id);
//             if (response) {
//                 dispatch(resetWorkflowsToInit());
//                 dispatch(setWorkflowsMessage("Deleted Successfully"));
//                 getData(Constant.defaultPageNumber, Constant.defaultPageSize, '');
//                 setShowDelete(false);
//             } else {
//                 dispatch(setWorkflowsMessage("Some error occured!"));
//             }
//         }
//     }

//     const handleRowSelection = (row) => {
//         console.log(row); // Row Selection Functionality can be written here
//     }
//     const handleAddButtonClick = () => {
//         // dispatch(setWorkflowsMessage(''));
//         // hideShowForm('add');
//         window.open(`/addWorkflow/${projectId}`, '_blank', 'noopener,noreferrer');
//     }
//     console.log("Before workflowsData : ", workflowsData, "workflowProjectData : ", workflowProjectsdata, "workflowBuildsData : ", workflowBuildsData);
//     const Project_workflowsData = workflowsData.list.filter((workflow_record) => workflowProjectsdata.list.find((workflow_project_record) => workflow_project_record.workflow_id === workflow_record.workflow_id));
//     console.log("Project_workflowsData : ", Project_workflowsData);
//     const buildData = Project_workflowsData.map(item => {
//         const buildData = workflowBuildsData.list.find(i => i.workflow_id === item.workflow_id);
//         const projectData = workflowProjectsdata.list.find(i => i.workflow_id === item.workflow_id);
//         return { ...item, ...buildData, ...projectData };
//     })

//     const newData = buildData.map(item => {
//         const testData = workflowRunsData.list.find(i => i.workflow_build_id === item.workflow_build_id);
//         return { ...item, ...testData };
//     })


//     const handleBuildClick = async (row) => {

//         if (row.workflow_build_status === 'Build') {
//             await getOneWorkflow_Builds(row.workflow_build_id).then(async (response) => {
//                 response.data.document.workflow_build_status = 'isBuilding';
//                 console.log(response);
//                 const message1 = await updateWorkflow_Builds(response.data.document.workflow_build_id, response.data.document);
//                 handleReset();
//                 if (message1) {
//                     dispatch(setWorkflow_BuildsMessage("Updated Successfully"));
//                     getWorkflowBuilds(Constant.defaultPageNumber, Constant.defaultPageSize, '');
//                 } else {
//                     dispatch(setWorkflow_BuildsMessage("Some error occured!"));
//                 }
//             })
//         }
//     }
//     const handleRunClick = async (row) => {

//         if (row.workflow_run_status === 'Run') {
//             await getOneWorkflow_Runs(row.workflow_build_id).then(async (response) => {
//                 response.data.document.workflow_run_status = 'isRunning';
//                 const message1 = await updateWorkflow_Runs(response.data.document.workflow_run_id, response.data.document);
//                 handleReset();
//                 if (message1) {
//                     dispatch(setWorkflow_RunsMessage("Updated Successfully"));
//                     getWorkflowRuns(Constant.defaultPageNumber, Constant.defaultPageSize, '');
//                 } else {
//                     dispatch(setWorkflow_RunsMessage("Some error occured!"));
//                 }
//             })
//         }
//     }

//     const editWorkflow = (row) => {
//         let tableName = "Backend_stacks";
//         let action = rowData.workflow_name
//         let workflowId = rowData.workflow_id;
//         let str = action;
//         let arr = str.split('_')
//         if (arr.length <= 1)
//             window.open(`/addWorkflow/${tableName}/${action}/${projectId}/${workflowId}`, '_blank', 'noopener,noreferrer');
//         else {
//             let last_ele = arr.pop();
//             str = "";
//             for (var s in arr) {
//                 str += arr[s] + "_";
//             }
//             str = str.slice(0, -1);
//             window.open(`/addWorkflow/${str}/${last_ele}/${projectId}/${workflowId}`, '_blank', 'noopener,noreferrer');
//         }
//     }

//     const columns = [
//         { name: 'workflow_id', selector: row => row.workflow_id, sortable: true },
//         { name: 'workflow_name', selector: row => row.workflow_name, sortable: true },
//         { name: 'workflow_description', selector: row => row.workflow_description, sortable: true },
//         { name: 'workflow_build_status', selector: row => row.workflow_build_status, sortable: true, omit: true },
//         { name: 'workflow_run_status', selector: row => row.workflow_run_status, sortable: true, omit: true },
//         { name: 'steps', selector: row => row.steps, sortable: true, omit: true },
//         { name: 'triggerpoint', selector: row => row.triggerpoint, sortable: true, omit: true },
//         // {name: 'steps', selector: row => row.steps, sortable: true},
//         // {name: 'triggerpoint', selector: row => row.triggerpoint, sortable: true},
//         , {
//             name: 'Edit',
//             button: true,
//             cell: (row) => <Button variant="Link" className="btn-sm" onClick={() => handleRowEdit(row)}>Edit</Button>,
//         }, {
//             name: 'Workflow BuildStatus',
//             button: true,
//             cell: (row) => <div>
//                 {row.workflow_build_status === 'isBuilding' ?
//                     <LoadingSpinner /> :
//                     row.workflow_build_status === 'Build' ?
//                         <Button type="button" className="float-right" variant="primary" onClick={() => handleBuildClick(row)} > {row.workflow_build_status} </Button> :
//                         <div>Built</div>
//                 }
//             </div>
//         },
//         {
//             name: 'Workflow RunStatus',
//             button: true,
//             cell: (row) => <div>
//                 {
//                     row.workflow_build_status === 'Built' ?
//                         (row.workflow_run_status === 'isRunning' ?
//                             <LoadingSpinner /> :
//                             row.workflow_run_status === 'Run' ?
//                                 <Button type="button" className="float-right" variant="primary" onClick={() => handleRunClick(row)} > {row.workflow_run_status} </Button> :
//                                 <div>Running</div>)
//                         :
//                         <Button type="button" disabled={true} className="float-right" variant="primary" onClick={() => handleRunClick(row)} > {row.workflow_run_status} </Button>
//                 }
//             </div>
//         },
//         {
//             name: '',
//             button: true,
//             cell: (row) => <Button variant="link" className="btn-sm" onClick={() => handleRowDeleteClick(row)}>Delete</Button>,
//         },
//     ];
//     return (
//         <Card className="shadow mb-4">
//             <Card.Header className="py-3">
//                 <h6 className="m-0 font-weight-bold text-primary">Workflows List ({workflowsData.totalCount})
//                     <Button variant="light" className="btn-circle btn-sm ml-2" onClick={handleReset}><i className="fa fa-refresh"></i></Button>
//                     <Button className="btn-icon-split float-right" onClick={handleAddButtonClick}>
//                         <span className="icon text-white-50">
//                             <i className="fas fa-add"></i>
//                         </span>
//                         <span className="text">Add New</span>
//                     </Button></h6>

//             </Card.Header>
//             <Card.Body>
//                 <Col className="ml-auto" md={3} xs={12} xl={3}>
//                     <InputGroup>
//                         <Form.Control
//                             placeholder="Search"
//                             aria-label="Search"
//                             aria-describedby="basic-search"
//                             onChange={(e) => setSearch(e.target.value)}
//                         />
//                         <Button disabled={search.length <= 2} variant="dark" className="rounded-0 rounded-right" id="button-search" onClick={handleSearch}>
//                             Search
//                         </Button>
//                     </InputGroup>
//                 </Col>
//                 <div className="table-responsive">
//                     <DataTable
//                         selectableRows={true}
//                         onSelectedRowsChange={handleRowSelection}
//                         paginationPerPage={Constant.defaultPageNumber}
//                         paginationRowsPerPageOptions={Constant.paginationRowsPerPageOptions}
//                         columns={columns} data={newData}
//                         onChangeRowsPerPage={handlePerRowsChange}
//                         paginationTotalRows={newData.length}
//                         className="table table-bordered"
//                         pagination
//                         paginationServer
//                         onChangePage={handlePageChange}></DataTable>
//                 </div>
//             </Card.Body>
//             <ConfirmationModal buttonNegative="Cancel" buttonPositive="Delete" title="Delete Confirmation" show={showDelete} body={"Are you sure?"} onNegative={() => setShowDelete(false)} onPositive={handleServerDelete} />
//         </Card>
//     );
// }

// // columns={columns} data={workflowsData.list}
// // paginationTotalRows={workflowsData.totalCount}


import React, { useEffect, useState } from "react";
import DataTable from 'react-data-table-component';
import { useSelector } from "react-redux";
import { RootState } from "redux/reducers";
import { useAppDispatch } from "redux/store";
import { resetWorkflowsToInit, setWorkflowsMessage } from "redux/actions";
import { Button, Card, Col, Form, InputGroup } from "react-bootstrap";
import { Constant } from "template/Constant";
import ConfirmationModal from "template/ConfirmationModal";
import { deleteWorkflows } from "services/workflowsService";
type Props = {
    hideShowForm: (action) => void;
    handleRowEdit: (row) => void;
    getData: (page, pageSize, searchKey) => void;
};
export const WorkflowsTable: React.FC<Props> = ({ hideShowForm, handleRowEdit, getData }) => {
    const dispatch = useAppDispatch();
    const [search, setSearch] = useState('');
    const [showDelete, setShowDelete] = useState(false);
    const [rowData, setRowData] = useState(undefined);
    const rData = useSelector((state: RootState) => state.workflows);
    const handleSearch = () => {
        if (search.length > 0) {
            getData(Constant.defaultPageNumber, Constant.defaultPageSize, search);
        }
    }
    const handlePerRowsChange = async (newPerPage, page) => {
        await getData(page, newPerPage, '');
    }
    const handlePageChange = (page) => {
        getData(page, rData.pageSize, '');
    };
    const handleRowDeleteClick = (row) => {
        setRowData(row);
        setShowDelete(true);
    }
    useEffect(() => {
        if (rData && rData.list && rData.list.length === 0) {
            dispatch(resetWorkflowsToInit());
            getData(Constant.defaultPageNumber, Constant.defaultPageSize, '');
        }
    })
    const handleReset = () => {

        dispatch(resetWorkflowsToInit());
        getData(Constant.defaultPageNumber, rData.pageSize, '');
    }
    const handleServerDelete = async () => {
        if (rowData) {
            const response = await deleteWorkflows(rowData.workflow_id);
            if (response) {
                dispatch(resetWorkflowsToInit());
                dispatch(setWorkflowsMessage("Deleted Successfully"));
                getData(Constant.defaultPageNumber, Constant.defaultPageSize, '');
                setShowDelete(false);
            } else {
                dispatch(setWorkflowsMessage("Some error occured!"));
            }
        }
    }

    const handleRowSelection = (row) => {
        console.log(row); // Row Selection Functionality can be written here
    }
    const handleAddButtonClick = () => {
        // dispatch(setWorkflowsMessage(''));
        // hideShowForm('add');
        // window.open(`/addWorkflow/${projectId}`, '_blank', 'noopener,noreferrer');
        window.open(`addWorkflow`, '_blank', 'noopener,noreferrer');

    }
    const columns = [
        { name: 'workflow_id', selector: row => row.workflow_id, sortable: true },
        { name: 'workflow_name', selector: row => row.workflow_name, sortable: true },
        { name: 'workflow_description', selector: row => row.workflow_description, sortable: true },
        { name: 'steps', selector: row => row.steps, sortable: true,omit:true },
        { name: 'triggerpoint', selector: row => row.triggerpoint, sortable: true,omit:true },
        { name: 'modifiedBy', selector: row => row.modifiedBy, sortable: true },
        { name: 'createdBy', selector: row => row.createdBy, sortable: true },
        { name: 'modifiedAt', selector: row => row.modifiedAt, sortable: true },
        { name: 'createdAt', selector: row => row.createdAt, sortable: true },
        { name: 'isActive', selector: row => row.isActive, sortable: true },

        , {
            name: '',
            button: true,
            cell: (row) => <Button variant="link" className="btn-sm" onClick={() => handleRowEdit(row)}>Edit</Button>,
        },
        {
            name: '',
            button: true,
            cell: (row) => <Button variant="link" className="btn-sm" onClick={() => handleRowDeleteClick(row)}>Delete</Button>,
        },
    ];
    return (
        <Card className="shadow mb-4">
            <Card.Header className="py-3">
                <h6 className="m-0 font-weight-bold text-primary">Workflows List ({rData.totalCount})
                    <Button variant="light" className="btn-circle btn-sm ml-2" onClick={handleReset}><i className="fa fa-refresh"></i></Button>
                    <Button className="btn-icon-split float-right" onClick={handleAddButtonClick}>
                        <span className="icon text-white-50">
                            <i className="fas fa-add"></i>
                        </span>
                        <span className="text">Add New</span>
                    </Button></h6>

            </Card.Header>
            <Card.Body>
                <Col className="ml-auto" md={3} xs={12} xl={3}>
                    <InputGroup>
                        <Form.Control
                            placeholder="Search"
                            aria-label="Search"
                            aria-describedby="basic-search"
                            onChange={(e) => setSearch(e.target.value)}
                        />
                        <Button disabled={search.length <= 2} variant="dark" className="rounded-0 rounded-right" id="button-search" onClick={handleSearch}>
                            Search
                        </Button>
                    </InputGroup>
                </Col>
                <div className="table-responsive">
                    <DataTable
                        selectableRows={true}
                        onSelectedRowsChange={handleRowSelection}
                        paginationPerPage={Constant.defaultPageNumber}
                        paginationRowsPerPageOptions={Constant.paginationRowsPerPageOptions}
                        columns={columns} data={rData.list}
                        onChangeRowsPerPage={handlePerRowsChange}
                        paginationTotalRows={rData.totalCount}
                        className="table table-bordered"
                        pagination
                        paginationServer
                        onChangePage={handlePageChange}></DataTable>
                </div>
            </Card.Body>
            <ConfirmationModal buttonNegative="Cancel" buttonPositive="Delete" title="Delete Confirmation" show={showDelete} body={"Are you sure?"} onNegative={() => setShowDelete(false)} onPositive={handleServerDelete} />
        </Card>
    );
}


