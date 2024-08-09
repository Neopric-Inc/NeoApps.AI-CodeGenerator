import React from "react";
import { useDrag } from "react-dnd";
import { COMPONENT } from "../Utility/constants";
import { Button } from "@mui/material";
import { makeStyles } from "@mui/styles";

const useStyles = makeStyles((theme) => ({
  sideBarItem: {
    border: "1px solid #93FAEB",
    borderRadius: "7px",
    //minHeight: "10px",
    height: "60px",
    width: "200px",
    // paddingTop: "10px",
    // paddingBottom: "10px",
    marginTop: "10px",
    marginBottom: "20px",
    //paddingLeft: "10px",
    textAlign: "left",
    background: "transparent",
    transition: "all 0.2s ease-in-out",
    display: "flex",

    flexWrap: "nowrap",
    alignItems: "center",
    // Add this to vertically center the content
    //padding: "10px",
    wordBreak: "break-word",
    // Add a bottom border to separate the items
    borderBottom: "1px solid #93FAEB",
    "&:hover": {
      transform: "scale(1.07)",
      color: "black",
      backgroundColor: "#93FAEB",
    },
    "&:last-child": {
      borderBottom: "none",
      // Remove the bottom border on the last item
    },
  },
  sidebarIcon: {
    fontSize: "120%",
    alignItems: "center",
    paddingRight: "10px",
    marginTop: "15px",
  },
  text: {
    flex: 1,
    fontSize: "120%",
    // Make the text take up remaining space
  },
}));

// You Have to modify COMPONENT here
const SideBarItem = ({ data }) => {
  const classes = useStyles();
  const [{ opacity }, drag] = useDrag({
    type: COMPONENT,
    item: data,
    collect: (monitor) => ({
      opacity: monitor.isDragging() ? 0.4 : 1,
    }),
  });

  return (
    // <div className="dnd sideBarItem" ref={drag} style={{ opacity }}>
    //     {data.component.icon}
    //     {data.component.type}
    // </div>
    <Button
      className="dnd sideBarItem"
      ref={drag}
      style={{ opacity, color: "white" }}
    >
      {data.component.icon}
      {data.component.type}
      {/* <span className={classes.sidebarIcon}>{data.component.icon}</span>
        <span className={classes.text}>{data.component.type}</span> */}
    </Button>
  );
};
export default SideBarItem;
