import { useState, type FC,useEffect} from 'react';
import type { ApexOptions } from 'apexcharts';
import { Box, Card, CardContent, CardHeader } from '@mui/material';
import { useTheme } from '@mui/material/styles';
import Chart from "react-apexcharts";
import { getChartData } from "services/flowService";

export const LineChart9 = (props) => {
  const theme = useTheme();
  let config = props.config;
  const chartSeries = [
    {
      name: "New Customers",
      data: [31, 40, 28, 51, 42, 109, 100, 120, 80, 42, 90, 140],
      color: "#ffb547",
    },
    {
      name: "Up/Cross-Selling",
      data: [11, 32, 45, 32, 34, 52, 41, 80, 96, 140, 30, 100],
      color: "#7783DB",
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
      type: "solid",
      opacity: 0,
    },
    grid: {
      borderColor: theme.palette.divider,
    },
    markers: {
      strokeColors: theme.palette.background.paper,
      size: 6,
    },
    stroke: {
      curve: "straight",
      width: 2,
    },
    theme: {
      mode: theme.palette.mode,
    },
    xaxis: {
      axisBorder: {
        color: theme.palette.divider,
        show: true,
      },
      axisTicks: {
        color: theme.palette.divider,
        show: true,
      },
      categories: chartCategories? chartCategories :  [
        "Jan",
        "Feb",
        "Mar",
        "Apr",
        "May",
        "Jun",
        "Jul",
        "Aug",
        "Sep",
        "Oct",
        "Nov",
        "Dec",
      ],
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
        <CardHeader title={config["Heading"]} />
        <CardContent>
          <Chart
            height={360}
            options={chartOptions}
            series={series}
            type="area"
          />
        </CardContent>
      </Card>
    </Box>
  );
};
