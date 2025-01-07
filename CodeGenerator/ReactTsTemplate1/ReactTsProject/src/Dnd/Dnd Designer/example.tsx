import React, { useState, useCallback, useEffect, useRef } from "react";
import DropZone from "./Components/DropZone";
import TrashDropZone from "./Components/TrashDropZone";
import SideBarItem from "./Components/SideBarItem";
import Row from "./Components/Row";
import {
    handleMoveWithinParent,
    handleMoveToDifferentParent,
    handleMoveSidebarComponentIntoParent,
    handleRemoveItemFromLayout,
} from "./Utility/helper";
import "./Static/style.css";
import ApiIcon from "@mui/icons-material/Api";
import { Definition } from "./Static/definition";
import { Search as SearchIcon } from "components/icons/search";
import {
    Button,
    InputAdornment,
    TextField,
} from "@mui/material";
import {
    getOneDnd_ui_versions,
    addDnd_ui_versions,
} from "../../services/dnd_ui_versionsService";

import {
    SIDEBAR_ITEMS,
    SIDEBAR_ITEM,
    COMPONENT,
    COLUMN,
    functionTOmap,
} from "./Utility/constants";
import shortid from "shortid";
import JSZip from "jszip";
import Sidebar from "./Components/DND_Sidebar_Pages/SideBar";
import { useNavigate } from "react-router";
import { updateDnd_ui_versions } from "../../services/dnd_ui_versionsService";
import { ImportGlobalConfigDialog } from "./importGlobalConfig";

interface containerProps {
    id: number;
}

