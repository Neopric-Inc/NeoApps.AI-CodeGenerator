import AccountBalanceWalletOutlined from "@mui/icons-material/AccountBalanceWalletOutlined";
import React from "react";
import TableChartOutlined from "@mui/icons-material/TableChartOutlined";
import PeopleOutline from "@mui/icons-material/PeopleOutline";
import DescriptionOutlined from "@mui/icons-material/DescriptionOutlined";
import EventOutlined from "@mui/icons-material/EventOutlined";
import AssignmentOutlined from "@mui/icons-material/AssignmentOutlined";
import ScienceOutlined from "@mui/icons-material/ScienceOutlined";
import BusinessOutlined from "@mui/icons-material/BusinessOutlined";
import ChatBubbleOutline from "@mui/icons-material/ChatBubbleOutline";
import LinkOutlined from "@mui/icons-material/LinkOutlined";
import BoltOutlined from "@mui/icons-material/BoltOutlined";
import TravelExploreOutlined from "@mui/icons-material/TravelExploreOutlined";
import ReceiptLongOutlined from "@mui/icons-material/ReceiptLongOutlined";
import Inventory2Outlined from "@mui/icons-material/Inventory2Outlined";
import FolderOutlined from "@mui/icons-material/FolderOutlined";
import DashboardOutlined from "@mui/icons-material/DashboardOutlined";
import SettingsOutlined from "@mui/icons-material/SettingsOutlined";
import AdminPanelSettingsOutlined from "@mui/icons-material/AdminPanelSettingsOutlined";
import GroupAddOutlined from "@mui/icons-material/GroupAddOutlined";
import PaletteOutlined from "@mui/icons-material/PaletteOutlined";
import CheckBoxOutlined from "@mui/icons-material/CheckBoxOutlined";
import LocalHospitalOutlined from "@mui/icons-material/LocalHospitalOutlined";
import CalendarMonthOutlined from "@mui/icons-material/CalendarMonthOutlined";
import StarOutline from "@mui/icons-material/StarOutline";
import ShoppingCartOutlined from "@mui/icons-material/ShoppingCartOutlined";
import SchoolOutlined from "@mui/icons-material/SchoolOutlined";
import HomeOutlined from "@mui/icons-material/HomeOutlined";
import AutoAwesomeOutlined from "@mui/icons-material/AutoAwesomeOutlined";

/** Icon names usable in ui.config.json ("icon": "science"). */
export const ICONS: Record<string, React.ComponentType<any>> = {
  auto_awesome: AutoAwesomeOutlined,
  table: TableChartOutlined,
  people: PeopleOutline,
  description: DescriptionOutlined,
  event: EventOutlined,
  assignment: AssignmentOutlined,
  science: ScienceOutlined,
  business: BusinessOutlined,
  chat: ChatBubbleOutline,
  link: LinkOutlined,
  bolt: BoltOutlined,
  travel_explore: TravelExploreOutlined,
  receipt: ReceiptLongOutlined,
  inventory: Inventory2Outlined,
  folder: FolderOutlined,
  dashboard: DashboardOutlined,
  settings: SettingsOutlined,
  security: AdminPanelSettingsOutlined,
  group_add: GroupAddOutlined,
  palette: PaletteOutlined,
  check: CheckBoxOutlined,
  hospital: LocalHospitalOutlined,
  calendar: CalendarMonthOutlined,
  star: StarOutline,
  cart: ShoppingCartOutlined,
  school: SchoolOutlined,
  home: HomeOutlined,
  wallet: AccountBalanceWalletOutlined,
};

export const Icon: React.FC<{ name?: string; fontSize?: "small" | "medium" | "large" | "inherit" }> = ({ name, fontSize = "small" }) => {
  const C = ICONS[name || "table"] || TableChartOutlined;
  return <C fontSize={fontSize} />;
};
