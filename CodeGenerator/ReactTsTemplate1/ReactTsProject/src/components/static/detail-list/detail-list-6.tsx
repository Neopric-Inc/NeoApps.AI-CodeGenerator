import type { FC } from 'react';
import Markdown from 'react-markdown';
import { Box, Card, CardContent, Chip, Grid, Typography } from '@mui/material';
import { styled } from '@mui/material/styles';

const MarkdownWrapper = styled('div')(
  ({ theme }) => ({
    color: theme.palette.text.primary,
    fontFamily: theme.typography.fontFamily,
    '& p': {
      marginBottom: theme.spacing(2)
    }
  })
);

const description = `
Design files are attached in the files tab.

Develop the web app screens for our product called "PDFace". Please look at the wireframes, system activity workflow and read the section above to understand what we're trying to archive.

There's not many screens we need designed, but there will be modals and various other system triggered events that will need to be considered.

The project has been created in Sketch so let me know if there are any problems opening this project and I'll try to convert into a usable file.
`;

export const DetailList6: FC = () => (
  <Box
    sx={{
      backgroundColor: 'background.default',
      minHeight: '100%',
      p: 3
    }}
  >
    <Card>
      <CardContent>
        <Grid
          container
          spacing={3}
        >
          <Grid
            item
            md={6}
            xs={12}
          >
            <Typography
              color="textSecondary"
              variant="overline"
            >
              Project Name
            </Typography>
            <Typography variant="subtitle2">
              Develop a PDF Export App
            </Typography>
            <Box sx={{ mt: 3 }}>
              <Typography
                color="textSecondary"
                variant="overline"
              >
                Tags
              </Typography>
              <Box sx={{ mt: 1 }}>
                {['React JS'].map((tag) => (
                  <Chip
                    key={tag}
                    label={tag}
                    variant="outlined"
                  />
                ))}
              </Box>
            </Box>
          </Grid>
        </Grid>
        <Box sx={{ mt: 3 }}>
          <Typography
            color="textSecondary"
            sx={{ mb: 2 }}
            variant="overline"
          >
            Description
          </Typography>
          <MarkdownWrapper>
            <Markdown children={description} />
          </MarkdownWrapper>
        </Box>
      </CardContent>
    </Card>
  </Box>
);
