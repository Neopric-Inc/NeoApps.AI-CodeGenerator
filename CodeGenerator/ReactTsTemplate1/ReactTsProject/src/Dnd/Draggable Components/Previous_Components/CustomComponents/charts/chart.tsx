
import type { FC } from "react";
import type { ApexOptions } from "apexcharts";
import { Box, Card, CardContent, CardHeader } from "@mui/material";
import { useTheme } from "@mui/material/styles";
import Chart from "react-apexcharts";
import React from "react";


export const DummyChart1: FC = () => {
  const theme = useTheme();

  const chartOptions: ApexOptions = {
    chart: {
      background: "transparent",
      toolbar: {
        show: false,
      },
    },
    colors: ["#13affe", "#fbab49"],
    dataLabels: {
      enabled: false,
    },
    fill: {
      opacity: 1,
    },
    grid: {
      borderColor: theme.palette.divider,
      yaxis: {
        lines: {
          show: false,
        },
      },
    },
    legend: {
      labels: {
        colors: theme.palette.text.secondary,
      },
      show: true,
    },
    plotOptions: {
      bar: {
        columnWidth: "40%",
      },
    },
    stroke: {
      colors: ["transparent"],
      show: true,
      width: 2,
    },
    theme: {
      mode: theme.palette.mode,
    },
    xaxis: {
      axisBorder: {
        show: true,
        color: theme.palette.divider,
      },
      axisTicks: {
        show: true,
        color: theme.palette.divider,
      },
      categories: ["Sun", "Mon", "Tue", "Wed", "Thu", "Fri", "Sat", "Sun"],
      labels: {
        style: {
          colors: theme.palette.text.secondary,
        },
      },
    },
    yaxis: {
      axisBorder: {
        color: theme.palette.divider,
        show: true,
      },
      axisTicks: {
        color: theme.palette.divider,
        show: true,
      },
      labels: {
        style: {
          colors: theme.palette.text.secondary,
        },
      },
    },
  };

  const chartSeries = [
    {
      data: [30, 40, 25, 50, 49, 21, 70, 51],
      name: "This week",
    },
    {
      data: [23, 12, 54, 61, 32, 56, 81, 19],
      name: "Last week",
    },
  ];

  return (
    <Box
      sx={{
        backgroundColor: "background.default",
        p: 3,
      }}
    >
      <Card>
        <CardHeader title="Sales" />
        <CardContent>
          <Chart
            height={300}
            options={chartOptions}
            series={chartSeries}
            type="bar"
          />
        </CardContent>
      </Card>
    </Box>
  );
};
