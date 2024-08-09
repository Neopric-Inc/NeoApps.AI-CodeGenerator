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
import { Lock as LockIcon } from "components/icons/lock";
import { User as UserIcon } from "components/icons/user";
import { SeverityPill } from "severity-pill";

export const DetailList2: FC = () => (
  <Box
    sx={{
      backgroundColor: "background.default",
      minHeight: "100%",
      p: 3,
    }}
  >
    <Card>
      <CardHeader title="Contact Details" />
      <Divider />
      <Table>
        <TableBody>
          <TableRow>
            <TableCell>
              <Typography variant="subtitle2">Email</Typography>
            </TableCell>
            <TableCell>
              <Typography color="textSecondary" variant="body2">
                miron.vitold@devias.io
              </Typography>
              <SeverityPill color="success">Email verified</SeverityPill>
            </TableCell>
          </TableRow>
          <TableRow>
            <TableCell>
              <Typography variant="subtitle2">Phone</Typography>
            </TableCell>
            <TableCell>
              <Typography color="textSecondary" variant="body2">
                +55 748 327 439
              </Typography>
            </TableCell>
          </TableRow>
          <TableRow>
            <TableCell>
              <Typography variant="subtitle2">Country</Typography>
            </TableCell>
            <TableCell>
              <Typography color="textSecondary" variant="body2">
                USA
              </Typography>
            </TableCell>
          </TableRow>
          <TableRow>
            <TableCell>
              <Typography variant="subtitle2">State/Region</Typography>
            </TableCell>
            <TableCell>
              <Typography color="textSecondary" variant="body2">
                New York
              </Typography>
            </TableCell>
          </TableRow>
          <TableRow>
            <TableCell>
              <Typography variant="subtitle2">Address 1</Typography>
            </TableCell>
            <TableCell>
              <Typography color="textSecondary" variant="body2">
                Street John Wick, no. 7
              </Typography>
            </TableCell>
          </TableRow>
          <TableRow>
            <TableCell>
              <Typography variant="subtitle2">Address 2</Typography>
            </TableCell>
            <TableCell>
              <Typography color="textSecondary" variant="body2">
                House #25
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
        <Button color="inherit" startIcon={<LockIcon fontSize="small" />}>
          Reset &amp; Send Password
        </Button>
        <Button
          color="inherit"
          startIcon={<UserIcon fontSize="small" />}
          sx={{ mt: 1 }}
        >
          Login as Customer
        </Button>
      </Box>
    </Card>
  </Box>
);
