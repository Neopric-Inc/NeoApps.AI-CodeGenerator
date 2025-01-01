import React, { useState } from 'react';
import { useDrop, DndProvider } from 'react-dnd';
import { HTML5Backend } from 'react-dnd-html5-backend';
import { COMPONENT, SIDEBAR_ITEM, functionTOmap } from "Dnd/Dnd Designer/Utility/constants";
import "./style.css";

const SideBar = () => {
    const ACCEPTS = [SIDEBAR_ITEM, COMPONENT];
    const [components, setComponents] = useState([]);
    // const ref = useRef(null);

    function onDrop(item, components) {
        const newItem = item;
        (item.component !== undefined && item.component.content !== undefined) ? setComponents([...components, newItem]) : setComponents([...components]);
       //console.log(item, "Item has Been Dropped on NAVABAR.");
       //console.log("Total Components are : ", components);
    }

    const [{ isOver, canDrop }, drop] = useDrop({
        accept: ACCEPTS,
        drop: (item, moniter) => {
            onDrop(item, components);
        },
        canDrop: (item, moniter) => {
            ////console.log(item, " in canDrop.");
            return true;
        },
        collect: (moniter) => ({
            isOver: moniter.isOver(),
            canDrop: moniter.canDrop()
        })
    })


    return (
        <>
            <DndProvider backend={HTML5Backend}>
                <div id="siderComponent" ref={drop}>
                    {components.map((item, index) => (
                        <div key={index} style={{ flex: 1, margin: "auto" }} >
                            {(item.component !== undefined && item.component.content !== undefined) ? functionTOmap(item.component.type, {}) : <p>Can't Render Component</p>}
                            {/* {functionTOmap(item.component.type, {})} */}
                            {/*//console.log("item inside Navbar : ", item) */}
                            {/* item */}
                        </div>
                    ))}
                </div>
            </DndProvider >
        </>
    );
};
export default SideBar;