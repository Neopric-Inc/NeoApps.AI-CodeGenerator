// import { useFormik } from "formik";
// import React, { useEffect, useState } from "react";
// import { Button, Card, Form } from "react-bootstrap";
// import { useSelector } from "react-redux";
// import { RootState } from "redux/reducers";
// import { setWorkflowsMessage } from "redux/actions";


// import { useAppDispatch } from "redux/store";
// import { addWorkflows, updateWorkflows } from "services/workflowsService";
// import { Constant } from "template/Constant";
// import * as yup from 'yup';
// type Props = {
//     row?: any,
//     hideShowForm: (actionName) => void;
//     getData: (page, pageSize, searchKey) => void;
//     action?: string
// };
// export const WorkflowsForm: React.FC<Props> = ({ row, hideShowForm, getData, action }) => {
//     const dispatch = useAppDispatch();
//     const iValue={workflow_id:'',workflow_name:'',workflow_description:'',steps:'',triggerpoint:''};
//     const initialValue = action === 'edit' ? row : iValue;


//     const formik = useFormik({
//         initialValues: initialValue,
//         onSubmit: async (values) => {
//             values.workflow_id = Number(values.workflow_id);
//             values.steps = JSON.stringify(values.steps);
//             values.triggerpoint = JSON.stringify(values.triggerpoint);
//             if (action === 'edit') {
//                 const response = await updateWorkflows(values.workflow_id,values);
//                 if (response) {
//                     dispatch(setWorkflowsMessage("Updated Successfully"));
//                     getData(Constant.defaultPageNumber, Constant.defaultPageSize, '');
//                     hideShowForm('');
//                 } else {
//                     dispatch(setWorkflowsMessage("Some error occured!"));
//                 }
//             } else if (action === 'add') {
//                 const response = await addWorkflows(values);
//                 if (response) {
//                     dispatch(setWorkflowsMessage("Added Successfully"));
//                     getData(Constant.defaultPageNumber, Constant.defaultPageSize, '');
//                     hideShowForm('');
//                 } else {
//                     dispatch(setWorkflowsMessage("Some error occured!"));
//                 }
//             }
//         },
//         validationSchema: yup.object({
//            workflow_name: yup.string().required('workflow_name is required'),
// workflow_description: yup.string().required('workflow_description is required'),
// steps: yup.string().required('steps is required'),
// triggerpoint: yup.string().required('triggerpoint is required'),

//         }),
//     });
//     return (
//         <Card className="shadow mb-4">
//             <Card.Header className="py-3">
//                 <h6 className="m-0 font-weight-bold text-primary text-capitalize">{action} Workflows
//                     <Button className="btn-icon-split float-right" onClick={() => hideShowForm(false)}>
//                         <span className="icon text-white-50">
//                             <i className="fas fa-list"></i>
//                         </span>
//                         <span className="text">View Workflows</span>
//                     </Button>
//                 </h6>

//             </Card.Header>
//             <Card.Body>
//                 <Form onSubmit={formik.handleSubmit}>
//                   <Form.Group>
// <label className="form -control-label">workflow_name</label>
// <Form.Control type="text" name="workflow_name" className="form-control" value={formik.values.workflow_name}
// onChange={formik.handleChange}
// onBlur ={formik.handleBlur}
// isInvalid ={!!formik.touched.workflow_name && !!formik.errors.workflow_name}
// isValid ={!!formik.touched.workflow_name && !formik.errors.workflow_name}
// ></Form.Control>
// {
//     formik.errors.workflow_name && (
//     <Form.Control.Feedback type="invalid">
//         {formik.errors.workflow_name}
//     </Form.Control.Feedback>
// )}
// </Form.Group>
// <Form.Group>
// <label className="form -control-label">workflow_description</label>
// <Form.Control type="text" name="workflow_description" className="form-control" value={formik.values.workflow_description}
// onChange={formik.handleChange}
// onBlur ={formik.handleBlur}
// isInvalid ={!!formik.touched.workflow_description && !!formik.errors.workflow_description}
// isValid ={!!formik.touched.workflow_description && !formik.errors.workflow_description}
// ></Form.Control>
// {
//     formik.errors.workflow_description && (
//     <Form.Control.Feedback type="invalid">
//         {formik.errors.workflow_description}
//     </Form.Control.Feedback>
// )}
// </Form.Group>
// <Form.Group>
// <label className="form -control-label">steps</label>
// <Form.Control type="text" name="steps" className="form-control" value={formik.values.steps}
// onChange={formik.handleChange}
// onBlur ={formik.handleBlur}
// isInvalid ={!!formik.touched.steps && !!formik.errors.steps}
// isValid ={!!formik.touched.steps && !formik.errors.steps}
// ></Form.Control>
// {
//     formik.errors.steps && (
//     <Form.Control.Feedback type="invalid">
//         {formik.errors.steps}
//     </Form.Control.Feedback>
// )}
// </Form.Group>
// <Form.Group>
// <label className="form -control-label">triggerpoint</label>
// <Form.Control type="text" name="triggerpoint" className="form-control" value={formik.values.triggerpoint}
// onChange={formik.handleChange}
// onBlur ={formik.handleBlur}
// isInvalid ={!!formik.touched.triggerpoint && !!formik.errors.triggerpoint}
// isValid ={!!formik.touched.triggerpoint && !formik.errors.triggerpoint}
// ></Form.Control>
// {
//     formik.errors.triggerpoint && (
//     <Form.Control.Feedback type="invalid">
//         {formik.errors.triggerpoint}
//     </Form.Control.Feedback>
// )}
// </Form.Group>

