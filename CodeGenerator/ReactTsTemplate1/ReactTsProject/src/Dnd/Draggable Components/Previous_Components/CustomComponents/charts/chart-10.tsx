import { useState, type FC,useEffect } from 'react';
import type { ApexOptions } from 'apexcharts';
import numeral from 'numeral';
import { Box, Card, CardContent, CardHeader, Container, Typography } from '@mui/material';
import { useTheme } from '@mui/material/styles';
import Chart from "react-apexcharts";
import { getChartData } from "services/flowService";
const data = {
  series: [
    {
      color: '#FFB547',
      data: 14859,
      label: 'Strategy'
    },
    {
      color: '#7BC67E',
      data: 35690,
      label: 'Outsourcing'
    },
    {
      color: '#7783DB',
      data: 45120,
      label: 'Marketing'
    }
  ]
};

export const PieChart10 = (props) => {
  const theme = useTheme();
  let config = props.config;
  const chartSeries = data.series;

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
    labels: series?.map((item) => item.label),
    legend: {
      show: false,
    },
    stroke: {
      colors: [theme.palette.background.paper],
      width: 1,
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
      <Container maxWidth="md">
        <Card>
          <CardHeader title={config["Heading"]} />
          <CardContent>
            <Chart
              height={260}
              options={chartOptions}
              series={series?.map((item) => item.data)}
              type="pie"
            />
            {series?.map((item) => (
              <Box
                key={item.label}
                sx={{
                  alignItems: "center",
                  display: "flex",
                  p: 1,
                }}
              >
                <Box
                  sx={{
                    backgroundColor: item.color,
                    borderRadius: "50%",
                    height: 8,
                    width: 8,
                  }}
                />
                <Typography sx={{ ml: 2 }} variant="subtitle2">
                  {item.label}
                </Typography>
                <Box sx={{ flexGrow: 1 }} />
                <Typography color="textSecondary" variant="subtitle2">
                  {numeral(item.data).format("$0,0.00")}
                </Typography>
              </Box>
            ))}
          </CardContent>
        </Card>
      </Container>
    </Box>
  );
};
