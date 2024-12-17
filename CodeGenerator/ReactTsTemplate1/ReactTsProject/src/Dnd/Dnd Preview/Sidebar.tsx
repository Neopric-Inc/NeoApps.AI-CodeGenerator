import React, { useState } from "react";
import SwipeableDrawer from "@mui/material/SwipeableDrawer";
import { 
  List, 
  ListItem, 
  ListItemText, 
  Collapse, 
  Typography, 
  Box,
  ListItemIcon,
  Divider
} from "@mui/material";
import IconButton from "@mui/material/IconButton";
import MenuIcon from "@mui/icons-material/Menu";
import ExpandLessIcon from "@mui/icons-material/ExpandLess";
import ExpandMoreIcon from "@mui/icons-material/ExpandMore";
// Import icons
import DashboardIcon from '@mui/icons-material/Dashboard';
import InputIcon from '@mui/icons-material/Input';
import TableViewIcon from '@mui/icons-material/TableView';
import WidgetsIcon from '@mui/icons-material/Widgets';
import LogoutIcon from '@mui/icons-material/Logout';

const submenu = [
  {
    id: -11,
    name: "Input Controls",
    icon: <InputIcon />,
    active: "false",
  },
  {
    id: -12,
    name: "Data Display Controls",
    icon: <TableViewIcon />,
    active: "false",
  },
  {
    id: -13,
    name: "Other Controls",
    icon: <WidgetsIcon />,
    active: "false",
  }
];

