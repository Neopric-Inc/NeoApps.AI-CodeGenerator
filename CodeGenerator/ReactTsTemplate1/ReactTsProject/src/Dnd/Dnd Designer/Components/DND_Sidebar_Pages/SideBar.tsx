import React, { useEffect, useRef, useState } from "react";
import {TreeView} from "@mui/x-tree-view/TreeView";
import {TreeItem} from "@mui/x-tree-view/TreeItem";
import ChevronRightIcon from "@mui/icons-material/ChevronRight";
import CircleIcon from "@mui/icons-material/Circle";
import {
  Box,
  Button,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  FormControl,
  FormControlLabel,
  FormLabel,
  Input,
  Radio,
  RadioGroup,
  Select,
  MenuItem,
  Typography,
  TextField,
  Accordion,
  AccordionSummary,
  AccordionDetails,
  ListItem,
  ListItemText,
  Grid,
} from "@mui/material";
import ExpandMoreIcon from "@mui/icons-material/ExpandMore";
import CloseIcon from "@mui/icons-material/Close";
import { SidebarComponent } from "@syncfusion/ej2-react-navigations";
import { Popup } from "./../Component";
// import "./style.css";
import { registerLicense } from "@syncfusion/ej2-base";
import IconButton from "@mui/material/IconButton";
import Divider from "@mui/material/Divider";
import SwipeableDrawer from "@mui/material/SwipeableDrawer";
import MenuIcon from "@mui/icons-material/Menu";
import { Tooltip } from "@mui/material";
import { Help } from "@mui/icons-material";

registerLicense(process.env.REACT_APP_SYNCFUSION_LICENSE_KEY);

const drawerWidth = 300;

