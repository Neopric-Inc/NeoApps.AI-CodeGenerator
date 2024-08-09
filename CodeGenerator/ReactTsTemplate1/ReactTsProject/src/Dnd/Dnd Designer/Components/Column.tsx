import React, { useRef } from "react";
import { useDrag } from "react-dnd";
import { COLUMN } from "../Utility/constants";
import DropZone from "./DropZone";
import Component from "./Component";

const style = {};
const Column = ({
  data,
  components,
  handleDrop,
  path,
  configurations,
  openLink,
  openTabLink,
  LayoutList,
  handleConfigurationChange,
}) => {
  const ref = useRef(null);

  const [{ isDragging }, drag] = useDrag({
    type: COLUMN,
    item: {
      type: COLUMN,
      id: data.id,
      children: data.children,
      path,
    },
    collect: (monitor) => ({
      isDragging: monitor.isDragging(),
    }),
  });

  const opacity = isDragging ? 0.3 : 1;
  drag(ref);

  const renderComponent = (component, currentPath) => {
    return (
      <Component
        key={component.id}
        openLink={openLink}
        openTabLink={openTabLink}
        data={component}
        components={components}
        LayoutList={LayoutList}
        path={currentPath}
        configurations={configurations}
        handleConfigurationChange={handleConfigurationChange}
      />
    );
  };

  return (
    <div
      ref={ref}
      style={{ ...style, opacity }}
      className="dnd base draggable column"
    >
      {/* {data.id} */}
      {data.children.map((component, index) => {
        const currentPath = `${path}-${index}`;

        return (
          <React.Fragment key={component.id}>
            <DropZone
              data={{
                path: currentPath,
                childrenCount: data.children.length,
              }}
              onDrop={handleDrop}
              isLast
              className=""
            />
            {renderComponent(component, currentPath)}
          </React.Fragment>
        );
      })}
      <DropZone
        data={{
          path: `${path}-${data.children.length}`,
          childrenCount: data.children.length,
        }}
        onDrop={handleDrop}
        isLast
        className=""
      />
    </div>
  );
};
export default Column;
