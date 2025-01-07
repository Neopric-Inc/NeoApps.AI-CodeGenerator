import type { FC } from "react";
import numeral from "numeral";
import { format, subMinutes, subSeconds } from "date-fns";
import {
  Avatar,
  Box,
  Card,
  CardHeader,
  Checkbox,
  Divider,
  IconButton,
  Table,
  TableBody,
  TableCell,
  TableHead,
  TablePagination,
  TableRow,
  TableSortLabel,
  Tooltip,
  Typography,
} from "@mui/material";
import type { SeverityPillColor } from "severity-pill";
import { SeverityPill } from "severity-pill";
import { Image as ImageIcon } from "components/icons/image";

export const TableView1Config = (name, value) => {
  switch (name) {
    case "SimpleView":
      return <TableCell>{value}</TableCell>;
    case "SimpleText":
      return (
        <TableCell>
          <Typography color="textSecondary" variant="body2">
            {value}
          </Typography>
        </TableCell>
      );
    case "Status":
      return (
        <TableCell>
          <SeverityPill color="success">{value}</SeverityPill>
        </TableCell>
      );
      
    case "Currency":
      return <TableCell>{numeral(value).format(`${value}0,0.00`)}</TableCell>;
      
    case "Name":
      return <TableCell>{value}</TableCell>;
      
    case "ID":
      return (
        <TableCell>
          <Typography variant="subtitle2">{value}</Typography>
        </TableCell>
      );
      
    case "Email":
      return (
        <TableCell>
          <Typography color="textPrimary" variant="subtitle2">
            {value}
          </Typography>
        </TableCell>
      );
      
    case "ID Sortable":
      return (
        <TableCell sortDirection="desc">
          <Tooltip enterDelay={300} title="Sort">
            <TableSortLabel active direction="desc">
              {value}
            </TableSortLabel>
          </Tooltip>
        </TableCell>
      );
      
    case "Date Time":
      return (
        <TableCell >
          <Typography color="textSecondary" variant="body2">
            {format(new Date(value), "dd MMM yyyy | HH:mm")}
          </Typography>
        </TableCell>
      );
      
    case "Date":
      return (
        <TableCell >
          <Typography color="textSecondary" variant="body2">
            {format(new Date(value), "dd/MM/yyyy")}
          </Typography>
        </TableCell>
      );
      

   
  }
};
export const TableView2Config = (name, value) => {
  switch (name) {
    case "SimpleView":
      return <TableCell>{value}</TableCell>;
      
    case "SimpleText":
      return (
        <TableCell>
          <Typography color="textSecondary" variant="body2">
            {value}
          </Typography>
        </TableCell>
      );
      
    case "Avatar":
      return <Avatar src={value} />;
      
    case "Currency":
      return <TableCell>{numeral(value).format(`${value}0,0.00`)}</TableCell>;
      

    case "ID":
      return (
        <TableCell>
          <Typography variant="subtitle2">{value}</Typography>
        </TableCell>
      );
      
    case "Email":
      return (
        <TableCell>
          <Typography color="textPrimary" variant="subtitle2">
            {value}
          </Typography>
        </TableCell>
      );
      
    case "ID Sortable":
      return (
        <TableCell sortDirection="desc">
          <Tooltip enterDelay={300} title="Sort">
            <TableSortLabel active direction="desc">
              {value}
            </TableSortLabel>
          </Tooltip>
        </TableCell>
      );
      
    case "Date Time":
      return (
        <TableCell>
          <Typography color="textSecondary" variant="body2">
            {format(new Date(value), "dd MMM yyyy | HH:mm")}
          </Typography>
        </TableCell>
      );
      
    case "Date":
      return (
        <TableCell>
          <Typography color="textSecondary" variant="body2">
            {format(new Date(value), "dd/MM/yyyy")}
          </Typography>
        </TableCell>
      );
      

  }
};
export const TableView3Config = (name, value, optionValue?) => {
  switch (name) {
    case "SimpleView":
      return <TableCell>{value}</TableCell>;
      
    case "SimpleText":
      return (
        <TableCell>
          <Typography color="textSecondary" variant="body2">
            {value}
          </Typography>
        </TableCell>
      );
      
    case "twoInOneCell":
      return (
        <TableCell>
          <Box sx={{ ml: 1 }}>
            <Typography color="inherit" variant="subtitle2">
              {value}
            </Typography>
            <Typography color="textSecondary" variant="body2">
              {optionValue}
            </Typography>
          </Box>
        </TableCell>
      );
      

    case "Status":
      return (
        <TableCell>
          <SeverityPill color="success">{value}</SeverityPill>
        </TableCell>
      );
      
    case "Currency":
      return <TableCell>{numeral(value).format(`${value}0,0.00`)}</TableCell>;
      
    case "Name":
      return (
        <Typography color="inherit" variant="subtitle2">
          {value}
        </Typography>
      );
      
    case "ID":
      return (
        <TableCell>
          <Typography variant="subtitle2">{value}</Typography>
        </TableCell>
      );
      
    case "Email":
      return (
        <Typography color="textSecondary" variant="body2">
          {value}
        </Typography>
      );
      
    case "Date Time":
      return (
        <TableCell>
          <Typography color="textSecondary" variant="body2">
            {format(new Date(value), "dd MMM yyyy | HH:mm")}
          </Typography>
        </TableCell>
      );
      
    case "Date":
      return (
        <TableCell>
          <Typography color="textSecondary" variant="body2">
            {format(new Date(value), "dd/MM/yyyy")}
          </Typography>
        </TableCell>
      );
      
    case "Location":
      return <TableCell>{value}</TableCell>;
      
  }
};
export const TableView4Config = (name, value, optionValue?) => {
  switch (name) {
    case "SimpleView":
      return <TableCell>{value}</TableCell>;
      
    case "SimpleText":
      return (
        <TableCell>
          <Typography color="textSecondary" variant="body2">
            {value}
          </Typography>
        </TableCell>
      );
      
    case "twoInOneCell":
      return (
        <TableCell>
          <Box sx={{ ml: 1 }}>
            <Typography color="inherit" variant="subtitle2">
              {value}
            </Typography>
            <Typography color="textSecondary" variant="body2">
              {optionValue}
            </Typography>
          </Box>
        </TableCell>
      );
      
    case "Status":
      return (
        <TableCell>
          <SeverityPill color="success">{value}</SeverityPill>
        </TableCell>
      );
      
    case "Currency":
      return <TableCell>{numeral(value).format(`${value}0,0.00`)}</TableCell>;
      
    case "Name":
      return <Typography variant="subtitle2">{value}</Typography>;
      
    case "Email":
      return (
        <Typography color="textSecondary" variant="body2">
          {value}
        </Typography>
      );
      
    case "ID":
      return <Typography variant="subtitle2">{value}</Typography>;
      
    case "Date Time":
      return (
        <TableCell>
          <Typography color="textSecondary" variant="body2">
            {format(new Date(value), "dd MMM yyyy | HH:mm")}
          </Typography>
        </TableCell>
      );
      
    case "Date":
      return (
        <TableCell>
          <Typography color="textSecondary" variant="body2">
            {format(new Date(value), "dd/MM/yyyy")}
          </Typography>
        </TableCell>
      );
      

  }
};
export const TableView5Config = (name, value) => {
  switch (name) {
    case "SimpleView":
      return <TableCell>{value}</TableCell>;
      
    case "SimpleText":
      return (
        <TableCell>
          <Typography color="textSecondary" variant="body2">
            {value}
          </Typography>
        </TableCell>
      );
      
    case "Image":
      return (
        <>
          {value ? (
            <Box
              sx={{
                alignItems: "center",
                backgroundColor: "background.default",
                display: "flex",
                height: 100,
                justifyContent: "center",
                overflow: "hidden",
                width: 100,
                "& img": {
                  height: "auto",
                  width: "100%",
                },
              }}
            >
              <img alt="Product" src={value} />
            </Box>
          ) : (
            <Box
              sx={{
                alignItems: "center",
                backgroundColor: "background.default",
                display: "flex",
                height: 100,
                justifyContent: "center",
                width: 100,
              }}
            >
              <ImageIcon fontSize="small" />
            </Box>
          )}
        </>
      );
      
    case "Currency":
      return <TableCell>{numeral(value).format(`${value}0,0.00`)}</TableCell>;
      

    case "Status":
      return (
        <TableCell>
          <SeverityPill color="success">{value}</SeverityPill>
        </TableCell>
      );
      
    case "Date Time":
      return (
        <TableCell>
          <Typography color="textSecondary" variant="body2">
            {format(new Date(value), "dd MMM yyyy | HH:mm")}
          </Typography>
        </TableCell>
      );
      
    case "Date":
      return (
        <TableCell>
          <Typography color="textSecondary" variant="body2">
            {format(new Date(value), "dd/MM/yyyy")}
          </Typography>
        </TableCell>
      );
      

    
  }
};
export const TableView6Config = (name, value, optionValue?) => {
  switch (name) {
    case "SimpleView":
      return <TableCell>{value}</TableCell>;
      
    case "SimpleText":
      return (
        <TableCell>
          <Typography color="textSecondary" variant="body2">
            {value}
          </Typography>
        </TableCell>
      );
      
    case "twoInOneCell":
      return (
        <TableCell>
          <Box sx={{ ml: 1 }}>
            <Typography color="inherit" variant="subtitle2">
              {value}
            </Typography>
            <Typography color="textSecondary" variant="body2">
              {optionValue}
            </Typography>
          </Box>
        </TableCell>
      );
      
    case "Status":
      return (
        <TableCell>
          <SeverityPill color="success">{value}</SeverityPill>
        </TableCell>
      );
      
    case "Currency":
      return <TableCell>{numeral(value).format(`${value}0,0.00`)}</TableCell>;
      
    case "Name":
      return <Typography variant="subtitle2">{value}</Typography>;
      
    case "Email":
      return (
        <Typography color="textSecondary" variant="body2">
          {value}
        </Typography>
      );
      
    case "Date Time":
      return (
        <TableCell>
          <Typography color="textSecondary" variant="body2">
            {format(new Date(value), "dd MMM yyyy | HH:mm")}
          </Typography>
        </TableCell>
      );
      
    case "Date":
      return (
        <TableCell>
          <Typography color="textSecondary" variant="body2">
            {format(new Date(value), "dd/MM/yyyy")}
          </Typography>
        </TableCell>
      );
      
  }
};
export const TableView7Config = (name, value) => {
  switch (name) {
    case "SimpleView":
      return <TableCell>{value}</TableCell>;
      
    case "SimpleText":
      return (
        <TableCell>
          <Typography color="textSecondary" variant="body2">
            {value}
          </Typography>
        </TableCell>
      );
      
    case "Number-formatted":
      return (
        <TableCell>
          <Typography variant="subtitle2">
            {numeral(value).format("0,0")}
          </Typography>
        </TableCell>
      );
      
    case "Date Time":
      return (
        <TableCell>
          <Typography color="textSecondary" variant="body2">
            {format(new Date(value), "dd MMM yyyy | HH:mm")}
          </Typography>
        </TableCell>
      );
      
    case "Date":
      return (
        <TableCell>
          <Typography color="textSecondary" variant="body2">
            {format(new Date(value), "dd/MM/yyyy")}
          </Typography>
        </TableCell>
      );
      
  }
};
export const TableView8Config = (name, value) => {
  switch (name) {
    case "SimpleView":
      return <TableCell>{value}</TableCell>;
      
    case "SimpleText":
      return (
        <TableCell>
          <Typography color="textSecondary" variant="body2">
            {value}
          </Typography>
        </TableCell>
      );
      
    case "Name":
      return (
        <TableCell>
          <Typography variant="subtitle2">{value}</Typography>
        </TableCell>
      );
      
    case "Number-formatted":
      return (
        <TableCell>
          <Typography variant="subtitle2">
            {numeral(value).format("0,0")}
          </Typography>
        </TableCell>
      );
      
    case "Date Time":
      return (
        <TableCell>
          <Typography color="textSecondary" variant="body2">
            {format(new Date(value), "dd MMM yyyy | HH:mm")}
          </Typography>
        </TableCell>
      );
      
    case "Date":
      return (
        <TableCell>
          <Typography color="textSecondary" variant="body2">
            {format(new Date(value), "dd/MM/yyyy")}
          </Typography>
        </TableCell>
      );
      
  }
};
export const TableView9Config = (name, value) => {
  switch (name) {
    case "SimpleView":
      return <TableCell>{value}</TableCell>;
      
    case "Name":
      return (
        <TableCell>
          <Typography variant="subtitle2">{value}</Typography>
        </TableCell>
      );
      
    case "Number-formatted":
      return (
        <TableCell>
          <Typography variant="subtitle2">
            {numeral(value).format("0,0")}
          </Typography>
        </TableCell>
      );
      
    case "Status":
      return (
        <TableCell>
          <SeverityPill color="primary">{value}</SeverityPill>
        </TableCell>
      );
      
    case "SimpleText":
      return (
        <TableCell>
          <Typography color="textSecondary" variant="body2">
            {value}
          </Typography>
        </TableCell>
      );
      
    case "Date Time":
      return (
        <TableCell>
          <Typography color="textSecondary" variant="body2">
            {format(new Date(value), "dd MMM yyyy | HH:mm")}
          </Typography>
        </TableCell>
      );
      
    case "Date":
      return (
        <TableCell>
          <Typography color="textSecondary" variant="body2">
            {format(new Date(value), "dd/MM/yyyy")}
          </Typography>
        </TableCell>
      );
      
  }
};
export const TableView10Config = (name, value) => {
  switch (name) {
    case "SimpleView":
      return <TableCell>{value}</TableCell>;
      
    case "Name":
      return (
        <TableCell>
          <Typography variant="subtitle2">{value}</Typography>
        </TableCell>
      );
      
    case "SimpleText":
      return (
        <TableCell>
          <Typography color="textSecondary" variant="body2">
            {value}
          </Typography>
        </TableCell>
      );
      
    case "Status":
      return (
        <TableCell>
          <SeverityPill color="primary">{value}</SeverityPill>
        </TableCell>
      );
      
    case "Date Time":
      return (
        <TableCell>
          <Typography color="textSecondary" variant="body2">
            {format(new Date(value), "dd MMM yyyy | HH:mm")}
          </Typography>
        </TableCell>
      );
      
    case "Date":
      return (
        <TableCell>
          <Typography color="textSecondary" variant="body2">
            {format(new Date(value), "dd/MM/yyyy")}
          </Typography>
        </TableCell>
      );
      
  }
};
export const TableView11Config = (name, value) => {
  switch (name) {
    case "SimpleView":
      return <TableCell>{value}</TableCell>;
      
    case "Name":
      return (
        <TableCell>
          <Typography color="textSecondary" variant="body2">
            {value}
          </Typography>
        </TableCell>
      );
      
    case "SimpleText":
      return (
        <TableCell>
          <Typography color="textSecondary" variant="body2">
            {value}
          </Typography>
        </TableCell>
      );
      
    case "Status":
      return (
        <TableCell>
          <SeverityPill color="primary">{value}</SeverityPill>
        </TableCell>
      );
      
    case "ID":
      return (
        <TableCell>
          <Typography variant="subtitle2">{value}</Typography>
        </TableCell>
      );
      
    case "Date Time":
      return (
        <TableCell>
          <Typography color="textSecondary" variant="body2">
            {format(new Date(value), "dd MMM yyyy | HH:mm")}
          </Typography>
        </TableCell>
      );
      
    case "Date":
      return (
        <TableCell>
          <Typography color="textSecondary" variant="body2">
            {format(new Date(value), "dd/MM/yyyy")}
          </Typography>
        </TableCell>
      );
      
  }
};