import type { FC } from "react";
import { formatDistanceToNowStrict, subHours } from "date-fns";
import {
  Avatar,
  Box,
  Card,
  CardActionArea,
  CardHeader,
  CardMedia,
  Grid,
  IconButton,
  Link,
  Tooltip,
  Typography,
} from "@mui/material";
import { red } from "@mui/material/colors";
import FavoriteIcon from "@mui/icons-material/Favorite";
import { Clock as ClockIcon } from "components/icons/clock";
import { Share as ShareIcon } from "components/icons/share";

const now = new Date();

const posts = [
  {
    id: "5e887faca2b7a1ddce01221a",
    author: {
      id: "5e86809283e28b96d2d38537",
      avatar: "/static/mock-images/avatars/avatar-anika_visser.png",
      name: "Anika Visser",
    },
    createdAt: subHours(now, 4).getTime(),
    likes: 24,
    media: "/static/mock-images/social/post_1.png",
    message: "Hey guys! What's your favorite framework?",
  },
  {
    id: "5e887faf03e78a5359765636",
    author: {
      id: "5e86809283e28b96d2d38537",
      avatar: "/static/mock-images/avatars/avatar-anika_visser.png",
      name: "Anika Visser",
    },
    createdAt: subHours(now, 7).getTime(),
    likes: 65,
    media: "/static/mock-images/social/post_2.jpg",
    message: "Just made this overview screen for a project, what-cha thinkin?",
  },
];

export const GridList5: FC = () => (
  <Box
    sx={{
      backgroundColor: "background.default",
      minHeight: "100%",
      p: 3,
    }}
  >
    <Grid container spacing={3}>
      {posts.map((post) => (
        <Grid item key={post.id} md={6} xs={12}>
          <Card>
            <CardHeader
              avatar={<Avatar src={post.author.avatar} />}
              disableTypography
              subheader={
                <Box
                  sx={{
                    alignItems: "center",
                    display: "flex",
                    mt: 1,
                  }}
                >
                  <ClockIcon
                    fontSize="small"
                    sx={{ color: "text.secondary" }}
                  />
                  <Typography
                    color="textSecondary"
                    sx={{ ml: "6px" }}
                    variant="caption"
                  >
                    {formatDistanceToNowStrict(post.createdAt)} ago
                  </Typography>
                </Box>
              }
              title={
                <Link color="textPrimary" variant="subtitle2">
                  {post.author.name}
                </Link>
              }
            />
            <Box
              sx={{
                pb: 2,
                px: 3,
              }}
            >
              <Typography variant="body1">{post.message}</Typography>
              <Box sx={{ mt: 2 }}>
                <CardActionArea>
                  <CardMedia
                    image={post.media}
                    sx={{
                      backgroundPosition: "top",
                      height: 350,
                    }}
                  />
                </CardActionArea>
              </Box>
              <Box
                sx={{
                  alignItems: "center",
                  display: "flex",
                  mt: 2,
                }}
              >
                <Tooltip title="Unlike">
                  <IconButton sx={{ color: red["600"] }}>
                    <FavoriteIcon fontSize="small" />
                  </IconButton>
                </Tooltip>
                <Typography color="textSecondary" variant="subtitle2">
                  {post.likes}
                </Typography>
                <Box sx={{ flexGrow: 1 }} />
                <IconButton>
                  <ShareIcon fontSize="small" />
                </IconButton>
              </Box>
            </Box>
          </Card>
        </Grid>
      ))}
    </Grid>
  </Box>
);
