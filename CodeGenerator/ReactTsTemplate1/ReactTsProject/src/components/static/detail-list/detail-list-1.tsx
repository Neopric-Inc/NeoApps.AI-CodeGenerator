import type { FC } from "react";
import numeral from "numeral";
import {
  Badge,
  Box,
  Button,
  Card,
  CardContent,
  CardHeader,
  Container,
  Divider,
  List,
  ListItem,
  ListItemText,
  Typography,
} from "@mui/material";
import { ArrowRight as ArrowRightIcon } from "components/icons/arrow-right";

const currencies = [
  {
    amount: 21500,
    color: "#6c76c4",
    name: "US Dollars",
  },
  {
    amount: 15300,
    color: "#33bb78",
    name: "Bitcoin",
  },
  {
    amount: 1076.81,
    color: "#ff4081",
    name: "XRP Ripple",
  },
];

export const DetailList1: FC = () => (
  <Box
    sx={{
      backgroundColor: "background.default",
      p: 3,
    }}
  >
    <Container maxWidth="sm">
      <Card>
        <CardHeader
          subheader={<Typography variant="h4">$3,787,681.00</Typography>}
          sx={{ pb: 0 }}
          title={
            <Typography color="textSecondary" variant="overline">
              Total balance
            </Typography>
          }
        />
        <CardContent>
          <Divider sx={{ mb: 2 }} />
          <Typography color="textSecondary" variant="overline">
            Available currency
          </Typography>
          <List disablePadding sx={{ pt: 2 }}>
            {currencies.map((currency) => (
              <ListItem
                disableGutters
                key={currency.name}
                sx={{
                  pb: 2,
                  pt: 0,
                }}
              >
                <ListItemText
                  disableTypography
                  primary={
                    <Box
                      sx={{
                        display: "flex",
                        justifyContent: "space-between",
                      }}
                    >
                      <Badge
                        anchorOrigin={{
                          vertical: "top",
                          horizontal: "left",
                        }}
                        variant="dot"
                        sx={{
                          pl: "20px",
                          "& .MuiBadge-badge": {
                            backgroundColor: currency.color,
                            left: 6,
                            top: 11,
                          },
                        }}
                      >
                        <Typography variant="subtitle2">
                          {currency.name}
                        </Typography>
                      </Badge>
                      <Typography color="textSecondary" variant="subtitle2">
                        {numeral(currency.amount).format("$0,0.00")}
                      </Typography>
                    </Box>
                  }
                />
              </ListItem>
            ))}
          </List>
          <Divider />
          <Box
            sx={{
              alignItems: "flex-start",
              display: "flex",
              flexDirection: "column",
              pt: 2,
            }}
          >
            <Button endIcon={<ArrowRightIcon fontSize="small" />}>
              Add money
            </Button>
            <Button endIcon={<ArrowRightIcon fontSize="small" />}>
              Withdraw funds
            </Button>
          </Box>
        </CardContent>
      </Card>
    </Container>
  </Box>
);
