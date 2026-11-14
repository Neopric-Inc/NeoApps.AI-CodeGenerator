import React from "react";
import {
  Box, CircularProgress, IconButton, Table, TableBody, TableCell, TableContainer, TableHead, TablePagination, TableRow,
  TableSortLabel, Tooltip, Typography,
} from "@mui/material";
import EditOutlined from "@mui/icons-material/EditOutlined";
import DeleteOutline from "@mui/icons-material/DeleteOutlineOutlined";
import { renderValue } from "./format";
import { ResolvedTable } from "./types";

interface Props {
  table: ResolvedTable;
  columns: string[];
  rows: Record<string, any>[];
  total: number;
  loading: boolean;
  page: number; // 0-based
  pageSize: number;
  sort?: { column: string; dir: "ASC" | "DESC" };
  labelFor: (field: string, id: any) => string | undefined;
  onPage?: (page: number, size: number) => void;
  onSort?: (column: string) => void;
  onOpen?: (row: Record<string, any>) => void;
  onEdit?: (row: Record<string, any>) => void;
  onDelete?: (row: Record<string, any>) => void;
  /** Per-row checks (lifecycle locks): a row's Edit/Delete icons show only when these allow it. */
  canEdit?: (row: Record<string, any>) => boolean;
  canDelete?: (row: Record<string, any>) => boolean;
  emptyText?: string;
  dense?: boolean;
}

export const DataTable: React.FC<Props> = ({
  table, columns, rows, total, loading, page, pageSize, sort, labelFor, onPage, onSort, onOpen, onEdit, onDelete, canEdit, canDelete, emptyText, dense,
}) => (
  <Box>
    <TableContainer>
      <Table size={dense ? "small" : "medium"} sx={{ "& td, & th": { whiteSpace: "nowrap" } }}>
        <TableHead>
          <TableRow>
            {columns.map((c) => (
              <TableCell key={c} sx={{ fontWeight: 600, color: "text.secondary", fontSize: 12, textTransform: "uppercase", letterSpacing: 0.4, bgcolor: "grey.50" }}>
                {onSort ? (
                  <TableSortLabel active={sort?.column === c} direction={sort?.column === c && sort.dir === "ASC" ? "asc" : "desc"} onClick={() => onSort(c)}>
                    {table.fields[c]?.label || c}
                  </TableSortLabel>
                ) : table.fields[c]?.label || c}
              </TableCell>
            ))}
            {(onEdit || onDelete) && <TableCell sx={{ bgcolor: "grey.50", width: 96 }} />}
          </TableRow>
        </TableHead>
        <TableBody>
          {loading && (
            <TableRow><TableCell colSpan={columns.length + 1} align="center" sx={{ py: 6 }}><CircularProgress size={28} /></TableCell></TableRow>
          )}
          {!loading && rows.length === 0 && (
            <TableRow><TableCell colSpan={columns.length + 1} align="center" sx={{ py: 6 }}>
              <Typography sx={{
                color: "text.secondary"
              }}>{emptyText || `No ${table.label.toLowerCase()} yet.`}</Typography>
            </TableCell></TableRow>
          )}
          {!loading && rows.map((r) => (
            <TableRow key={r[table.primaryKey]} hover sx={{ cursor: onOpen ? "pointer" : undefined }} onClick={() => onOpen && onOpen(r)}>
              {columns.map((c, i) => (
                <TableCell key={c} sx={{ fontWeight: i === 0 ? 600 : 400, maxWidth: 320, overflow: "hidden", textOverflow: "ellipsis" }}>
                  {renderValue(table.fields[c], r[c], labelFor(c, r[c]))}
                </TableCell>
              ))}
              {(onEdit || onDelete) && (
                <TableCell align="right" onClick={(e) => e.stopPropagation()}>
                  {onEdit && (!canEdit || canEdit(r)) && <Tooltip title="Edit"><IconButton size="small" onClick={() => onEdit(r)}><EditOutlined fontSize="small" /></IconButton></Tooltip>}
                  {onDelete && (!canDelete || canDelete(r)) && <Tooltip title="Delete"><IconButton size="small" onClick={() => onDelete(r)}><DeleteOutline fontSize="small" /></IconButton></Tooltip>}
                </TableCell>
              )}
            </TableRow>
          ))}
        </TableBody>
      </Table>
    </TableContainer>
    {onPage && (
      <TablePagination component="div" count={total} page={page} rowsPerPage={pageSize} rowsPerPageOptions={[10, 25, 50, 100]}
        onPageChange={(_, p) => onPage(p, pageSize)} onRowsPerPageChange={(e) => onPage(0, parseInt(e.target.value, 10))} />
    )}
  </Box>
);
