import type { FC } from "react";
import { useRef, useState } from "react";
import {
  Box,
  Button,
  Card,
  CardActions,
  CardContent,
  CardMedia,
  Divider,
  Grid,
  IconButton,
  ListItemIcon,
  ListItemText,
  Menu,
  MenuItem,
  Tooltip,
  Typography,
} from "@mui/material";
import { blueGrey } from "@mui/material/colors";
import { Archive as ArchiveIcon } from "components/icons/archive";
import { DocumentText as DocumentTextIcon } from "components/icons/document-text";
import { DotsHorizontal as DotsHorizontalIcon } from "components/icons/dots-horizontal";
import { Download as DownloadIcon } from "components/icons/download";
import { PencilAlt as PencilAltIcon } from "components/icons/pencil-alt";
import { Trash as TrashIcon } from "components/icons/trash";

const files = [
  {
    id: "5e8dd0721b9e0fab56d7238b",
    mimeType: "image/png",
    name: "example-project1.jpg",
    size: 1024 * 1024 * 3,
    url: "/static/mock-images/projects/project_4.png",
  },
  {
    id: "5e8dd0784431995a30eb2586",
    mimeType: "application/zip",
    name: "docs.zip",
    size: 1024 * 1024 * 25,
    url: "#",
  },
  {
    id: "5e8dd07cbb62749296ecee1c",
    mimeType: "image/png",
    name: "example-project2.jpg",
    size: 1024 * 1024 * 2,
    url: "/static/mock-images/projects/project_1.png",
  },
];

export const GridList3: FC = () => {
  const moreRef = useRef<HTMLButtonElement | null>(null);
  const [openMenu, setOpenMenu] = useState<boolean>(false);

  const handleMenuOpen = (): void => {
    setOpenMenu(true);
  };

  const handleMenuClose = (): void => {
    setOpenMenu(false);
  };

  return (
    <Box
      sx={{
        backgroundColor: "background.default",
        display: "flex",
        justifyContent: "center",
        minHeight: "100%",
        p: 3,
      }}
    >
      <Grid container spacing={3}>
        {files.map((file) => (
          <Grid item key={file.id} md={4} xs={12}>
            <Card>
              {file.mimeType.includes("image/") ? (
                <CardMedia image={file.url} sx={{ height: 140 }} />
              ) : (
                <Box
                  sx={{
                    alignItems: "center",
                    backgroundColor: blueGrey["50"],
                    color: "#000000",
                    display: "flex",
                    height: 140,
                    justifyContent: "center",
                  }}
                >
                  <DocumentTextIcon fontSize="large" />
                </Box>
              )}
              <CardContent
                sx={{
                  display: "flex",
                  justifyContent: "space-between",
                }}
              >
                <div>
                  <Typography variant="subtitle2">{file.name}</Typography>
                  <Typography color="textSecondary" variant="caption">
                    {file.size}
                  </Typography>
                </div>
                <div>
                  <Tooltip title="More options">
                    <IconButton
                      edge="end"
                      onClick={handleMenuOpen}
                      ref={moreRef}
                      size="small"
                    >
                      <DotsHorizontalIcon fontSize="small" />
                    </IconButton>
                  </Tooltip>
                </div>
              </CardContent>
              <Divider />
              <CardActions>
                <Button fullWidth startIcon={<DownloadIcon fontSize="small" />}>
                  Download
                </Button>
              </CardActions>
              <Menu
                anchorEl={moreRef.current}
                anchorOrigin={{
                  horizontal: "left",
                  vertical: "top",
                }}
                elevation={1}
                onClose={handleMenuClose}
                open={openMenu}
                PaperProps={{
                  sx: {
                    maxWidth: "100%",
                    width: 250,
                  },
                }}
                transformOrigin={{
                  horizontal: "left",
                  vertical: "top",
                }}
              >
                <MenuItem divider>
                  <ListItemIcon>
                    <PencilAltIcon fontSize="small" />
                  </ListItemIcon>
                  <ListItemText primary="Rename" />
                </MenuItem>
                <MenuItem divider>
                  <ListItemIcon>
                    <TrashIcon fontSize="small" />
                  </ListItemIcon>
                  <ListItemText primary="Delete" />
                </MenuItem>
                <MenuItem>
                  <ListItemIcon>
                    <ArchiveIcon fontSize="small" />
                  </ListItemIcon>
                  <ListItemText primary="Archive" />
                </MenuItem>
              </Menu>
            </Card>
          </Grid>
        ))}
      </Grid>
    </Box>
  );
};
