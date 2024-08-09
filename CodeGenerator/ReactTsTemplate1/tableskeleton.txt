import React from "react";
import {
  Card,
  CardContent,
  CardHeader,
  Grid,
  Skeleton,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
} from "@mui/material";

const TableSkeleton = () => {
  // Generate column headers
  const numRows = 5;
  const numCols = 8;
  const headers = Array.from({ length: numCols }, (_, index) => (
    <TableCell key={index}>
      <Skeleton />
    </TableCell>
  ));

  // Generate table rows
  const rows = Array.from({ length: numRows }, (_, rowIndex) => (
    <TableRow key={rowIndex}>
      {Array.from({ length: numCols }, (_, colIndex) => (
        <TableCell key={colIndex}>
          <Skeleton />
        </TableCell>
      ))}
    </TableRow>
  ));

  return (
    <Card>
      <CardHeader>
        <div>
          <Skeleton width={200} />
        </div>
      </CardHeader>
      <CardContent>
        <Grid container spacing={2} justifyContent="flex-end">
          <Grid item xs={12} md={4}>
            <Skeleton width={250} height={60} />
          </Grid>
        </Grid>
        <TableContainer>
          <Table>
            <TableHead>
              <TableRow>{headers}</TableRow>
            </TableHead>
            <TableBody>{rows}</TableBody>
          </Table>
        </TableContainer>
      </CardContent>
    </Card>
  );
};

export default TableSkeleton;
