import type { FC } from "react";
import numeral from "numeral";
import {
  Avatar,
  Box,
  Card,
  CardHeader,
  Container,
  Divider,
  IconButton,
  List,
  ListItem,
  ListItemAvatar,
  ListItemText,
  Typography,
} from "@mui/material";
import { DotsHorizontal as DotsHorizontalIcon } from "components/icons/dots-horizontal";

const referrals = [
  {
    color: "#455a64",
    initials: "GT",
    name: "GitHub",
    value: 53032,
  },
  {
    color: "#00bcd4",
    initials: "TW",
    name: "Twitter",
    value: 39551,
  },
  {
    color: "#3949ab",
    initials: "HN",
    name: "Hacker News",
    value: 23150,
  },
  {
    color: "#f44336",
    initials: "SO",
    name: "Stack Overflow",
    value: 14093,
  },
  {
    color: "#e65100",
    initials: "RD",
    name: "Reddit.com",
    value: 7251,
  },
  {
    color: "#263238",
    initials: "DE",
    name: "Dev.to",
    value: 5694,
  },
  {
    color: "#0d47a1",
    initials: "FB",
    name: "Facebook",
    value: 3643,
  },
  {
    color: "#263238",
    initials: "MD",
    name: "Medium",
    value: 1654,
  },
];

export const GroupedList2: FC = () => (
  <Box
    sx={{
      backgroundColor: "background.default",
      p: 3,
    }}
  >
    <Container maxWidth="sm">
      <Card
        sx={{
          display: "flex",
          flexDirection: "column",
        }}
      >
        <CardHeader
          action={
            <IconButton>
              <DotsHorizontalIcon fontSize="small" />
            </IconButton>
          }
          title="Top Referrals"
        />
        <Divider />
        <List disablePadding>
          {referrals.map((referral, i) => (
            <ListItem divider={i < referrals.length - 1} key={referral.name}>
              <ListItemAvatar>
                <Avatar
                  sx={{
                    backgroundColor: referral.color,
                    color: "common.white",
                    fontSize: 14,
                    fontWeight: 600,
                  }}
                >
                  {referral.initials}
                </Avatar>
              </ListItemAvatar>
              <ListItemText
                primary={referral.name}
                primaryTypographyProps={{
                  color: "textPrimary",
                  variant: "subtitle2",
                }}
              />
              <Typography color="textSecondary" variant="body2">
                {numeral(referral.value).format("0,0")}
              </Typography>
            </ListItem>
          ))}
        </List>
      </Card>
    </Container>
  </Box>
);
