import type { FC } from "react";
import { useState } from "react";
import { format, subDays, subHours, subMinutes } from "date-fns";
import {
  Box,
  Button,
  Card,
  CardContent,
  CardHeader,
  Divider,
  Table,
  TableBody,
  TableCell,
  TableRow,
  TextField,
  Typography,
} from "@mui/material";
import { Mail as MailIcon } from "components/icons/mail";

const now = new Date();

const emails = [
  {
    id: "5ece2ce3613486d95ffaea58",
    createdAt: subDays(subHours(subMinutes(now, 34), 5), 3).getTime(),
    description: "Order confirmation",
  },
  {
    id: "5ece2ce8cebf7ad1d100c0cd",
    createdAt: subDays(subHours(subMinutes(now, 49), 11), 4).getTime(),
    description: "Order confirmation",
  },
];

const emailOptions = [
  "Resend last invoice",
  "Send password reset",
  "Send verification",
];

export const DetailList4: FC = () => {
  const [emailOption, setEmailOption] = useState<string>(emailOptions[0]);

  return (
    <Box
      sx={{
        backgroundColor: "background.default",
        minHeight: "100%",
        p: 3,
      }}
    >
      <Card>
        <CardHeader title="Emails" />
        <Divider />
        <CardContent>
          <TextField
            fullWidth
            name="option"
            onChange={(event): void => setEmailOption(event.target.value)}
            select
            SelectProps={{ native: true }}
            value={emailOption}
          >
            {emailOptions.map((option) => (
              <option key={option} value={option}>
                {option}
              </option>
            ))}
          </TextField>
          <Box sx={{ mt: 2 }}>
            <Button
              startIcon={<MailIcon fontSize="small" />}
              variant="contained"
            >
              Send email
            </Button>
          </Box>
          <Box sx={{ mt: 2 }}>
            <Table>
              <TableBody>
                {emails.map((email) => (
                  <TableRow key={email.id}>
                    <TableCell>
                      <Typography variant="subtitle2">
                        {email.description}
                      </Typography>
                    </TableCell>
                    <TableCell>
                      {format(email.createdAt, "dd/MM/yyyy | HH:mm")}
                    </TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          </Box>
        </CardContent>
      </Card>
    </Box>
  );
};
