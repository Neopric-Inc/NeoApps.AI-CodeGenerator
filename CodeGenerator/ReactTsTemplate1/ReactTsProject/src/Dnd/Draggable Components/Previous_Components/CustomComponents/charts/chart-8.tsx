import { useState, type FC,useEffect } from 'react';
import type { ApexOptions } from 'apexcharts';
import {
  Box,
  Button,
  Card,
  CardContent,
  CardHeader,
  Container,
  Tooltip,
  Typography
} from '@mui/material';
import { useTheme } from '@mui/material/styles';
import { ArrowRight as ArrowRightIcon } from 'components/icons/arrow-right';
import { InformationCircleOutlined as InformationCircleOutlinedIcon } from 'components/icons/information-circle-outlined';
import Chart from "react-apexcharts";
import { getChartData } from "services/flowService";

export const PieChart8 = (props) => {
  const theme = useTheme();
  let config = props.config;
  const chartData = [
    {
      series: [10],
      color: theme.palette.primary.light,
      size: "60%",
      labels: "Linkedin",
    },
    {
      series: [10],
      color: theme.palette.warning.light,
      size: "60%",
      labels: "Facebook",
    },
    {
      series: [20],
      color: theme.palette.success.light,
      size: "60%",
      labels: "Instagram",
    },
    {
      series: [10],
      color: theme.palette.info.light,
      size: "60%",
      labels: "Twitter",
    },
    {
      series: [70],
      color: "#455a64",
      size: "60%",
      labels: "Other",
    },
  ];

  const [chartCategories, setChartCategories] = useState(null);
  const [series, setSeries] = useState(chartData);
  const chartSeries = series.flatMap((item) => item.series);
  const chartOptions: ApexOptions = {
    chart: {
      background: "transparent",
      stacked: false,
      toolbar: {
        show: false,
      },
    },
    colors: series?.flatMap((item) => item.color),
    dataLabels: {
      enabled: false,
    },
    fill: {
      opacity: 1,
    },
    labels: series?.flatMap((item) => item.labels),
    legend: {
      labels: {
        colors: theme.palette.text.secondary,
      },
      show: true,
    },
    stroke: {
      width: 0,
    },
    theme: {
      mode: theme.palette.mode,
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
      <Container maxWidth="sm">
        <Card>
          <CardHeader
            disableTypography
            title={
              <Box
                sx={{
                  alignItems: "center",
                  display: "flex",
                  justifyContent: "space-between",
                }}
              >
                <Typography variant="h6">{config["Heading"]}</Typography>
                {/* <Tooltip title="Traffic by Social Media platforms">
                  <InformationCircleOutlinedIcon fontSize="small" />
                </Tooltip> */}
              </Box>
            }
          />
          <CardContent>
            <Chart
              height={300}
              options={chartOptions}
              series={series?.flatMap((item) => item.series)}
              type="donut"
            />
            {/* <Box
              sx={{
                display: "flex",
                justifyContent: "flex-end",
              }}
            >
              <Button endIcon={<ArrowRightIcon fontSize="small" />}>
                See all
              </Button>
            </Box> */}
          </CardContent>
        </Card>
      </Container>
    </Box>
  );
};
