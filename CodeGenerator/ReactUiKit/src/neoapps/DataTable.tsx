import React from "react";
import { Link as RouterLink } from "react-router-dom";
import {
  Box, IconButton, MenuItem, Pagination, Skeleton, Table, TableBody, TableCell, TableContainer, TableHead, TableRow,
  TableSortLabel, TextField, Tooltip, Typography,
} from "@mui/material";
import EditOutlined from "@mui/icons-material/EditOutlined";
import DeleteOutline from "@mui/icons-material/DeleteOutlineOutlined";
import { renderValue } from "./format";
import { ResolvedTable } from "./types";

import { t, lower, formatNumber } from "./i18n";

export const PAGE_SIZES = [10, 25, 50, 100, 200];

/** Page numbers, a page-size choice and "go to page" for long lists. `page` is 0-based. */
export const Pager: React.FC<{ total: number; page: number; pageSize: number; onPage: (page: number, size: number) => void; loading?: boolean }> = ({ total, page, pageSize, onPage, loading }) => {
  const pages = Math.max(1, Math.ceil(total / pageSize));
  const [jump, setJump] = React.useState("");
  const from = total === 0 ? 0 : page * pageSize + 1;
  const to = Math.min(total, (page + 1) * pageSize);
  return (
    <Box sx={{ display: "flex", flexWrap: "wrap", alignItems: "center", gap: 2, px: 2, py: 1.25, borderTop: 1, borderColor: "divider" }}>
      <Typography variant="body2" sx={{ color: "text.secondary", minWidth: 120 }}>
        {loading && total === 0 ? <Skeleton width={100} /> : t("{from}–{to} of {count}", { from: formatNumber(from), to: formatNumber(to), count: formatNumber(total) })}
      </Typography>
      <TextField select size="small" variant="standard" value={pageSize} onChange={(e) => onPage(0, parseInt(e.target.value, 10))}
        slotProps={{ htmlInput: { "aria-label": t("Rows per page:") } }} sx={{ minWidth: 110 }}>
        {PAGE_SIZES.map((n) => <MenuItem key={n} value={n}>{t("{count} per page", { count: n })}</MenuItem>)}
      </TextField>
      <Box sx={{ flex: 1 }} />
      {pages > 1 && <Pagination count={pages} page={page + 1} onChange={(_, p) => onPage(p - 1, pageSize)} shape="rounded" size="small"
        siblingCount={1} boundaryCount={1} showFirstButton={pages > 5} showLastButton={pages > 5} />}
      {pages > 7 && (
        <form onSubmit={(e) => { e.preventDefault(); const n = parseInt(jump, 10); if (n >= 1 && n <= pages) { onPage(n - 1, pageSize); setJump(""); } }}>
          <TextField size="small" value={jump} onChange={(e) => setJump(e.target.value.replace(/\D/g, ""))} placeholder={t("Go to page")}
            slotProps={{ htmlInput: { inputMode: "numeric", "aria-label": t("Go to page"), style: { width: 90 } } }} />
        </form>
      )}
    </Box>
  );
};
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
      <Table size={dense ? "small" : "medium"} sx={{ "& th": { whiteSpace: "nowrap" } }}>
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
          {loading && Array.from({ length: Math.min(pageSize, dense ? 5 : 8) }, (_, i) => (
            <TableRow key={"loading" + i}>
              {columns.map((c, j) => <TableCell key={c}><Skeleton width={j === 0 ? "70%" : "50%"} /></TableCell>)}
              {(onEdit || onDelete) && <TableCell />}
            </TableRow>
          ))}
          {!loading && rows.length === 0 && (
            <TableRow><TableCell colSpan={columns.length + 1} align="center" sx={{ py: 6 }}>
              <Typography sx={{
                color: "text.secondary"
              }}>{emptyText || t("No {items} yet.", { items: lower(table.label) })}</Typography>
            </TableCell></TableRow>
          )}
          {!loading && rows.map((r) => (
            <TableRow key={r[table.primaryKey]} hover sx={{ cursor: onOpen ? "pointer" : undefined }} onClick={() => onOpen && onOpen(r)}>
              {columns.map((c, i) => (
                <TableCell key={c} title={typeof r[c] === "string" && r[c].length > 40 ? r[c] : undefined}
                  sx={i === 0 ? { fontWeight: 600, minWidth: 180, maxWidth: 480, whiteSpace: "normal", wordBreak: "break-word" }
                    : { whiteSpace: "nowrap", maxWidth: 320, overflow: "hidden", textOverflow: "ellipsis" }}>
                  {i === 0 && onOpen
                    ? <Box component={RouterLink} to={`/r/${table.name}/${encodeURIComponent(r[table.primaryKey])}`} onClick={(e: React.MouseEvent) => e.stopPropagation()}
                        sx={{ color: "primary.main", textDecoration: "none", "&:hover": { textDecoration: "underline" } }}>
                        {renderValue(table.fields[c], r[c], labelFor(c, r[c]))}
                      </Box>
                    : renderValue(table.fields[c], r[c], labelFor(c, r[c]))}
                </TableCell>
              ))}
              {(onEdit || onDelete) && (
                <TableCell align="right" onClick={(e) => e.stopPropagation()}>
                  {onEdit && (!canEdit || canEdit(r)) && <Tooltip title={t("Edit")}><IconButton size="small" onClick={() => onEdit(r)}><EditOutlined fontSize="small" /></IconButton></Tooltip>}
                  {onDelete && (!canDelete || canDelete(r)) && <Tooltip title={t("Delete")}><IconButton size="small" onClick={() => onDelete(r)}><DeleteOutline fontSize="small" /></IconButton></Tooltip>}
                </TableCell>
              )}
            </TableRow>
          ))}
        </TableBody>
      </Table>
    </TableContainer>
    {onPage && <Pager total={total} page={page} pageSize={pageSize} onPage={onPage} loading={loading} />}
  </Box>
);
