import { useState, type FC ,useEffect} from 'react';
import numeral from 'numeral';
import type { ApexOptions } from 'apexcharts';
import { Box, Card, CardContent, CardHeader, Container, Typography } from '@mui/material';
import { useTheme } from '@mui/material/styles';
import Chart from "react-apexcharts";
import { getChartData } from "services/flowService";

const data = {
  series: [
    {
      color: '#7783DB',
      category: 'Email',
      data: 37530
    },
    {
      color: '#7BC67E',
      category: 'GDN',
      data: 52717
    },
    {
      color: '#FFB547',
      category: 'Instagram',
      data: 62935
    },
    {
      color: '#F06191',
      category: 'Facebook',
      data: 90590
    },
    {
      color: '#64B6F7',
      category: 'Google Ads Search',
      data: 13219
    }
  ]
};

export const BarChart11 = (props) => {
  const theme = useTheme();
  let config = props.config;
  const chartSeries = [
    {
      color: data.series.map((item) => item.color),
      data: data.series.map((item) => item.data),
      name: "Sales",
    },
  ];

  const [chartCategories, setChartCategories] = useState(null);
  const [series, setSeries] = useState(chartSeries);
  const chartOptions: ApexOptions = {
    chart: {
      background: 'transparent',
      stacked: false,
      toolbar: {
        show: false
      }
    },
    colors: series?.map((item) => item.color),
    dataLabels: {
      enabled: false
    },
    fill: {
      opacity: 1
    },
    grid: {
      borderColor: theme.palette.divider,
      yaxis: {
        lines: {
          show: false
        }
      }
    },
    legend: {
      show: false
    },
    plotOptions: {
      bar: {
        horizontal: true,
        barHeight: '45',
        distributed: true
      }
    },
    theme: {
      mode: theme.palette.mode
    },
    tooltip: {
      y: {
        formatter: (value: number): string => numeral(value).format('$0,0.00')
      }
    },
    xaxis: {
      axisBorder: {
        color: theme.palette.divider,
        show: true
      },
      axisTicks: {
        color: theme.palette.divider,
        show: true
      },
      categories: chartCategories ? chartCategories : data.series.map((item) => item.category)
    },
    yaxis: {
      labels: {
        show: false
      }
    }
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
            {chartCategories?.map((item) => (
              <Box
                key={item.category}
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
                  {item.category}
                </Typography>
              </Box>
            ))}
            <Chart
              height={350}
              options={chartOptions}
              series={series?.map(({ data, name }) => ({ data, name }))}
              type="bar"
            />
          </CardContent>
        </Card>
      </Container>
    </Box>
  );
};
