import React, { useState, useEffect } from "react";
import { BarChart1 } from "./chart-1";
import { LineChart2 } from "./chart-2";
import { PieChart3 } from "./chart-3";
import { LineChart4 } from "./chart-4";
import { BarChart6 } from "./chart-6";
import { LineChart7 } from "./chart-7";
import { PieChart8 } from "./chart-8";
import { LineChart9 } from "./chart-9";
import { PieChart10 } from "./chart-10";
import { BarChart11 } from "./chart-11";
import { BarChart12 } from "./chart-12";

//import Kanban from "./kanban/indexKanban";
//import Calendar from "./Calendar";

export const ChartIndex = (props) => {
  const config = props.config;
 //console.log(config["chart"]);
  switch (config["chart"]) {
    case "Simple_BarChart":
      return <BarChart1 {...props} />;
      break;
    case "Simple_LineChart":
      return <LineChart2 {...props} />;
      break;
    case "Simple_PieChart":
      return <PieChart3 {...props} />;
      break;
    case "Shadow_LineChart":
      return <LineChart4 {...props} />;
      break;
    case "Simple_BarChart2":
      return <BarChart6 {...props} />;
      break;
    case "Complex_LineChart":
      return <LineChart7 {...props} />;
      break;
    case "Complex_PieChart":
      return <PieChart8 {...props} />;
      break;
    case "Straight_LineChart":
      return <LineChart9 {...props} />;
      break;
    case "Simple_PieChart2":
      return <PieChart10 {...props} />;
      break;
    case "Horizontal_BarChart":
      return <BarChart11 {...props} />;
      break;
    case "Multi_BarChart":
      return <BarChart12 {...props} />;
      break;
    default:
      return <BarChart1 {...props} />;
      break;
  }
};
