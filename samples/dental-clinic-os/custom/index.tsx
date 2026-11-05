import React, { useEffect, useState } from "react";
import {
  Box,
  Card,
  CardContent,
  Grid,
  Typography,
  Button,
  Chip,
  IconButton,
  Menu,
  MenuItem,
  Tooltip,
  Dialog,
  DialogTitle,
  DialogContent,
  DialogActions,
  TextField,
  FormControl,
  InputLabel,
  Select,
  FormHelperText,
} from "@mui/material";
import {
  Add as AddIcon,
  Visibility as ViewIcon,
  Edit as EditIcon,
  Delete as DeleteIcon,
  Person as PersonIcon,
  CalendarToday as CalendarIcon,
  AttachMoney as MoneyIcon,
  MedicalServices as MedicalIcon,
} from "@mui/icons-material";
import { useApp } from "../neoapps/AppContext";
import { api } from "../neoapps/api";

/** Custom extensions for Dental Clinic */
export const extensions = {
  /** Widget shown at top of dashboard */
  dashboardTop: () => <TodayDashboardWidget />,
  
  /** Custom actions on record pages */
  recordActions: ({ table, record, onChanged }) => {
    switch (table) {
      case "patients":
        return <PatientRecordActions record={record} onChanged={onChanged} />;
      case "appointments":
        return <AppointmentRecordActions record={record} onChanged={onChanged} />;
      case "treatments":
        return <TreatmentRecordActions record={record} onChanged={onChanged} />;
      case "invoices":
        return <InvoiceRecordActions record={record} onChanged={onChanged} />;
      default:
        return null;
    }
  },
};

/** Today's stats widget for dashboard */
const TodayDashboardWidget: React.FC = () => {
  const [stats, setStats] = useState<any>(null);
  const [balance, setBalance] = useState<any>(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    const load = async () => {
      setLoading(true);
      try {
        const [statsRes, balanceRes] = await Promise.all([
          api("GET", "Custom/Dashboard/TodayStats"),
          api("GET", "Custom/Dashboard/OutstandingBalance"),
        ]);
        if (statsRes.ok) setStats(await statsRes.json());
        if (balanceRes.ok) setBalance(await balanceRes.json());
      } catch (e) {
        console.error("Failed to load dashboard stats:", e);
      } finally {
        setLoading(false);
      }
    };
    load();
  }, []);

  if (loading) return <Box sx={{ py: 3, textAlign: "center" }}>Loading dashboard…</Box>;

  return (
    <Box sx={{ mb: 3 }}>
      <Typography variant="h6" sx={{ mb: 2, fontWeight: 600 }}>
        Today's Overview
      </Typography>
      <Grid container spacing={2}>
        <Grid item xs={12} sm={6} md={3}>
          <StatCard
            title="Total Appointments"
            value={stats?.total_appointments ?? 0}
            icon={<CalendarIcon />}
            color="primary"
            subtitle={`${stats?.confirmed ?? 0} confirmed • ${stats?.completed ?? 0} done`}
          />
        </Grid>
        <Grid item xs={12} sm={6} md={3}>
          <StatCard
            title="Hygiene"
            value={stats?.hygiene_appointments ?? 0}
            icon={<MedicalIcon />}
            color="success"
            subtitle="Cleanings & checkups"
          />
        </Grid>
        <Grid item xs={12} sm={6} md={3}>
          <StatCard
            title="Treatments"
            value={stats?.treatment_appointments ?? 0}
            icon={<MedicalIcon />}
            color="warning"
            subtitle="Crowns, fillings, etc."
          />
        </Grid>
        <Grid item xs={12} sm={6} md={3}>
          <StatCard
            title="Outstanding"
            value={`$${(balance?.total_overdue ?? 0).toLocaleString()}`}
            icon={<MoneyIcon />}
            color="error"
            subtitle={`${balance?.overdue_invoices ?? 0} invoices overdue`}
          />
        </Grid>
      </Grid>
    </Box>
  );
};

const StatCard: React.FC<{
  title: string;
  value: string | number;
  icon: React.ReactNode;
  color: "primary" | "success" | "warning" | "error";
  subtitle?: string;
}> = ({ title, value, icon, color, subtitle }) => (
  <Card sx={{ height: "100%", boxShadow: 1 }}>
    <CardContent>
      <Box sx={{ display: "flex", alignItems: "flex-start", justifyContent: "space-between" }}>
        <Box>
          <Typography variant="caption" color="text.secondary" sx={{ textTransform: "uppercase", fontWeight: 600 }}>
            {title}
          </Typography>
          <Typography variant="h4" sx={{ mt: 0.5, mb: 0.5, fontWeight: 700 }}>
            {value}
          </Typography>
          {subtitle && <Typography variant="caption" color="text.secondary">{subtitle}</Typography>}
        </Box>
        <Box sx={{ p: 1, borderRadius: 2, backgroundColor: `var(--palette-${color}-light)` }}>
          {icon}
        </Box>
      </Box>
    </CardContent>
  Card>
);

