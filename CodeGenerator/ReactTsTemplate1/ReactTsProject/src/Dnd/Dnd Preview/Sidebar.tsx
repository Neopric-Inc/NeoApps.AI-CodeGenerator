import React, { useState } from "react";
import { ButtonComponent } from "@syncfusion/ej2-react-buttons";
import { SidebarComponent } from "@syncfusion/ej2-react-navigations";
import "./style.css";
import { Nav, NavDropdown } from "react-bootstrap";
import {
  List,
  ListItem,
  ListItemText,
  Collapse,
  Typography,
} from "@mui/material";
import ExpandLessIcon from "@mui/icons-material/ExpandLess";
import ExpandMoreIcon from "@mui/icons-material/ExpandMore";
import IconButton from "@mui/material/IconButton";
import CloseIcon from "@mui/icons-material/Close";
import MenuIcon from "@mui/icons-material/Menu";
import SwipeableDrawer from "@mui/material/SwipeableDrawer";
import Divider from "@mui/material/Divider";

const submenu = [
  {
    id: -11,
    name: "Input Controls",
    active: "false",
  },
  {
    id: -12,
    name: "Data Display Controls",
    active: "false",
  },
  {
    id: -13,
    name: "Other Controls",
    active : "false",
  }
];
export const PreviewSidebar = ({ links, onLinkClick, config }) => {
  // const [toogle, settoogle] = useState(false);
  const [open, setOpen] = React.useState(false);
  const [openid, setOpenid] = React.useState(null);
  const [state, setState] = useState(false);


  const [custom,setCustom] = React.useState(false);
  const [customId, setCustomId] = React.useState(null);
  

  const toggleDrawer =
    (open: boolean) => (event: React.KeyboardEvent | React.MouseEvent) => {
      if (
        event &&
        event.type === "keydown" &&
        ((event as React.KeyboardEvent).key === "Tab" ||
          (event as React.KeyboardEvent).key === "Shift")
      ) {
        return;
      }
      setState(open);
    };

  const handleToggle = (eve, ind) => {
    if(ind >= 0){
      if (openid === ind) {
        setOpenid(null);
        setOpen(false);
      } else {
        setOpenid(ind);
        setOpen(true);
      }
    }else
    {
      if(customId === ind){
        setCustomId(null);
        setCustom(false);
      }
      else{
        setCustomId(ind);
        setCustom(true);
      }
    }

  };
  return (
    // <div
    //   className="sidebar"
    //   style={{
    //     backgroundColor:
    //       config["globalConfig"] !== undefined
    //         ? config["globalConfig"]["SidebarBackgroundColor"]
    //         : "#f1f1f1",
    //   }}
    // >
    //   {/* <h2>Sidebar</h2> */}

    //   <Nav className="flex-column">
    //     {links.map((link, index) => {
    //       return (
    //         <>
    //           <NavDropdown
    //             title={
    //               <span
    //                 style={{
    //                   color:
    //                     config["globalConfig"] !== undefined
    //                       ? config["globalConfig"]["SidebarTextColor"]
    //                       : "black",
    //                   fontWeight: "bold",
    //                 }}
    //               >
    //                 {link.name}
    //               </span>
    //             }
    //             id="basic-nav-dropdown"
    //             style={{
    //               backgroundColor:
    //                 config["globalConfig"] !== undefined
    //                   ? config["globalConfig"]["SidebarTextBackgroundColor"]
    //                   : "black",
    //             }}
    //           >
    //             {link.submenu !== undefined &&
    //               link.submenu.map((linka, indexa) => (
    //                 <NavDropdown.Item
    //                   key={indexa}
    //                   style={{
    //                     color:
    //                       config["globalConfig"] !== undefined
    //                         ? config["globalConfig"]["SidebarTextColor"]
    //                         : "black",
    //                   }}
    //                   onClick={() => {
    //                     const updatedLinks = [...links];
    //                     updatedLinks.forEach((l) => {
    //                       return l.submenu.forEach((l1) => (l1.active = false));
    //                     });
    //                     updatedLinks[index].submenu[indexa].active = true;
    //                     // setLinks(updatedLinks);
    //                     onLinkClick(linka.id);
    //                   }}
    //                   selected={index === 0 && indexa === 0 ? true : null}
    //                 >
    //                   {linka.name}
    //                 </NavDropdown.Item>
    //               ))}
    //           </NavDropdown>
    //         </>
    //       );
    //     })}
    //   </Nav>
    // </div>

    <div>
      <IconButton
        edge="start"
        color="inherit"
        aria-label="menu"
        style={{ paddingLeft: "20px" }}
        onClick={toggleDrawer(true)}
      >
        <MenuIcon />
      </IconButton>
      <SwipeableDrawer
        anchor="left"
        open={state}
        onClose={toggleDrawer(false)}
        onOpen={toggleDrawer(true)}
        PaperProps={{
          style: {
            backgroundColor:
              config?.globalConfig?.SidebarBackgroundColor ||
              "background.default",
          },
        }}
      >
        <Typography variant="h6">
          {config?.globalConfig?.innerContent || "Heading"}
        </Typography>

        <Divider />
        <List style={{ width: "25vw" }}>
          {links.map((link, index) => (
            <React.Fragment key={index}>
              <ListItem
                button
                onClick={(eve) => handleToggle(eve, index)}
                style={{
                  backgroundColor:
                    config?.globalConfig?.SidebarMenuTextBackgroundColor ||
                    "background.default",
                }}
              >
                <ListItemText
                  primary={
                    <span
                      style={{
                        color:
                          config?.globalConfig?.SidebarMenuTextColor || "blue",
                        fontWeight: "bold",
                      }}
                    >
                      {link.name}
                    </span>
                  }
                />
                {open && openid === index ? (
                  <ExpandLessIcon
                    style={{
                      color:
                        config?.globalConfig?.SidebarMenuTextColor || "blue",
                      fontWeight: "bold",
                    }}
                  />
                ) : (
                  <ExpandMoreIcon
                    style={{
                      color:
                        config?.globalConfig?.SidebarMenuTextColor || "blue",
                      fontWeight: "bold",
                    }}
                  />
                )}
              </ListItem>
              {openid === index && (
                <Collapse in={open} timeout="auto" unmountOnExit>
                  {link.submenu?.map((submenuLink, submenuIndex) => (
                    <ListItem
                      key={submenuIndex}
                      button
                      style={{
                        color:
                          config?.globalConfig?.SidebarSubMenuTextColor ||
                          "black",
                        backgroundColor:
                          config?.globalConfig
                            ?.SidebarSubMenuTextBackgroundColor ||
                          "background.default",
                      }}
                      onClick={() => {
                        const updatedLinks = [...links];
                        updatedLinks.forEach((l) => {
                          return (
                            l.submenu &&
                            l.submenu.forEach((l1) => (l1.active = false))
                          );
                        });
                        updatedLinks[index].submenu[submenuIndex].active = true;
                        // setLinks(updatedLinks);
                        onLinkClick(submenuLink.id);
                      }}
                      selected={index === 0 && submenuIndex === 0}
                    >
                      <ListItemText primary={submenuLink.name} />
                    </ListItem>
                  ))}
                </Collapse>
              )}
            </React.Fragment>
          ))}
          <React.Fragment>
            <ListItem
              button
              onClick={(eve) => handleToggle(eve, -1)}
              style={{
                backgroundColor:
                  config?.globalConfig?.SidebarMenuTextBackgroundColor ||
                  "background.default",
              }}
            >
              <ListItemText
                primary={
                  <span
                    style={{
                      color:
                        config?.globalConfig?.SidebarMenuTextColor || "blue",
                      fontWeight: "bold",
                    }}
                  >
                  Controls
                  </span>
                }
              />
              {custom && customId === -1 ? (
                <ExpandLessIcon
                  style={{
                    color: config?.globalConfig?.SidebarMenuTextColor || "blue",
                    fontWeight: "bold",
                  }}
                />
              ) : (
                <ExpandMoreIcon
                  style={{
                    color: config?.globalConfig?.SidebarMenuTextColor || "blue",
                    fontWeight: "bold",
                  }}
                />
              )}
            </ListItem>
            {customId === -1 && (
              <Collapse in={custom} timeout="auto" unmountOnExit>
                {submenu?.map((submenuLink, submenuIndex) => (
                  <ListItem
                    key={submenuIndex}
                    button
                    style={{
                      color:
                        config?.globalConfig?.SidebarSubMenuTextColor ||
                        "black",
                      backgroundColor:
                        config?.globalConfig
                          ?.SidebarSubMenuTextBackgroundColor ||
                        "background.default",
                    }}
                    onClick={() => {
                      //navigate("")
                      // const updatedLinks = [...links];
                      // updatedLinks.forEach((l) => {
                      //   return (
                      //     l.submenu &&
                      //     l.submenu.forEach((l1) => (l1.active = false))
                      //   );
                      // });
                      // updatedLinks[index].submenu[submenuIndex].active = true;
                      // // setLinks(updatedLinks);
                      onLinkClick(submenuLink.id);
                    }}
                    //selected={index === 0 && submenuIndex === 0}
                  >
                    <ListItemText primary={submenuLink.name} />
                  </ListItem>
                ))}
              </Collapse>
            )}
          </React.Fragment>
        </List>
      </SwipeableDrawer>
    </div>
  );
};

// {
//   /* <SidebarComponent width="auto">
// <br /><br />
// <h3>Sidebar</h3>
// <hr />
// <br />
// <div className="sidebar-content">
//     <div style={{ display: 'flex', flexDirection: 'column', justifyContent: 'center', margin: "auto" }}>
//         {links.map((link, index) => (
//             <ButtonComponent style={{ margin: "10px 20px" }}
//                 key={index}
//                 cssClass={link.active ? 'active' : ''}
//                 onClick={() => {
//                     const updatedLinks = [...links];
//                     updatedLinks.forEach(l => l.active = false);
//                     updatedLinks[index].active = true;
//                     // setLinks(updatedLinks);
//                     onLinkClick(link.id);
//                 }}
//             >
//                 {link.name}
//             </ButtonComponent>
//         ))}
//         <hr />
//     </div>
// </div>
// </SidebarComponent> */
// }
