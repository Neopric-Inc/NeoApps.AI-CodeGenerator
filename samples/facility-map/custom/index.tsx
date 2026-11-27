import React from "react";
import { Extensions } from "../neoapps/extensions";
import { FacilityMap } from "./FacilityMap";

export const extensions: Extensions = {
  routes: [{ path: "/map", element: <FacilityMap /> }],
  nav: [{ to: "/map", icon: "view_in_ar", label: "3D map" }],
};
