import React from "react";
import { Extensions } from "../neoapps/extensions";
import { Play } from "./Play";

export const extensions: Extensions = {
  publicRoutes: [{ path: "/play", element: <Play /> }],
  nav: [{ to: "/play", icon: "rocket_launch", label: "Play" }],
};
