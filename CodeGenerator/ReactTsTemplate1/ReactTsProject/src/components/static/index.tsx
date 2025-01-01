import React, {  } from "react";
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
    case "detailList2":
      return <DetailList2 {...props} />;
    case "detailList3":
      return <DetailList3 {...props} />;
    case "detailList6":
      return <DetailList6 {...props} />;
    case "detailList7":
      return <DetailList7 {...props} />;
    case "groupedList1":
      return <GroupedList1 {...props} />;
    case "groupedList2":
      return <GroupedList2 {...props} />;
    case "groupedList3":
      return <GroupedList3 {...props} />;
    case "groupedList4":
      return <GroupedList4 {...props} />;
    case "groupedList5":
      return <GroupedList5 {...props} />; 
    case "groupedList6":
      return <GroupedList6 {...props} />;    
    case "groupedList7":
      return <GroupedList7 {...props} />;
    case "groupedList8":
      return <GroupedList8 {...props} />;
    case "groupedList9":
      return <GroupedList9 {...props} />;
    case "groupedList10":
      return <GroupedList10 {...props} />;
    case "groupedList11":
      return <GroupedList11 {...props} />;
    case "gridView1":
      return <GridList1 {...props} />;
    case "gridView2":
      return <GridList2 {...props} />;
    case "gridView3":
      return <GridList3 {...props} />;
    case "gridView4":
      return <GridList4 {...props} />;
    case "gridView5":
      return <GridList5 {...props} />;
    case "gridView6":
      return <GridList6 {...props} />;
    default:
      return <label>no preview Available</label>;
  }
};
