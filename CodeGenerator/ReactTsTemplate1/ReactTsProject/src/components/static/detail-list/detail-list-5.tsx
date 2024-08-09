import type { ChangeEvent, FC } from 'react';
import { useState } from 'react';
import { format } from 'date-fns';
import numeral from 'numeral';
import {
  Box,
  Button,
  Card,
  CardActions,
  CardHeader,
  Divider,
  Table,
  TableBody,
  TableCell,
  TableRow,
  TextField,
  Typography
} from '@mui/material';
import ReceiptIcon from '@mui/icons-material/Receipt';

const order = {
  id: '5ecb8a6879877087d4aa2690',
  coupon: null,
  createdAt: new Date().getTime(),
  currency: '$',
  customer: {
    address1: 'Street John Wick, no. 7',
    address2: 'House #25',
    city: 'San Diego',
    country: 'USA',
    email: 'miron.vitold@devias.io',
    name: 'Miron Vitold'
  },
  items: [
    {
      id: '5ecb8abbdd6dfb1f9d6bf98b',
      billingCycle: 'monthly',
      currency: '$',
      name: 'Project Points',
      quantity: 25,
      unitAmount: 50.25
    },
    {
      id: '5ecb8ac10f116d04bed990eb',
      billingCycle: 'monthly',
      currency: '$',
      name: 'Freelancer Subscription',
      quantity: 1,
      unitAmount: 5.00
    }
  ],
  number: 'DEV-103',
  paymentMethod: 'CreditCard',
  status: 'pending',
  totalAmount: 500.00
};

const statusOptions = ['Canceled', 'Completed', 'Rejected'];

export const DetailList5: FC = () => {
  const [status, setStatus] = useState<string>(statusOptions[0]);

  const handleChange = (event: ChangeEvent<HTMLInputElement>): void => {
    setStatus(event.target.value);
  };

  return (
    <Box
      sx={{
        backgroundColor: 'background.default',
        minHeight: '100%',
        p: 3
      }}
    >
      <Card>
        <CardHeader title="Order info" />
        <Divider />
        <Table>
          <TableBody>
            <TableRow>
              <TableCell>
                <Typography variant="subtitle2">
                  Customer
                </Typography>
              </TableCell>
              <TableCell>
                <div>
                  {order.customer.name}
                </div>
                <div>
                  {order.customer.address1}
                </div>
                <div>
                  {order.customer.city}
                </div>
                <div>
                  {order.customer.country}
                </div>
              </TableCell>
            </TableRow>
            <TableRow>
              <TableCell>
                <Typography variant="subtitle2">
                  ID
                </Typography>
              </TableCell>
              <TableCell>
                #
                {order.id}
              </TableCell>
            </TableRow>
            <TableRow>
              <TableCell>
                <Typography variant="subtitle2">
                  Number
                </Typography>
              </TableCell>
              <TableCell>
                {order.number}
              </TableCell>
            </TableRow>
            <TableRow>
              <TableCell>
                <Typography variant="subtitle2">
                  Date
                </Typography>
              </TableCell>
              <TableCell>
                {format(order.createdAt, 'dd/MM/yyyy HH:mm')}
              </TableCell>
            </TableRow>
            <TableRow>
              <TableCell>
                <Typography variant="subtitle2">
                  Promotion Code
                </Typography>
              </TableCell>
              <TableCell>
                {order.coupon ? order.coupon : 'N/A'}
              </TableCell>
            </TableRow>
            <TableRow>
              <TableCell>
                <Typography variant="subtitle2">
                  Total Amount
                </Typography>
              </TableCell>
              <TableCell>
                {numeral(order.totalAmount)
                  .format(`${order.currency}0,0.00`)}
              </TableCell>
            </TableRow>
            <TableRow>
              <TableCell>
                <Typography variant="subtitle2">
                  Status
                </Typography>
              </TableCell>
              <TableCell>
                <TextField
                  fullWidth
                  name="option"
                  onChange={handleChange}
                  select
                  SelectProps={{ native: true }}
                  value={status}
                >
                  {statusOptions.map((option) => (
                    <option
                      key={option}
                      value={option}
                    >
                      {option}
                    </option>
                  ))}
                </TextField>
              </TableCell>
            </TableRow>
          </TableBody>
        </Table>
        <CardActions>
          <Button startIcon={<ReceiptIcon fontSize="small" />}>
            Resend Invoice
          </Button>
        </CardActions>
      </Card>
    </Box>
  );
};
