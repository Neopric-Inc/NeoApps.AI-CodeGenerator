// import React, { useState } from "react";
// import { Alert, Button } from "react-bootstrap";
// import { useSelector } from "react-redux";
// import { setWorkflowsList, setWorkflowsMessage } from "redux/actions";
// import { RootState } from "redux/reducers";
// import { useAppDispatch } from "redux/store";
// import { getWorkflows } from "services/workflowsService";
// import Layout from "template";
// import { Constant } from "template/Constant";
// import { WorkflowsForm } from "./form";
// import { WorkflowsTable } from "./table";
// import { useParams } from "react-router";
// import { getWorkflows_Projects } from "services/workflows_projectsService";

// export const Workflows: React.FC = () => {
//   const { projectId } = useParams();
//   const projectId_int = parseInt(projectId);
//   console.log("projectId : ", projectId);
//   const dispatch = useAppDispatch();
//   const rData = useSelector((state: RootState) => state.workflows);
//   const workflowProjectsdata = useSelector((state: RootState) => state.workflows_projects);
//   const [row, setRow] = useState(undefined);
//   const [action, setAction] = useState('');
//   const getData = (page, pageSize, searchKey) => {
//     getWorkflows_Projects(page, pageSize, searchKey).then((workflow_projects) => {
//       if (workflow_projects && workflow_projects.records) {
//         workflow_projects = workflow_projects.records.filter((workflow_project_record) => workflow_project_record.project_id === projectId_int).map((workflow_project_record) => workflow_project_record.workflow_id);
//         getWorkflows(page, pageSize, searchKey).then((workflows_reponse) => {
//           if (workflows_reponse && workflows_reponse.records) {
//             workflows_reponse = workflows_reponse.records.filter((workflow_record) => workflow_projects.includes(workflow_record.workflow_id));
//             console.log("workflows_reponse : ", workflows_reponse);
//             dispatch(setWorkflowsList({ pageNo: page, pageSize: pageSize, list: workflows_reponse, totalCount: workflows_reponse.total_count, searchKey: searchKey }));
//           } else {
//             dispatch(setWorkflowsMessage("No Record Found"));
//           }
//         })
//       }
//     });
//   }

//   const handleRowEdit = (rowData) => {
//     // setRow(rowData);
//     // dispatch(setWorkflowsMessage(''));
//     // setAction('edit');
//     let tableName = "Backend_stacks";
//     let action = rowData.workflow_name
//     let workflowId = rowData.workflow_id;
//     let str = action;
//     let arr = str.split('_')
//     if (arr.length <= 1)
//       window.open(`/addWorkflow/${tableName}/${action}/${projectId}/${workflowId}`, '_blank', 'noopener,noreferrer');
//     else {
//       let last_ele = arr.pop();
//       str = "";
//       for (var s in arr) {
//         str += arr[s] + "_";
//       }
//       str = str.slice(0, -1);
//       window.open(`/addWorkflow/${str}/${last_ele}/${projectId}/${workflowId}`, '_blank', 'noopener,noreferrer');
//     }
//   }



//   return (
//     <Layout>
//       <div className="container-fluid">
//         <div className="d-sm-flex align-items-center justify-content-between mb-4">
//           <h1 className="h3 mb-0 text-gray-800">Workflows</h1>
//         </div>
//         <div className="d-flex flex-column min-vh-100">
//           {rData.message ?
//             <Alert variant={Constant.defaultAlertVarient} className="alert-dismissible fade">{rData.message}
//               <Button type="button" className="close" data-dismiss="alert" aria-label="Close" onClick={() => dispatch(setWorkflowsMessage(''))}>
//                 <span aria-hidden="true">&times;</span>
//               </Button>
//             </Alert> : null}
//           {action ? <WorkflowsForm hideShowForm={setAction} action={action} row={row} getData={getData} /> :
//             <WorkflowsTable handleRowEdit={handleRowEdit} hideShowForm={setAction} getData={getData} projectId={projectId} />}
//         </div>

//       </div>
//     </Layout >
//   );
// };

import React, { useState } from "react";
import { Alert, Button } from "react-bootstrap";
import { useSelector } from "react-redux";
import { setWorkflowsList, setWorkflowsMessage } from "redux/actions";
import { RootState } from "redux/reducers";
import { useAppDispatch } from "redux/store";
import { getWorkflows } from "services/workflowsService";
import Layout from "template";
import { Constant } from "template/Constant";
import { WorkflowsForm } from "./form";
import { WorkflowsTable } from "./table";

export const Workflows: React.FC = () => {
  const dispatch = useAppDispatch();
  const rData = useSelector((state: RootState) => state.workflows);
  const [row, setRow] = useState(undefined);
  const [action, setAction] = useState('');
  const getData = (page, pageSize, searchKey) => {
    getWorkflows(page, pageSize, searchKey).then((response) => {
      if (response && response.records) {
        dispatch(setWorkflowsList({ pageNo: page, pageSize: pageSize, list: response.records, totalCount: response.total_count, searchKey: searchKey }));
      } else {
        dispatch(setWorkflowsMessage("No Record Found"));
      }
    })
  }

  const handleRowEdit = (rowData) => {
    // setRow(rowData);
    // dispatch(setWorkflowsMessage(''));
    // setAction('edit');
    let tableName = "Backend_stacks";
    let action = rowData.workflow_name
    let workflowId = rowData.workflow_id;
    let str = action;
    let arr = str.split('_')
    if (arr.length <= 1) {
      window.open(`addWorkflow/${tableName}/${action}/${workflowId}`, '_blank', 'noopener,noreferrer');
      //window.open(`/addWorkflow/${tableName}/${action}/${projectId}/${workflowId}`, '_blank', 'noopener,noreferrer');
    }
    else {
      let last_ele = arr.pop();
      str = "";
      for (var s in arr) {
        str += arr[s] + "_";
      }
      str = str.slice(0, -1);
      window.open(`addWorkflow/${str}/${last_ele}/${workflowId}`, '_blank', 'noopener,noreferrer');
      //window.open(`/addWorkflow/${str}/${last_ele}/${projectId}/${workflowId}`, '_blank', 'noopener,noreferrer');
    }
  }
  return (
    <Layout>
      <div className="container-fluid">
        <div className="d-sm-flex align-items-center justify-content-between mb-4">
          <h1 className="h3 mb-0 text-gray-800">Workflows</h1>
        </div>
        <div className="d-flex flex-column min-vh-100">
          {rData.message ?
            <Alert variant={Constant.defaultAlertVarient} className="alert-dismissible fade">{rData.message}
              <Button type="button" className="close" data-dismiss="alert" aria-label="Close" onClick={() => dispatch(setWorkflowsMessage(''))}>
                <span aria-hidden="true">&times;</span>
              </Button>
            </Alert> : null}
          {action ? <WorkflowsForm hideShowForm={setAction} action={action} row={row} getData={getData} /> :
            <WorkflowsTable handleRowEdit={handleRowEdit} hideShowForm={setAction} getData={getData} />}
        </div>

      </div>
    </Layout >
  );
};
