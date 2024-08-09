import React from "react";
import Example from "./example";
import { DndProvider } from "react-dnd";
import { HTML5Backend } from "react-dnd-html5-backend";
import { useParams } from "react-router-dom";

export const DndBuilder: React.FC = () => {
    const { id: idValue, project_id } = useParams();
    return (
        <DndProvider backend={HTML5Backend}>
            <Example id={idValue} project_id={project_id} />
        </DndProvider>
    )
}



