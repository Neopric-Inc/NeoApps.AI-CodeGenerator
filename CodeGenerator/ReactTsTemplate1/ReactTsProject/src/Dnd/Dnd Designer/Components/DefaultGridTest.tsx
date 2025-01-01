
import React, { useRef, useState, useCallback, useEffect } from "react";
import { Responsive, WidthProvider } from "react-grid-layout";
import "react-grid-layout/css/styles.css";
import "react-resizable/css/styles.css";

import {
  Card,
  CardHeader,
  CardContent,
  Typography,
  Box,
  Grid,
  IconButton} from '@mui/material';
import EditIcon from '@mui/icons-material/Edit';
import DeleteIcon from '@mui/icons-material/Delete';
const ResponsiveGridLayout = WidthProvider(Responsive);
// Conversion of DefaultGridTest class component


export const CustomGridLayoutDynamicV3 = ({ 
  rowData,
  layoutItems,
  filteredColumns,
  onLayoutChange,
  config,
}) => {
  console.log('filtered column',JSON.stringify(filteredColumns, null, 2));
  console.log('config',JSON.stringify(config, null, 2));
  const calculateTotalHeight = (layoutItems, rowHeight) => {
    const totalRows = layoutItems.reduce((max, item) => {
      const bottomY = item.y + item.h;
      return bottomY > max ? bottomY : max;
    }, 0);
    return totalRows * rowHeight;
  };

  const totalHeight = calculateTotalHeight(layoutItems, 30); // Adjust rowHeight if needed

  // Filter the columns based on the config visibility settings
  const visibleColumns = filteredColumns.filter((column) => {
    if (config.hasOwnProperty(`${column.field}_visible`)) {
      return config[`${column.field}_visible`];
    }
    if (column.field === "edit" && config.hasOwnProperty("edit_button_visible1")) {
      return config["edit_button_visible1"];
    }
    if (column.field === "delete" && config.hasOwnProperty("delete_button_visible1")) {
      return config["delete_button_visible1"];
    }
    // Default to showing the column if no specific config is found
    return true;
  });


//  Dynamic Renderer using chatgpt
  return (
      <DynamicComponentRenderer visibleColumns={visibleColumns}
  rowData={rowData}
  totalHeight={totalHeight}
  filteredColumns={filteredColumns} config={config} />
  );
};

// Hardcoded system prompt for component generation

