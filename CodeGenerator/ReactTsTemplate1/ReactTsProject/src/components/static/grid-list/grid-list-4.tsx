import type { FC } from 'react';
import {
  Avatar,
  Box,
  Card,
  CardContent,
  CardMedia,
  Chip,
  Divider,
  Grid,
  Link,
  Typography
} from '@mui/material';

const applicants = [
  {
    id: '5e887a62195cc5aef7e8ca5d',
    avatar: '/static/mock-images/avatars/avatar-marcus_finn.png',
    commonConnections: 12,
    cover: '/static/mock-images/covers/cover_2.jpg',
    name: 'Marcus Finn',
    skills: [
      'User Experience',
      'FrontEnd development',
      'HTML5',
      'VueJS',
      'ReactJS'
    ]
  },
  {
    id: '5e887ac47eed253091be10cb',
    avatar: '/static/mock-images/avatars/avatar-carson_darrin.png',
    commonConnections: 5,
    cover: '/static/mock-images/covers/cover_3.jpg',
    name: 'Carson Darrin',
    skills: [
      'User Interface',
      'FullStack development',
      'Angular',
      'ExpressJS'
    ]
  },
  {
    id: '5e86809283e28b96d2d38537',
    avatar: '/static/mock-images/avatars/avatar-anika_visser.png',
    commonConnections: 17,
    cover: '/static/mock-images/covers/cover_1.jpg',
    name: 'Anika Visser',
    skills: [
      'BackEnd development',
      'Firebase',
      'MongoDB',
      'ExpressJS'
    ]
  }
];

export const GridList4: FC = () => (
  <Box
    sx={{
      backgroundColor: 'background.default',
      minHeight: '100%',
      p: 3
    }}
  >
    <Grid
      container
      spacing={3}
    >
      {applicants.map((applicant) => (
        <Grid
          item
          key={applicant.id}
          md={4}
          xs={12}
        >
          <Card>
            <CardMedia
              image={applicant.cover}
              sx={{ height: 200 }}
            />
            <CardContent sx={{ pt: 0 }}>
              <Box
                sx={{
                  display: 'flex',
                  justifyContent: 'center',
                  mb: 2,
                  mt: '-50px'
                }}
              >
                <Avatar
                  alt="Applicant"
                  src={applicant.avatar}
                  sx={{
                    border: '3px solid #FFFFFF',
                    height: 100,
                    width: 100
                  }}
                />
              </Box>
              <Link
                align="center"
                color="textPrimary"
                sx={{ display: 'block' }}
                underline="none"
                variant="h6"
              >
                {applicant.name}
              </Link>
              <Typography
                align="center"
                variant="body2"
                color="textSecondary"
              >
                {applicant.commonConnections}
                {' '}
                contacts in common
              </Typography>
              <Divider sx={{ my: 2 }} />
              <Box sx={{ m: -0.5 }}>
                {applicant.skills.map((skill) => (
                  <Chip
                    key={skill}
                    label={skill}
                    sx={{ m: 0.5 }}
                    variant="outlined"
                  />
                ))}
              </Box>
            </CardContent>
          </Card>
        </Grid>
      ))}
    </Grid>
  </Box>
);
