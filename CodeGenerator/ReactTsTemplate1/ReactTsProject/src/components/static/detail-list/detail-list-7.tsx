import type { FC } from 'react';
import { addDays, format, subMinutes } from 'date-fns';
import numeral from 'numeral';
import {
  Avatar,
  Box,
  Card,
  CardContent,
  CardHeader,
  Link,
  List,
  ListItem,
  Typography
} from '@mui/material';

export const DetailList7: FC = () => (
  <Box
    sx={{
      backgroundColor: 'background.default',
      minHeight: '100%',
      p: 3
    }}
  >
    <Card>
      <CardHeader
        avatar={<Avatar src="/static/mock-images/avatars/avatar-omar_darobe.png" />}
        disableTypography
        subheader={(
          <Link
            color="textPrimary"
            underline="none"
            variant="subtitle2"
          >
            Omar Darobe
          </Link>
        )}
        style={{ paddingBottom: 0 }}
        title={(
          <Typography
            color="textSecondary"
            sx={{ display: 'block' }}
            variant="overline"
          >
            Contest holder
          </Typography>
        )}
      />
      <CardContent sx={{ pt: 0 }}>
        <List>
          <ListItem
            disableGutters
            divider
            sx={{
              justifyContent: 'space-between',
              padding: 2
            }}
          >
            <Typography variant="subtitle2">
              Deadline
            </Typography>
            <Typography
              color="textSecondary"
              variant="body2"
            >
              {format(addDays(new Date(), 14).getTime(), 'dd MMM yyyy')}
            </Typography>
          </ListItem>
          <ListItem
            disableGutters
            divider
            sx={{
              justifyContent: 'space-between',
              padding: 2
            }}
          >
            <Typography variant="subtitle2">
              Budget
            </Typography>
            <Typography
              color="textSecondary"
              variant="body2"
            >
              {numeral(12500.00).format('$0,0.00')}
            </Typography>
          </ListItem>
          <ListItem
            disableGutters
            sx={{
              justifyContent: 'space-between',
              padding: 2
            }}
          >
            <Typography variant="subtitle2">
              Last Update
            </Typography>
            <Typography
              color="textSecondary"
              variant="body2"
            >
              {format(subMinutes(new Date(), 23).getTime(), 'dd MMM yyyy')}
            </Typography>
          </ListItem>
        </List>
      </CardContent>
    </Card>
  </Box>
);