const DynamicComponentRenderer =({ 
  visibleColumns,
  rowData,
  totalHeight,
  config,
  filteredColumns
}) => {
  const [component, setComponent] = useState(null);
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState(null);
  const systemPrompt = `You are a React component generator specializing in Material-UI (MUI) components. You must maintain specific display logic for data rendering. Just Provide JSX component only no extra clarification.

  Available imports:
  - Card, CardHeader, CardContent from @mui/material
  - Typography, Box, Grid from @mui/material
  - Select, MenuItem from @mui/material
  - IconButton, Tooltip from @mui/material
  - Pagination, Toolbar from @mui/material
  
  Core Display Logic Requirements:
  1. Always maintain the column mapping structure:
     {visibleColumns.map((column, index) => {
       const cellContent = column.renderCell 
         ? column.renderCell({ row: rowData, field: column.field })
         : rowData[column.field];
     })}
  
  2. Preserve the Grid layout structure:
     - Grid container with spacing
     - Grid items with responsive breakpoints (xs, sm, md)
     - Box wrapper for content layout
  
  3. Keep the base structure:
     - Card with variant="outlined"
     - CardContent with flexGrow and overflow handling
     - Nested Grid system for layout
     - Typography components for headers and content
  
  Styling Requirements:
  1. Use sx prop for styling
  2. Maintain height control: sx={{ height: totalHeight }}
  3. Keep flex layout: sx={{ display: 'flex', flexDirection: 'column' }}
  4. Preserve overflow handling: sx={{ flexGrow: 1, overflowY: 'auto' }}
  
  Data Display Pattern:
  1. Always handle both direct field access and renderCell function
  2. Maintain the column.headerName || column.field pattern
  3. Keep the nested Typography structure for labels and values
  4. Preserve the key={index} pattern for mapped elements
  
  Base Component Structure:
  <Card variant="outlined" sx={{height: totalHeight, display: 'flex', flexDirection: 'column'}}>
    <CardContent sx={{ flexGrow: 1, overflowY: 'auto' }}>
      <Grid container spacing={2}>
        {visibleColumns.map((column, index) => {
          const cellContent = column.renderCell 
            ? column.renderCell({ row: rowData, field: column.field })
            : rowData[column.field];
          
          return (
            <Grid item xs={12} sm={6} md={4} key={index}>
              <Box display="flex" flexDirection="column">
                <Typography variant="body2" fontWeight="bold">
                  {column.headerName || column.field}
                </Typography>
                <Typography variant="body2">
                  {cellContent}
                </Typography>
              </Box>
            </Grid>
          );
        })}
      </Grid>
    </CardContent>
  </Card>
  
  Any modifications should build upon this base structure while maintaining all core display logic and data handling. Also sending config and column settings for better decision`+config+'columns'+filteredColumns;
  
  
  const generateComponent = async (userPrompt) => {
    setIsLoading(true);
    setError(null);

    try {
      const response = await fetch('https://api.openai.com/v1/chat/completions', {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          'Authorization': `Bearer ${process.env.API_KEY}`,
        },
        body: JSON.stringify({
          model: 'gpt-4o-mini',
          messages: [
            { role: 'system', content: systemPrompt },
            { role: 'user', content: userPrompt }
          ],
          temperature: 0.7,
          max_tokens: 8000
        }),
      });

      if (!response.ok) {
        throw new Error(`OpenAI API error: ${response.statusText}`);
      }

      const data = await response.json();
      const componentJSX = data.choices[0].message.content;
    
      let componentString = data.choices[0].message.content
      .replace(/```jsx/g, '')
      .replace(/```/g, '')
      .trim();
      console.log('Generated Component',componentString);
      // Replace dynamic values in the string
      const evaluatedString = eval('`' + componentString + '`');
      setComponent(evaluatedString);
    } catch (err) {
     // console.error('Error generating component:', err);
      setError(err.message);
    } finally {
      setIsLoading(false);
    }
  };

  useEffect(() => {
    // Example prompt
 //   const prompt = "Create grid view with tabular format and needs to look solid, hide column headers and keep only one column header like checklist_name as Image , Height auto , hide all columns except image";
 //   generateComponent(prompt);
  }, []);

  if (isLoading) return <div className="p-4">Loading component...</div>;
  if (error) return <div className="p-4 text-red-500">Error: {error}</div>;

  return (
    <div className="dynamic-component-container">
     {/* <div 
      className="dynamic-component-container"
      dangerouslySetInnerHTML={{ __html: component }} 
    /> */}
    <Card variant="outlined" sx={{ height: totalHeight, display: 'flex', flexDirection: 'column' }}>
  <CardContent sx={{ flexGrow: 1, overflowY: 'auto' }}>
    <Grid container spacing={2}>
      <Grid item xs={12}>
        <Typography variant="h6" fontWeight="bold" align="center">
          Checklist Name
        </Typography>
      </Grid>
      {visibleColumns.map((column, index) => {
        const cellContent = column.renderCell 
          ? column.renderCell({ row: rowData, field: column.field })
          : rowData[column.field];
        
        return (
          <Grid item xs={12} sm={6} md={4} key={index}>
            <Box display="flex" flexDirection="column">
              <Typography variant="body2">
                {cellContent}
              </Typography>
            </Box>
          </Grid>
        );
      })}
    </Grid>
  </CardContent>
</Card>
    <Card variant="outlined" sx={{ height: 'auto', display: 'flex', flexDirection: 'column' }}>
  <CardContent sx={{ flexGrow: 1, overflowY: 'auto' }}>
    <Grid container spacing={2}>
      {visibleColumns.map((column, index) => {
        const cellContent = column.renderCell 
          ? column.renderCell({ row: rowData, field: column.field })
          : rowData[column.field];
        
        return (
          <Grid item xs={12} sm={6} md={4} key={index}>
            <Box display="flex" flexDirection="column" sx={{ bgcolor: 'background.paper', p: 2, borderRadius: 1, boxShadow: 1 }}>
              <Typography variant="body2" fontWeight="bold" sx={{ mb: 1 }}>
                {column.headerName || column.field}
              </Typography>
              <Typography variant="body2">
                {cellContent}
              </Typography>
            </Box>
          </Grid>
        );
      })}
    </Grid>
  </CardContent>
</Card>
    <Card variant="outlined" sx={{ height: 'auto', display: 'flex', flexDirection: 'column' }}>
  <CardContent sx={{ flexGrow: 1, overflowY: 'auto' }}>
    <Grid container spacing={2}>
      {visibleColumns.filter(column => column.field === 'image').map((column, index) => {
        const cellContent = column.renderCell 
          ? column.renderCell({ row: rowData, field: column.field })
          : rowData[column.field];
        
        return (
          <Grid item xs={12} sm={6} md={4} key={index}>
            <Box display="flex" flexDirection="column">
              <Box display="flex" justifyContent="space-between" alignItems="center">
                <IconButton aria-label="edit">
                  <EditIcon />
                </IconButton>
                <IconButton aria-label="delete">
                  <DeleteIcon />
                </IconButton>
              </Box>
              <Box>
                {cellContent}
              </Box>
            </Box>
          </Grid>
        );
      })}
    </Grid>
  </CardContent>
</Card>
    <Card variant="outlined" sx={{ height: 'auto', display: 'flex', flexDirection: 'column' }}>
  <CardContent sx={{ flexGrow: 1, overflowY: 'auto' }}>
    <Grid container spacing={2}>
      {visibleColumns.map((column, index) => {
        const cellContent = column.renderCell 
          ? column.renderCell({ row: rowData, field: column.field })
          : rowData[column.field];
          
        return (
          <Grid item xs={12} sm={6} md={4} key={index}>
            <Box display="flex" flexDirection="column">
              <Typography variant="body2" sx={{ display: 'none' }}>
                {column.headerName || column.field}
              </Typography>
              <Typography variant="body2">
                {cellContent}
              </Typography>
            </Box>
          </Grid>
        );
      })}
    </Grid>
  </CardContent>
</Card>
    <Card variant="outlined" sx={{ height: totalHeight, display: 'flex', flexDirection: 'column' }}>
  <CardContent sx={{ flexGrow: 1, overflowY: 'auto' }}>
    <Grid container spacing={2}>
      {visibleColumns.map((column, index) => {
        const cellContent = column.renderCell 
          ? column.renderCell({ row: rowData, field: column.field })
          : rowData[column.field];
        
        return (
          <Grid item xs={12} sm={6} md={4} key={index}>
            <Box display="flex" flexDirection="column">
              <Typography variant="body2" sx={{ display: 'none' }}>
                {column.headerName || column.field}
              </Typography>
              <Typography variant="body2">
                {cellContent}
              </Typography>
            </Box>
          </Grid>
        );
      })}
    </Grid>
  </CardContent>
</Card>
      <Card
      variant="outlined"
      sx={{
        height: totalHeight,
        display: 'flex',
        flexDirection: 'column'
      }}
    >
      <CardContent sx={{ flexGrow: 1, overflowY: 'auto' }}>
        <Grid container spacing={2}>
          {visibleColumns.map((column, index) => {
            const cellContent = column.renderCell 
              ? column.renderCell({ row: rowData, field: column.field })
              : rowData[column.field];
            
            return (
              <Grid item xs={12} sm={6} md={4} key={index}>
                <Box display="flex" flexDirection="column">
                  <Typography variant="body2" fontWeight="bold">
                    {column.headerName || column.field}
                  </Typography>
                  <Typography variant="body2">
                    {cellContent}
                  </Typography>
                </Box>
              </Grid>
            );
          })}
        </Grid>
      </CardContent>
    </Card> <Card variant="outlined" sx={{ 
      height: 'auto', 
      display: 'flex', 
      flexDirection: 'column',
      mb: 4,
      borderRadius: '12px',
      boxShadow: '0 4px 8px rgba(0,0,0,0.1)'
    }}>
      <CardContent sx={{ flexGrow: 1, overflowY: 'auto' }}>
        <Grid container spacing={2}>
          {visibleColumns.map((column, index) => (
            <Grid item xs={12} sm={6} md={4} key={index}>
              <Box display="flex" flexDirection="column">
                <Box display="flex" justifyContent="space-between" alignItems="center">
                  <Typography variant="h6" gutterBottom>
                    {column.headerName || 'Event'}
                  </Typography>
                  <Box>
                    <IconButton aria-label="edit">
                      <EditIcon />
                    </IconButton>
                    <IconButton aria-label="delete">
                      <DeleteIcon />
                    </IconButton>
                  </Box>
                </Box>
                <Box sx={{ mt: 2 }}>
                  {column.renderCell ? column.renderCell({ row: rowData, field: column.field }) : rowData[column.field]}
                </Box>
              </Box>
            </Grid>
          ))}
        </Grid>
      </CardContent>
    </Card>

    {/* Blog Style Card */}
    <Card variant="outlined" sx={{ 
      height: 'auto', 
      display: 'flex', 
      flexDirection: 'column',
      mb: 4,
      borderRadius: '8px',
      bgcolor: '#fff'
    }}>
      <CardContent sx={{ flexGrow: 1, overflowY: 'auto' }}>
        <Grid container spacing={2}>
          {visibleColumns.map((column, index) => (
            <Grid item xs={12} sm={6} md={4} key={index}>
              <Box display="flex" flexDirection="column" sx={{ p: 2, bgcolor: '#f5f5f5', borderRadius: '4px' }}>
                <Typography variant="overline" color="text.secondary">
                  {column.headerName || column.field}
                </Typography>
                <Box sx={{ mt: 2 }}>
                  {column.renderCell ? column.renderCell({ row: rowData, field: column.field }) : rowData[column.field]}
                </Box>
              </Box>
            </Grid>
          ))}
        </Grid>
      </CardContent>
    </Card>

    {/* Social Media Style Card */}
    <Card variant="outlined" sx={{ 
      height: 'auto', 
      display: 'flex', 
      flexDirection: 'column',
      mb: 4,
      borderRadius: '8px'
    }}>
      <CardHeader
        action={
          <Box>
            <IconButton aria-label="edit">
              <EditIcon />
            </IconButton>
            <IconButton aria-label="delete">
              <DeleteIcon />
            </IconButton>
          </Box>
        }
      />
      <CardContent sx={{ flexGrow: 1, overflowY: 'auto' }}>
        <Grid container spacing={2}>
          {visibleColumns.map((column, index) => (
            <Grid item xs={12} key={index}>
              <Box display="flex" flexDirection="column" sx={{ borderBottom: '1px solid #eee', pb: 2 }}>
                <Box sx={{ mb: 2 }}>
                  {column.renderCell ? column.renderCell({ row: rowData, field: column.field }) : rowData[column.field]}
                </Box>
              </Box>
            </Grid>
          ))}
        </Grid>
      </CardContent>
    </Card>

    {/* Netflix Style Card */}
    <Card variant="outlined" sx={{ 
      height: 'auto', 
      display: 'flex', 
      flexDirection: 'column',
      mb: 4,
      borderRadius: '4px',
      bgcolor: '#141414',
      color: 'white'
    }}>
      <CardContent sx={{ flexGrow: 1, overflowY: 'auto' }}>
        <Grid container spacing={2}>
          {visibleColumns.map((column, index) => (
            <Grid item xs={12} sm={6} md={3} key={index}>
              <Box 
                display="flex" 
                flexDirection="column" 
                sx={{ 
                  position: 'relative',
                  '&:hover': {
                    transform: 'scale(1.05)',
                    zIndex: 1
                  }
                }}
              >
                <Box>
                  {column.renderCell ? column.renderCell({ row: rowData, field: column.field }) : rowData[column.field]}
                </Box>
                <Box 
                  sx={{ 
                    position: 'absolute',
                    bottom: 0,
                    left: 0,
                    right: 0,
                    bgcolor: 'rgba(0,0,0,0.8)',
                    p: 1
                  }}
                >
                  <Typography variant="body2">
                    {column.headerName || column.field}
                  </Typography>
                </Box>
              </Box>
            </Grid>
          ))}
        </Grid>
      </CardContent>
    </Card>

    {/* Twitter Style Card */}
    <Card variant="outlined" sx={{ 
      height: 'auto', 
      display: 'flex', 
      flexDirection: 'column',
      mb: 4,
      borderRadius: '16px',
      p: 2
    }}>
      <CardContent sx={{ flexGrow: 1, overflowY: 'auto' }}>
        <Grid container spacing={2}>
          {visibleColumns.map((column, index) => (
            <Grid item xs={12} key={index}>
              <Box display="flex" alignItems="flex-start" sx={{ mb: 2 }}>
                <Box sx={{ ml: 2, flex: 1 }}>
                  <Typography variant="subtitle1" component="div">
                    {column.headerName || column.field}
                  </Typography>
                  <Box sx={{ mt: 1 }}>
                    {column.renderCell ? column.renderCell({ row: rowData, field: column.field }) : rowData[column.field]}
                  </Box>
                </Box>
              </Box>
            </Grid>
          ))}
        </Grid>
      </CardContent>
    </Card>
  </div>
  );
};

export default DynamicComponentRenderer;


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
  console.log('Filtered Columns',JSON.stringify(filteredColumns, null, 2));
  console.log('config',JSON.stringify(filteredColumns, null, 2));
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
function useStyles() {
  throw new Error("Function not implemented.");
}

