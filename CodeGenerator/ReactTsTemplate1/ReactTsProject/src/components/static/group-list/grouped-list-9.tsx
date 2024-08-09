import type { FC } from 'react';
import { formatDistanceToNowStrict, subHours } from 'date-fns';
import { Avatar, Box, Card, CardHeader, Link, Rating, Typography } from '@mui/material';

const now = new Date();

const reviews = [
  {
    id: '5f0366cd843161f193ebadd4',
    author: {
      avatar: '/static/mock-images/avatars/avatar-marcus_finn.png',
      name: 'Marcus Finn'
    },
    comment: 'Great company, providing an awesome & easy to use product.',
    createdAt: subHours(now, 2).getTime(),
    value: 5
  },
  {
    id: 'to33twsyjphcfj55y3t07261',
    author: {
      avatar: '/static/mock-images/avatars/avatar-miron_vitold.png',
      name: 'Miron Vitold'
    },
    comment: 'Not the best people managers, poor management skills, poor career development programs. Communication from corporate & leadership isn\'t always clear and is sometime one-sided. Low pay compared to FANG.',
    createdAt: subHours(now, 2).getTime(),
    value: 2
  },
  {
    id: '6z9dwxjzkqbmxuluxx2681jd',
    author: {
      avatar: '/static/mock-images/avatars/avatar-carson_darrin.png',
      name: 'Carson Darrin'
    },
    comment: 'I have been working with this company full-time. Great for the work life balance. Cons, decentralized decision making process across the organization.',
    createdAt: subHours(now, 2).getTime(),
    value: 4
  }
];

export const GroupedList9: FC = () => (
  <Box
    sx={{
      backgroundColor: 'background.default',
      minHeight: '100%',
      p: 3
    }}
  >
    {reviews.map((review) => (
      <Card
        key={review.id}
        sx={{
          '& + &': {
            mt: 2
          }
        }}
      >
        <CardHeader
          avatar={<Avatar src={review.author.avatar} />}
          disableTypography
          subheader={(
            <Box
              sx={{
                alignItems: 'center',
                display: 'flex',
                flexWrap: 'wrap',
                mt: 1
              }}
            >
              <Box
                sx={{
                  alignItems: 'center',
                  display: 'flex',
                  mr: 1
                }}
              >
                <Rating
                  readOnly
                  value={5}
                />
                <Typography
                  sx={{ ml: 1 }}
                  variant="subtitle2"
                >
                  {review.value}
                </Typography>
              </Box>
              <Typography
                color="textSecondary"
                variant="body2"
              >
                | For
                {' '}
                <Link
                  color="textPrimary"
                  variant="subtitle2"
                >
                  Low Budget
                </Link>
                {' '}
                |
                {' '}
                {formatDistanceToNowStrict(review.createdAt)}
                {' '}
                ago
              </Typography>
            </Box>
          )}
          title={(
            <Link
              color="textPrimary"
              variant="subtitle2"
            >
              {review.author.name}
            </Link>
          )}
        />
        <Box
          sx={{
            pb: 2,
            px: 3
          }}
        >
          <Typography
            color="textSecondary"
            variant="body1"
          >
            Great company, providing an awesome &amp; easy to use
            product.
          </Typography>
        </Box>
      </Card>
    ))}
  </Box>
);
