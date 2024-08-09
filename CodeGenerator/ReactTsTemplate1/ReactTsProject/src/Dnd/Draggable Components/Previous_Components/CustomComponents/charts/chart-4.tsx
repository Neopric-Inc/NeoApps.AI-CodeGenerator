import { useState, type FC,useEffect } from 'react';
import type { ApexOptions } from 'apexcharts';
import { Box, Card, CardContent, CardHeader, Divider, IconButton } from '@mui/material';
import { useTheme } from '@mui/material/styles';
import { DotsHorizontal as DotsHorizontalIcon } from 'components/icons/dots-horizontal';
import Chart from "react-apexcharts";
import { getChartData } from "services/flowService";




export const LineChart4 = (props) => {
  const theme = useTheme();
  let config = props.config;
  const chartSeries = [
    { data: [10, 5, 11, 20, 13, 28, 18, 4, 13, 12, 13, 5], color: "#00ab57" },
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
      gradient: {
        opacityFrom: 0.4,
        opacityTo: 0.1,
        shadeIntensity: 1,
        stops: [0, 100],
        type: "vertical",
      },
      type: "gradient",
    },
    grid: {
      borderColor: theme.palette.divider,
      strokeDashArray: 2,
    },
    markers: {
      size: 6,
      strokeColors: theme.palette.background.default,
      strokeWidth: 3,
    },
    stroke: {
      curve: "smooth",
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
      categories: chartCategories ? chartCategories : [
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
      labels: {
        offsetY: 5,
        style: {
          colors: theme.palette.text.secondary,
        },
      },
    },
    yaxis: {
      labels: {
        formatter: (value) => (value > 0 ? `${value}K` : `${value}`),
        offsetX: -10,
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
          action={
            <IconButton>
              <DotsHorizontalIcon fontSize="small" />
            </IconButton>
          }
          title={config["Heading"]}
        />
        <Divider />
        <CardContent>
          <Box
            sx={{
              height: 375,
              minWidth: 500,
              position: "relative",
            }}
          >
            <Chart
              height={350}
              options={chartOptions}
              series={series}
              type="area"
            />
          </Box>
        </CardContent>
      </Card>
    </Box>
  );
};
