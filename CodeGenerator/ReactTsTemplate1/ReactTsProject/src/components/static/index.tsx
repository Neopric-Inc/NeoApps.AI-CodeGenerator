import React, { useState, useEffect } from "react";
import { Typography } from "@mui/material";
import { DetailList1 } from "./detail-list/detail-list-1";
import { DetailList2 } from "./detail-list/detail-list-2";
import { DetailList3 } from "./detail-list/detail-list-3";
import { DetailList6 } from "./detail-list/detail-list-6";
import { DetailList7 } from "./detail-list/detail-list-7";
import { GroupedList1 } from "./group-list/grouped-list-1";
import { GroupedList2 } from "./group-list/grouped-list-2";
import { GroupedList3 } from "./group-list/grouped-list-3";
import { GroupedList4 } from "./group-list/grouped-list-4";
import { GroupedList5 } from "./group-list/grouped-list-5";
import { GroupedList6 } from "./group-list/grouped-list-6";
import { GroupedList7 } from "./group-list/grouped-list-7";
import { GroupedList8 } from "./group-list/grouped-list-8";
import { GroupedList9 } from "./group-list/grouped-list-9";
import { GroupedList10 } from "./group-list/grouped-list-10";
import { GroupedList11 } from "./group-list/grouped-list-11";
import { GridList1 } from "./grid-list/grid-list-1";
import { GridList2 } from "./grid-list/grid-list-2";
import { GridList3 } from "./grid-list/grid-list-3";
import { GridList4 } from "./grid-list/grid-list-4";
import { GridList5 } from "./grid-list/grid-list-5";
import { GridList6 } from "./grid-list/grid-list-6";

export const DemoViews = (props) => {
  const selectedView = props.selectedView;
  switch (selectedView) {
    case "detailList1":
      return <DetailList1 {...props} />;
      break;
    case "detailList2":
      return <DetailList2 {...props} />;
      break;
    case "detailList3":
      return <DetailList3 {...props} />;
      break;
    case "detailList6":
      return <DetailList6 {...props} />;
      break;
    case "detailList7":
      return <DetailList7 {...props} />;
      break;
    case "groupedList1":
      return <GroupedList1 {...props} />;
      break;
    case "groupedList2":
      return <GroupedList2 {...props} />;
      break;
    case "groupedList3":
      return <GroupedList3 {...props} />;
      break;
    case "groupedList4":
      return <GroupedList4 {...props} />;
      break;
    case "groupedList5":
      return <GroupedList5 {...props} />;
      break;
    case "groupedList6":
      return <GroupedList6 {...props} />;
      break;
    case "groupedList7":
      return <GroupedList7 {...props} />;
      break;
    case "groupedList8":
      return <GroupedList8 {...props} />;
      break;
    case "groupedList9":
      return <GroupedList9 {...props} />;
      break;
    case "groupedList10":
      return <GroupedList10 {...props} />;
      break;
    case "groupedList11":
      return <GroupedList11 {...props} />;
      break;
    case "gridView1":
      return <GridList1 {...props} />;
      break;
    case "gridView2":
      return <GridList2 {...props} />;
      break;
    case "gridView3":
      return <GridList3 {...props} />;
      break;
    case "gridView4":
      return <GridList4 {...props} />;
      break;
    case "gridView5":
      return <GridList5 {...props} />;
      break;
    case "gridView6":
      return <GridList6 {...props} />;
      break;
    default:
      return <label>no preview Available</label>;
      break;
  }
};
