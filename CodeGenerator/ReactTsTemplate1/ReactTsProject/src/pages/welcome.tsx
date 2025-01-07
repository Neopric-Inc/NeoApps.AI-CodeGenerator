import React from 'react';
import { Button, Card, CardContent, Grid, Typography } from "@mui/material";
import { useNavigate } from "react-router";

const Welcome: React.FC = () => {
    const navigate = useNavigate();

    return (
        <div style={{ backgroundColor: "white" }}>

            <Grid
                container
                justifyContent="center"
                alignItems="center"
                height="100vh"
                spacing={3} // Set spacing between cards
            >
                <Grid item xs={10} sm={6} md={2}></Grid>
                <Grid item xs={10} sm={6} md={2}>
                    <Card
                        sx={{
                            minWidth: 300, // Set a minimum width for the card
                            bgcolor: "rgba(0,0,0,0.8)", // Set a dark background color for the card
                        }}
                    >
                        <CardContent>
                            <Typography
                                variant="h6"
                                align="center"
                                gutterBottom
                                color="white" // Set the text color to be the default text color
                            >

                                Workflows List
                            </Typography>
                            <Button
                                variant="contained"
                                fullWidth
                                sx={{
                                    bgcolor: "#1976d2", // Set a blue background color for the button
                                    color: "white", // Set the text color to be white
                                }}
                                onClick={() => {
                                    navigate('/workflows');
                                }}
                            >
                                Go to Workflows List
                            </Button>
                        </CardContent>
                    </Card>
                </Grid>
                <Grid item xs={12} sm={6} md={1}>

                </Grid>
                <Grid item xs={12} sm={6} md={2}>
                    <Card
                        sx={{
                            minWidth: 300, // Set a minimum width for the card
                            bgcolor: "rgba(0,0,0,0.8)", // Set a dark background color for the card
                        }}
                    >
                        <CardContent>
                            <Typography
                                variant="h6"
                                align="center"
                                gutterBottom
                                color="white" // Set the text color to be the default text color
                            >
                                Drag and Drop UI Versions
                            </Typography>
                            <Button
                                variant="contained"
                                fullWidth
                                sx={{
                                    bgcolor: "#1976d2", // Set a blue background color for the button
                                    color: "white", // Set the text color to be white
                                }}
                                onClick={() => navigate('/dndUI')}
                            >
                                Go to Drag and Drop UI Versions
                            </Button>
                        </CardContent>
                    </Card>
                </Grid>
                <Grid item xs={12} sm={6} md={1}>

                </Grid>
                <Grid item xs={12} sm={6} md={2}>
                    <Card
                        sx={{
                            minWidth: 300, // Set a minimum width for the card
                            bgcolor: "rgba(0,0,0,0.8)", // Set a dark background color for the card
                        }}
                    >
                        <CardContent>

                            <Typography
                                variant="h6"
                                align="center"
                                gutterBottom
                                color="white" // Set the text color to be the default text color
                            >
                                Dashboard
                            </Typography>
                            <Button
                                variant="contained"
                                fullWidth
                                sx={{
                                    bgcolor: "#1976d2", // Set a blue background color for the button
                                    color: "white", // Set the text color to be white
                                }}
                                onClick={() => navigate('/dashboard')}
                            >
                                Dashboard
                            </Button>
                        </CardContent>
                    </Card>
                </Grid>
                <Grid item xs={10} sm={6} md={2}></Grid>
            </Grid>
        </div>
    );
};

export default Welcome;