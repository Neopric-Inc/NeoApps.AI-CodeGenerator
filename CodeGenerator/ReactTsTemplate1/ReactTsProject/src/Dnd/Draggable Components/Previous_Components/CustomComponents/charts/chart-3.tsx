import { useState, type FC, useEffect } from 'react';
import type { ApexOptions } from 'apexcharts';
import { Box, Card, CardContent, Container, Typography } from '@mui/material';
import { useTheme } from '@mui/material/styles';
import Chart from "react-apexcharts";
import { getChartData } from 'services/flowService';

export const PieChart3 = (props) => {
  const theme = useTheme();

  let config = props.config;
  const chartData = [
    {
      series: [83],
      color: ["#27c6db"],
      size: ["60%"],
      labels: ["System Health"],
    },
  ];
  const chartSeries = chartData.flatMap((item) => item.series);
  const [chartCategories, setChartCategories] = useState(null);
  const [series, setSeries] = useState(chartData);
  const chartOptions: ApexOptions = {
    chart: {
      background: "transparent",
      stacked: false,
      toolbar: {
        show: false,
      },
    },
    colors: series?.flatMap((item) => item.color),
    fill: {
      opacity: 1,
    },
    labels: series?.flatMap((item) => item.labels),
    plotOptions: {
      radialBar: {
        dataLabels: {
          name: {
            color: theme.palette.text.primary,
            fontFamily: theme.typography.fontFamily,
          },
          value: {
            color: theme.palette.text.secondary,
          },
        },
        hollow: {
          size: "60%",
        },
        track: {
          background: theme.palette.background.default,
        },
      },
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
          <CardContent>
            <Chart
              height={300}
              options={chartOptions}
              series={series?.flatMap((data) => data.series)}
              type="radialBar"
            />
            {/* <Typography
              align="center"
              color="textSecondary"
              component="p"
              variant="caption"
            >
              This shouldn&apos;t be bellow 80%
            </Typography> */}
          </CardContent>
        </Card>
      </Container>
    </Box>
  );
};
