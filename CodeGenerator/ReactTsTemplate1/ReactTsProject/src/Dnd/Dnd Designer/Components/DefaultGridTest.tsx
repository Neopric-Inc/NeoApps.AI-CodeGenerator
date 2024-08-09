import { Box } from "@mui/material";
import React, { useRef, useState, useCallback } from "react";
import { Responsive, WidthProvider } from "react-grid-layout";
import "react-grid-layout/css/styles.css";
import "react-resizable/css/styles.css";

const ResponsiveGridLayout = WidthProvider(Responsive);
// Conversion of DefaultGridTest class component

export const CustomGridLayout = ({ layoutItems }) => {
  return (
    <ResponsiveGridLayout
      className="layout"
      layouts={{ lg: layoutItems }} // Use the layoutItems prop for the layout
      breakpoints={{ lg: 1200, md: 996, sm: 768, xs: 480, xxs: 0 }}
      cols={{ lg: 12, md: 10, sm: 6, xs: 4, xxs: 2 }}
      rowHeight={30}
      isDraggable={true}
      isResizable={true}
    >
      {layoutItems.map((item) => (
        <div key={item.i}>
          <span className="text">{`Item ${item.i}`}</span>
        </div>
      ))}
    </ResponsiveGridLayout>
  );
};

// This will be useful to design the layout of row in configuration screen, how row is going to be displayed in preview.
export const CustomGridLayoutConfig = ({
  layoutItems,
  filteredColumns,
  onLayoutUpdate,
}) => {
  // Assuming layoutItems is already in the correct format for react-grid-layout
  const [layout, setLayout] = useState(layoutItems);

  // Handle layout change events
  const handleLayoutChange = useCallback(
    (newLayout) => {
      // Update local state
      setLayout(newLayout);

      // If there's a callback provided, call it with the updated layout
      if (onLayoutUpdate) {
        onLayoutUpdate(newLayout);
      }
    },
    [onLayoutUpdate]
  );

  return (
    <ResponsiveGridLayout
      className="layout"
      layouts={{ lg: layout }}
      breakpoints={{ lg: 1200, md: 996, sm: 768, xs: 480, xxs: 0 }}
      cols={{ lg: 12, md: 10, sm: 6, xs: 4, xxs: 2 }}
      rowHeight={30}
      isDraggable={true}
      isResizable={true}
      onLayoutChange={handleLayoutChange} // Pass the handleLayoutChange callback
    >
      {layoutItems.map((item) => (
        <Box key={item.i} mt={2}>
          <div>
            <span className="text">{`Column ${item.i} Layout`}</span>
          </div>
        </Box>
      ))}
    </ResponsiveGridLayout>
  );
};

export const CustomGridLayoutDynamic = ({ layoutItems, filteredColumns }) => {
  const [currentPage, setCurrentPage] = useState(0);
  return (
    <ResponsiveGridLayout
      className="layout"
      layouts={{ lg: layoutItems }} // Use the layoutItems prop for the layout
      breakpoints={{ lg: 1200, md: 996, sm: 768, xs: 480, xxs: 0 }}
      cols={{ lg: 12, md: 10, sm: 6, xs: 4, xxs: 2 }}
      rowHeight={30}
      isDraggable={true}
      isResizable={true}
    >
      {filteredColumns.map((item) => (
        <div key={item.field}>
          <span className="text">{item.headerName}</span>
        </div>
      ))}
    </ResponsiveGridLayout>
  );
};
// This layout will be displayed in Preview rendering
// Locking all of the size change and other properies.
export const CustomGridLayoutDynamicV2 = ({
  rowData,
  layoutItems,
  filteredColumns,
  onLayoutChange, // New prop for handling layout changes
  config,
}) => {
  const calculateTotalHeight = (layoutItems, rowHeight) => {
    const totalRows = layoutItems.reduce((max, item) => {
      const bottomY = item.y + item.h;
      return bottomY > max ? bottomY : max;
    }, 0);
    return totalRows * rowHeight;
  };

  const totalHeight = calculateTotalHeight(layoutItems, 30); // Assuming rowHeight is 30

  return (
    <ResponsiveGridLayout
      className="layout"
      layouts={{ lg: layoutItems }}
      onLayoutChange={onLayoutChange} // Use this to update the layout in the parent
      // Other props as before...
      isDraggable={false} // Prevent grid items from being dragged
      isResizable={false} // Prevent grid items from being resized
      // You might also want to set compactType to null to disable automatic compacting if you haven't already
      compactType={null}
    >
      {/* {filteredColumns.map((column, index) => (
        <div key={index}>
          <span className="text">{rowData[column.field]}</span>
        </div>
      ))} */}

      {filteredColumns
        // Filter columns based on the config
        .filter((column) => {
          // Check if the visibility flag exists in config and return its opposite if hide is based on visibility
          if (config.hasOwnProperty(`${column.field}_visible`)) {
            return config[`${column.field}_visible`];
          }
          if (
            column.field === "edit" &&
            config.hasOwnProperty("edit_button_visible1")
          ) {
            // Invert the config flag to determine visibility
            return config["edit_button_visible1"];
          }
          if (
            column.field === "delete" &&
            config.hasOwnProperty("delete_button_visible1")
          ) {
            // Invert the config flag to determine visibility
            return config["delete_button_visible1"];
          }
          // Default to showing the column if no specific config is found
          return true;
        })
        .map((column, index) => {
          // Directly render the cell content without additional div styling
          return (
            <div style={{ height: `${totalHeight}px` }} key={index}>
              {column.renderCell ? (
                // Execute the renderCell function, passing necessary params
                column.renderCell({ row: rowData, field: column.field })
              ) : (
                // Directly display the data for columns without a custom renderCell
                <span>{rowData[column.field]}</span>
              )}
            </div>
          );
        })}
    </ResponsiveGridLayout>
  );
};

