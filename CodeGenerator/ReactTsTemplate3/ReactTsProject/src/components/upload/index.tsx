import { useFormik } from "formik";
import React, { useState } from "react";
import { Row, Col, Card, Form, Alert, Button } from "react-bootstrap";
import { uploadFileService, uploadImageService } from "services/fileUploadService";
import Layout from "template";
import * as yup from 'yup';
export const FileUpload: React.FC = () => {
    const [message, setMessage] = useState('')
    const formikImage = useFormik({
        initialValues: { image: [] },
        onSubmit: async (values) => {
            var formData = new FormData();
            formData.append("image", values.image[0]);
            uploadImageService(formData).then((res: any) => {
                if (res && res.data && res.data.document) {
                    setMessage('Image Successfully Uploaded : ' + res.data.document);
                    formikImage.resetForm();
                }else{
                    setMessage('Failed to upload image');
                    formikFile.resetForm();
                }
            })
        },
        validationSchema: yup.object({
            image: yup.array().min(1, "Select at least 1 image")
        }),
    });

    const formikFile = useFormik({
        initialValues: { fileData: [] },
        onSubmit: async (values) => {
            var formData = new FormData();
            formData.append("file", values.fileData[0]);
            uploadFileService(formData).then((res: any) => {
                if (res && res.data && res.data.document) {
                    setMessage('File Successfully Uploaded : ' + res.data.document);
                    formikFile.resetForm();
                }else{
                    setMessage('Failed to upload file');
                    formikFile.resetForm();
                }
            })
        },
        validationSchema: yup.object({
            fileData: yup.array().min(1, "Select at least 1 file")
        }),
    });
    return (
        <Layout>
            <div className="container-fluid" >
                <div className="d-sm-flex align-items-center justify-content-between mb-4" >
                    <h1 className="h3 mb-0 text-gray-800" > File Upload </h1>
                </div>
                <div className="d-flex flex-column min-vh-100" >
                    <Row>
                        {message ? <Col md={12}><Alert variant="info">{message}</Alert></Col> : null}
                        <Col md={6}>
                            <Card>
                                <Card.Header>Upload Image</Card.Header>
                                <Card.Body>
                                    <Form onSubmit={formikImage.handleSubmit}>
                                        <Form.Group controlId="formFile" className="mb-3">
                                            <Form.Label>Upload png | jpg | jpeg files</Form.Label>
                                            <Form.Control type="file" 
                                                onChange={(event: React.ChangeEvent) => {
                                                    const imgFile = event.target as HTMLInputElement;
                                                    if (imgFile.files && imgFile.files[0]) {
                                                        formikImage.setFieldValue("image", Array.from(imgFile.files));
                                                    }
                                                }}
                                                onBlur={formikImage.handleBlur}
                                                isInvalid={!!formikImage.touched.image && !!formikImage.errors.image}
                                                isValid={!!formikImage.touched.image && !formikImage.errors.image}
                                            />
                                            {
                                                formikImage.errors.image && (
                                                    <Form.Control.Feedback type="invalid">
                                                        {formikImage.errors.image}
                                                    </Form.Control.Feedback>
                                                )}
                                        </Form.Group>
                                        <Form.Group>
                                            <Button type="submit" className="float-right" variant="primary">Upload</Button>
                                        </Form.Group>
                                    </Form>


                                </Card.Body>
                            </Card>
                        </Col>
                        <Col md={6}>
                            <Card>
                                <Card.Header>Upload Files</Card.Header>
                                <Card.Body>
                                <Form onSubmit={formikFile.handleSubmit}>
                                        <Form.Group controlId="formFile" className="mb-3">
                                            <Form.Label>Upload pdf | docs | xsls files</Form.Label>
                                            <Form.Control type="file" 
                                                onChange={(event: React.ChangeEvent) => {
                                                    const dFile = event.target as HTMLInputElement;
                                                    if (dFile.files && dFile.files[0]) {
                                                        formikFile.setFieldValue("fileData", Array.from(dFile.files));
                                                    }
                                                }}
                                                onBlur={formikFile.handleBlur}
                                                isInvalid={!!formikFile.touched.fileData && !!formikFile.errors.fileData}
                                                isValid={!!formikFile.touched.fileData && !formikFile.errors.fileData}
                                            />
                                            {
                                                formikFile.errors.fileData && (
                                                    <Form.Control.Feedback type="invalid">
                                                        {formikFile.errors.fileData}
                                                    </Form.Control.Feedback>
                                                )}
                                        </Form.Group>
                                        <Form.Group>
                                            <Button type="submit" className="float-right" variant="primary">Upload</Button>
                                        </Form.Group>
                                    </Form>

                                </Card.Body>
                            </Card>
                        </Col>
                    </Row>
                </div>
            </div></Layout >
    );
};

