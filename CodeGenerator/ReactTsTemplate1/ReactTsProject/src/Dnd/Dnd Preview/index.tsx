import {
  Box,
  Container,
  Grid,
  Drawer,
  Dialog,
  DialogContent,
  CircularProgress,
  Typography,
} from "@mui/material";

import { functionTOmap } from "Dnd/Dnd Designer/Utility/constants";
import React, { useEffect, useState } from "react";
import { getOneDnd_ui_versions } from "services/dnd_ui_versionsService";
import "./style.css";
import ApiIcon from "@mui/icons-material/Api";
import { PreviewSidebar } from "./Sidebar";
import { DndProvider } from "react-dnd";
import { HTML5Backend } from "react-dnd-html5-backend";
import { useNavigate } from "react-router";
import IconButton from "@mui/material/IconButton";
import CloseIcon from "@mui/icons-material/Close";

export const PreviewDndBuilder = () => {
  const [id, setId] = useState(null);
  const [activeLink, setActiveLink] = useState(0);
  const [sidbarLinks, setSidebarLinks] = useState([]);
  const [popupLinks, setPopupLinks] = useState([]);
  const [drawerLinks, setDrawerLinks] = useState([]);
  const [layout, setLayout] = useState([]);
  const [layoutList, setLayoutList] = useState({});
  const [Components, setComponents] = useState({});
  const [configurations, setConfigurations] = useState({});
  const [prevLayouts, setPrevLayouts] = useState([]);
  const [prevActiveLinks, setPrevActiveLinks] = useState([]);
  const [custom, setCustom] = useState(null);
  const [error, setError] = useState(null);
  const [isLoading, setIsLoading] = useState(true);

  let navigate = useNavigate();

  useEffect(() => {
    setPrevActiveLinks((prevActiveLinks) => [...prevActiveLinks, activeLink]);
  }, [activeLink]);

  useEffect(() => {
    setPrevLayouts((prevLayouts) => [...prevLayouts, layout]);
  }, [layout]);

  useEffect(() => {
    const params = new URLSearchParams(window.location.search);
    setId(params.get("id"));
    fetchData(params.get("id"));
  }, []);

  const handleDrawerClose = () => {
    setPrevLayouts((prevLayouts) => {
      if (prevLayouts.length > 1) {
        const newPrevLayouts = [...prevLayouts];
        newPrevLayouts.pop();
        const lastLayout = newPrevLayouts[newPrevLayouts.length - 1];
        setLayout(lastLayout);
        return newPrevLayouts;
      }
      return prevLayouts;
    });

    setPrevActiveLinks((prevActiveLinks) => {
      if (prevActiveLinks.length > 1) {
        const newPrevActiveLinks = [...prevActiveLinks];
        newPrevActiveLinks.pop();
        const lastActiveLink = newPrevActiveLinks[newPrevActiveLinks.length - 1];
        setActiveLink(lastActiveLink);
        return newPrevActiveLinks;
      }
      return prevActiveLinks;
    });
  };

  const fetchData = async (id) => {
    try {
      setIsLoading(true);
      const response = await getOneDnd_ui_versions(id);
      if (response["data"]["document"] !== null) {
        const dnd_builder_data = response["data"]["document"];
        let dnd_builder_layout = JSON.parse(dnd_builder_data["layout"]);
        let dnd_builder_layoutList = dnd_builder_layout["layoutList"];
        let dnd_builder_pages = JSON.parse(dnd_builder_data["ui_pages"]);
        let dnd_builder_components = JSON.parse(dnd_builder_data["components"]);

        let formatted_components = {};
        Object.keys(dnd_builder_components).forEach((key) => {
          const component_id = dnd_builder_components[key]["id"];
          let temp_config = {};
          temp_config[component_id] = dnd_builder_components[key]["config"];
          if (temp_config[component_id] !== undefined) {
            setConfigurations((prevState) => ({ ...prevState, ...temp_config }));
          }

          let temp_component = {
            id: dnd_builder_components[key]["id"],
            type: dnd_builder_components[key]["type"],
            content: functionTOmap(dnd_builder_components[key]["type"], {}),
            icon: ApiIcon,
          };
          formatted_components[component_id] = temp_component;
        });

        setSidebarLinks(dnd_builder_pages["sidbarLinks"] || []);
        setPopupLinks(dnd_builder_pages["popupLinks"] || []);
        setDrawerLinks(dnd_builder_pages["drawerLinks"] || []);
        setComponents(formatted_components);
        setLayoutList(dnd_builder_layoutList);

        // Set initial layout and active link
        if (dnd_builder_layoutList && Object.keys(dnd_builder_layoutList).length > 0) {
          const firstLayoutKey = Object.keys(dnd_builder_layoutList)[0];
          setLayout(dnd_builder_layoutList[firstLayoutKey]);
          setActiveLink(Number(firstLayoutKey));
        }
      } else {
        setError("No data found");
      }
    } catch (err) {
      setError("Error loading data");
      console.error(err);
    } finally {
      setIsLoading(false);
    }
  };

  function getLayoutContent() {
    return (
      layout &&
      layout.map((row, index_r) => (
        <Box key={index_r} sx={{ m: 0, p: 0, lineHeight: 0 }}>
          <Grid 
            container 
            spacing={0}
            sx={{ 
              m: 0,
              p: 0,
              flexWrap: 'nowrap',
              '& .MuiGrid-root': {
                m: 0,
                p: 0
              }
            }}
          >
            {row.children.map((column, index_c) => (
              <Grid 
                key={index_c} 
                item 
                xs={12 / row.children.length}
                sx={{ 
                  m: 0,
                  p: 0,
                  minWidth: 0,
                  flexShrink: 0
                }}
              >
                {column.children.map((row_column, index_rc) => {
                  component_id = row_column.id;
                  if (Components[component_id]?.content !== undefined) {
                    Component_to_render = functionTOmap(
                      Components[component_id].type,
                      configurations[component_id],
                      handleButtonLinkClick,
                      "",
                      "",
                      handleTabLinkClick
                    );
                  }
                  return Component_to_render;
                })}
              </Grid>
            ))}
          </Grid>
        </Box>
      ))
    );
  }

  function onLinkClick(id) {
    if (id >= 0) {
      setActiveLink(id);
      setLayout(layoutList[id]);
      setCustom(null);
    } else {
      setActiveLink(id);
      setCustom(id);
    }
  }

  function handleButtonLinkClick(id, condition?) {
    if (condition) {
      layoutList[id].map((row) => {
        row.children.map((column) => {
          column.children.map((component) => {
            configurations[component.id]["filtercondition"] = JSON.stringify([condition]);
            setConfigurations(configurations);
          });
        });
      });
    }
    setLayoutList((prevState) => ({ ...prevState, [activeLink]: layout }));
    setActiveLink(id);
    setLayout(layoutList[id]);
  }

  function handleTabLinkClick(id, condition?) {
    return (
      layoutList[id] &&
      layoutList[id].map((row, index_r) => (
        <Box key={index_r} sx={{ m: 0, p: 0, lineHeight: 0 }}>
          <Grid 
            container 
            spacing={0}
            sx={{ 
              m: 0,
              p: 0,
              flexWrap: 'nowrap',
              '& .MuiGrid-root': {
                m: 0,
                p: 0
              }
            }}
          >
            {row.children.map((column, index_c) => (
              <Grid 
                key={index_c} 
                item 
                xs={12 / row.children.length}
                sx={{ 
                  m: 0,
                  p: 0,
                  minWidth: 0,
                  flexShrink: 0
                }}
              >
                {column.children.map((row_column) => {
                  component_id = row_column.id;
                  if (Components[component_id]?.content !== undefined) {
                    Component_to_render = functionTOmap(
                      Components[component_id].type,
                      configurations[component_id],
                      handleButtonLinkClick,
                      "",
                      "",
                      handleTabLinkClick
                    );
                  }
                  return Component_to_render;
                })}
              </Grid>
            ))}
          </Grid>
        </Box>
      ))
    );
  }

  let component_id;
  let Component_to_render: any;

  if (isLoading) {
    return (
      <Box 
        display="flex" 
        justifyContent="center" 
        alignItems="center" 
        minHeight="100vh"
      >
        <CircularProgress />
      </Box>
    );
  }

  if (error) {
    return (
      <Box 
        display="flex" 
        justifyContent="center" 
        alignItems="center" 
        minHeight="100vh"
      >
        <Typography color="error">{error}</Typography>
      </Box>
    );
  }

  return (
    <DndProvider backend={HTML5Backend}>
      <div style={{ display: "flex", flexDirection: "row" }}>
        <PreviewSidebar
          links={sidbarLinks}
          config={configurations}
          onLinkClick={onLinkClick}
        />
        {popupLinks.some((link) => link.id === activeLink) ? (
          <Dialog
          open={popupLinks.some((link) => link.id === activeLink)}
          onClose={handleDrawerClose}
          maxWidth="xl"
          fullWidth={true}
          PaperProps={{
            sx: {
              borderRadius: '12px',
              border: '2px solid black',
              boxShadow: '0 8px 24px rgba(0,0,0,0.2)',
              position: 'relative',
              overflow: 'visible', // Allow close button to overflow
              m: 2, // Add margin around dialog
              maxHeight: '90vh' // Prevent dialog from being too tall
            }
          }}
        >
          {/* Close Button */}
          <IconButton
            onClick={handleDrawerClose}
            aria-label="close"
            sx={{
              position: 'absolute',
              right: -16,
              top: -16,
              backgroundColor: 'black',
              color: 'white',
              border: '2px solid black',
              width: 40,
              height: 40,
              '&:hover': {
                backgroundColor: '#333',
                color: 'white',
              },
              '&:active': {
                backgroundColor: '#000',
              },
              boxShadow: '0 2px 8px rgba(0,0,0,0.2)',
              zIndex: 1
            }}
          >
            <CloseIcon />
          </IconButton>
        
          {/* Dialog Content */}
          <DialogContent 
            sx={{
              p: 3, // Increased padding
              '&:first-of-type': {
                pt: 3 // Remove default extra top padding
              },
              overflow: 'auto',
              '&::-webkit-scrollbar': {
                width: '8px',
              },
              '&::-webkit-scrollbar-track': {
                background: '#f1f1f1',
                borderRadius: '4px',
              },
              '&::-webkit-scrollbar-thumb': {
                background: '#888',
                borderRadius: '4px',
                '&:hover': {
                  background: '#555',
                },
              },
            }}
          >
            {getLayoutContent()}
          </DialogContent>
        </Dialog>
        ) : drawerLinks.some((link) => link.id === activeLink) ? (
          <Drawer
            anchor="right"
            open={drawerLinks.some((link) => link.id === activeLink)}
            onClose={handleDrawerClose}
            PaperProps={{ style: { width: "70%" } }}
          >
            {getLayoutContent()}
          </Drawer>
        ) : (
          <Container
            style={{
              backgroundColor: configurations["globalConfig"]?.backgroundColor || "white",
              border: "1px solid #ddd",
              padding: 0,
              margin: 0,
              maxWidth: '100%',
              overflow: 'hidden'
            }}
            maxWidth={false}
            disableGutters
          >
            {custom === -11 ? (
              <></>
            ) : custom === -12 ? (
              <></>
            ) : (
              getLayoutContent()
            )}
          </Container>
        )}
      </div>
    </DndProvider>
  );
};