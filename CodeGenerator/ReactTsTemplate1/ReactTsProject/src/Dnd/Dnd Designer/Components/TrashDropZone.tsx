import React from "react";
import classNames from "classnames";
import { useDrop } from "react-dnd";
import { COMPONENT, ROW, COLUMN } from "../Utility/constants";
import { Trash as TrashIcon } from "components/icons/trash";

const ACCEPTS = [ROW, COLUMN, COMPONENT];

interface TrashDropZoneProps {
  data: { layout: any[] };
  onDrop: (data: any, item: any) => void;
}

interface ComponentInterface {
  type: string;
  icon: JSX.Element | string;
  content: string | JSX.Element;
}

interface ItemInterface {
  id: string;
  type: string;
  path?: string;
  component?: ComponentInterface;
}
const TrashDropZone: React.FC<TrashDropZoneProps> = ({ data, onDrop }) => {
  const [{ isOver, canDrop }, drop] = useDrop({
    accept: ACCEPTS,
    drop: (item, monitor) => {
      onDrop(data, item);
    },
    canDrop: (item: ItemInterface, monitor) => {
      ////console.log(item);
      const layout = data.layout;
      if (item.type === "sidebarItem" || item.type === "sidebarItemCRUD") {
        ////console.log("SideBar-Item");
      } else {
        const itemPath = item.path;
        const splitItemPath = itemPath.split("-");
        const itemPathRowIndex = splitItemPath[0];
        const itemRowChildrenLength =
          layout[itemPathRowIndex] && layout[itemPathRowIndex].children.length;

        // prevent removing a col when row has only one col
        if (
          item.type === COLUMN &&
          itemRowChildrenLength &&
          itemRowChildrenLength < 2
        ) {
          return false;
        }
      }
      return true;
    },
    collect: (monitor) => ({
      isOver: monitor.isOver(),
      canDrop: monitor.canDrop(),
    }),
  });

  const isActive = isOver && canDrop;
  return (
    <div
      className={classNames("dnd", "trashDropZone", { active: isActive })}
      ref={drop}
    >
      <TrashIcon />
    </div>
  );
};
export default TrashDropZone;
