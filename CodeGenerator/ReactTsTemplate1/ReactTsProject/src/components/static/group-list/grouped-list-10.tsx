import type { FC } from "react";
import { formatDistanceToNowStrict, subHours, subMinutes } from "date-fns";
import { Avatar, Box, Card, Link, Typography } from "@mui/material";
import { Download as DownloadIcon } from "components/icons/download";

const now = new Date();

const activities = [
  {
    id: "5e8dd0828d628e6f40abdfe8",
    createdAt: subMinutes(now, 23).getTime(),
    description: "has uploaded a new file",
    subject: "Project author",
    type: "upload_file",
  },
  {
    id: "5e8dd0893a6725f2bb603617",
    createdAt: subHours(now, 2).getTime(),
    description: "joined team as a Front-End Developer",
    subject: "Adrian Stefan",
    type: "join_team",
  },
  {
    id: "5e8dd08f44603e3300b75cf1",
    createdAt: subHours(now, 9).getTime(),
    description: "joined team as a Full Stack Developer",
    subject: "Alexndru Robert",
    type: "join_team",
  },
];

export const GroupedList10: FC = () => (
  <Box
    sx={{
      backgroundColor: "background.default",
      minHeight: "100%",
      p: 3,
    }}
  >
    {activities.map((activity) => (
      <Card
        key={activity.id}
        sx={{
          alignItems: "center",
          display: "flex",
          p: 2,
          "& + &": {
            mt: 2,
          },
        }}
      >
        <Avatar
          sx={{
            backgroundColor: "primary.main",
            color: "common.white",
          }}
        >
          <DownloadIcon fontSize="small" />
        </Avatar>
        <Typography sx={{ ml: 2 }} variant="body2">
          <Link color="textPrimary" variant="subtitle2">
            {activity.subject}
          </Link>{" "}
          {activity.description}
        </Typography>
        <Typography sx={{ ml: "auto" }} variant="caption">
          {formatDistanceToNowStrict(activity.createdAt)} ago
        </Typography>
      </Card>
    ))}
  </Box>
);
