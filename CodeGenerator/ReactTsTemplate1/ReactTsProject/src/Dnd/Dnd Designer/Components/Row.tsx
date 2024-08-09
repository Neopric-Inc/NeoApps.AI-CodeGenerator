import React, { useRef } from "react";
import { useDrag } from "react-dnd";
import { ROW } from "../Utility/constants";
import DropZone from "./DropZone";
import Column from "./Column";

const style = {};
const Row = ({
  data,
  components,
  handleDrop,
  path,
  openLink,
  openTabLink,
  configurations,
  LayoutList,
  handleConfigurationChange,
}) => {
  const ref = useRef(null);

  const [{ isDragging }, drag] = useDrag({
    type: ROW,
    item: {
      type: ROW,
      id: data.id,
      children: data.children,
      path,
    },
    collect: (monitor) => ({
      isDragging: monitor.isDragging(),
    }),
  });

  const opacity = isDragging ? 0 : 1;
  drag(ref);

  const renderColumn = (column, currentPath) => {
    return (
      <Column
        key={column.id}
        data={column}
        openLink={openLink}
        openTabLink={openTabLink}
        components={components}
        handleDrop={handleDrop}
        path={currentPath}
        LayoutList={LayoutList}
        configurations={configurations}
        handleConfigurationChange={handleConfigurationChange}
      />
    );
  };

  return (
    <div
      ref={ref}
      style={{ ...style, opacity }}
      className="dnd base draggable row"
    >
      {/* {data.id} */}
      <div className="dnd columns">
        {data.children.map((column, index) => {
          const currentPath = `${path}-${index}`;

          return (
            <React.Fragment key={column.id}>
              <DropZone
                data={{
                  path: currentPath,
                  childrenCount: data.children.length,
                }}
                onDrop={handleDrop}
                isLast
                className="dnd horizontalDrag"
              />
              {renderColumn(column, currentPath)}
            </React.Fragment>
          );
        })}
        <DropZone
          data={{
            path: `${path}-${data.children.length}`,
            childrenCount: data.children.length,
          }}
          onDrop={handleDrop}
          className="dnd horizontalDrag"
          isLast
        />
      </div>
    </div>
  );
};
export default Row;
