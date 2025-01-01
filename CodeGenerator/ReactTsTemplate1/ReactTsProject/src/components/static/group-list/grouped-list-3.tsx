import type { FC } from "react";
import { addDays, addHours, differenceInDays, isAfter } from "date-fns";
import {
  Box,
  Card,
  CardHeader,
  Divider,
  IconButton,
  Link,
  List,
  ListItem,
  ListItemText,
} from "@mui/material";
import { DotsHorizontal as DotsHorizontalIcon } from "components/icons/dots-horizontal";

interface Task {
  id: string;
  deadline: number | null;
  members: { avatar: string; name: string }[];
  title: string;
}

const now = new Date();

const tasks: Task[] = [
  {
    id: "5eff24b501ba5281ddb5378c",
    deadline: addDays(addHours(now, 1), 1).getTime(),
    members: [
      {
        avatar: "/static/mock-images/avatars/avatar-marcus_finn.png",
        name: "Marcus Finn",
      },
      {
        avatar: "/static/mock-images/avatars/avatar-carson_darrin.png",
        name: "Carson Darrin",
      },
    ],
    title: "Update the API for the project",
  },
  {
    id: "5eff24bb5bb3bd1beeddde78",
    deadline: addDays(addHours(now, 1), 2).getTime(),
    members: [
      {
        avatar: "/static/mock-images/avatars/avatar-penjani_inyene.png",
        name: "Penjani Inyene",
      },
      {
        avatar: "/static/mock-images/avatars/avatar-anika_visser.png",
        name: "Anika Visser",
      },
      {
        avatar: "/static/mock-images/avatars/avatar-nasimiyu_danai.png",
        name: "Nasimiyu Danai",
      },
    ],
    title: "Redesign the landing page",
  },
  {
    id: "5eff24c019175119993fc1ff",
    deadline: now.getTime(),
    members: [
      {
        avatar: "/static/mock-images/avatars/avatar-miron_vitold.png",
        name: "Miron Vitold",
      },
    ],
    title: "Solve the bug for the showState",
  },
  {
    id: "5eff24c52ce9fdadffa11959",
    deadline: null,
    members: [
      {
        avatar: "/static/mock-images/avatars/avatar-marcus_finn.png",
        name: "Marcus Finn",
      },
      {
        avatar: "/static/mock-images/avatars/avatar-siegbert_gottfried.png",
        name: "Siegbert Gottfried",
      },
    ],
    title: "Release v1.0 Beta",
  },
  {
    id: "5eff24ca3ffab939b667258b",
    deadline: null,
    members: [
      {
        avatar: "/static/mock-images/avatars/avatar-jie_yan_song.png",
        name: "Jie Yan Song",
      },
      {
        avatar: "/static/mock-images/avatars/avatar-marcus_finn.png",
        name: "Marcus Finn",
      },
      {
        avatar: "/static/mock-images/avatars/avatar-anika_visser.png",
        name: "Anika Visser",
      },
    ],
    title: "GDPR Compliance",
  },
  {
    id: "5eff24cf8740fc9faca4e463",
    deadline: null,
    members: [
      {
        avatar: "/static/mock-images/avatars/avatar-penjani_inyene.png",
        name: "Penjani Inyene",
      },
    ],
    title: "Redesign Landing Page",
  },
];

const getDeadline = (task: Task): string => {
  let deadline = "";

  if (task.deadline) {
    const deadlineDate = task.deadline;

    if (isAfter(deadlineDate, now) && differenceInDays(deadlineDate, now) < 3) {
      deadline = `${differenceInDays(deadlineDate, now)} days remaining`;
    }
  }

  return deadline;
};

export const GroupedList3: FC = () => (
  <Box
    sx={{
      backgroundColor: "background.default",
      p: 3,
    }}
  >
    <Card>
      <CardHeader
        action={
          <IconButton>
            <DotsHorizontalIcon fontSize="small" />
          </IconButton>
        }
        title="Team Tasks"
      />
      <Divider />
      <List sx={{ minWidth: 400 }}>
        {tasks.map((task, i) => (
          <ListItem key={task.id} divider={i < tasks.length - 1}>
            <ListItemText
              primary={
                <Link
                  color="textPrimary"
                  noWrap
                  sx={{ cursor: "pointer" }}
                  variant="subtitle2"
                >
                  {task.title}
                </Link>
              }
              secondary={getDeadline(task)}
            />
          </ListItem>
        ))}
      </List>
    </Card>
  </Box>
);
