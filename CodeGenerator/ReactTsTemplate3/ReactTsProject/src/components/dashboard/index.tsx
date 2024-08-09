import React from "react";
import { Row } from "react-bootstrap";
import Layout from "template";
import { MenuItems } from "template/MenuItems";
import { DashboardCard } from "./DasboardCard";

export const Dashboard: React.FC = () => {
  return (
    <Layout>
      <div className="container-fluid" >
        <div className="d-sm-flex align-items-center justify-content-between mb-4" >
          <h1 className="h3 mb-0 text-gray-800" > Dashboard </h1>
        </div>
        <div className="d-flex flex-column min-vh-100" >
          <Row>
            {MenuItems.map((item, i) => {
              return <DashboardCard key={`Card-${i}`} name={item.title} path={item.path} />
            })}
          </Row>
        </div>
      </div></Layout >
  );
};
