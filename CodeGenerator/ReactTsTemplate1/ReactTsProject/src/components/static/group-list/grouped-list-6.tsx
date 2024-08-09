import type { FC } from 'react';
import { format, subDays } from 'date-fns';
import numeral from 'numeral';
import {
  Box,
  Card,
  CardHeader,
  Table,
  TableBody,
  TableCell,
  TableRow,
  Typography
} from '@mui/material';

const transactions = [
  {
    id: 'd46800328cd510a668253b45',
    amount: 25000,
    currency: 'usd',
    date: new Date(),
    sender: 'Devias',
    type: 'receive'
  },
  {
    id: 'b4b19b21656e44b487441c50',
    amount: 6843,
    currency: 'usd',
    date: subDays(new Date(), 1),
    sender: 'Zimbru',
    type: 'send'
  },
  {
    id: '56c09ad91f6d44cb313397db',
    amount: 91823,
    currency: 'usd',
    date: subDays(new Date(), 1),
    sender: 'Vertical Jelly',
    type: 'send'
  },
  {
    id: 'aaeb96c5a131a55d9623f44d',
    amount: 49550,
    currency: 'usd',
    date: subDays(new Date(), 3),
    sender: 'Devias',
    type: 'receive'
  }
];

export const GroupedList6: FC = () => (
  <Box
    sx={{
      backgroundColor: 'background.default',
      p: 3
    }}
  >
    <Card>
      <CardHeader title="Latest Transactions" />
      <Table>
        <TableBody>
          {transactions.map((transaction) => (
            <TableRow
              key={transaction.id}
              sx={{
                '&:last-child td': {
                  border: 0
                }
              }}
            >
              <TableCell width={100}>
                <Box sx={{ p: 1 }}>
                  <Typography
                    align="center"
                    color="textSecondary"
                    variant="subtitle2"
                  >
                    {format(transaction.date, 'LLL').toUpperCase()}
                  </Typography>
                  <Typography
                    align="center"
                    color="textSecondary"
                    variant="h6"
                  >
                    {format(transaction.date, 'd')}
                  </Typography>
                </Box>
              </TableCell>
              <TableCell>
                <Typography variant="subtitle2">
                  {transaction.sender}
                </Typography>
                <Typography
                  color="textSecondary"
                  variant="body2"
                >
                  {
                    transaction.type === 'receive'
                      ? 'Payment received'
                      : 'Payment sent'
                  }
                </Typography>
              </TableCell>
              <TableCell align="right">
                <Typography
                  color={
                    transaction.type === 'receive'
                      ? 'success.main'
                      : 'error.main'
                  }
                  variant="subtitle2"
                >
                  {transaction.type === 'receive' ? '+' : '-'}
                  {' '}
                  {numeral(transaction.amount).format('$0,0.00')}
                </Typography>
                <Typography
                  color="textSecondary"
                  variant="body2"
                >
                  {transaction.currency.toUpperCase()}
                </Typography>
              </TableCell>
            </TableRow>
          ))}
        </TableBody>
      </Table>
    </Card>
  </Box>
);
