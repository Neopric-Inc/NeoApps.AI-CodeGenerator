import React, { useState } from "react";
import { Col, Card } from "react-bootstrap";
import { useNavigate } from "react-router";
import { activateMenu } from "redux/actions";
import { useAppDispatch } from "redux/store";
type Props = {
    name: string;
    path: string;
};
export const DashboardCard: React.FC<Props> = ({ name, path }) => {
    const navigate = useNavigate();
    const dispatch = useAppDispatch();
    const handleCardClick = (path) => {
        dispatch(activateMenu(path))
        navigate(path, { replace: true });
    }
    return (
        <Col md={4} xl={3} xs={6} className="cursor-pointer mb-4" onClick={() => { handleCardClick(path); }}>
            <Card className="border-left-primary shadow h-100 py-2">
                <Card.Body>
                    <div className="row no-gutters align-items-center">
                        <div className="col mr-2">
                            <div className="text-xs font-weight-bold text-primary text-uppercase mb-1">
                                {name}</div>
                            <div className="h5 mb-0 font-weight-bold text-gray-800"></div>
                        </div>
                        <div className="col-auto">
                            <i className="fas fa-clipboard-list fa-2x text-gray-300"></i>
                        </div>
                    </div>
                </Card.Body>
            </Card>
        </Col>
    );
};
