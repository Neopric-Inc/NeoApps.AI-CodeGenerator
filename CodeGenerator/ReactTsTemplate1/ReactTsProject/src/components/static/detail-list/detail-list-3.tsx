import type { FC } from "react";
import {
  Box,
  Button,
  Card,
  CardHeader,
  Divider,
  Table,
  TableBody,
  TableCell,
  TableRow,
  Typography,
} from "@mui/material";
import ReceiptIcon from "@mui/icons-material/Receipt";
import { CurrencyDollar as CurrencyDollarIcon } from "components/icons/currency-dollar";

export const DetailList3: FC = () => (
  <Box
    sx={{
      backgroundColor: "background.default",
      minHeight: "100%",
      p: 3,
    }}
  >
    <Card>
      <CardHeader title="Invoices/CheckoutBilling" />
      <Divider />
      <Table>
        <TableBody>
          <TableRow>
            <TableCell>
              <Typography variant="subtitle2">Credit Card</Typography>
            </TableCell>
            <TableCell>
              <Typography color="textSecondary" variant="body2">
                **** **** **** **** 4142
              </Typography>
            </TableCell>
          </TableRow>
          <TableRow>
            <TableCell>
              <Typography variant="subtitle2">Paid</Typography>
            </TableCell>
            <TableCell>
              <Typography color="textSecondary" variant="body2">
                2 ($50.00)
              </Typography>
            </TableCell>
          </TableRow>
          <TableRow>
            <TableCell>
              <Typography variant="subtitle2">Draft</Typography>
            </TableCell>
            <TableCell>
              <Typography color="textSecondary" variant="body2">
                1 ($5.00)
              </Typography>
            </TableCell>
          </TableRow>
          <TableRow>
            <TableCell>
              <Typography variant="subtitle2">Unpaid/Due</Typography>
            </TableCell>
            <TableCell>
              <Typography color="textSecondary" variant="body2">
                1 ($12.00)
              </Typography>
            </TableCell>
          </TableRow>
          <TableRow>
            <TableCell>
              <Typography variant="subtitle2">Refunded</Typography>
            </TableCell>
            <TableCell>
              <Typography color="textSecondary" variant="body2">
                0 ($0.00)
              </Typography>
            </TableCell>
          </TableRow>
          <TableRow>
            <TableCell>
              <Typography variant="subtitle2">Gross Income</Typography>
            </TableCell>
            <TableCell>
              <Typography color="textSecondary" variant="body2">
                $1,200.00
              </Typography>
            </TableCell>
          </TableRow>
        </TableBody>
      </Table>
      <Box
        sx={{
          alignItems: "flex-start",
          display: "flex",
          flexDirection: "column",
          p: 1,
        }}
      >
        <Button
          color="inherit"
          startIcon={<CurrencyDollarIcon fontSize="small" />}
        >
          Create Invoice
        </Button>
        <Button
          color="inherit"
          startIcon={<ReceiptIcon fontSize="small" />}
          sx={{ mt: 1 }}
        >
          Resend Due Invoices
        </Button>
      </Box>
    </Card>
  </Box>
);
