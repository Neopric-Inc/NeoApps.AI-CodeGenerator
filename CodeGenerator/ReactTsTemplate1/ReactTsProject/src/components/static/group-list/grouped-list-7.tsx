import type { FC } from 'react';
import { formatDistanceToNowStrict, subHours, subMinutes } from 'date-fns';
import {
  Avatar,
  Badge,
  Box,
  Button,
  Card,
  CardActions,
  CardHeader,
  List,
  ListItem,
  ListItemAvatar,
  ListItemText,
  Typography
} from '@mui/material';

const messages = [
  {
    id: 'b91cbe81ee3efefba6b915a7',
    content: 'Hello, we spoke earlier on the phone',
    date: subMinutes(new Date(), 2),
    senderAvatar: '/static/mock-images/avatars/avatar-alcides_antonio.png',
    senderName: 'Alcides Antonio'
  },
  {
    id: 'de0eb1ac517aae1aa57c0b7e',
    content: 'Is the job still available?',
    date: subMinutes(new Date(), 56),
    senderAvatar: '/static/mock-images/avatars/avatar-marcus_finn.png',
    senderName: 'Marcus Finn'
  },
  {
    id: '38e2b0942c90d0ad724e6f40',
    content: 'What is a screening task? I’d like to',
    date: subHours(subMinutes(new Date(), 23), 3),
    senderAvatar: '/static/mock-images/avatars/avatar-carson_darrin.png',
    senderName: 'Carson Darrin'
  },
  {
    id: '467505f3356f25a69f4c4890',
    content: 'Still waiting for feedback',
    date: subHours(subMinutes(new Date(), 6), 8),
    senderAvatar: '/static/mock-images/avatars/avatar-fran_perez.png',
    senderName: 'Fran Perez'
  },
  {
    id: '7e6af808e801a8361ce4cf8b',
    content: 'Need more information about current campaigns',
    date: subHours(subMinutes(new Date(), 18), 10),
    senderAvatar: '/static/mock-images/avatars/avatar-jie_yan_song.png',
    senderName: 'Jie Yan Song'
  }
];

export const GroupedList7: FC = () => (
  <Box
    sx={{
      backgroundColor: 'background.default',
      display: 'flex',
      justifyContent: 'center',
      minHeight: '100%',
      p: 3
    }}
  >
    <Card sx={{ maxWidth: 363 }}>
      <CardHeader title="Inbox" />
      <List disablePadding>
        {messages.map((message, index) => (
          <Box key={message.id}>
            <ListItem divider={index + 1 < messages.length}>
              <ListItemAvatar>
                <Avatar src={message.senderAvatar} />
              </ListItemAvatar>
              <ListItemText
                disableTypography
                primary={(
                  index === 0
                    ? (
                      <Badge
                        color="primary"
                        sx={{
                          '.MuiBadge-badge': {
                            right: -16,
                            top: 11
                          }
                        }}
                        variant="dot"
                      >
                        <Typography variant="subtitle2">
                          {message.senderName}
                        </Typography>
                      </Badge>
                    )
                    : (
                      <Typography variant="subtitle2">
                        {message.senderName}
                      </Typography>
                    )
                )}
                secondary={(
                  <Box
                    sx={{
                      alignItems: 'center',
                      display: 'flex',
                      justifyContent: 'space-between'
                    }}
                  >
                    <Typography
                      color="textSecondary"
                      sx={{
                        overflow: 'hidden',
                        pr: 2,
                        textOverflow: 'ellipsis',
                        whiteSpace: 'nowrap'
                      }}
                      variant="body2"
                    >
                      {message.content}
                    </Typography>
                    <Typography
                      color="textSecondary"
                      variant="caption"
                    >
                      {`${formatDistanceToNowStrict(
                        message.date,
                        { addSuffix: true }
                      )}`}
                    </Typography>
                  </Box>
                )}
              />
            </ListItem>
          </Box>
        ))}
      </List>
      <CardActions>
        <Button>
          Go to chat
        </Button>
      </CardActions>
    </Card>
  </Box>
);