//                     <Form.Group>
//                         <Button type="submit" className="float-right" variant="primary">Save</Button>
//                     </Form.Group>
//                 </Form>
//             </Card.Body>
//         </Card>
//     );
// }

import { useFormik } from "formik";
import React, { useEffect, useState } from "react";
import { Button, Card, Form } from "react-bootstrap";
import { useSelector } from "react-redux";
import { RootState } from "redux/reducers";
import { setWorkflowsMessage } from "redux/actions";




import { useAppDispatch } from "redux/store";
import { addWorkflows, updateWorkflows } from "services/workflowsService";
import { Constant } from "template/Constant";
import * as yup from 'yup';
type Props = {
    row?: any,
    hideShowForm: (actionName) => void;
    getData: (page, pageSize, searchKey) => void;
    action?: string
};
export const WorkflowsForm: React.FC<Props> = ({ row, hideShowForm, getData, action }) => {
    const dispatch = useAppDispatch();
    const iValue = { createdAt: '', createdBy: '', isActive: '', modifiedAt: '', modifiedBy: '', steps: '', triggerpoint: '', workflow_description: '', workflow_id: '', workflow_name: '' };
    const initialValue = action === 'edit' ? row : iValue;




    const formik = useFormik({
        initialValues: initialValue,
        onSubmit: async (values) => {
            values.isActive = Number(values.isActive)
            values.workflow_id = Number(values.workflow_id)

            if (action === 'edit') {
                const response = await updateWorkflows(values.workflow_id, values);
                if (response) {
                    dispatch(setWorkflowsMessage("Updated Successfully"));
                    getData(Constant.defaultPageNumber, Constant.defaultPageSize, '');
                    hideShowForm('');
                } else {
                    dispatch(setWorkflowsMessage("Some error occured!"));
                }
            } else if (action === 'add') {
                const response = await addWorkflows(values);
                if (response) {
                    dispatch(setWorkflowsMessage("Added Successfully"));
                    getData(Constant.defaultPageNumber, Constant.defaultPageSize, '');
                    hideShowForm('');
                } else {
                    dispatch(setWorkflowsMessage("Some error occured!"));
                }
            }
        },
        validationSchema: yup.object({
            createdAt: yup.date().required('createdAt is required'),
            createdBy: yup.string().required('createdBy is required'),
            isActive: yup.number().required('isActive is required'),
            modifiedAt: yup.date().required('modifiedAt is required'),
            modifiedBy: yup.string().required('modifiedBy is required'),
            steps: yup.string().required('steps is required'),
            triggerpoint: yup.string().required('triggerpoint is required'),
            workflow_description: yup.string().required('workflow_description is required'),
            workflow_name: yup.string().required('workflow_name is required'),

        }),
    });
    return (
        <Card className="shadow mb-4">
            <Card.Header className="py-3">
                <h6 className="m-0 font-weight-bold text-primary text-capitalize">Add Workflows
                    <Button className="btn-icon-split float-right" onClick={() => hideShowForm(false)}>
                        <span className="icon text-white-50">
                            <i className="fas fa-list"></i>
                        </span>
                        <span className="text">View Workflows</span>
                    </Button>
                </h6>

            </Card.Header>
            <Card.Body>
                <Form onSubmit={formik.handleSubmit}>
                    <Form.Group>
                        <label className="form -control-label">createdAt</label>
                        <Form.Control type="text" name="createdAt" className="form-control" value={formik.values.createdAt}
                            onChange={formik.handleChange}
                            onBlur={formik.handleBlur}
                            isInvalid={!!formik.touched.createdAt && !!formik.errors.createdAt}
                            isValid={!!formik.touched.createdAt && !formik.errors.createdAt}
                        ></Form.Control>
                        {
                            formik.errors.createdAt && (
                                <Form.Control.Feedback type="invalid">
                                    {formik.errors.createdAt}
                                </Form.Control.Feedback>
                            )}
                    </Form.Group>
                    <Form.Group>
                        <label className="form -control-label">createdBy</label>
                        <Form.Control type="text" name="createdBy" className="form-control" value={formik.values.createdBy}
                            onChange={formik.handleChange}
                            onBlur={formik.handleBlur}
                            isInvalid={!!formik.touched.createdBy && !!formik.errors.createdBy}
                            isValid={!!formik.touched.createdBy && !formik.errors.createdBy}
                        ></Form.Control>
                        {
                            formik.errors.createdBy && (
                                <Form.Control.Feedback type="invalid">
                                    {formik.errors.createdBy}
                                </Form.Control.Feedback>
                            )}
                    </Form.Group>
                    <Form.Group>
                        <label className="form -control-label">isActive</label>
                        <Form.Control type="text" name="isActive" className="form-control" value={formik.values.isActive}
                            onChange={formik.handleChange}
                            onBlur={formik.handleBlur}
                            isInvalid={!!formik.touched.isActive && !!formik.errors.isActive}
                            isValid={!!formik.touched.isActive && !formik.errors.isActive}
                        ></Form.Control>
                        {
                            formik.errors.isActive && (
                                <Form.Control.Feedback type="invalid">
                                    {formik.errors.isActive}
                                </Form.Control.Feedback>
                            )}
                    </Form.Group>
                    <Form.Group>
                        <label className="form -control-label">modifiedAt</label>
                        <Form.Control type="text" name="modifiedAt" className="form-control" value={formik.values.modifiedAt}
                            onChange={formik.handleChange}
                            onBlur={formik.handleBlur}
                            isInvalid={!!formik.touched.modifiedAt && !!formik.errors.modifiedAt}
                            isValid={!!formik.touched.modifiedAt && !formik.errors.modifiedAt}
                        ></Form.Control>
                        {
                            formik.errors.modifiedAt && (
                                <Form.Control.Feedback type="invalid">
                                    {formik.errors.modifiedAt}
                                </Form.Control.Feedback>
                            )}
                    </Form.Group>
                    <Form.Group>
                        <label className="form -control-label">modifiedBy</label>
                        <Form.Control type="text" name="modifiedBy" className="form-control" value={formik.values.modifiedBy}
                            onChange={formik.handleChange}
                            onBlur={formik.handleBlur}
                            isInvalid={!!formik.touched.modifiedBy && !!formik.errors.modifiedBy}
                            isValid={!!formik.touched.modifiedBy && !formik.errors.modifiedBy}
                        ></Form.Control>
                        {
                            formik.errors.modifiedBy && (
                                <Form.Control.Feedback type="invalid">
                                    {formik.errors.modifiedBy}
                                </Form.Control.Feedback>
                            )}
                    </Form.Group>
                    <Form.Group>
                        <label className="form -control-label">steps</label>
                        <Form.Control type="text" name="steps" className="form-control" value={formik.values.steps}
                            onChange={formik.handleChange}
                            onBlur={formik.handleBlur}
                            isInvalid={!!formik.touched.steps && !!formik.errors.steps}
                            isValid={!!formik.touched.steps && !formik.errors.steps}
                        ></Form.Control>
                        {
                            formik.errors.steps && (
                                <Form.Control.Feedback type="invalid">
                                    {formik.errors.steps}
                                </Form.Control.Feedback>
                            )}
                    </Form.Group>
                    <Form.Group>
                        <label className="form -control-label">triggerpoint</label>
                        <Form.Control type="text" name="triggerpoint" className="form-control" value={formik.values.triggerpoint}
                            onChange={formik.handleChange}
                            onBlur={formik.handleBlur}
                            isInvalid={!!formik.touched.triggerpoint && !!formik.errors.triggerpoint}
                            isValid={!!formik.touched.triggerpoint && !formik.errors.triggerpoint}
                        ></Form.Control>
                        {
                            formik.errors.triggerpoint && (
                                <Form.Control.Feedback type="invalid">
                                    {formik.errors.triggerpoint}
                                </Form.Control.Feedback>
                            )}
                    </Form.Group>
                    <Form.Group>
                        <label className="form -control-label">workflow_description</label>
                        <Form.Control type="text" name="workflow_description" className="form-control" value={formik.values.workflow_description}
                            onChange={formik.handleChange}
                            onBlur={formik.handleBlur}
                            isInvalid={!!formik.touched.workflow_description && !!formik.errors.workflow_description}
                            isValid={!!formik.touched.workflow_description && !formik.errors.workflow_description}
                        ></Form.Control>
                        {
                            formik.errors.workflow_description && (
                                <Form.Control.Feedback type="invalid">
                                    {formik.errors.workflow_description}
                                </Form.Control.Feedback>
                            )}
                    </Form.Group>
                    <Form.Group>
                        <label className="form -control-label">workflow_name</label>
                        <Form.Control type="text" name="workflow_name" className="form-control" value={formik.values.workflow_name}
                            onChange={formik.handleChange}
                            onBlur={formik.handleBlur}
                            isInvalid={!!formik.touched.workflow_name && !!formik.errors.workflow_name}
                            isValid={!!formik.touched.workflow_name && !formik.errors.workflow_name}
                        ></Form.Control>
                        {
                            formik.errors.workflow_name && (
                                <Form.Control.Feedback type="invalid">
                                    {formik.errors.workflow_name}
                                </Form.Control.Feedback>
                            )}
                    </Form.Group>

                    <Form.Group>
                        <Button type="submit" className="float-right" variant="primary">Save</Button>
                    </Form.Group>
                </Form>
            </Card.Body>
        </Card>
    );
}
