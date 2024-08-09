import React, { useRef, useState } from 'react';
import { useDrag, useDrop, DndProvider } from 'react-dnd';
import { HTML5Backend } from 'react-dnd-html5-backend';
import "./style.css"
import { COMPONENT, SIDEBAR_ITEM } from 'Dnd/Dnd Designer/Utility/constants';
import { Button } from 'react-bootstrap';

const ACCEPTS = [SIDEBAR_ITEM, COMPONENT, "AUTOCOMPLETE"];


export const NavBar = () => {
    const [components, setComponents] = useState([]);
    const ref = useRef(null);

    function onDrop(item, components) {
        const newItem = { type: item.type };
        setComponents([...components, newItem]);
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


    const [{ isDragging }, drag] = useDrag({
        type: "AUTOCOMPLETE",
        item: { type: "AUTOCOMPLETE" },
        collect: monitor => ({
            isDragging: monitor.isDragging()
        })
    });
    const opacity = isDragging ? 0.3 : 1;
    drag(ref);

    return (
        <>
            <DndProvider backend={HTML5Backend}>
                <Button ref={ref} style={{ opacity }}>Submit</Button>
                <div id="navbar" ref={drop}>
                    {components.map((item, index) => (
                        <div key={index} style={{ flex: 1 }} >
                            <Button style={{ marginBottom: '10px' }}>Button</Button>
                        </div>
                    ))}
                </div>
                {/* <div id="autoComplete">
                    <AutoComplete />
                </div> */}
            </DndProvider >
        </>
    );
};
