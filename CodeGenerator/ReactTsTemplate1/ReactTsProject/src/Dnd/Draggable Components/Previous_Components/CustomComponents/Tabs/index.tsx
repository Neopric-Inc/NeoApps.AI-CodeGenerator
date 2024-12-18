// src/Dnd/Draggable Components/Previous_Components/CustomComponents/Tabs/index.tsx

import type { ChangeEvent } from "react";
import { useEffect, useState } from "react";
import { Box, Container, Divider, Tab, Tabs, Typography } from "@mui/material";
import Grid from "@mui/material/Grid";

const TabsView = (props) => {
  const config = props.config;
  // Set default tab value to 1 (first tab)
  const [currentTab, setCurrentTab] = useState<number>(1);
  const [currentComponent, setCurrentComponent] = useState<string>();
  const [currentComponentId, setCurrentComponentId] = useState<string>();

  const [tabs1, settabs1] = useState([]);
  const newTabs1 = [];

  useEffect(() => {
    const newTabs1 = [];
    for (let i = 0; i < config["tab_nav_count"]; i++) {
      newTabs1.push({
        label: config[`tab_nav_${i}_button_name`],
        value: config[`tab_nav_${i}_page`],
        component: config[`tab_nav_${i}_type`],
        value1: i + 1,
      });
    }
    settabs1(newTabs1);
    
    // Set initial component and componentId for the first tab
    if (newTabs1.length > 0) {
      setCurrentComponent(newTabs1[0].component);
      setCurrentComponentId(newTabs1[0].value);
    }
  }, [config["tab_nav_count"]]);

  const handleTabsChange = (event: ChangeEvent<{}>, value: any): void => {
    setCurrentTab(value);
    const tempTab = tabs1.filter((tb) => tb["value1"] === value);
    setCurrentComponent(tempTab[0].component);
    setCurrentComponentId(tempTab[0].value);
  };

  return (
    <Grid container>
      <Grid item xs={12} sm={12} md={12} lg={12} xl={12}>
        <Typography variant="h4">
          {config.Heading ? config.Heading : "Tab Heading"}
        </Typography>
        <Tabs
          indicatorColor="primary"
          onChange={handleTabsChange}
          scrollButtons="auto"
          textColor="primary"
          value={currentTab}
          variant="scrollable"
          sx={{
            mt: 3,
            '& .MuiTab-root': {
              color: '#000000',  // Black color for all tabs
              '&.Mui-selected': {
                color: '#000000',  // Black color for selected tab
              },
            },
            '& .MuiTabs-indicator': {
              backgroundColor: '#000000',  // Black color for the indicator line
            },
          }}
        >
          {tabs1.map((tab) => (
            <Tab 
              key={tab.value1} 
              label={tab.label} 
              value={tab.value1}
            />
          ))}
        </Tabs>
        <Divider sx={{ mb: 3 }} />

        {currentComponent &&
          currentComponent === "components Pages" &&
          props.openTabLink(currentComponentId)}
        {currentComponent &&
          currentComponent !== "components Pages" &&
          props.openLink(currentComponentId)}
      </Grid>
    </Grid>
  );
};

export default TabsView;