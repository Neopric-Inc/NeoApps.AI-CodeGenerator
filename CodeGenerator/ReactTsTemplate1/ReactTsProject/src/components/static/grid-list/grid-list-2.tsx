import type { FC } from "react";
import { formatDistanceToNowStrict, subHours, subMinutes } from "date-fns";
import numeral from "numeral";
import {
  Avatar,
  Box,
  Card,
  CardMedia,
  Divider,
  Grid,
  IconButton,
  Link,
  Rating,
  Tooltip,
  Typography,
} from "@mui/material";
import { red } from "@mui/material/colors";
import FavoriteIcon from "@mui/icons-material/Favorite";
import { Users as UsersIcon } from "components/icons/users";

const now = new Date();

const projects = [
  {
    id: "5e8dcef8f95685ce21f16f3d",
    author: {
      id: "5e887b7602bdbc4dbb234b27",
      avatar: "/static/mock-images/avatars/avatar-jie_yan_song.png",
      name: "Jie Yan Song",
    },
    budget: 6125.0,
    caption:
      "We're looking for experienced Developers and Product Designers to come aboard and help us build succesful businesses through software.",
    currency: "$",
    isLiked: true,
    likes: 7,
    location: "Europe",
    image: "/static/mock-images/projects/project_1.png",
    rating: 5,
    membersCount: 2,
    title: "Mella Full Screen Slider",
    type: "Full-Time",
    updatedAt: subMinutes(now, 24).getTime(),
  },
  {
    id: "5e8dcf076c50b9d8e756a5a2",
    author: {
      id: "5e887d0b3d090c1b8f162003",
      avatar: "/static/mock-images/avatars/avatar-omar_darboe.png",
      name: "Omar Darobe",
    },
    budget: 4205.0,
    caption:
      "We're looking for experienced Developers and Product Designers to come aboard and help us build succesful businesses through software.",
    currency: "$",
    isLiked: true,
    likes: 12,
    location: "Europe",
    image: "/static/mock-images/projects/project_2.png",
    rating: 4.5,
    membersCount: 3,
    title: "Overview Design",
    type: "Full-Time",
    updatedAt: subHours(now, 1).getTime(),
  },
  {
    id: "5e8dcf105a6732b3ed82cf7a",
    author: {
      id: "5e88792be2d4cfb4bf0971d9",
      avatar: "/static/mock-images/avatars/avatar-siegbert_gottfried.png",
      name: "Siegbert Gottfried",
    },
    budget: 2394.0,
    caption:
      "We're looking for experienced Developers and Product Designers to come aboard and help us build succesful businesses through software.",
    currency: "$",
    isLiked: true,
    likes: 18,
    location: "Europe",
    image: "/static/mock-images/projects/project_3.png",
    rating: 4.7,
    membersCount: 8,
    title: "Ten80 Web Design",
    type: "Full-Time",
    updatedAt: subHours(now, 16).getTime(),
  },
];

export const GridList2: FC = () => (
  <Box
    sx={{
      backgroundColor: "background.default",
      minHeight: "100%",
      p: 3,
    }}
  >
    <Grid container spacing={3}>
      {projects.map((project) => (
        <Grid item key={project.id} md={4} xs={12}>
          <Card>
            <Box sx={{ p: 2 }}>
              <CardMedia
                image={project.image}
                sx={{
                  backgroundColor: "background.default",
                  height: 200,
                }}
              />
              <Box
                sx={{
                  alignItems: "center",
                  display: "flex",
                  mt: 2,
                }}
              >
                <Avatar src={project.author.avatar} />
                <Box sx={{ ml: 2 }}>
                  <Link color="textPrimary" variant="h6">
                    {project.title}
                  </Link>
                  <Typography color="textSecondary" variant="body2">
                    by{" "}
                    <Link color="textPrimary" variant="subtitle2">
                      {project.author.name}
                    </Link>{" "}
                    | Updated {formatDistanceToNowStrict(project.updatedAt)} ago
                  </Typography>
                </Box>
              </Box>
            </Box>
            <Box
              sx={{
                pb: 2,
                px: 3,
              }}
            >
              <Typography color="textSecondary" variant="body2">
                {project.caption}
              </Typography>
            </Box>
            <Box
              sx={{
                px: 3,
                py: 2,
              }}
            >
              <Grid
                alignItems="center"
                container
                justifyContent="space-between"
                spacing={3}
              >
                <Grid item>
                  <Typography variant="subtitle2">
                    {numeral(project.budget).format(
                      `${project.currency}0,0.00`
                    )}
                  </Typography>
                  <Typography color="textSecondary" variant="body2">
                    Budget
                  </Typography>
                </Grid>
                <Grid item>
                  <Typography variant="subtitle2">
                    {project.location}
                  </Typography>
                  <Typography color="textSecondary" variant="body2">
                    Location
                  </Typography>
                </Grid>
                <Grid item>
                  <Typography variant="subtitle2">{project.type}</Typography>
                  <Typography color="textSecondary" variant="body2">
                    Type
                  </Typography>
                </Grid>
              </Grid>
            </Box>
            <Divider />
            <Box
              sx={{
                alignItems: "center",
                display: "flex",
                pl: 2,
                pr: 3,
                py: 2,
              }}
            >
              <Box
                sx={{
                  alignItems: "center",
                  display: "flex",
                }}
              >
                <Tooltip title="Unlike">
                  <IconButton sx={{ color: red["600"] }}>
                    <FavoriteIcon fontSize="small" />
                  </IconButton>
                </Tooltip>
                <Typography color="textSecondary" variant="subtitle2">
                  {project.likes}
                </Typography>
              </Box>
              <Box
                sx={{
                  alignItems: "center",
                  display: "flex",
                  ml: 2,
                }}
              >
                <UsersIcon fontSize="small" />
                <Typography
                  color="textSecondary"
                  sx={{ ml: 1 }}
                  variant="subtitle2"
                >
                  {project.membersCount}
                </Typography>
              </Box>
              <Box sx={{ flexGrow: 1 }} />
              <Rating readOnly size="small" value={project.rating} />
            </Box>
          </Card>
        </Grid>
      ))}
    </Grid>
  </Box>
);