/** Patient record custom actions */
const PatientRecordActions: React.FC<{ record: any; onChanged: () => void }> = ({ record, onChanged }) => {
  const [anchorEl, setAnchorEl] = useState<null | HTMLElement>(null);

  const handleMenuOpen = (event: React.MouseEvent<HTMLElement>) => setAnchorEl(event.currentTarget);
  const handleMenuClose = () => { setAnchorEl(null); onChanged(); };

  return (
    <Box sx={{ mt: 2, display: "flex", gap: 1, flexWrap: "wrap" }}>
      <Button variant="outlined" startIcon={<ViewIcon />} onClick={() => window.open(`/r/patients/${record.patient_id}`, "_blank")}>
        View Details
      </Button>
      <Button variant="outlined" startIcon={<CalendarIcon />} onClick={() => window.open(`/r/patients/${record.patient_id}?tab=appointments`, "_blank")}>
        Appointments
      </Button>
      <Button variant="outlined" startIcon={<MedicalIcon />} onClick={() => window.open(`/r/patients/${record.patient_id}?tab=treatments`, "_blank")}>
        Treatments
      </Button>
      <Button variant="outlined" startIcon={<MoneyIcon />} onClick={() => window.open(`/r/patients/${record.patient_id}?tab=invoices`, "_blank")}>
        Invoices
      </Button>
      <Tooltip title="More actions">
        <Button variant="outlined" onClick={handleMenuOpen} endIcon={<AddIcon />}>
          More
        </Button>
      </Tooltip>
      <Menu anchorEl={anchorEl} open={Boolean(anchorEl)} onClose={handleMenuClose}>
        <MenuItem onClick={() => { handleMenuClose(); window.open(`/r/patients/${record.patient_id}?tab=create_appointment`, "_blank"); }}>
          <AddIcon sx={{ mr: 1, fontSize: 18 }} /> New Appointment
        </MenuItem>
        <MenuItem onClick={() => { handleMenuClose(); window.open(`/r/patients/${record.patient_id}?tab=create_treatment`, "_blank"); }}>
          <MedicalIcon sx={{ mr: 1, fontSize: 18 }} /> New Treatment
        </MenuItem>
        <MenuItem onClick={() => { handleMenuClose(); window.open(`/r/patients/${record.patient_id}?tab=create_invoice`, "_blank"); }}>
          <MoneyIcon sx={{ mr: 1, fontSize: 18 }} /> New Invoice
        </MenuItem>
      </Menu>
    </Box>
  );
};

/** Appointment record custom actions */
const AppointmentRecordActions: React.FC<{ record: any; onChanged: () => void }> = ({ record, onChanged }) => {
  const statusColors: Record<string, "success" | "warning" | "error" | "info" | "default"> = {
    scheduled: "info",
    confirmed: "success",
    in_progress: "warning",
    completed: "success",
    cancelled: "error",
    no_show: "error",
  };

  return (
    <Box sx={{ mt: 2, display: "flex", gap: 1, flexWrap: "wrap", alignItems: "center" }}>
      <Chip
        label={record.status}
        color={statusColors[record.status] || "default"}
        size="small"
        icon={<CalendarIcon />}
      />
      {record.status === "completed" && (
        <Button variant="outlined" startIcon={<MedicalIcon />} onClick={() => window.open(`/r/appointments/${record.appointment_id}?tab=treatments`, "_blank")}>
          View Treatments
        </Button>
      )}
      {record.status === "completed" && (
        <Button variant="outlined" startIcon={<MoneyIcon />} onClick={() => window.open(`/r/appointments/${record.appointment_id}?tab=invoices`, "_blank")}>
          View Invoices
        </Button>
      )}
      <Button variant="outlined" startIcon={<EditIcon />} onClick={() => window.open(`/r/appointments/${record.appointment_id}?edit=1`, "_blank")}>
        Edit
      </Button>
    </Box>
  );
};