const Sidebar = ({
  sidbarLinks,
  popupLinks,
  drawerLinks,
  componentLinks,
  onLinkClick,
  config,
  configChange,
  handleLinkAdd,
  handleLinkDelete,
  handleLabelDelete,
  handlepopupDelete,
  handledrawerDelete,
  handleComponentDelete,
  handlePopupLinkAdd,
  handleDrawerLinkAdd,
  handleComponentLinkAdd,
}) => {
  let sidebarObj: SidebarComponent;
  function onCreate(): void {
    sidebarObj.element.style.visibility = "";
  }

  const [links, setLinks] = useState(sidbarLinks);
  const [popuplinks, setpopupLinks] = useState(popupLinks);
  const [drawerlinks, setdrawerLinks] = useState(drawerLinks);
  const [componentlinks, setComponentLinks] = useState(componentLinks);
  const [editing, setEditing] = useState(null);
  const [pageName, setPageName] = useState("");
  const [addName, setAddName] = useState("");
  const [openPopup, setOpenPopup] = useState(false);
  const [clicked, setClicked] = useState(false);
  const [blurred, setBlurred] = useState(false);
  const [open, setOpen] = useState(false);
  const [selectMenu, setSelectMenu] = useState(false);
  const [addPageId, setAddPageId] = useState(null);
  const nameInputRef = useRef(null);
  const [openAccordion, setOpenAccordion] = useState(null);
  const [openS, setOpenS] = useState(null);
  const [state, setState] = useState(false);

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
      if (!open) {
        setEditing(null);
      }
    };

  const handleconfigClick = () => {
    if (config["globalConfig"] === undefined) config["globalConfig"] = {};
    setOpen(true);
  };
  const saveLink1 = (e) => {
    setAddPageId(-1);
    setOpenPopup(true);
  };
  const saveLink = (e) => {
    const name = addName;
    if (name) {
      if (addPageId === -1) {
        handleLinkAdd([
          ...links,
          { id: links.length, name: name, active: false, submenu: [] },
        ]);
        setLinks([
          ...links,
          { id: links.length, name: name, active: false, submenu: [] },
        ]);
      } else {
        const temp = links.map((link) => {
          if (link.id === parseInt(addPageId)) {
            link.submenu = [
              ...link.submenu,
              {
                id: link.id.toString() + link.submenu.length.toString(),
                name: name,
                active: false,
              },
            ];
            return link;
          }
          return link;
        });
        setLinks(temp);
        handleLinkAdd(temp);
      }
      setAddName("");
      setAddPageId(null);
      // props.handleLinkAdd(links);
    }
    setOpenPopup(false);
    setSelectMenu(false);
  };
  const savepopupLink = () => {
    const name = addName;
    if (name) {
      handlePopupLinkAdd([
        ...popuplinks,
        { id: "p" + popuplinks.length.toString(), name: name, active: false },
      ]);
      setpopupLinks([
        ...popuplinks,
        { id: "p" + popuplinks.length.toString(), name: name, active: false },
      ]);
      setAddName("");
      // props.handleLinkAdd(links);
    }
  };
  const savedrawerLink = () => {
    const name = addName;
    if (name) {
      handleDrawerLinkAdd([
        ...drawerlinks,
        { id: "s" + drawerlinks.length.toString(), name: name, active: false },
      ]);
      setdrawerLinks([
        ...drawerlinks,
        { id: "s" + drawerlinks.length.toString(), name: name, active: false },
      ]);
      setAddName("");
      // props.handleLinkAdd(links);
    }
  };
  const saveComponentLink = () => {
    const name = addName;
    if (name) {
      handleComponentLinkAdd([
        ...componentlinks,
        {
          id: "c" + componentlinks.length.toString(),
          name: name,
          active: false,
        },
      ]);
      setComponentLinks([
        ...componentlinks,
        {
          id: "c" + componentlinks.length.toString(),
          name: name,
          active: false,
        },
      ]);
      setAddName("");
      // props.handleLinkAdd(links);
    }
  };

  useEffect(() => {
    setLinks(sidbarLinks);
    setpopupLinks(popupLinks);
    setdrawerLinks(drawerLinks);
    setComponentLinks(componentLinks);
    setEditing(null);
    setEditing(null);

    ////console.log("Links for Sidebar : ", links);
  }, [sidbarLinks, popupLinks, drawerLinks, componentLinks]);

  const handleDoubleClick = (index, name) => {
   //console.log(index, name);
    setEditing(index);
    setPageName(name);
  };

  function onFocus(component) {
    setClicked(true);
    setBlurred(false);
    setOpen(true);
  }

  function handleClose() {
    setOpen(false);
  }

  const handleonChange = (e) => {
    setPageName(e.target.value);
  };
  const handleAddonChange = (e) => {
    setAddName(e.target.value);
  };

  const handleonClick = () => {
   //console.log(editing);
    setLinks((prev) =>
      prev.map((data, index) => {
        if (data.id === editing) {
          data.name = pageName;
          return data;
        } else return data;
      })
    );
    setEditing(null);
    setPageName("");
  };
  const handleonpopupClick = () => {
   //console.log(editing);
    setpopupLinks((prev) =>
      prev.map((data, index) => {
        if (data.id === editing) {
          data.name = pageName;
          return data;
        } else return data;
      })
    );
    setEditing(null);
    setPageName("");
  };
  const handleondrawerClick = () => {
   //console.log(editing);
    setdrawerLinks((prev) =>
      prev.map((data, index) => {
        if (data.id === editing) {
          data.name = pageName;
          return data;
        } else return data;
      })
    );
    setEditing(null);
    setPageName("");
  };
  const handleonComponentClick = () => {
   //console.log(editing);
    setComponentLinks((prev) =>
      prev.map((data, index) => {
        if (data.id === editing) {
          data.name = pageName;
          return data;
        } else return data;
      })
    );
    setEditing(null);
    setPageName("");
  };

  const handleonSubClick = (e, nd) => {
   //console.log(editing);
    setLinks((prev) =>
      prev.map((data, index) => {
        if (data.id === nd && data.submenu && data.submenu.length > 0) {
          return {
            ...data,
            submenu: data.submenu.map((dt, ind) => {
              if (dt.id === editing) {
                dt.name = pageName;
              }
              return dt;
            }),
          };
        }
        return data;
      })
    );
    setEditing(null);
    setPageName("");
  };

  const handleDelete = (id, e) => {
    e.stopPropagation();
    handleLinkDelete(id);
  };
  const handleLDelete = (id, e) => {
    e.stopPropagation();
    handleLabelDelete(id);
  };
  const handlepopDelete = (id, e) => {
    e.stopPropagation();
    handlepopupDelete(id);
  };
  const handledrawDelete = (id, e) => {
    e.stopPropagation();
    handledrawerDelete(id);
  };
  const hanbleClosePopup = () => {
    setOpenPopup(false);
    setSelectMenu(false);
  };

  const addPageChange = (e) => {
    if (e.target.value === "-1") {
      setSelectMenu(false);
      setAddPageId(-1);
    } else {
      setAddPageId(0);
      setSelectMenu(true);
    }
  };

  const handlePageIdChange = (e) => {
    setAddPageId(e.target.value);
   //console.log(e.target.value);
  };

  const openMenu = (ind) => {
    if (openS === ind) setOpenS(null);
    else setOpenS(ind);
   //console.log(ind);
  };

  const renderListItem = (item, index) => (
    <ListItem
      button
      style={{ backgroundColor: "#9B9B9B" }}
      key={item.name + item.id}
      onClick={(e) => openMenu(index)}
      onDoubleClick={() => handleDoubleClick(item.id, item.name)}
    >
      <ListItemText primary={item.name} />
      <CloseIcon onClick={(e) => handleLDelete(item.id, e)} />
    </ListItem>
  );

  const renderSubListItem = (subitem, index, subindex) => (
    <ListItem
      button
      key={subitem.name + subitem.id}
      onClick={() => {
        const updatedLinks = [...links];
        updatedLinks.forEach((l) => {
          return l.submenu.forEach((l1) => (l1.active = false));
        });
        if (updatedLinks[index].submenu[subindex] !== undefined) {
          updatedLinks[index].submenu[subindex].active = true;
        }
        setLinks(updatedLinks);
        onLinkClick(subitem.id);
      }}
      onDoubleClick={() => handleDoubleClick(subitem.id, subitem.name)}
    >
      <ListItemText primary={subitem.name} />
      <CloseIcon onClick={(e) => handleDelete(subitem.id, e)} />
    </ListItem>
  );
  const renderPopListItem = (subitem, index) => (
    <ListItem
      button
      key={subitem.name + subitem.id}
      onClick={() => {
        const updatedLinks = [...popuplinks];
        updatedLinks.forEach((l) => (l.active = false));
        updatedLinks[index].active = true;
        setpopupLinks(updatedLinks);
        onLinkClick(subitem.id);
      }}
      onDoubleClick={() => handleDoubleClick(subitem.id, subitem.name)}
    >
      <ListItemText primary={subitem.name} />
      <CloseIcon onClick={(e) => handlepopDelete(subitem.id, e)} />
    </ListItem>
  );
  const renderDrawerListItem = (subitem, index) => (
    <ListItem
      button
      key={subitem.name + subitem.id}
      onClick={() => {
        const updatedLinks = [...drawerlinks];
        updatedLinks.forEach((l) => (l.active = false));
        updatedLinks[index].active = true;
        setdrawerLinks(updatedLinks);
        onLinkClick(subitem.id);
      }}
      onDoubleClick={() => handleDoubleClick(subitem.id, subitem.name)}
    >
      <ListItemText primary={subitem.name} />
      <CloseIcon onClick={(e) => handledrawDelete(subitem.id, e)} />
    </ListItem>
  );
  const renderComponentListItem = (subitem, index) => (
    <ListItem
      button
      key={subitem.name + subitem.id}
      onClick={() => {
        const updatedLinks = [...componentlinks];
        updatedLinks.forEach((l) => (l.active = false));
        updatedLinks[index].active = true;
        setComponentLinks(updatedLinks);
        onLinkClick(subitem.id);
      }}
      onDoubleClick={() => handleDoubleClick(subitem.id, subitem.name)}
    >
      <ListItemText primary={subitem.name} />
      <CloseIcon onClick={(e) => handleComponentDelete(subitem.id, e)} />
    </ListItem>
  );

  return (
    <div>
      <Dialog maxWidth="lg" open={openPopup} onClose={hanbleClosePopup}>
        <DialogTitle>Add pages</DialogTitle>
        <DialogContent>
          <FormControl component="fieldset">
            <FormLabel component="legend">Page Type</FormLabel>
            <RadioGroup
              defaultValue={-1}
              onChange={addPageChange}
              aria-label="page type"
              name="addPage"
            >
              <FormControlLabel
                value={-1}
                control={<Radio />}
                label="Main Page"
              />
              <FormControlLabel
                value={-2}
                control={<Radio />}
                label="Sub Menu Page"
              />
            </RadioGroup>
          </FormControl>
          {selectMenu && (
            <FormControl fullWidth>
              <Select
                value={addPageId}
                onChange={(e) => {
                  setAddPageId(e.target.value);
                }}
                defaultValue={addPageId}
              >
                {links.map((link, index) => (
                  <MenuItem key={link.name + link.id} value={link.id}>
                    {link.name}
                  </MenuItem>
                ))}
              </Select>
            </FormControl>
          )}
        </DialogContent>
        <DialogActions>
          <Button onClick={hanbleClosePopup}>Close</Button>
          <Button onClick={saveLink}>Add</Button>
        </DialogActions>
      </Dialog>
      <IconButton
        edge="start"
        color="inherit"
        aria-label="menu"
        onClick={toggleDrawer(true)}
      >
        <MenuIcon />
      </IconButton>
      <SwipeableDrawer
        anchor="right"
        open={state}
        onClose={toggleDrawer(false)}
        onOpen={toggleDrawer(true)}
      >
        <Accordion
          expanded={openAccordion === 0}
          onChange={() => {
            if (openAccordion === 0) setOpenAccordion(null);
            else setOpenAccordion(0);
          }}
        >
          <AccordionSummary expandIcon={<ExpandMoreIcon />}>
            <Typography variant="h6" gutterBottom>
              Add Links
            </Typography>
          </AccordionSummary>
          <AccordionDetails>
            {/* Dev1 For all threee form field liek this */}
            <Grid
              container
              spacing={2}
              alignItems="center"
              sx={{ justifyContent: "space-between" }}
            >
              <Grid item xs={12} sm={10}>
                {/* Adjust the grid size as needed */}
                <TextField
                  id="pageName"
                  value={addName}
                  placeholder="Enter Page Name"
                  onChange={handleAddonChange}
                  fullWidth
                />
              </Grid>
              <Grid item xs={12} sm={2}>
                {/* Adjust the grid size as needed */}
                <Button variant="contained" size="medium" onClick={saveLink1}>
                  Add Page
                </Button>
              </Grid>
            </Grid>
          </AccordionDetails>
          <AccordionDetails>
            <Typography variant="h6">List of Pages</Typography>
          </AccordionDetails>
          <Divider />
          <AccordionDetails>
            {}
            {}
            <TreeView
              defaultCollapseIcon={<ExpandMoreIcon />}
              defaultExpandIcon={<ChevronRightIcon />}
              sx={{ width: "100%", maxWidth: "100%", maxHeight: "65vh" }} // Updated styling
            >
              {links.map((link, index) => (
                <TreeItem
                  key={link.id}
                  nodeId={link.id.toString()}
                  label={
                    editing === link.id ? (
                      <Grid
                        container
                        spacing={1}
                        alignItems="center"
                        justifyContent="space-between"
                        sx={{ my: 2 }}
                      >
                        {/* Edit mode */}
                        <Grid item xs={9} md={10}>
                          <Input
                            id="pageName"
                            placeholder={link.name}
                            onChange={handleonChange}
                            defaultValue={pageName}
                            fullWidth
                          />
                        </Grid>
                        <Grid item xs={3} md={2}>
                          <Button
                            variant="text"
                            size="medium"
                            onClick={handleonClick}
                          >
                            Change
                          </Button>
                        </Grid>
                      </Grid>
                    ) : (
                      <Grid container alignItems="center">
                        {/* Display mode */}
                        <Grid item xs={9} md={10}>
                          {/* Add a click handler to stop event propagation */}
                          <Button
                            fullWidth
                            onClick={(e) => {
                              e.stopPropagation(); // Stop event propagation
                              openMenu(index);
                            }}
                            onDoubleClick={() =>
                              handleDoubleClick(link.id, link.name)
                            }
                            sx={{ textAlign: "start", color: "inherit" }}
                          >
                            {link.name}
                          </Button>
                        </Grid>
                        <Grid item xs={3} md={2}>
                          {!editing && (
                            <IconButton
                              onClick={(e) => handleDelete(link.id, e)}
                              sx={{ float: "right" }}
                            >
                              <CloseIcon />
                            </IconButton>
                          )}
                        </Grid>
                      </Grid>
                    )
                  }
                  endIcon={
                    <IconButton onClick={() => openMenu(index)}>
                      <ChevronRightIcon />
                    </IconButton>
                  }
                >
                  {/* Submenu items */}
                  {link.submenu !== undefined && index === openS && (
                    <>
                      {link.submenu.map((sublink, subindex) => (
                        <TreeItem
                          key={sublink.id}
                          nodeId={sublink.id.toString()}
                          label={
                            editing === sublink.id ? (
                              <Grid container spacing={1} alignItems="center">
                                {/* Edit mode for submenu */}
                                <Grid item xs={9} md={10}>
                                  {" "}
                                  {/* Decreased width for responsiveness */}
                                  <Input
                                    id="pageName"
                                    placeholder={sublink.name}
                                    onChange={handleonChange}
                                    defaultValue={pageName}
                                    fullWidth
                                  />
                                </Grid>
                                <Grid item xs={3} md={2}>
                                  {" "}
                                  {/* Decreased width for responsiveness */}
                                  <Button
                                    variant="text"
                                    size="medium"
                                    onClick={(e) =>
                                      handleonSubClick(e, link.id)
                                    }
                                  >
                                    Change
                                  </Button>
                                </Grid>
                              </Grid>
                            ) : (
                              <Grid container alignItems="center">
                                {/* Display mode for submenu */}
                                <Grid item xs={9} md={10}>
                                  {" "}
                                  {/* Decreased width for responsiveness */}
                                  <Typography
                                    onClick={() => {
                                      const updatedLinks = [...links];
                                      updatedLinks.forEach((l) => {
                                        return l.submenu.forEach(
                                          (l1) => (l1.active = false)
                                        );
                                      });
                                      if (
                                        updatedLinks[index].submenu[
                                          subindex
                                        ] !== undefined
                                      ) {
                                        updatedLinks[index].submenu[
                                          subindex
                                        ].active = true;
                                      }
                                      // You may want to update the state with the updatedLinks
                                      // setLinks(updatedLinks);
                                      onLinkClick(sublink.id);
                                    }}
                                    onDoubleClick={() =>
                                      handleDoubleClick(
                                        sublink.id,
                                        sublink.name
                                      )
                                    }
                                  >
                                    {sublink.name}
                                  </Typography>
                                </Grid>
                                <Grid item xs={3} md={2}>
                                  {" "}
                                  {/* Decreased width for responsiveness */}
                                  {!editing && (
                                    <IconButton
                                      onClick={(e) =>
                                        handleDelete(sublink.id, e)
                                      }
                                      sx={{ float: "right" }} // Move the close button to the right
                                    >
                                      <CloseIcon />
                                    </IconButton>
                                  )}
                                </Grid>
                              </Grid>
                            )
                          }
                          endIcon={
                            <IconButton size="small">
                              <CircleIcon style={{ fontSize: 10 }} />
                            </IconButton>
                          }
                        />
                      ))}
                    </>
                  )}
                </TreeItem>
              ))}
            </TreeView>
          </AccordionDetails>
        </Accordion>
        <Accordion
          expanded={openAccordion === 1}
          onChange={() => {
            if (openAccordion === 1) setOpenAccordion(null);
            else setOpenAccordion(1);
          }}
        >
          <AccordionSummary expandIcon={<ExpandMoreIcon />}>
            <Typography variant="h6" gutterBottom>
              Add popup navigation pages
            </Typography>
          </AccordionSummary>
          <AccordionDetails>
            <Grid
              container
              spacing={2}
              alignItems="center"
              sx={{ justifyContent: "space-between" }}
            >
              <Grid item xs={12} md={10}>
                <TextField
                  id="pageName"
                  value={addName}
                  placeholder="Enter Page Name"
                  onChange={handleAddonChange}
                  fullWidth
                />
              </Grid>
              <Grid item xs={12} md={2}>
                <Button
                  variant="contained"
                  size="medium"
                  onClick={savepopupLink}
                >
                  Add Page
                </Button>
              </Grid>
            </Grid>
          </AccordionDetails>
          <AccordionDetails>
            <Typography variant="h6">List of Pages</Typography>
          </AccordionDetails>
          <Divider />
          <AccordionDetails>
            {}
            <TreeView sx={{ width: "60vw", maxHeight: "65vh" }}>
              {popuplinks.map((sublink, subindex) => (
                <TreeItem
                  key={sublink.id}
                  nodeId={sublink.id.toString()}
                  label={
                    editing === sublink.id ? (
                      <Grid
                        container
                        spacing={2}
                        alignItems="center"
                        sx={{ justifyContent: "space-between", my: 2 }}
                      >
                        <Grid item xs={10} md={10}>
                          <Input
                            id="pageName"
                            placeholder={sublink.name}
                            onChange={handleonChange}
                            defaultValue={pageName}
                            fullWidth
                          />
                        </Grid>
                        <Grid item xs={2} md={2}>
                          <Button
                            variant="text"
                            size="medium"
                            onClick={handleonpopupClick}
                          >
                            Change
                          </Button>
                        </Grid>
                      </Grid>
                    ) : (
                      <Grid container alignItems="center">
                        <Grid item xs={10} md={10}>
                          <Button
                            fullWidth
                            onClick={() => {
                              const updatedLinks = [...popuplinks];
                              updatedLinks.forEach((l) => (l.active = false));
                              updatedLinks[subindex].active = true;
                              setpopupLinks(updatedLinks);
                              onLinkClick(sublink.id);
                            }}
                            onDoubleClick={() =>
                              handleDoubleClick(sublink.id, sublink.name)
                            }
                            sx={{ textAlign: "start", color: "inherit" }}
                          >
                            {sublink.name}
                          </Button>
                        </Grid>
                        <Grid item xs={2} md={2}>
                          <IconButton
                            sx={{ float: "right" }}
                            onClick={(e) => handleLDelete(sublink.id, e)}
                          >
                            <CloseIcon />
                          </IconButton>
                        </Grid>
                      </Grid>
                    )
                  }
                  endIcon={
                    <IconButton size="small">
                      <CircleIcon style={{ fontSize: 10 }} />
                    </IconButton>
                  }
                />
              ))}
            </TreeView>
          </AccordionDetails>
        </Accordion>
        <Accordion
          expanded={openAccordion === 2}
          onChange={() => {
            if (openAccordion === 2) setOpenAccordion(null);
            else setOpenAccordion(2);
          }}
        >
          <AccordionSummary expandIcon={<ExpandMoreIcon />}>
            <Typography variant="h6" gutterBottom>
              Add sidebar drawer pages
            </Typography>
          </AccordionSummary>
          <AccordionDetails>
            <Grid
              container
              spacing={2}
              alignItems="center"
              sx={{ justifyContent: "space-between" }}
            >
              <Grid item xs={12} md={10}>
                <TextField
                  id="pageName"
                  value={addName}
                  placeholder="Enter Page Name"
                  onChange={handleAddonChange}
                  fullWidth
                />
              </Grid>
              <Grid item xs={12} md={2}>
                <Button
                  variant="contained"
                  size="medium"
                  onClick={savedrawerLink}
                >
                  Add Page
                </Button>
              </Grid>
            </Grid>
          </AccordionDetails>
          <AccordionDetails>
            <Typography variant="h6">List of Pages</Typography>
          </AccordionDetails>
          <Divider />
          <AccordionDetails>
            {}
            <TreeView sx={{ width: "60vw", maxHeight: "65vh" }}>
              {drawerlinks.map((sublink, subindex) => (
                <TreeItem
                  key={sublink.id}
                  nodeId={sublink.id.toString()}
                  label={
                    editing === sublink.id ? (
                      <Grid
                        container
                        spacing={2}
                        alignItems="center"
                        sx={{ justifyContent: "space-between", my: 2 }}
                      >
                        <Grid item xs={10} md={10}>
                          <Input
                            id="pageName"
                            placeholder={sublink.name}
                            onChange={handleonChange}
                            defaultValue={pageName}
                            fullWidth
                          />
                        </Grid>
                        <Grid item xs={2} md={2}>
                          <Button
                            variant="text"
                            size="medium"
                            onClick={handleondrawerClick}
                          >
                            Change
                          </Button>
                        </Grid>
                      </Grid>
                    ) : (
                      <Grid container alignItems="center">
                        <Grid item xs={10} md={10}>
                          <Button
                            fullWidth
                            onClick={() => {
                              const updatedLinks = [...drawerlinks];
                              updatedLinks.forEach((l) => (l.active = false));
                              updatedLinks[subindex].active = true;
                              setdrawerLinks(updatedLinks);
                              onLinkClick(sublink.id);
                            }}
                            onDoubleClick={() =>
                              handleDoubleClick(sublink.id, sublink.name)
                            }
                            sx={{ textAlign: "start", color: "inherit" }}
                          >
                            {sublink.name}
                          </Button>
                        </Grid>
                        <Grid item xs={2} md={2}>
                          <IconButton
                            sx={{ float: "right" }}
                            onClick={(e) => handledrawDelete(sublink.id, e)}
                          >
                            <CloseIcon />
                          </IconButton>
                        </Grid>
                      </Grid>
                    )
                  }
                  endIcon={
                    <IconButton size="small">
                      <CircleIcon style={{ fontSize: 10 }} />
                    </IconButton>
                  }
                />
              ))}
            </TreeView>
          </AccordionDetails>
        </Accordion>
        <Accordion
          expanded={openAccordion === 3}
          onChange={() => {
            if (openAccordion === 3) setOpenAccordion(null);
            else setOpenAccordion(3);
          }}
        >
          <AccordionSummary expandIcon={<ExpandMoreIcon />}>
            <Typography variant="h6" gutterBottom>
              Add sidebar component pages
            </Typography>
          </AccordionSummary>
          <AccordionDetails>
            <Grid
              container
              spacing={2}
              alignItems="center"
              sx={{ justifyContent: "space-between" }}
            >
              <Grid item xs={12} md={10}>
                <TextField
                  id="pageName"
                  value={addName}
                  placeholder="Enter Page Name"
                  onChange={handleAddonChange}
                  fullWidth
                />
              </Grid>
              <Grid item xs={12} md={2}>
                <Button
                  variant="contained"
                  size="medium"
                  onClick={saveComponentLink}
                >
                  Add Page
                </Button>
              </Grid>
            </Grid>
          </AccordionDetails>
          <AccordionDetails>
            <Typography variant="h6">List of Pages</Typography>
          </AccordionDetails>
          <Divider />
          <AccordionDetails>
            {}
            <TreeView sx={{ width: "60vw", maxHeight: "65vh" }}>
              {componentlinks.map((sublink, subindex) => (
                <TreeItem
                  key={sublink.id}
                  nodeId={sublink.id.toString()}
                  label={
                    editing === sublink.id ? (
                      <Grid
                        container
                        spacing={2}
                        alignItems="center"
                        sx={{ justifyContent: "space-between", my: 2 }}
                      >
                        <Grid item xs={10} md={10}>
                          <Input
                            id="pageName"
                            placeholder={sublink.name}
                            onChange={handleonChange}
                            defaultValue={pageName}
                            fullWidth
                          />
                        </Grid>
                        <Grid item xs={2} md={2}>
                          <Button
                            variant="text"
                            size="medium"
                            onClick={handleonComponentClick}
                          >
                            Change
                          </Button>
                        </Grid>
                      </Grid>
                    ) : (
                      <Grid container alignItems="center">
                        <Grid item xs={10} md={10}>
                          <Button
                            fullWidth
                            onClick={() => {
                              const updatedLinks = [...componentlinks];
                              updatedLinks.forEach((l) => (l.active = false));
                              updatedLinks[subindex].active = true;
                              setComponentLinks(updatedLinks);
                              onLinkClick(sublink.id);
                            }}
                            onDoubleClick={() =>
                              handleDoubleClick(sublink.id, sublink.name)
                            }
                            sx={{ textAlign: "start", color: "inherit" }}
                          >
                            {sublink.name}
                          </Button>
                        </Grid>
                        <Grid item xs={2} md={2}>
                          <IconButton
                            sx={{ float: "right" }}
                            onClick={(e) =>
                              handleComponentDelete(sublink.id, e)
                            }
                          >
                            <CloseIcon />
                          </IconButton>
                        </Grid>
                      </Grid>
                    )
                  }
                  endIcon={
                    <IconButton size="small">
                      <CircleIcon style={{ fontSize: 10 }} />
                    </IconButton>
                  }
                />
              ))}
            </TreeView>
          </AccordionDetails>
        </Accordion>
        <Box sx={{ position: "fixed", bottom: 10, right: 10 }}>
          <Tooltip title="global configuration section" placement="left">
            <IconButton>
              <Help />
            </IconButton>
          </Tooltip>
          <Button variant="contained" onClick={handleconfigClick}>
            Configuration
          </Button>
        </Box>
        {open && (
          <Popup
            nconfig={config}
            componentType="GlobalConfig"
            componentId="globalConfig"
            handleConfigurationChange={configChange}
            handleClose={() => {
              setOpen(!open);
              setClicked(false);
            }}
          />
        )}
      </SwipeableDrawer>
    </div>
  );
};

export default Sidebar;
