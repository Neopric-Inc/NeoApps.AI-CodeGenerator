import { useState, type FC, useEffect } from 'react';
import type { ApexOptions } from 'apexcharts';
import { Box, Card, CardContent, CardHeader } from '@mui/material';
import { useTheme } from '@mui/material/styles';
import Chart from "react-apexcharts";
import { getChartData } from 'services/flowService';

export const LineChart2 = (props) => {
  const theme = useTheme();

  const chartSeries = [
    {
      data: [
        3350, 1840, 2254, 5780, 9349, 5241, 2770, 2051, 3764, 2385, 5912, 8323,
      ],
      name: "Page Views",
      color: "#1f87e6",
    },
    {
      data: [35, 41, 62, 42, 13, 18, 29, 37, 36, 51, 32, 35],
      name: "Session Duration",
      color: "#ff5c7c",
    },
  ];
  const [chartCategories, setChartCategories] = useState(null);
  const [series, setSeries] = useState(chartSeries);
  const chartOptions: ApexOptions = {
    chart: {
      background: "transparent",
      stacked: false,
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
      yaxis: {
        lines: {
          show: false,
        },
      },
    },
    legend: {
      horizontalAlign: "right",
      labels: {
        colors: theme.palette.text.secondary,
      },
      position: "top",
      show: true,
    },
    markers: {
      hover: {
        size: undefined,
        sizeOffset: 2,
      },
      radius: 2,
      shape: "circle",
      size: 4,
      strokeColors: series?.map((item) => item.color),
      strokeWidth: 0,
    },
    stroke: {
      curve: "smooth",
      dashArray: [0, 3],
      lineCap: "butt",
      width: 3,
    },
    theme: {
      mode: theme.palette.mode,
    },
    xaxis: {
      axisBorder: {
        color: theme.palette.divider,
      },
      axisTicks: {
        color: theme.palette.divider,
        show: true,
      },
      categories: chartCategories ? chartCategories : [
        "01 Jan",
        "02 Jan",
        "03 Jan",
        "04 Jan",
        "05 Jan",
        "06 Jan",
        "07 Jan",
        "08 Jan",
        "09 Jan",
        "10 Jan",
        "11 Jan",
        "12 Jan",
      ],
      labels: {
        style: {
          colors: theme.palette.text.secondary,
        },
      },
    },
    yaxis: [
      {
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
      {
        axisTicks: {
          color: theme.palette.divider,
          show: true,
        },
        axisBorder: {
          color: theme.palette.divider,
          show: true,
        },
        labels: {
          style: {
            colors: theme.palette.text.secondary,
          },
        },
        opposite: true,
      },
    ],
  };

  let config = props.config;
 //console.log(config);
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
        <CardHeader title={config["Heading"]} />
        <CardContent>
          <Chart
            height={300}
            options={chartOptions}
            series={series}
            type="line"
          />
        </CardContent>
      </Card>
    </Box>
  );
};