export const PreviewSidebar = ({ links, onLinkClick, config }) => {
  const [open, setOpen] = React.useState(false);
  const [openid, setOpenid] = React.useState(null);
  const [state, setState] = useState(false);
  const [custom, setCustom] = React.useState(false);
  const [customId, setCustomId] = React.useState(null);

  const toggleDrawer = (open: boolean) => (event: React.KeyboardEvent | React.MouseEvent) => {
    if (event && event.type === "keydown" &&
      ((event as React.KeyboardEvent).key === "Tab" ||
        (event as React.KeyboardEvent).key === "Shift")
    ) {
      return;
    }
    setState(open);
  };

  const handleToggle = (eve, ind) => {
    if (ind >= 0) {
      if (openid === ind) {
        setOpenid(null);
        setOpen(false);
      } else {
        setOpenid(ind);
        setOpen(true);
      }
    } else {
      if (customId === ind) {
        setCustomId(null);
        setCustom(false);
      } else {
        setCustomId(ind);
        setCustom(true);
      }
    }
  };

  const handleLogout = () => {
    localStorage.removeItem('token'); // Remove JWT token
    localStorage.removeItem('expire');
    window.location.href = '/'; // Redirect to root path
  };

  return (
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
          sx: {
            backgroundColor: config?.globalConfig?.SidebarBackgroundColor || "white",
            color: config?.globalConfig?.SidebarTextColor || "black",
            display: 'flex',
            flexDirection: 'column',
            height: '100%'
          }
        }}
      >
        <Box sx={{ p: 2 }}>
          <Typography variant="h6" component="div">
            {config?.globalConfig?.innerContent || "Dashboard"}
          </Typography>
        </Box>

        <Divider />

        {/* Main content */}
        <Box sx={{ flex: 1, overflow: 'auto' }}>
          <List sx={{ width: "280px" }}>
            {links.map((link, index) => (
              <React.Fragment key={index}>
                <ListItem
                  button
                  onClick={(eve) => handleToggle(eve, index)}
                  sx={{
                    backgroundColor: config?.globalConfig?.SidebarMenuTextBackgroundColor || "transparent",
                    '&:hover': {
                      backgroundColor: 'rgba(0, 0, 0, 0.04)'
                    }
                  }}
                >
                  <ListItemIcon>
                    <DashboardIcon sx={{ color: config?.globalConfig?.SidebarMenuTextColor || "inherit" }} />
                  </ListItemIcon>
                  <ListItemText
                    primary={link.name}
                    sx={{
                      color: config?.globalConfig?.SidebarMenuTextColor || "inherit",
                      '& .MuiTypography-root': {
                        fontWeight: 'medium'
                      }
                    }}
                  />
                  {open && openid === index ? <ExpandLessIcon /> : <ExpandMoreIcon />}
                </ListItem>

                <Collapse in={openid === index} timeout="auto" unmountOnExit>
                  <List component="div" disablePadding>
                    {link.submenu?.map((submenuLink, submenuIndex) => (
                      <ListItem
                        key={submenuIndex}
                        button
                        onClick={() => {
                          const updatedLinks = [...links];
                          updatedLinks.forEach((l) => {
                            l.submenu?.forEach((l1) => (l1.active = false));
                          });
                          updatedLinks[index].submenu[submenuIndex].active = true;
                          onLinkClick(submenuLink.id);
                        }}
                        sx={{
                          pl: 4,
                          backgroundColor: config?.globalConfig?.SidebarSubMenuTextBackgroundColor || "transparent",
                          color: config?.globalConfig?.SidebarSubMenuTextColor || "inherit",
                          '&:hover': {
                            backgroundColor: 'rgba(0, 0, 0, 0.04)'
                          }
                        }}
                      >
                        <ListItemText primary={submenuLink.name} />
                      </ListItem>
                    ))}
                  </List>
                </Collapse>
              </React.Fragment>
            ))}

            {/* Controls Section */}
            <ListItem
              button
              onClick={(eve) => handleToggle(eve, -1)}
              sx={{
                backgroundColor: config?.globalConfig?.SidebarMenuTextBackgroundColor || "transparent",
                '&:hover': {
                  backgroundColor: 'rgba(0, 0, 0, 0.04)'
                }
              }}
            >
              <ListItemIcon>
                <WidgetsIcon sx={{ color: config?.globalConfig?.SidebarMenuTextColor || "inherit" }} />
              </ListItemIcon>
              <ListItemText
                primary="Controls"
                sx={{
                  color: config?.globalConfig?.SidebarMenuTextColor || "inherit",
                  '& .MuiTypography-root': {
                    fontWeight: 'medium'
                  }
                }}
              />
              {custom && customId === -1 ? <ExpandLessIcon /> : <ExpandMoreIcon />}
            </ListItem>

            <Collapse in={customId === -1} timeout="auto" unmountOnExit>
              <List component="div" disablePadding>
                {submenu?.map((submenuLink, submenuIndex) => (
                  <ListItem
                    key={submenuIndex}
                    button
                    onClick={() => onLinkClick(submenuLink.id)}
                    sx={{
                      pl: 4,
                      backgroundColor: config?.globalConfig?.SidebarSubMenuTextBackgroundColor || "transparent",
                      color: config?.globalConfig?.SidebarSubMenuTextColor || "inherit",
                      '&:hover': {
                        backgroundColor: 'rgba(0, 0, 0, 0.04)'
                      }
                    }}
                  >
                    <ListItemIcon>
                      {submenuLink.icon}
                    </ListItemIcon>
                    <ListItemText primary={submenuLink.name} />
                  </ListItem>
                ))}
              </List>
            </Collapse>
          </List>
        </Box>

        {/* Logout button at bottom */}
        <Box sx={{ p: 2 }}>
          <Divider />
          <ListItem
            button
            onClick={handleLogout}
            sx={{
              mt: 1,
              backgroundColor: 'rgba(0, 0, 0, 0.08)',
              '&:hover': {
                backgroundColor: 'rgba(0, 0, 0, 0.12)'
              }
            }}
          >
            <ListItemIcon>
              <LogoutIcon color="error" />
            </ListItemIcon>
            <ListItemText 
              primary="Logout" 
              sx={{ 
                color: 'error.main',
                '& .MuiTypography-root': {
                  fontWeight: 'medium'
                }
              }} 
            />
          </ListItem>
        </Box>
      </SwipeableDrawer>
    </div>
  );
};