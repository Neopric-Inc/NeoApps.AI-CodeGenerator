import type { ChangeEvent } from "react";
import { useEffect, useState } from "react";
import { Box, Container, Divider, Tab, Tabs, Typography } from "@mui/material";
import Grid from "@mui/material/Grid";
const TabsView = (props) => {
  const config = props.config;
  const [currentTab, setCurrentTab] = useState<string>();
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
        sx={{ mt: 3 }}
      >
        {tabs1.map((tab) => (
          <Tab key={tab.value1} label={tab.label} value={tab.value1} />
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