export const DefaultGridTest = () => {
  const generateLayout = () => {
    const layout = [];
    for (let i = 0; i < 100; i++) {
      // Adjust the number for more items
      layout.push({
        i: i.toString(),
        x: (i * 2) % 12,
        y: Math.floor(i / 6), // Adjust if necessary for your layout
        w: 1,
        h: 1,
        minW: 1,
        maxW: 12,
        minH: 1,
        maxH: 1000,
      });
    }
    return layout;
  };

  const layout = generateLayout();

  return (
    <ResponsiveGridLayout
      className="layout"
      layouts={{ lg: layout }}
      breakpoints={{ lg: 1200, md: 996, sm: 768, xs: 480, xxs: 0 }}
      cols={{ lg: 12, md: 10, sm: 6, xs: 4, xxs: 2 }}
      rowHeight={30}
      isDraggable={true}
      isResizable={true}
    >
      <div>
        {layout.map((item) => (
          <div key={item.i}>
            <span className="text">{`Item ${item.i}`}</span>
          </div>
        ))}
      </div>
    </ResponsiveGridLayout>
  );
};

// Conversion of ResponsiveGridTest class component
export const ResponsiveGridTest = () => {
  const generateLayout = () => {
    const layout = [];
    for (let i = 0; i < 100; i++) {
      // Adjust the number for more items
      layout.push({
        i: i.toString(),
        x: (i * 2) % 12,
        y: Math.floor(i / 6), // Adjust if necessary for your layout
        w: 1,
        h: 1,
        minW: 1,
        maxW: 12,
        minH: 1,
        maxH: 1000,
      });
    }
    return layout;
  };

  const layouts = generateLayout();

  return (
    <ResponsiveGridLayout
      className="layout"
      layouts={{ lg: layouts }}
      breakpoints={{ lg: 1200, md: 996, sm: 768, xs: 480, xxs: 0 }}
      cols={{ lg: 12, md: 10, sm: 6, xs: 4, xxs: 2 }}
      rowHeight={30}
      isDraggable={true}
      isResizable={true}
    >
      {layouts.map((item) => (
        <div key={item.i}>
          <span className="text">{`Item ${item.i}`}</span>
        </div>
      ))}
    </ResponsiveGridLayout>
  );
};

// Conversion of ResponsiveGridWidthProviderTest class component
export const ResponsiveGridWidthProviderTest = () => {
  return (
    <ResponsiveGridLayout
      className="layout"
      breakpoints={{ lg: 1200, md: 996, sm: 768, xs: 480, xxs: 0 }}
      cols={{ lg: 12, md: 10, sm: 6, xs: 4, xxs: 2 }}
      rowHeight={30}
      measureBeforeMount={true}
    >
      <div key="1">1</div>
      <div key="2">2</div>
      <div key="3">3</div>
    </ResponsiveGridLayout>
  );
};

// Conversion of InnerRefObjectTest class component
export const InnerRefObjectTest = () => {
  const ref = useRef<HTMLDivElement>(null);

  return <ResponsiveGridLayout innerRef={ref} />;
};

// Conversion of InnerRefCallbackTest class component
export const InnerRefCallbackTest = () => {
  const handleRef = (ref: HTMLDivElement | null) => {
    // handle the ref
  };

  return <ResponsiveGridLayout innerRef={handleRef} />;
};
