import {
  Box,
  Container,
  Grid,
  Drawer,
  Dialog,
  DialogContent,
} from "@mui/material";

import { functionTOmap } from "Dnd/Dnd Designer/Utility/constants";
import React, { useEffect, useState } from "react";
import { getOneDnd_ui_versions } from "services/dnd_ui_versionsService";
import "./style.css";
import ApiIcon from "@mui/icons-material/Api";
// import { SyncFusion_Component_List } from "Dnd/Dnd Designer/Utility/constants";
// import { MUI_Component_List } from "Dnd/Dnd Designer/Utility/constants";
import { PreviewSidebar } from "./Sidebar";
import { DndProvider } from "react-dnd";
import { HTML5Backend } from "react-dnd-html5-backend";
import { useNavigate } from "react-router";
import IconButton from "@mui/material/IconButton";
import CloseIcon from "@mui/icons-material/Close";
import { InputControls } from "components/Controls/inputControls";

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
  useEffect(() => {
    // Whenever activeLink changes, push the previous active link to the stack
    setPrevActiveLinks((prevActiveLinks) => [...prevActiveLinks, activeLink]);
  }, [activeLink]);
  useEffect(() => {
    // Whenever layout changes, push the previous layout to the stack
    setPrevLayouts((prevLayouts) => [...prevLayouts, layout]);
  }, [layout]);

  // Add a handler to pop the top layout from the stack and set it as the current layout
  const handleDrawerClose = () => {
   //console.log("close close close close close closeclose close");

    setPrevLayouts((prevLayouts) => {
      if (prevLayouts.length > 1) {
        const newPrevLayouts = [...prevLayouts];
        newPrevLayouts.pop(); // Remove the current layout
        const lastLayout = newPrevLayouts[newPrevLayouts.length - 1]; // Get the previous layout
       //console.log(lastLayout);
        setLayout(lastLayout); // Set the previous layout
        return newPrevLayouts;
      } else {
        return prevLayouts; // If there is only one layout in the stack, do not pop
      }
    });
    setPrevActiveLinks((prevActiveLinks) => {
      if (prevActiveLinks.length > 1) {
        const newPrevActiveLinks = [...prevActiveLinks];
        newPrevActiveLinks.pop();
        const lastActiveLink =
          newPrevActiveLinks[newPrevActiveLinks.length - 1];
        setActiveLink(lastActiveLink);
        return newPrevActiveLinks;
      } else {
        return prevActiveLinks;
      }
    });
  };

  let navigate = useNavigate();

  useEffect(() => {
    const params = new URLSearchParams(window.location.search);
    setId(params.get("id"));
    fetchData(params.get("id"));
  }, []);

  const fetchData = async (id) => {
    const response = await getOneDnd_ui_versions(id);
    if (response["data"]["document"] !== null) {
     //console.log("Fetched Successfully");
      const dnd_builder_data = response["data"]["document"];
      let dnd_builder_layout = JSON.parse(dnd_builder_data["layout"]);
      let dnd_builder_layoutList = dnd_builder_layout["layoutList"];
      let dnd_builder_pages = JSON.parse(dnd_builder_data["ui_pages"]);
      let dnd_builder_components = JSON.parse(dnd_builder_data["components"]);
     //console.log(dnd_builder_components);
      ////console.log("layoutList : ", dnd_builder_layoutList, "pages : ", dnd_builder_pages);
      let formatted_components = {};
      ////console.log("Raw Components Data : ", dnd_builder_components)
      Object.keys(dnd_builder_components).forEach((key) => {
        const component_id = dnd_builder_components[key]["id"];
        // let temp_component = { id: dnd_builder_components[key]['id'], type: SIDEBAR_ITEM, component: { type: dnd_builder_components[key]['type'], content: dnd_builder_components[key]['content'], icon: ApiIcon } };
        // let temp_component = { id: dnd_builder_components[key]['id'], type: dnd_builder_components[key]['type'], content: mapNametoComponent[dnd_builder_components[key]['content']], icon: ApiIcon }
        let temp_config = {};
        temp_config[component_id] = dnd_builder_components[key]["config"];
        if (temp_config[component_id] !== undefined) {
          setConfigurations((prevState) => ({ ...prevState, ...temp_config }));
         //console.log(
           // "temp_config[component_id] : ",
           // temp_config[component_id]
          //);
        }

        let temp_component = {
          id: dnd_builder_components[key]["id"],
          type: dnd_builder_components[key]["type"],
          content: functionTOmap(dnd_builder_components[key]["type"], {}),
          icon: ApiIcon,
        };
        // dnd_builder_components[key]['icon_name'] !== undefined ? temp_component["component"]["icon"] = '<' + dnd_builder_components[key]['icon_name'] + " className='dnd sidebarIcon' />" : temp_component["component"]["icon"] = temp_component["component"]["icon"];
        formatted_components[component_id] = temp_component;
        ////console.log("mapNametoComponent : ", dnd_builder_components[key], mapNametoComponent[dnd_builder_components[key]['content']])
      });
      ////console.log("formatted components : ", formatted_components)
      // setLayout(dnd_builder_layout);
      // setComponents(formatted_components);

      ////console.log("layoutList : ", dnd_builder_layoutList, "pages : ", dnd_builder_pages, "components : ", formatted_components);
      ////console.log("layout from getLayout : ", dnd_builder_layoutList[0])
      setActiveLink(0);
      setLayoutList(dnd_builder_layoutList);
      setSidebarLinks(dnd_builder_pages["sidbarLinks"]);
      setPopupLinks(dnd_builder_pages["popupLinks"]);
      setDrawerLinks(dnd_builder_pages["drawerLinks"]);
      ////console.log("errorneous layout ", dnd_builder_layoutList[0]);
      // (dnd_builder_layoutList[0] === undefined || dnd_builder_layoutList[0] === null) ? setLayout([]) : setLayout(dnd_builder_layoutList[0]);
      setComponents(formatted_components);
      setLayout(dnd_builder_layoutList[0]);
    } else {
     //console.log("Some error occured While Fetching!");
    }
  };
  function getLayoutContent() {
    return (
      layout &&
      layout.map((row, index_r) => (
        <Box key={index_r}>
          <Grid>
            {row.children.map((column, index_c) => (
              <Grid key={index_c} item xs={13 / row.children.length}>
                <Box>
                  <Grid>
                    {column.children.map((row_column, index_rc) => (
                      <Grid key={index_rc} item xs={13 / column.length}>
                        <Box p={1}>
                          {(() => {
                            component_id = row_column.id;
                            if (
                              Components[component_id] !== undefined &&
                              Components[component_id].content !== undefined
                            )
                              Component_to_render = functionTOmap(
                                Components[component_id].type,
                                configurations[component_id],
                                handleButtonLinkClick,
                                "",
                                "",
                                handleTabLinkClick
                              );
                          })()}
                          {Component_to_render}
                        </Box>
                      </Grid>
                    ))}
                  </Grid>
                </Box>
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
   //console.log(layoutList);
    ////console.log("set active ID in Example :- ", id)
    // const [layoutList, setLayoutList] = useState({ activeLink: layout });
    // setLayoutList(prevState => ({
    //     ...prevState,
    //     [id]: [],
    // }));

    ////console.log("layoutList[id] from handleClick : ", layoutList[id]);
    // setLayoutList(prevState => ({ ...prevState, [activeLink]: layout }));
    ////console.log("LayoutList from handleLinkClick : ", layoutList)
    // setLayout(layout)
    if (condition) {
      layoutList[id].map((row, index) => {
        row.children.map((column, index) => {
          column.children.map((component, index) => {
           //console.log(configurations);
           //console.log(component);
            configurations[component.id]["filtercondition"] = JSON.stringify([
              condition,
            ]);

            setConfigurations(configurations);
          });
        });
      });
    }
   //console.log(condition);
   //console.log(configurations);
    setLayoutList((prevState) => ({ ...prevState, [activeLink]: layout }));
    setActiveLink(id);
    setLayout(layoutList[id]);
  }
  function handleTabLinkClick(id, condition?) {
    return (
      layoutList[id] &&
      layoutList[id].map((row, index_r) => (
        <Box key={index_r}>
          <Grid>
            {row.children.map((column, index_c) => (
              <Grid key={index_c} item xs={13 / row.children.length}>
                <Box>
                  <Grid>
                    {column.children.map((row_column, index_rc) => (
                      <Grid key={index_rc} item xs={13 / column.length}>
                        <Box p={1}>
                          {(() => {
                            component_id = row_column.id;
                            if (
                              Components[component_id] !== undefined &&
                              Components[component_id].content !== undefined
                            )
                              Component_to_render = functionTOmap(
                                Components[component_id].type,
                                configurations[component_id],
                                handleButtonLinkClick,
                                "",
                                "",
                                handleTabLinkClick
                              );
                          })()}
                          {Component_to_render}
                        </Box>
                      </Grid>
                    ))}
                  </Grid>
                </Box>
              </Grid>
            ))}
          </Grid>
        </Box>
      ))
    );
  }
  let component_id;
  let Component_to_render: any;
  return (
    <DndProvider backend={HTML5Backend}>
      {/* <Sidebar sidbarLinks={sidbarLinks} onLinkClick={() =>//console.log("onClick Event Happend.")} handleLinkAdd={() =>//console.log("onAdd Event Happend.")} /> */}
      {/* <h3><button onClick={fetchData} style={{ 'background': 'grey', 'color': 'white', justifyContent: 'center', textAlign: 'center', margin: 'auto' }}>Get Preview</button></h3> */}
      {/* <h3 style={{ 'background': 'grey', 'color': 'white', justifyContent: 'center', textAlign: 'center', margin: 'auto', padding: '5px', borderRadius: '10px' }}>Preview</h3> */}
      {/* <hr /> */}
      {console.log(
        "Outside fetch Layout : ",
        layout,
        "components : ",
        Components,
        "configurations : ",
        configurations
      )}
      {/* <p>ID : {id}</p> */}
      {/* <button onClick={fetchData} style={{ 'background': 'gray', 'color': 'white' }}>Get Preview</button> */}
      {/* {console.log("Layout : ", layout, "components : ", components)} */}
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
            maxWidth="xl" // Set the max width of the dialog to 'md', 'sm', 'lg', 'xl', or 'false'
            fullWidth={true}
            style={{ overflow: "visible" }}
          >
            <IconButton
              edge="end"
              onClick={handleDrawerClose}
              aria-label="close"
              style={{
                position: "fixed",
                top: 13,
                right: 25,
                color: "white",
                borderRadius: "50%", // Set to 50% for a round shape
                width: "40px", // Adjust the width to make it a circle (e.g., 40px)
                height: "40px", // Adjust the height to match the width (e.g., 40px)
                overflow: "visible",
                backgroundColor: "blue",
              }}
            >
              <CloseIcon />
            </IconButton>
            <DialogContent style={{ overflow: "visible" }}>
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
              backgroundColor:
                configurations["globalConfig"] !== undefined
                  ? configurations["globalConfig"]["backgroundColor"]
                  : "white",
              border: "1px solid #ddd",
            }}
            maxWidth={false}
          >
            {custom === -11 ? (
                                  <></>
              //<InputControls />
            ) : custom === -12 ? (
              <></>
            ) : (
              getLayoutContent()
            )}
            {/* {getLayoutContent()} */}
          </Container>
        )}
      </div>
    </DndProvider>
  );
};

// async function getLayout() {
//    //console.log("Get Layout is Clicked.... ", id)
//     // we are using id which is passed from index.tsx as a prop

//     const response = await getOneDnd_Builder(id);
//     if (response) {
//        //console.log("Fetched Successfully");
//         const dnd_builder_data = response["data"]["document"];
//         let dnd_builder_layout = JSON.parse(dnd_builder_data['layout']);
//         let dnd_builder_layoutList = dnd_builder_layout["layoutList"];
//         let dnd_builder_pages = dnd_builder_layout["pages"];
//         let dnd_builder_components = JSON.parse(dnd_builder_data['components']);
//         ////console.log("layoutList : ", dnd_builder_layoutList, "pages : ", dnd_builder_pages);
//         let formatted_components = {};
//         ////console.log("Raw Components Data : ", dnd_builder_components)
//         Object.keys(dnd_builder_components).forEach((key) => {
//             const component_id = dnd_builder_components[key]['id'];
//             // let temp_component = { id: dnd_builder_components[key]['id'], type: SIDEBAR_ITEM, component: { type: dnd_builder_components[key]['type'], content: dnd_builder_components[key]['content'], icon: ApiIcon } };
//             // let temp_component = { id: dnd_builder_components[key]['id'], type: dnd_builder_components[key]['type'], content: mapNametoComponent[dnd_builder_components[key]['content']], icon: ApiIcon }
//             let temp_component = { id: dnd_builder_components[key]['id'], type: dnd_builder_components[key]['type'], content: functionTOmap(dnd_builder_components[key]['content'], {}), icon: ApiIcon }
//             // dnd_builder_components[key]['icon_name'] !== undefined ? temp_component["component"]["icon"] = '<' + dnd_builder_components[key]['icon_name'] + " className='dnd sidebarIcon' />" : temp_component["component"]["icon"] = temp_component["component"]["icon"];
//             formatted_components[component_id] = temp_component;
//             ////console.log("mapNametoComponent : ", dnd_builder_components[key], mapNametoComponent[dnd_builder_components[key]['content']])
//         })
//         ////console.log("formatted components : ", formatted_components)
//         // setLayout(dnd_builder_layout);
//         // setComponents(formatted_components);

//         ////console.log("layoutList : ", dnd_builder_layoutList, "pages : ", dnd_builder_pages, "components : ", formatted_components);
//         ////console.log("layout from getLayout : ", dnd_builder_layoutList[0])
//         setActiveLink(0);
//         setLayoutList(dnd_builder_layoutList);
//         setSidebarLinks(dnd_builder_pages);
//         ////console.log("errorneous layout ", dnd_builder_layoutList[0]);
//         // (dnd_builder_layoutList[0] === undefined || dnd_builder_layoutList[0] === null) ? setLayout([]) : setLayout(dnd_builder_layoutList[0]);
//         setComponents(formatted_components);
//         setLayout(dnd_builder_layoutList[0]);
//     } else {
//        //console.log("Some error occured While Fetching!");
//     }
// }

// {(() => {
//     component_id = row_column.id;
//     Components[component_id] !== undefined && Components[component_id].content !== undefined ? Component_to_render = Components[component_id].content : Component_to_render = "";
// })()}
// {/* component_to_render = */}

// {Components[component_id] !== undefined && Components[component_id].content !== undefined ?
//     ((CRUD_Component_List.includes(Components[component_id].type) || SyncFusion_Component_List.includes(Components[component_id].type)) ? <Component_to_render /> : (MUI_Component_List.includes(Components[component_id].type) ? Components[component_id].content : <p>{Components[component_id].content}</p>)) : <p>Cant Render Component</p>}

// const fetchData = async () => {
//     const response = await getOneDnd_Builder(id);
//     if (response) {
//        //console.log("Fetched Successfully for ID : ", id);
//         const dnd_builder_data = response["data"]["document"];
//         let dnd_builder_layout = JSON.parse(dnd_builder_data['layout']);
//         let dnd_builder_components = JSON.parse(dnd_builder_data['components']);
//         let formatted_components = {};
//        //console.log("Raw Components Data : ", dnd_builder_components)
//         Object.keys(dnd_builder_components).forEach((key) => {
//             const component_id = dnd_builder_components[key]['id'];
//             let temp_component = { id: dnd_builder_components[key]['id'], type: dnd_builder_components[key]['type'], content: mapNametoComponent[dnd_builder_components[key]['content']], icon: ApiIcon }
//             formatted_components[component_id] = temp_component;
//         })
//        //console.log("Inside fetch layout : ", dnd_builder_layout, "formatted components : ", formatted_components)
//         setLayout(dnd_builder_layout);
//         setComponents(formatted_components);
//     }
//     else {
//     }
// }
