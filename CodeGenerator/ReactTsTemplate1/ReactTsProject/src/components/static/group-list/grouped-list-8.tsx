import type { FC } from 'react';
import {
  Avatar,
  Box,
  Button,
  Card,
  CardActions,
  CardContent,
  CardHeader,
  Container,
  Divider,
  List,
  ListItem,
  ListItemAvatar,
  ListItemText,
  Typography
} from '@mui/material';

const members = [
  {
    id: '5e887a62195cc5aef7e8ca5d',
    avatar: '/static/mock-images/avatars/avatar-marcus_finn.png',
    job: 'Front End Developer',
    name: 'Marcus Finn'
  },
  {
    id: '5e887ac47eed253091be10cb',
    avatar: '/static/mock-images/avatars/avatar-carson_darrin.png',
    job: 'UX Designer',
    name: 'Carson Darrin'
  },
  {
    id: '5e887b7602bdbc4dbb234b27',
    avatar: '/static/mock-images/avatars/avatar-jie_yan_song.png',
    job: 'Copyright',
    name: 'Jie Yan Song'
  }
];

export const GroupedList8: FC = () => (
  <Box
    sx={{
      backgroundColor: 'background.default',
      minHeight: '100%',
      p: 3
    }}
  >
    <Container maxWidth="sm">
      <Card>
        <CardHeader
          sx={{ pb: 0 }}
          title="Project members"
          titleTypographyProps={{ variant: 'overline' }}
        />
        <CardContent sx={{ pt: 0 }}>
          <List>
            {members.map((member) => (
              <ListItem
                disableGutters
                key={member.id}
              >
                <ListItemAvatar>
                  <Avatar src={member.avatar} />
                </ListItemAvatar>
                <ListItemText
                  primary={(
                    <Typography variant="subtitle2">
                      {member.name}
                    </Typography>
                  )}
                  secondary={(
                    <Typography
                      color="textSecondary"
                      variant="body2"
                    >
                      {member.job}
                    </Typography>
                  )}
                />
              </ListItem>
            ))}
          </List>
        </CardContent>
        <Divider />
        <CardActions>
          <Button fullWidth>
            Manage members
          </Button>
        </CardActions>
      </Card>
    </Container>
  </Box>
);
