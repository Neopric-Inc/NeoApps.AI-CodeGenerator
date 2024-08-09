import { useState, type FC ,useEffect} from 'react';
import type { ApexOptions } from 'apexcharts';
import { Box, Card, CardHeader, IconButton } from '@mui/material';
import { alpha, useTheme } from '@mui/material/styles';
import { DotsHorizontal as DotsHorizontalIcon } from "components/icons/dots-horizontal";
import Chart from "react-apexcharts";
import { getChartData } from "services/flowService";


export const BarChart6 = (props) => {
  const theme = useTheme();
  let config = props.config;
  const chartSeries = [
    { data: [18, 16, 5, 8, 3, 14, 14, 16, 17, 19, 18, 20], color: "#00ab57" },
    {
      data: [12, 11, 4, 6, 2, 9, 9, 10, 11, 12, 13, 13],
      color: alpha("#00ab57", 0.25),
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
      strokeDashArray: 2,
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
    plotOptions: {
      bar: {
        columnWidth: "20px",
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

        <Box
          sx={{
            minWidth: 700,
            px: 2,
          }}
        >
          <Chart
            height={375}
            options={chartOptions}
            series={series}
            type="bar"
          />
        </Box>
      </Card>
    </Box>
  );
};
