import { ChangeEvent, FC ,useEffect} from 'react';
import { useState } from 'react';
import type { ApexOptions } from 'apexcharts';
import { Box, Card, CardContent, CardHeader, Checkbox, Tooltip, Typography } from '@mui/material';
import { useTheme } from '@mui/material/styles';
import { InformationCircleOutlined as InformationCircleOutlinedIcon } from 'components/icons/information-circle-outlined';
import Chart from "react-apexcharts";
import { getChartData } from "services/flowService";

const data = {
  series: [
    {
      color: "#4CAF50",
      data: [
        3350, 1840, 2254, 5780, 9349, 5241, 2770, 2051, 3764, 2385, 5912, 8323,
      ],
      name: "Organic",
    },
    {
      color: "#FF9800",
      data: [35, 41, 62, 42, 13, 18, 29, 37, 36, 51, 32, 35],
      name: "Referral",
    },
    {
      color: "#F44336",
      data: [100, 122, 50, 300, 250, 400, 312, 200, 10, 60, 90, 400],
      name: "Social Media",
    },
  ],
  xaxis: {
    dataPoints: [
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
  },
};

export const LineChart7 = (props) => {
  const theme = useTheme();
  const [selectedSeries, setSelectedSeries] = useState([
    "Organic",
    "Referral",
    "Social Media",
  ]);
  let config = props.config;
  const handleChange = (
    event: ChangeEvent<HTMLInputElement>,
    name: string
  ): void => {
    if (!event.target.checked) {
      setSelectedSeries(selectedSeries.filter((item) => item !== name));
    } else {
      setSelectedSeries([...selectedSeries, name]);
    }
  };

  const chartSeries = data.series.filter((item) =>
    selectedSeries.includes(item.name)
  );

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
      show: false,
    },
    markers: {
      hover: {
        size: undefined,
        sizeOffset: 2,
      },
      radius: 2,
      shape: "circle",
      size: 4,
      strokeWidth: 0,
    },
    stroke: {
      curve: "smooth",
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
              {/* <Tooltip title="Chart 7 Source by channel">
                <InformationCircleOutlinedIcon fontSize="small" />
              </Tooltip> */}
            </Box>
          }
        />
        <CardContent>
          <Box
            sx={{
              alignItems: "center",
              display: "flex",
              flexWrap: "wrap",
            }}
          >
            {series?.map((item) => (
              <Box
                key={item.name}
                sx={{
                  alignItems: "center",
                  display: "flex",
                  mr: 2,
                }}
              >
                <Checkbox
                  checked={selectedSeries.some(
                    (visibleItem) => visibleItem === item.name
                  )}
                  onChange={(event) => handleChange(event, item.name)}
                />
                <Box
                  sx={{
                    backgroundColor: item.color,
                    borderRadius: "50%",
                    height: 8,
                    ml: 1,
                    mr: 2,
                    width: 8,
                  }}
                />
                <Typography variant="subtitle2">{item.name}</Typography>
              </Box>
            ))}
          </Box>
          <Chart
            height={350}
            options={chartOptions}
            series={series}
            type="line"
          />
        </CardContent>
      </Card>
    </Box>
  );
};