/** Treatment record custom actions */
const TreatmentRecordActions: React.FC<{ record: any; onChanged: () => void }> = ({ record, onChanged }) => {
  const statusColors: Record<string, "success" | "warning" | "error" | "info" | "default"> = {
    planned: "info",
    in_progress: "warning",
    completed: "success",
    cancelled: "error",
  };

  return (
    <Box sx={{ mt: 2, display: "flex", gap: 1, flexWrap: "wrap", alignItems: "center" }}>
      <Chip
        label={record.status}
        color={statusColors[record.status] || "default"}
        size="small"
        icon={<MedicalIcon />}
      />
      <Chip label={`$${record.fee}`} color="primary" size="small" icon={<MoneyIcon />} variant="outlined" />
      {record.tooth_numbers && <Chip label={`Tooth ${record.tooth_numbers}`} size="small" variant="outlined" />}
      <Button variant="outlined" startIcon={<EditIcon />} onClick={() => window.open(`/r/treatments/${record.treatment_id}?edit=1`, "_blank")}>
        Edit
      </Button>
    </Box>
  );
};

/** Invoice record custom actions */
const InvoiceRecordActions: React.FC<{ record: any; onChanged: () => void }> = ({ record, onChanged }) => {
  const [payDialog, setPayDialog] = useState(false);
  const [payAmount, setPayAmount] = useState("");
  const [payMethod, setPayMethod] = useState("credit_card");
  const [payRef, setPayRef] = useState("");

  const handlePay = async () => {
    if (!payAmount || parseFloat(payAmount) <= 0) return;
    try {
      await api("PUT", `Invoices/${record.invoice_id}`, {
        ...record,
        status: parseFloat(payAmount) >= record.balance_due ? "paid" : "partial",
        amount_paid: record.amount_paid + parseFloat(payAmount),
        balance_due: record.balance_due - parseFloat(payAmount),
        payment_method: payMethod,
        payment_reference: payRef,
        paid_at: new Date().toISOString(),
        modifiedBy: "system",
      });
      setPayDialog(false);
      onChanged();
    } catch (e) {
      console.error("Payment failed:", e);
    }
  };

  const statusColors: Record<string, "success" | "warning" | "error" | "info" | "default"> = {
    draft: "default",
    sent: "info",
    partial: "warning",
    paid: "success",
    overdue: "error",
    cancelled: "error",
  };

  return (
    <Box sx={{ mt: 2, display: "flex", gap: 1, flexWrap: "wrap", alignItems: "center" }}>
      <Chip
        label={record.status}
        color={statusColors[record.status] || "default"}
        size="small"
        icon={<MoneyIcon />}
      />
      <Chip label={`$${record.total_amount}`} color="primary" size="small" variant="outlined" />
      {record.balance_due > 0 && (
        <Chip label={`Due: $${record.balance_due}`} color="error" size="small" variant="outlined" icon={<MoneyIcon />} />
      )}
      {record.balance_due > 0 && (
        <Button variant="contained" startIcon={<MoneyIcon />} onClick={() => setPayDialog(true)}>
          Record Payment
        </Button>
      )}
      <Button variant="outlined" startIcon={<EditIcon />} onClick={() => window.open(`/r/invoices/${record.invoice_id}?edit=1`, "_blank")}>
        Edit
      </Button>

      {/* Payment Dialog */}
      <Dialog open={payDialog} onClose={() => setPayDialog(false)} maxWidth="sm" fullWidth>
        <DialogTitle>Record Payment for {record.invoice_number}</DialogTitle>
        <DialogContent>
          <Box sx={{ display: "flex", flexDirection: "column", gap: 2, mt: 1, minWidth: 300 }}>
            <Typography variant="body2" color="text.secondary">
              Balance Due: <strong>${record.balance_due}</strong>
            </Typography>
            <TextField
              label="Payment Amount"
              type="number"
              value={payAmount}
              onChange={(e) => setPayAmount(e.target.value)}
              inputProps={{ min: 0.01, max: record.balance_due, step: 0.01 }}
              fullWidth
            />
            <FormControl fullWidth>
              <InputLabel>Payment Method</InputLabel>
              <Select value={payMethod} label="Payment Method" onChange={(e) => setPayMethod(e.target.value)}>
                <MenuItem value="cash">Cash</MenuItem>
                <MenuItem value="check">Check</MenuItem>
                <MenuItem value="credit_card">Credit Card</MenuItem>
                <MenuItem value="debit_card">Debit Card</MenuItem>
                <MenuItem value="insurance">Insurance</MenuItem>
                <MenuItem value="financing">Financing</MenuItem>
                <MenuItem value="other">Other</MenuItem>
              </Select>
            </FormControl>
            <TextField
              label="Reference / Check #"
              value={payRef}
              onChange={(e) => setPayRef(e.target.value)}
              fullWidth
              placeholder="Transaction ID, check number, etc."
            />
          </Box>
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setPayDialog(false)}>Cancel</Button>
          <Button variant="contained" onClick={handlePay} disabled={!payAmount || parseFloat(payAmount) <= 0}>
            Record Payment
          </Button>
        </DialogActions>
      </Dialog>
    </Box>
  );
};