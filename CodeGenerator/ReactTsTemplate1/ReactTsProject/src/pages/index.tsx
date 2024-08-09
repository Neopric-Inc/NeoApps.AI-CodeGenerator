import React from "react";
import { BrowserRouter, Routes, Route, Navigate } from "react-router-dom";
import Login from "./login";
import Register from "./register";
import AuthenticatedRoute from "components/auth";
import { NotFound } from "./404";
import PostLoginRoute from "components/auth/PostLoginRoute";
import { DndBuilder } from "Dnd/Dnd Designer";
import { PreviewDndBuilder } from "components";

const Pages: React.FC = () => {
    const isDndOn = process.env.REACT_APP_IS_DND_ON === "1";

    return (
        <BrowserRouter>
            <Routes>
                <Route path="/login" element={<Login />} />
                <Route path="/register" element={<Register />} />

                {/* Conditionally render DnD and Preview routes based on REACT_APP_IS_DND_ON */}
                {isDndOn ? (
                    <>
                        {/* Assuming "/dnd" is the route you want for DnD when it's enabled */}
                        <Route
                            path="/dnd/dndBuilder/:id"
                            element={<AuthenticatedRoute element={<DndBuilder />} />}
                        />
                        <Route
                            path="/dnd/dndBuilder/preview"
                            element={<AuthenticatedRoute element={<PreviewDndBuilder />} />}
                        />
                        {/* Redirect root to DnD or Preview based on additional logic or preference */}
                        <Route
                            path="/"
                            element={<Navigate to="dnd/dndBuilder/1" replace />}
                        />
                    </>
                ) : (
                    <>
                        {/* When DnD is off, make Preview the root */}
                        <Route
                            path="/"
                            element={<AuthenticatedRoute element={<PreviewDndBuilder />} />}
                        />
                    </>
                )}

                <Route path="*" element={<NotFound />} />
            </Routes>
        </BrowserRouter>
    );
};
export default Pages;
