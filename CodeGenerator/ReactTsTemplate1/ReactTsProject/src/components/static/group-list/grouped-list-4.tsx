import type { FC } from "react";
import {
  Box,
  Card,
  IconButton,
  List,
  ListItem,
  ListItemIcon,
  ListItemSecondaryAction,
  ListItemText,
  SvgIcon,
  Tooltip,
  Typography,
} from "@mui/material";
import SendIcon from "@mui/icons-material/Send";
import { CreditCard as CreditCardIcon } from "components/icons/credit-card";
import { Mail as MailIcon } from "components/icons/mail";
import { ChevronRight as ChevronRightIcon } from "components/icons/chevron-right";

type NotificationType = "message" | "invite" | "payout";

interface Notification {
  id: string;
  message: string;
  type: NotificationType;
  value: number;
}

const notifications: Notification[] = [
  {
    id: "5e8883a4f7877f898c408c27",
    message: "to send service quotes",
    type: "invite",
    value: 6,
  },
  {
    id: "5e8883aa34190e0457a6e2b9",
    message: "from clients",
    type: "message",
    value: 2,
  },
  {
    id: "5e8883af168cad3e1f4fe0ae",
    message: "that needs your confirmation",
    type: "payout",
    value: 1,
  },
];

const iconsMap: Record<NotificationType, typeof SvgIcon> = {
  invite: SendIcon,
  message: MailIcon,
  payout: CreditCardIcon,
};

export const GroupedList4: FC = () => (
  <Box
    sx={{
      backgroundColor: "background.default",
      p: 3,
    }}
  >
    <Card>
      <List>
        {notifications.map((notification, i) => {
          const Icon = iconsMap[notification.type];

          return (
            <ListItem
              divider={i < notifications.length - 1}
              key={notification.id}
            >
              <ListItemIcon>
                <Icon fontSize="small" />
              </ListItemIcon>
              <ListItemText>
                <Typography color="textPrimary" variant="subtitle2">
                  {`${notification.value} ${notification.type}s ${notification.message}`}
                </Typography>
              </ListItemText>
              <ListItemSecondaryAction>
                <Tooltip title="View">
                  <IconButton edge="end">
                    <ChevronRightIcon fontSize="small" />
                  </IconButton>
                </Tooltip>
              </ListItemSecondaryAction>
            </ListItem>
          );
        })}
      </List>
    </Card>
  </Box>
);