const Container = ({ id, project_id }) => {
    const userString = localStorage.getItem("userInfo");
    const user = JSON.parse(userString);
    const [configurations, setConfigurations] = useState({});
    const [activeLink, setActiveLink] = useState(null);
    const [sidbarLinks, setSidebarLinks] = useState([
        { id: 0, name: "Main", active: true, submenu: [] },
    ]);
    const [popupLinks, setPopupLinks] = useState([
        { id: "p0", name: "Main", active: false },
    ]);
    const [drawerLinks, setDrawerLinks] = useState([
        { id: "s0", name: "Main", active: false },
    ]);
    const [componentLinks, setComponentLinks] = useState([
        { id: "c0", name: "Main", active: false },
    ]);
    let navigate = useNavigate();
    const [searchText, setSearchText] = useState("");

    function handleSearchInputChange(e) {
        setSearchText(e.target.value);
    }
    const [openGlobalConfig, setOpenGlobalConfig] = useState(false);
    const handleOpenGlobalConfig = () => {
        setOpenGlobalConfig(true);
    };

    const handleCloseGlobalConfig = (): void => {
        //setProceed(true);

        setOpenGlobalConfig(false);
    };
    async function getLayout() {
       //console.log("Get Layout is Clicked.... ", id);
        // we are using id which is passed from index.tsx as a prop
        const response1 = await getOneDnd_ui_versions(2);
       //console.log(response1);
        const response = await getOneDnd_ui_versions(id);
        if (response && response.data.document !== null) {
           //console.log("Fetched Successfully");
            const dnd_builder_data = response["data"]["document"];
            const dnd_builder_layout = JSON.parse(dnd_builder_data["layout"]);
            let dnd_builder_layoutList = dnd_builder_layout["layoutList"];
            let dnd_builder_pages = JSON.parse(dnd_builder_data["ui_pages"]);
           //console.log(dnd_builder_layout);
            let dnd_builder_components = JSON.parse(dnd_builder_data["components"]);
            ////console.log("layoutList : ", dnd_builder_layoutList, "pages : ", dnd_builder_pages);
            let formatted_components = {};
            ////console.log("Raw Components Data : ", dnd_builder_components)
            Object.keys(dnd_builder_components).forEach((key) => {
                const component_id = dnd_builder_components[key]["id"];
                // let temp_component = { id: dnd_builder_components[key]['id'], type: SIDEBAR_ITEM, component: { type: dnd_builder_components[key]['type'], content: dnd_builder_components[key]['content'], icon: ApiIcon } };
                // let temp_component = { id: dnd_builder_components[key]['id'], type: dnd_builder_components[key]['type'], content: mapNametoComponent[dnd_builder_components[key]['content']], icon: ApiIcon }
                let temp_config = {};
                temp_config[component_id] = dnd_builder_components[key]["config"];
                // dnd_builder_components[key]['config'] ? temp_config = dnd_builder_components[key]['config'] : temp_config = {};
                setConfigurations((prevState) => ({ ...prevState, ...temp_config }));
                let temp_component = {
                    id: dnd_builder_components[key]["id"],
                    type: dnd_builder_components[key]["type"],
                    content: functionTOmap(
                        dnd_builder_components[key]["content"],
                        temp_config
                    ),
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
            setActiveLink(null);
            setLayoutList(dnd_builder_layoutList);
            setSidebarLinks(dnd_builder_pages["sidbarLinks"]);
            setPopupLinks(dnd_builder_pages["popupLinks"]);
            setDrawerLinks(dnd_builder_pages["drawerLinks"]);
            setComponentLinks(dnd_builder_pages["componentLinks"]);
           //console.log(dnd_builder_pages);
            ////console.log("errorneous layout ", dnd_builder_layoutList[0]);
            // (dnd_builder_layoutList[0] === undefined || dnd_builder_layoutList[0] === null) ? setLayout([]) : setLayout(dnd_builder_layoutList[0]);
            setComponents(formatted_components);
            setLayout([]);

           
        } else {
           //console.log("Some error occured While Fetching!");
        }

       
    }
    const previousInputValue = useRef(true);
    //Dev2
    useEffect(() => {
        if (id !== undefined && previousInputValue.current) {
            getLayout();
        }
        previousInputValue.current = false;
    }, [getLayout, id, previousInputValue]);
   
    function handleLinkClick(id) {
       //console.log(layoutList);
        setLayoutList((prevState) => ({ ...prevState, [activeLink]: layout }));
        setActiveLink(id);
        
        setLayout(layoutList[id]);
        
    }
    function handleTabLinkClick(id, condition?) {
        return (
            <div className="dnd page">
                {layoutList[id] &&
                    layoutList[id].map((row, index) => {
                        const currentPath = `${index}`;
                        return (
                            <React.Fragment key={row.id}>
                                <DropZone
                                    data={{
                                        path: currentPath,
                                        childrenCount: layout.length,
                                    }}
                                    onDrop={handleDrop}
                                    path={currentPath}
                                    className=""
                                    isLast
                                />
                                {renderRow(row, currentPath)}
                            </React.Fragment>
                        );
                    })}
            </div>
        );
    }
    function handleButtonLinkClick(id, condition?) {
       
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
    function handleSidebarLinkAdd(sidebarItems) {
        setSidebarLinks(sidebarItems);
        for (let obj of sidebarItems) {
            for (let obj1 of obj.submenu) {
                if (obj1.id in layoutList)
                   console.log("id is already in layoutList.");
                else {
                    setLayoutList((prevState) => ({
                        ...prevState,
                        [obj1.id]: [],
                    }));
                }
            }
        }
       //console.log(layoutList);
       //console.log("total sidebar Items in Example :- ", sidebarItems);
    }
    function handleDrawerLinkAdd(sidebarItems) {
        setDrawerLinks(sidebarItems);
        for (let obj of sidebarItems) {
            if (obj.id in layoutList)
                console.log("id is already in layoutList.");
            else {
                setLayoutList((prevState) => ({
                    ...prevState,
                    [obj.id]: [],
                }));
            }
        }
       //console.log("total sidebar Items in Example :- ", sidebarItems);
    }
    function handleComponentLinkAdd(sidebarItems) {
        setComponentLinks(sidebarItems);
        for (let obj of sidebarItems) {
            if (obj.id in layoutList)
                console.log("id is already in layoutList.");
            else {
                setLayoutList((prevState) => ({
                    ...prevState,
                    [obj.id]: [],
                }));
            }
        }
       //console.log("total sidebar Items in Example :- ", sidebarItems);
    }
    function handlePopupLinkAdd(sidebarItems) {
        setPopupLinks(sidebarItems);
        for (let obj of sidebarItems) {
            if (obj.id in layoutList)
                console.log("id is already in layoutList.");
            else {
                setLayoutList((prevState) => ({
                    ...prevState,
                    [obj.id]: [],
                }));
            }
        }
       //console.log("total sidebar Items in Example :- ", sidebarItems);
    }

    function handleSidebarLinkDelete(id) {
        setSidebarLinks((pv) => {
            return pv.map((tv) => {
                const newTv = { ...tv }; // Create a shallow copy of the tv object.
                newTv.submenu = tv.submenu.filter((pt) => pt.id !== id);
                return newTv;
            });
        });
    }
    function handleSidebarLabelDelete(id) {
        setSidebarLinks((pv) => {
            return pv.filter((te) => te.id !== id);
        });
    }
    function handlepopupDelete(id) {
       //console.log(id);
        setPopupLinks((pv) => {
            return pv.filter((te) => te.id !== id);
        });
    }
    function handledrawerDelete(id) {
        setDrawerLinks((pv) => {
            return pv.filter((te) => te.id !== id);
        });
    }
    function handleComponentDelete(id) {
        setComponentLinks((pv) => {
            return pv.filter((te) => te.id !== id);
        });
    }
    // Define a callback function to handle the configuration data
    const handleConfigurationChange = (configuration) => {
        setConfigurations(configuration);
       //console.log("Configuratins from Main : ", configurations);
    };

    var initialLayout, initialComponents;
    if (id === null || id === undefined) {
        // For Empty Canvas
        // initialLayout = initialData.layout;
        // initialComponents = initialData.components;

        // for Predefined Canvas
        initialLayout = Definition.layout;
        initialComponents = Definition.components;
    } else {
        initialLayout = Definition.layout;
        initialComponents = Definition.components;
    }
    const [layout, setLayout] = useState(initialLayout);
    const [components, setComponents] = useState(initialComponents);

    useEffect(() => {
        setLayoutList((prevState) => ({ ...prevState, [activeLink]: layout }));
       //console.log("Layout Has been changed from useEffect.");
    }, [layout]);

   

    const handleDropToTrashBin = useCallback(
        (dropZone, item) => {
            const splitItemPath = item.path.split("-");
            setLayout(handleRemoveItemFromLayout(layout, splitItemPath));
        },
        [layout]
    );

    const handleDrop = useCallback(
        (dropZone, item) => {
            ////console.log('dropZone', dropZone)
            ////console.log('item', item)

            const splitDropZonePath = dropZone.path.split("-");
            const pathToDropZone = splitDropZonePath.slice(0, -1).join("-");

            const newItem = { id: item.id, type: item.type, children: item.children };
            if (item.type === COLUMN) {
                newItem.children = item.children;
            }

            // sidebar into
            if (item.type === SIDEBAR_ITEM) {
                // 1. Move sidebar item into page
                const newComponent = {
                    id: shortid.generate(),
                    ...item.component,
                };
                const newItem = {
                    id: newComponent.id,
                    type: COMPONENT,
                };
                setComponents({
                    ...components,
                    [newComponent.id]: newComponent,
                });
                setLayout(
                    handleMoveSidebarComponentIntoParent(
                        layout,
                        splitDropZonePath,
                        newItem
                    )
                );
                return;
            }

            // move down here since sidebar items dont have path
            const splitItemPath = item.path.split("-");
            const pathToItem = splitItemPath.slice(0, -1).join("-");

            // 2. Pure move (no create)
            if (splitItemPath.length === splitDropZonePath.length) {
                // 2.a. move within parent
                if (pathToItem === pathToDropZone) {
                    setLayout(
                        handleMoveWithinParent(layout, splitDropZonePath, splitItemPath)
                    );
                    return;
                }

                // 2.b. OR move different parent
                // TODO FIX columns. item includes children
                setLayout(
                    handleMoveToDifferentParent(
                        layout,
                        splitDropZonePath,
                        splitItemPath,
                        newItem
                    )
                );
                return;
            }

            // 3. Move + Create
            setLayout(
                handleMoveToDifferentParent(
                    layout,
                    splitDropZonePath,
                    splitItemPath,
                    newItem
                )
            );
        },
        [layout, components]
    );

    const renderRow = (row, currentPath) => {
        return (
            <Row
                key={row.id}
                data={row}
                openLink={handleButtonLinkClick}
                openTabLink={handleTabLinkClick}
                LayoutList={{ sidbarLinks, popupLinks, drawerLinks, componentLinks }}
                handleDrop={handleDrop}
                components={components}
                path={currentPath}
                configurations={configurations}
                handleConfigurationChange={handleConfigurationChange}
            />
        );
    };
    //Dev1
    async function exportGlobalConfig() {
        let component_details = [];
        Object.keys(components).forEach((key) => {
            let temp_obj = {};
            temp_obj["id"] = components[key]["id"];
            temp_obj["type"] = components[key]["type"];
            components[key]["icon_name"] != undefined
                ? (temp_obj["icon"] = components[key]["icon_name"])
                : (temp_obj["content"] = components[key]["content"]);
            components[key]["component_name"] != undefined
                ? (temp_obj["content"] = components[key]["component_name"])
                : (temp_obj["content"] = components[key]["content"]);
            if (configurations[components[key]["id"]] !== undefined)
                temp_obj["config"] = configurations[components[key]["id"]];
            component_details.push(temp_obj);
        });
        component_details.push({
            id: "globalConfig",
            config: configurations["globalConfig"],
        });

        const layoutObj = { layoutList: layoutList };
       //console.log("layoutlist in update: ", layoutList);
        // let date = new Date();
        // let currentDate = `${date.getFullYear()}-${(date.getMonth() + 1).toString().padStart(2, "0")}-${date.getDate().toString().padStart(2, "0")}T${date.getHours().toString().padStart(2, "0")}:${date.getMinutes().toString().padStart(2, "0")}:${date.getSeconds().toString().padStart(2, "0")}.000Z`;
        // const dnd_builder_obj = { "layout": JSON.stringify(layoutList, undefined, 4), "pages": JSON.stringify(sidbarLinks, undefined, 4), "components": JSON.stringify(component_details, undefined, 4), "page_type": "demo_page", "createdBy": "Jeelesh", "modifiedBy": "Jeelesh", "isActive": 1, "createdAt": "2012-07-26T01:25:58", "modifiedAt": "2012-07-26T01:25:58" };
        const layoutFile = JSON.stringify(layoutObj, undefined, 4);
        const componentsFile = JSON.stringify(component_details, undefined, 4);
        const Ui_pagesFile = JSON.stringify(
            { sidbarLinks, popupLinks, drawerLinks, componentLinks },
            undefined,
            4
        );

        // Create a new zip file
        const zip = new JSZip();
        zip.file("layoutFile.json", layoutFile);
        zip.file("componentsFile.json", componentsFile);
        zip.file("Ui_pagesFile.json", Ui_pagesFile);

        // Generate a Blob from the zip content
        zip
            .generateAsync({ type: "blob" })
            .then((blob) => {
                // Create a download link for the zip file
                const url = URL.createObjectURL(blob);
                const a = document.createElement("a");
                a.href = url;
                a.download = "ConfigurationFiles.zip";

                // Trigger the download
                a.click();

                // Clean up
                URL.revokeObjectURL(url);
            })
            .catch((error) => {
                console.error("Error generating zip file:", error);
            });
    }
    async function saveLayout() {
        let component_details = [];
        Object.keys(components).forEach((key) => {
            let temp_obj = {};
            temp_obj["id"] = components[key]["id"];
            temp_obj["type"] = components[key]["type"];
            components[key]["icon_name"] != undefined
                ? (temp_obj["icon"] = components[key]["icon_name"])
                : (temp_obj["content"] = components[key]["content"]);
            components[key]["component_name"] != undefined
                ? (temp_obj["content"] = components[key]["component_name"])
                : (temp_obj["content"] = components[key]["content"]);
            if (configurations[components[key]["id"]] !== undefined)
                temp_obj["config"] = configurations[components[key]["id"]];
            component_details.push(temp_obj);
        });

        const layoutObj = { layoutList: layoutList };
        // let date = new Date();
        // let currentDate = `${date.getFullYear()}-${(date.getMonth() + 1).toString().padStart(2, "0")}-${date.getDate().toString().padStart(2, "0")}T${date.getHours().toString().padStart(2, "0")}:${date.getMinutes().toString().padStart(2, "0")}:${date.getSeconds().toString().padStart(2, "0")}.000Z`;
        // const dnd_builder_obj = { "layout": JSON.stringify(layoutList, undefined, 4), "pages": JSON.stringify(sidbarLinks, undefined, 4), "components": JSON.stringify(component_details, undefined, 4), "page_type": "demo_page", "createdBy": "Jeelesh", "modifiedBy": "Jeelesh", "isActive": 1, "createdAt": "2012-07-26T01:25:58", "modifiedAt": "2012-07-26T01:25:58" };
        const dnd_builder_obj = {
            layout: JSON.stringify(layoutObj, undefined, 4),
            components: JSON.stringify(component_details, undefined, 4),
            ui_pages: JSON.stringify(
                { sidbarLinks, popupLinks, drawerLinks, componentLinks },
                undefined,
                4
            ),

            dnd_ui_type: "demo_page",
            createdBy: user?.username,
            modifiedBy: user?.username,
            isActive: 1,
            createdAt: "2012-07-26T01:25:58",
            modifiedAt: "2012-07-26T01:25:58",
        };
        const response = await addDnd_ui_versions(dnd_builder_obj);
        if (response) {
           //console.log("Added Successfully : ", response);
        } else {
           //console.log("Some error occured While Adding!");
        }
        navigate(`/dndBuilder/${response["data"]["document"]}`);
      
    }
    const [releaseLoad, setReleaseLoad] = useState(false);
    async function ReleaseUi() {
        setReleaseLoad(true);
        let component_details = [];
        Object.keys(components).forEach((key) => {
            let temp_obj = {};
            temp_obj["id"] = components[key]["id"];
            temp_obj["type"] = components[key]["type"];
            components[key]["icon_name"] != undefined
                ? (temp_obj["icon"] = components[key]["icon_name"])
                : (temp_obj["content"] = components[key]["content"]);
            components[key]["component_name"] != undefined
                ? (temp_obj["content"] = components[key]["component_name"])
                : (temp_obj["content"] = components[key]["content"]);
            if (configurations[components[key]["id"]] !== undefined)
                temp_obj["config"] = configurations[components[key]["id"]];
            component_details.push(temp_obj);
        });
        component_details.push({
            id: "globalConfig",
            config: configurations["globalConfig"],
        });

        const layoutObj = { layoutList: layoutList };
       //console.log("layoutlist in update: ", layoutList);
        // let date = new Date();
        // let currentDate = `${date.getFullYear()}-${(date.getMonth() + 1).toString().padStart(2, "0")}-${date.getDate().toString().padStart(2, "0")}T${date.getHours().toString().padStart(2, "0")}:${date.getMinutes().toString().padStart(2, "0")}:${date.getSeconds().toString().padStart(2, "0")}.000Z`;
        // const dnd_builder_obj = { "layout": JSON.stringify(layoutList, undefined, 4), "pages": JSON.stringify(sidbarLinks, undefined, 4), "components": JSON.stringify(component_details, undefined, 4), "page_type": "demo_page", "createdBy": "Jeelesh", "modifiedBy": "Jeelesh", "isActive": 1, "createdAt": "2012-07-26T01:25:58", "modifiedAt": "2012-07-26T01:25:58" };
        const dnd_builder_obj = {
            layout: JSON.stringify(layoutObj, undefined, 4),
            components: JSON.stringify(component_details, undefined, 4),
            ui_pages: JSON.stringify(
                { sidbarLinks, popupLinks, drawerLinks, componentLinks },
                undefined,
                4
            ),

            dnd_ui_type: "demo_page",
            createdBy: user?.username,
            modifiedBy: user?.username,
            isActive: 1,
            createdAt: "2012-07-26T01:25:58",
            modifiedAt: "2012-07-26T01:25:58",
        };
        const response1 = await getOneDnd_ui_versions(2);
       //console.log(response1);
        if (response1["data"]["document"] !== null) {
            const response = await updateDnd_ui_versions(2, dnd_builder_obj);
            if (response) {
               //console.log("Updated Successfully : ", response);
            } else {
               //console.log("Some error occured While Adding!");
            }
        } else {
            const response = await addDnd_ui_versions(dnd_builder_obj);
            if (response) {
               //console.log("Added Successfully : ", response);
            } else {
               //console.log("Some error occured While Adding!");
            }
        }
        setReleaseLoad(false);
    }
    const [updateLoad, setUpdateLoad] = useState(false);
    async function updateLayout() {
        setUpdateLoad(true);
        let component_details = [];
        Object.keys(components).forEach((key) => {
            let temp_obj = {};
            temp_obj["id"] = components[key]["id"];
            temp_obj["type"] = components[key]["type"];
            components[key]["icon_name"] != undefined
                ? (temp_obj["icon"] = components[key]["icon_name"])
                : (temp_obj["content"] = components[key]["content"]);
            components[key]["component_name"] != undefined
                ? (temp_obj["content"] = components[key]["component_name"])
                : (temp_obj["content"] = components[key]["content"]);
            if (configurations[components[key]["id"]] !== undefined)
                temp_obj["config"] = configurations[components[key]["id"]];
            component_details.push(temp_obj);
        });
        component_details.push({
            id: "globalConfig",
            config: configurations["globalConfig"],
        });

        const layoutObj = { layoutList: layoutList };
       //console.log("layoutlist in update: ", layoutList);
        // let date = new Date();
        // let currentDate = `${date.getFullYear()}-${(date.getMonth() + 1).toString().padStart(2, "0")}-${date.getDate().toString().padStart(2, "0")}T${date.getHours().toString().padStart(2, "0")}:${date.getMinutes().toString().padStart(2, "0")}:${date.getSeconds().toString().padStart(2, "0")}.000Z`;
        // const dnd_builder_obj = { "layout": JSON.stringify(layoutList, undefined, 4), "pages": JSON.stringify(sidbarLinks, undefined, 4), "components": JSON.stringify(component_details, undefined, 4), "page_type": "demo_page", "createdBy": "Jeelesh", "modifiedBy": "Jeelesh", "isActive": 1, "createdAt": "2012-07-26T01:25:58", "modifiedAt": "2012-07-26T01:25:58" };
        const dnd_builder_obj = {
            layout: JSON.stringify(layoutObj, undefined, 4),
            components: JSON.stringify(component_details, undefined, 4),
            ui_pages: JSON.stringify(
                { sidbarLinks, popupLinks, drawerLinks, componentLinks },
                undefined,
                4
            ),

            dnd_ui_type: "demo_page",
            createdBy: user?.username,
            modifiedBy: user?.username,
            isActive: 1,
            createdAt: "2012-07-26T01:25:58",
            modifiedAt: "2012-07-26T01:25:58",
        };
        const response1 = await getOneDnd_ui_versions(id);
       //console.log(response1);
        if (response1["data"]["document"] !== null) {
            const response = await updateDnd_ui_versions(id, dnd_builder_obj);
            if (response) {
               //console.log("Updated Successfully : ", response);
            } else {
               //console.log("Some error occured While Adding!");
            }
        } else {
            const response = await addDnd_ui_versions(dnd_builder_obj);
            if (response) {
               //console.log("Added Successfully : ", response);
            } else {
               //console.log("Some error occured While Adding!");
            }
        }
        setUpdateLoad(false);
    }

    function previewPage() {
        // window.open(`preview?id=${id}`, "_blank", "noreferrer");
        window.open(`preview?id=2`, "_blank", "noreferrer");
    }

    //Dev2
    const [exitLoad, setExitLoad] = useState(false);
    function ExitPage() {
        setExitLoad(true);
        localStorage.clear();
        navigate("/", { replace: true });
        setExitLoad(false);
    }
    const [layoutList, setLayoutList] = useState({
        [activeLink]: layout,
        ["p0"]: [],
        ["s0"]: [],
        ["c0"]: [],
    });

    async function copytoClipboard() {
        setLayoutList((prevState) => ({ ...prevState, [activeLink]: layout }));
        // setLayoutList(prevState => ({ ...prevState, [activeLink]: layout }));
        const content = JSON.stringify(
            { layout: layoutList, pages: sidbarLinks, components: components },
            undefined,
            4
        );
       
        navigator.clipboard.writeText(content);
       //console.log("layoutList from Clipboard : ", layoutList);
    }
   //console.log(layoutList);

    // dont use index for key when mapping over items
    // causes this issue - https://github.com/react-dnd/react-dnd/issues/342
    return (
        <div className="dnd body">
            {/* {console.log("Layout List = ", layoutList, "Pages from Main = ", sidbarLinks, "components from Main = ", components)} */}
            {console.log(
                "Layout : ",
                layout,
                "Components : ",
                components,
                "Configurations : ",
                configurations
            )}
            <div className="dnd sideBar">
                <TextField
                    style={{ width: "200px", height: "45px" }}
                    InputProps={{
                        startAdornment: (
                            <InputAdornment position="start">
                                <SearchIcon fontSize="small" />
                            </InputAdornment>
                        ),
                        style: { color: "white", height: "100%" },
                    }}
                    InputLabelProps={{
                        style: { color: "#93FAEB", borderColor: "#93FAEB" },
                    }}
                    label="Search"
                    onChange={handleSearchInputChange}
                    placeholder="Search..."
                    sx={{ mt: 1 }}
                    value={searchText}
                />

                {Object.values(SIDEBAR_ITEMS)
                    .filter((sideBarItem) =>
                        sideBarItem.component.type
                            .toLowerCase()
                            .includes(searchText.toLowerCase())
                    )
                    .map((sideBarItem, index) => (
                        <SideBarItem key={sideBarItem.id} data={sideBarItem} />
                    ))}
                <hr />
             
                <Button
                    disabled={updateLoad}
                    style={{
                        //height: "60px",
                        width: "150px",
                        //fontSize: "120%",
                        //color: "white",
                    }}
                    onClick={updateLayout}
                    variant="contained"
                >
                    Update UI
                </Button>
                <hr />
                <Button
                    disabled={releaseLoad}
                    style={{
                        //height: "60px",
                        width: "150px",
                        //fontSize: "120%",
                        //color: "white",
                    }}
                    onClick={ReleaseUi}
                    variant="contained"
                >
                    Release UI 🙂
                </Button>
                <hr />
                <Button
                    style={{
                        //height: "60px",
                        width: "150px",
                        //fontSize: "120%",
                        //color: "white",
                    }}
                    onClick={previewPage}
                    variant="contained"
                >
                    Preview UI 🙂
                </Button>
                <hr />
                <Button
                    disabled={openGlobalConfig}
                    style={{
                        //height: "60px",
                        width: "150px",
                        //fontSize: "120%",
                        //color: "white",
                    }}
                    onClick={handleOpenGlobalConfig}
                    variant="contained"
                >
                    ImportConfig
                </Button>
                <hr />
                <Button
                    style={{
                        //height: "60px",
                        width: "150px",
                        //fontSize: "120%",
                        //color: "white",
                    }}
                    onClick={exportGlobalConfig}
                    variant="contained"
                >
                    ExportConfig
                </Button>
                <hr />
                <Button
                    disabled={exitLoad}
                    style={{
                        //height: "60px",
                        width: "150px",
                        //fontSize: "120%",
                        //color: "white",
                    }}
                    onClick={ExitPage}
                    variant="contained"
                >
                    Exit
                </Button>
                <hr />
                <TrashDropZone
                    data={{
                        layout,
                    }}
                    onDrop={handleDropToTrashBin}
                />
            </div>
            {openGlobalConfig && (
                <ImportGlobalConfigDialog
                    open={openGlobalConfig}
                    onClose={handleCloseGlobalConfig}
                    id={id}
                />
            )}
            {/* <div style={{ display: 'flex', flexDirection: 'row-reverse', marginTop: "100px" }}> */}
            {/* </div> */}
            <div className="dnd pageContainer">
                <div className="dnd page">
                    {layout &&
                        layout.map((row, index) => {
                            const currentPath = `${index}`;
                            return (
                                <React.Fragment key={row.id}>
                                    <DropZone
                                        data={{
                                            path: currentPath,
                                            childrenCount: layout.length,
                                        }}
                                        onDrop={handleDrop}
                                        path={currentPath}
                                        className=""
                                        isLast
                                    />
                                    {renderRow(row, currentPath)}
                                </React.Fragment>
                            );
                        })}
                    {activeLink ? (
                        <DropZone
                            data={{
                                path: `${layout.length}`,
                                childrenCount: layout.length,
                            }}
                            onDrop={handleDrop}
                            isLast
                            className=""
                        />
                    ) : (
                        <div>Welcome to Drag and Drop UI Design</div>
                    )}
                </div>

                <TrashDropZone
                    data={{
                        layout,
                    }}
                    onDrop={handleDropToTrashBin}
                />
            </div>
            <Sidebar
                sidbarLinks={sidbarLinks}
                popupLinks={popupLinks}
                drawerLinks={drawerLinks}
                componentLinks={componentLinks}
                config={configurations}
                configChange={handleConfigurationChange}
                handleLinkDelete={handleSidebarLinkDelete}
                handleLabelDelete={handleSidebarLabelDelete}
                handlepopupDelete={handlepopupDelete}
                handledrawerDelete={handledrawerDelete}
                handleComponentDelete={handleComponentDelete}
                onLinkClick={handleLinkClick}
                handleLinkAdd={handleSidebarLinkAdd}
                handlePopupLinkAdd={handlePopupLinkAdd}
                handleDrawerLinkAdd={handleDrawerLinkAdd}
                handleComponentLinkAdd={handleComponentLinkAdd}
            />
        </div>
    );
};
export default Container;
