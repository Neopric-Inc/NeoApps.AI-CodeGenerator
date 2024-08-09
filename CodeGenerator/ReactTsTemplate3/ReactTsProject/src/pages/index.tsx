import React from "react";
import { BrowserRouter, Routes, Route } from "react-router-dom";
import Login from "./login";
import Register from "./register";
import AuthenticatedRoute from "components/auth";
import { Dashboard } from "components/dashboard";
import { NotFound } from "./404";
const Pages: React.FC = () => {
  return (
    <BrowserRouter>
      <Routes>
        <Route path="/" element={<Login />}></Route>
        <Route path="/register" element={<Register />}></Route>
        <Route
          path="/dashboard"
          element={<AuthenticatedRoute element={<Dashboard />} />}
        ></Route>
        <Route path="*" element={<NotFound />} />
      </Routes>
    </BrowserRouter>
  );
};

export default Pages;
