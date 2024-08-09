import { useState, type FC,useEffect } from 'react';
import type { ApexOptions } from 'apexcharts';
import { format } from 'date-fns';
import { Box, Card, CardHeader, Typography } from '@mui/material';
import { useTheme } from '@mui/material/styles';
import Chart from "react-apexcharts";
import { getChartData } from "services/flowService";

const data = {
  series: [
    {
      data: [12, 24, 36, 48, 60, 72, 84],
      color: "#3C4693",
    },
    {
      data: [12, 24, 36, 48, 60, 72, 84],
      color: "#5664D2",
    },
    {
      data: [12, 24, 36, 48, 60, 72, 84],
      color: "#7783DB",
    },
  ],
  categories: [
    "Capital One",
    "Ally Bank",
    "ING",
    "Ridgewood",
    "BT Transilvania",
    "CEC",
    "CBC",
  ],
};

export const BarChart12 = (props) => {
  const theme = useTheme();

  let config = props.config;
  const chartSeries = data.series;
  const [chartCategories, setChartCategories] = useState(null);
  const [series, setSeries] = useState(chartSeries);
  const chartOptions: ApexOptions = {
    chart: {
      background: "transparent",
      stacked: true,
      toolbar: {
        show: false,
      },
    },
    colors: series?.map((item) => item.color),
    dataLabels: {
      enabled: false,
    },
    fill: {
      opacity: 1,
    },
    grid: {
      borderColor: theme.palette.divider,
      xaxis: {
        lines: {
          show: true,
        },
      },
      yaxis: {
        lines: {
          show: true,
        },
      },
    },
    states: {
      active: {
        filter: {
          type: "none",
        },
      },
      hover: {
        filter: {
          type: "none",
        },
      },
    },
    legend: {
      show: false,
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
        show: false,
      },
      axisTicks: {
        show: false,
      },
      categories: chartCategories ? chartCategories :data.categories,
      labels: {
        style: {
          colors: theme.palette.text.secondary,
        },
      },
    },
    yaxis: {
      labels: {
        offsetX: -12,
        style: {
          colors: theme.palette.text.secondary,
        },
      },
    },
  };
const getData = async () => {
  try {
    const resp = (await getChartData(config["chartURL"])).data;
   //console.log(resp);
    if (resp != null) {
      setChartCategories(resp.chartCategories);
      setSeries(resp.chartSeries);
    }
  } catch (error) {
    // Handle the error gracefully
    console.error("Error fetching chart data:", error);
    // Optionally, you can set a default state or show a user-friendly message
  }
};
useEffect(() => {
  getData();
}, []);
useEffect(() => {
  getData();
}, [config["chartURL"]]);
  return (
    <Box
      sx={{
        backgroundColor: "background.default",
        p: 3,
      }}
    >
      <Card>
        <CardHeader
          // subheader={
          //   // <Typography color="textSecondary" variant="body2">
          //   //   {format(new Date(), "MMM yyyy")}
          //   // </Typography>
          // }
          title={config["Heading"]}
        />

        <Box
          sx={{
            height: 336,
            minWidth: 500,
            px: 2,
          }}
        >
          <Chart
            height={300}
            options={chartOptions}
            series={series}
            type="bar"
          />
        </Box>
      </Card>
    </Box>
  );
};
