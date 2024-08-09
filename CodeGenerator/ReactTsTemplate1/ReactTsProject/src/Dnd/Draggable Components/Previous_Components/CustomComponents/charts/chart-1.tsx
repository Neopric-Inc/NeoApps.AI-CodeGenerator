import type { FC } from 'react';
import type { ApexOptions } from 'apexcharts';
import { Box, Card, CardContent, CardHeader } from '@mui/material';
import { useTheme } from '@mui/material/styles';
import Chart from "react-apexcharts";
import React, { useEffect, useState } from 'react';
import { getChartData } from 'services/flowService';


export const BarChart1 = (props) => {
  const theme = useTheme();
  const chartSeries = [
    {
      data: [30, 40, 25, 50, 49, 21, 70, 51],
      name: "This week",
      color: "#13affe",
    },
    {
      data: [23, 12, 54, 61, 32, 56, 81, 19],
      name: "Last week",
      color: "#fbab49",
    },
  ];
  const [chartCategories, setChartCategories] = useState(null);
  const [series, setSeries] = useState(chartSeries);
  const chartOptions: ApexOptions = {
    chart: {
      background: "transparent",
      toolbar: {
        show: false,
      },
    },
    colors: series.map((item) => item.color),
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
      categories: chartCategories
        ? chartCategories
        : ["Sun", "Mon", "Tue", "Wed", "Thu", "Fri", "Sat", "Sun"],
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
  useEffect(()=> {
    
    getData();
  },[])
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
            type="bar"
          />
        </CardContent>
      </Card>
    </Box>
  );
};
