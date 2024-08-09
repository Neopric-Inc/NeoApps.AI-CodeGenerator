import React, { useEffect, useState } from "react";
import { makeStyles } from "@mui/styles";
import Tabs from "@mui/material/Tabs";
import Tab from "@mui/material/Tab";
import { useTheme } from "@mui/material/styles"; // Import useTheme

const useStyles = makeStyles(() => ({
  tabs: {
    backgroundColor: "primary", // Replace with your primary color
    padding: "16px", // Adjust padding as needed
    display: "flex",
    justifyContent: "center",
  },
  tab: {
    margin: "8px", // Adjust margin as needed
    padding: "8px 16px", // Adjust padding as needed
    color: "primary",
    //color: "#fff",
    borderRadius: "4px",
    cursor: "pointer",
    transition: "background-color 0.3s ease",
    "&.active": {
      backgroundColor: "secondary",
      color: "primary", // Replace with your secondary color
    },
  },
}));

type TabsProps = {
  selectedTab: string;
  onTabChange: (tabName: string) => void;
  customConfig: any;
  componentId: any;
};

function TabNavigation({
  selectedTab,
  onTabChange,
  customConfig,
  componentId,
}: TabsProps) {
  const classes = useStyles();
  const theme = useTheme(); // Use useTheme to access the theme object
  const [flag, setflag] = useState(false);
  const changeRender = () => {
    setflag(!flag);
  };
  useEffect(() => {
    changeRender();
  }, [customConfig]);

  const tabs = [
    "Mode",
    "General",
    "Navigation",
    "Create",
    "View",
    "Validation",
    "Filter",
    "Advanced",
    "Save&Exit",
  ];
  const CreateMode = ["Create", "Validation"];
  const ViewMode = ["View"];
  return (
    //Dev1 Code
    // <div className={classes.tabs}>
    //   <Tabs
    //     value={selectedTab}
    //     onChange={(event, newValue) => onTabChange(newValue)}
    //   >
    //     {tabs
    //       .filter((tab) => {
    //         if (customConfig[componentId]["mode"] === "create mode") {
    //           return !ViewMode.includes(tab);
    //         } else if (customConfig[componentId]["mode"] === "view mode") {
    //           return !CreateMode.includes(tab);
    //         }
    //         return true; // Render all tabs if mode doesn't match either condition
    //       })
    //       .map((tab) => (
    //         <Tab
    //           key={tab}
    //           value={tab}
    //           label={tab}
    //           className={`${classes.tab} ${
    //             selectedTab === tab ? "active" : ""
    //           }`}
    //         />
    //       ))}
    //   </Tabs>
    // </div>
    <div>
      <Tabs
        value={selectedTab}
        onChange={(event, newValue) => onTabChange(newValue)}
      >
        {tabs
          .filter((tab) => {
            if (customConfig[componentId]["mode"] === "create mode") {
              return !ViewMode.includes(tab);
            } else if (customConfig[componentId]["mode"] === "view mode") {
              return !CreateMode.includes(tab);
            }
            return true; // Render all tabs if mode doesn't match either condition
          })
          .map((tab) => (
            <Tab
              key={tab}
              value={tab}
              label={tab}
              className={`${classes.tab} ${
                selectedTab === tab ? "active" : ""
              }`}
            />
          ))}
      </Tabs>
    </div>
  );
}

export default TabNavigation;
