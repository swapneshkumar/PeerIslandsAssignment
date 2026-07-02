import {
  Alert,
  AppBar,
  Box,
  Button,
  Chip,
  Container,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  Divider,
  Drawer,
  FormControl,
  Grid,
  IconButton,
  InputAdornment,
  InputLabel,
  LinearProgress,
  MenuItem,
  Paper,
  Select,
  Stack,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TablePagination,
  TableRow,
  TextField,
  Toolbar,
  Tooltip,
  Typography,
} from '@mui/material';
import { alpha } from '@mui/material/styles';
import { useEffect, useMemo, useState } from 'react';
import type { ReactNode } from 'react';
import {
  Activity,
  AlertCircle,
  Ban,
  BarChart3,
  CheckCircle2,
  ClipboardList,
  Clock3,
  Database,
  Eye,
  Gauge,
  Layers3,
  LockKeyhole,
  PackageCheck,
  Plus,
  RefreshCcw,
  ServerCog,
  Search,
  Settings,
  Truck,
  Workflow
} from 'lucide-react';
import { createOrdersApi } from './api/ordersApi';
import { StatusChip } from './components/StatusChip';
import type { CreateOrderRequest, OrderResponse, OrderStatus, PagedResult } from './types/orders';

const statuses: Array<OrderStatus | 'All'> = ['All', 'Pending', 'Processing', 'Shipped', 'Delivered', 'Cancelled'];

const dockerDevAdminToken =
  'eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJPcmRlclByb2Nlc3NpbmciLCJhdWQiOiJPcmRlclByb2Nlc3NpbmcuQ2xpZW50Iiwic3ViIjoidWktdGVzdGVyIiwibmFtZSI6InVpLXRlc3RlciIsInJvbGUiOiJBZG1pbiIsImlhdCI6MTc4MzAxNTI4NiwibmJmIjoxNzgzMDE1Mjg2LCJleHAiOjE4OTM0NTYwMDB9.xzhd5Royo5kNdSMeS0jCxucwMhgcsZVcGB5heRg9NDQ';

const nextStatuses: Partial<Record<OrderStatus, OrderStatus[]>> = {
  Pending: ['Processing'],
  Processing: ['Shipped'],
  Shipped: ['Delivered']
};

const emptyPage: PagedResult<OrderResponse> = {
  items: [],
  pageNumber: 1,
  pageSize: 10,
  totalCount: 0,
  totalPages: 0
};

const initialCreateOrder: CreateOrderRequest = {
  customerId: '00000000-0000-0000-0000-000000000001',
  shippingAddress: {
    line1: '100 Market Street',
    line2: '',
    city: 'San Francisco',
    state: 'CA',
    postalCode: '94105',
    country: 'US'
  },
  items: [
    {
      productId: '00000000-0000-0000-0000-000000000010',
      productSku: 'SKU-LAPTOP-001',
      productName: 'Enterprise Laptop',
      quantity: 1,
      unitPrice: 1299.99,
      currency: 'USD'
    }
  ]
};

export default function App() {
  const [apiBaseUrl, setApiBaseUrl] = useState('/api');
  const [jwt, setJwt] = useState(dockerDevAdminToken);
  const [status, setStatus] = useState<OrderStatus | 'All'>('All');
  const [customerId, setCustomerId] = useState('');
  const [page, setPage] = useState(0);
  const [rowsPerPage, setRowsPerPage] = useState(10);
  const [orders, setOrders] = useState<PagedResult<OrderResponse>>(emptyPage);
  const [selectedOrder, setSelectedOrder] = useState<OrderResponse | null>(null);
  const [createOpen, setCreateOpen] = useState(false);
  const [settingsOpen, setSettingsOpen] = useState(false);
  const [createOrder, setCreateOrder] = useState<CreateOrderRequest>(initialCreateOrder);
  const [actionReason, setActionReason] = useState('Updated from operations console.');
  const [busy, setBusy] = useState(false);
  const [notice, setNotice] = useState<{ type: 'success' | 'error' | 'info'; message: string } | null>({
    type: 'info',
    message: 'Dev console connected with an Admin JWT for local Docker testing.'
  });

  const api = useMemo(() => createOrdersApi({ baseUrl: apiBaseUrl, token: jwt }), [apiBaseUrl, jwt]);

  useEffect(() => {
    loadOrders(0, rowsPerPage);
    // Initial load only; manual Search/Refresh handles later filter changes.
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  const metrics = useMemo(() => {
    const values = orders.items;
    return {
      total: orders.totalCount,
      pending: values.filter((order) => order.status === 'Pending').length,
      processing: values.filter((order) => order.status === 'Processing').length,
      revenue: values.reduce((sum, order) => sum + order.totalAmount, 0)
    };
  }, [orders]);

  async function loadOrders(nextPage = page, nextSize = rowsPerPage) {
    setBusy(true);
    setNotice(null);
    try {
      const data = await api.getOrders({
        pageNumber: nextPage + 1,
        pageSize: nextSize,
        status,
        customerId: customerId.trim() || undefined
      });
      setOrders(data);
      setPage(nextPage);
    } catch (error) {
      setNotice({ type: 'error', message: getErrorMessage(error) });
    } finally {
      setBusy(false);
    }
  }

  async function openOrderDetails(orderId: string) {
    setBusy(true);
    setNotice(null);
    try {
      const data = await api.getOrder(orderId);
      setSelectedOrder(data);
    } catch (error) {
      setNotice({ type: 'error', message: getErrorMessage(error) });
    } finally {
      setBusy(false);
    }
  }

  async function submitCreateOrder() {
    setBusy(true);
    setNotice(null);
    try {
      const data = await api.createOrder(createOrder);
      setCreateOpen(false);
      setSelectedOrder(data);
      setNotice({ type: 'success', message: `Order ${data.orderNumber} created.` });
      await loadOrders(0, rowsPerPage);
    } catch (error) {
      setNotice({ type: 'error', message: getErrorMessage(error) });
    } finally {
      setBusy(false);
    }
  }

  async function updateStatus(order: OrderResponse, nextStatus: OrderStatus) {
    setBusy(true);
    setNotice(null);
    try {
      const updated = await api.updateStatus(order.id, nextStatus, actionReason);
      setSelectedOrder(updated);
      setNotice({ type: 'success', message: `Order moved to ${nextStatus}.` });
      await loadOrders(page, rowsPerPage);
    } catch (error) {
      setNotice({ type: 'error', message: getErrorMessage(error) });
    } finally {
      setBusy(false);
    }
  }

  async function cancelOrder(order: OrderResponse) {
    setBusy(true);
    setNotice(null);
    try {
      await api.cancelOrder(order.id, actionReason || 'Cancelled from operations console.');
      setSelectedOrder(null);
      setNotice({ type: 'success', message: 'Order cancelled.' });
      await loadOrders(page, rowsPerPage);
    } catch (error) {
      setNotice({ type: 'error', message: getErrorMessage(error) });
    } finally {
      setBusy(false);
    }
  }

  return (
    <Box sx={{ minHeight: '100vh', bgcolor: 'background.default', display: 'flex' }}>
      <Box
        component="aside"
        sx={{
          display: { xs: 'none', md: 'flex' },
          width: 76,
          minHeight: '100vh',
          position: 'sticky',
          top: 0,
          flexDirection: 'column',
          alignItems: 'center',
          gap: 1.5,
          py: 2,
          bgcolor: '#0f172a',
          borderRight: '1px solid rgba(255,255,255,0.08)'
        }}
      >
        <Box sx={{ width: 44, height: 44, borderRadius: 2, display: 'grid', placeItems: 'center', bgcolor: '#2563eb', color: 'white' }}>
          <PackageCheck size={23} />
        </Box>
        <NavIcon active icon={<Gauge size={20} />} label="Dashboard" />
        <NavIcon icon={<ClipboardList size={20} />} label="Orders" />
        <NavIcon icon={<Workflow size={20} />} label="Workflow" />
        <NavIcon icon={<Database size={20} />} label="Storage" />
        <Box sx={{ flex: 1 }} />
        <NavIcon icon={<Settings size={20} />} label="Settings" onClick={() => setSettingsOpen(true)} />
      </Box>

      <Box sx={{ flex: 1, minWidth: 0 }}>
        <AppBar position="sticky" elevation={0} sx={{ bgcolor: 'rgba(255,255,255,0.92)', color: 'text.primary', backdropFilter: 'blur(14px)', borderBottom: '1px solid', borderColor: 'divider' }}>
          <Toolbar sx={{ gap: 2, minHeight: 70 }}>
          <Box sx={{ flex: 1, minWidth: 0 }}>
            <Stack direction="row" spacing={1} alignItems="center" sx={{ mb: 0.3 }}>
              <Chip size="small" label="OrderOps" sx={{ bgcolor: '#dbeafe', color: '#1d4ed8', fontWeight: 900 }} />
              <Chip size="small" icon={<Activity size={14} />} label="Live API" color="success" variant="outlined" />
              <Chip size="small" icon={<LockKeyhole size={14} />} label="Admin JWT" color="primary" variant="outlined" />
            </Stack>
            <Typography variant="h2" sx={{ fontSize: 20 }}>
              Order Operations Console
            </Typography>
            <Typography sx={{ color: 'text.secondary', fontSize: 13 }}>
              Enterprise order lifecycle testing, fulfillment controls, and audit visibility
            </Typography>
          </Box>
          <Tooltip title="API settings">
            <IconButton onClick={() => setSettingsOpen(true)}>
              <Settings size={20} />
            </IconButton>
          </Tooltip>
          <Button variant="outlined" startIcon={<RefreshCcw size={16} />} onClick={() => loadOrders()} disabled={busy}>
            Refresh
          </Button>
          <Button aria-label="New order" variant="contained" startIcon={<Plus size={16} />} onClick={() => setCreateOpen(true)}>
            New Order
          </Button>
        </Toolbar>
        {busy && <LinearProgress color="secondary" />}
      </AppBar>

      <Container maxWidth="xl" sx={{ py: 3 }}>
        <Paper
          variant="outlined"
          sx={{
            mb: 2,
            overflow: 'hidden',
            borderColor: 'rgba(37,99,235,0.18)',
            bgcolor: '#ffffff'
          }}
        >
          <Box
            sx={{
              p: { xs: 2, md: 2.5 },
              display: 'grid',
              gap: 2,
              gridTemplateColumns: { xs: '1fr', lg: '1.4fr 1fr' },
              alignItems: 'center'
            }}
          >
            <Box>
              <Typography variant="h1" sx={{ fontSize: { xs: 24, md: 30 }, mb: 0.6 }}>
                Fulfillment Command Center
              </Typography>
              <Typography color="text.secondary" sx={{ maxWidth: 760 }}>
                Track orders from placement to delivery, validate status transitions, and inspect audit history against the live .NET API.
              </Typography>
            </Box>
            <Stack direction={{ xs: 'column', sm: 'row' }} spacing={1}>
              <SystemPill icon={<ServerCog size={17} />} label="API" value="localhost:8080" />
              <SystemPill icon={<Database size={17} />} label="PostgreSQL" value="Healthy" />
              <SystemPill icon={<Layers3 size={17} />} label="Redis" value="Cache ready" />
            </Stack>
          </Box>
        </Paper>

        {notice && (
          <Alert severity={notice.type} icon={notice.type === 'success' ? <CheckCircle2 size={18} /> : <AlertCircle size={18} />} sx={{ mb: 2 }}>
            {notice.message}
          </Alert>
        )}

        <Grid container spacing={2} sx={{ mb: 2 }}>
          <Metric icon={<ClipboardList size={20} />} label="Total orders" value={String(metrics.total)} color="#2563eb" caption="Across active filters" />
          <Metric icon={<Clock3 size={20} />} label="Pending" value={String(metrics.pending)} color="#b45309" caption="Awaiting processing" />
          <Metric icon={<Truck size={20} />} label="Processing" value={String(metrics.processing)} color="#0f766e" caption="In fulfillment flow" />
          <Metric icon={<BarChart3 size={20} />} label="Page value" value={formatMoney(metrics.revenue, 'USD')} color="#7c3aed" caption="Visible order value" />
        </Grid>

        <Paper variant="outlined" sx={{ mb: 2, p: 2, borderColor: 'rgba(15,23,42,0.12)' }}>
          <Stack direction={{ xs: 'column', md: 'row' }} spacing={2} alignItems={{ xs: 'stretch', md: 'center' }}>
            <TextField
              label="Customer Id"
              value={customerId}
              onChange={(event) => setCustomerId(event.target.value)}
              size="small"
              fullWidth
              InputProps={{
                startAdornment: (
                  <InputAdornment position="start">
                    <Search size={16} />
                  </InputAdornment>
                )
              }}
            />
            <FormControl size="small" sx={{ minWidth: 180 }}>
              <InputLabel>Status</InputLabel>
              <Select label="Status" value={status} onChange={(event) => setStatus(event.target.value as OrderStatus | 'All')}>
                {statuses.map((item) => (
                  <MenuItem value={item} key={item}>
                    {item}
                  </MenuItem>
                ))}
              </Select>
            </FormControl>
            <Button variant="contained" startIcon={<Search size={16} />} onClick={() => loadOrders(0, rowsPerPage)} disabled={busy}>
              Search
            </Button>
          </Stack>
        </Paper>

        <TableContainer component={Paper} variant="outlined" sx={{ borderColor: 'rgba(15,23,42,0.12)', boxShadow: '0 18px 55px rgba(15, 23, 42, 0.08)' }}>
          <Table sx={{ minWidth: 920 }}>
            <TableHead>
              <TableRow>
                <TableCell>Order</TableCell>
                <TableCell>Customer</TableCell>
                <TableCell>Status</TableCell>
                <TableCell>Items</TableCell>
                <TableCell align="right">Total</TableCell>
                <TableCell>Created</TableCell>
                <TableCell align="right">Actions</TableCell>
              </TableRow>
            </TableHead>
            <TableBody>
              {orders.items.length === 0 && (
                <TableRow>
                  <TableCell colSpan={7}>
                    <Box sx={{ py: 7, textAlign: 'center' }}>
                      <Typography variant="h3">No orders loaded</Typography>
                      <Typography color="text.secondary" sx={{ mt: 1 }}>
                        Add API settings and press Search or create a new order.
                      </Typography>
                    </Box>
                  </TableCell>
                </TableRow>
              )}
              {orders.items.map((order) => (
                <TableRow key={order.id} hover sx={{ '&:hover': { bgcolor: 'rgba(37,99,235,0.035)' } }}>
                  <TableCell>
                    <Stack direction="row" alignItems="center" spacing={1}>
                      <Box sx={{ width: 32, height: 32, display: 'grid', placeItems: 'center', borderRadius: 1.5, bgcolor: alpha('#2563eb', 0.1), color: '#2563eb' }}>
                        <PackageCheck size={17} />
                      </Box>
                      <Box sx={{ minWidth: 0 }}>
                        <Typography fontWeight={850}>{order.orderNumber}</Typography>
                        <Typography color="text.secondary" fontSize={12} noWrap>
                          {order.id}
                        </Typography>
                      </Box>
                    </Stack>
                  </TableCell>
                  <TableCell sx={{ maxWidth: 220 }}>
                    <Typography noWrap>{order.customerId}</Typography>
                  </TableCell>
                  <TableCell>
                    <StatusChip status={order.status} />
                  </TableCell>
                  <TableCell>{order.items.length}</TableCell>
                  <TableCell align="right">{formatMoney(order.totalAmount, order.currency)}</TableCell>
                  <TableCell>{formatDate(order.createdAt)}</TableCell>
                  <TableCell align="right">
                    <Tooltip title="View details">
                      <IconButton aria-label="View details" onClick={() => openOrderDetails(order.id)} sx={{ border: '1px solid', borderColor: 'divider' }}>
                        <Eye size={18} />
                      </IconButton>
                    </Tooltip>
                  </TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>
          <TablePagination
            component="div"
            count={orders.totalCount}
            page={page}
            rowsPerPage={rowsPerPage}
            rowsPerPageOptions={[10, 20, 50]}
            onPageChange={(_, nextPage) => loadOrders(nextPage, rowsPerPage)}
            onRowsPerPageChange={(event) => {
              const nextSize = Number(event.target.value);
              setRowsPerPage(nextSize);
              loadOrders(0, nextSize);
            }}
          />
        </TableContainer>
      </Container>
      </Box>

      <OrderDrawer
        order={selectedOrder}
        reason={actionReason}
        setReason={setActionReason}
        onClose={() => setSelectedOrder(null)}
        onUpdate={updateStatus}
        onCancel={cancelOrder}
      />

      <CreateOrderDialog
        open={createOpen}
        order={createOrder}
        setOrder={setCreateOrder}
        busy={busy}
        onClose={() => setCreateOpen(false)}
        onSubmit={submitCreateOrder}
      />

      <Dialog open={settingsOpen} onClose={() => setSettingsOpen(false)} fullWidth maxWidth="sm">
        <DialogTitle>API Settings</DialogTitle>
        <DialogContent>
          <Stack spacing={2} sx={{ pt: 1 }}>
            <TextField label="API base URL" value={apiBaseUrl} onChange={(event) => setApiBaseUrl(event.target.value)} />
            <TextField
              label="JWT bearer token"
              value={jwt}
              onChange={(event) => setJwt(event.target.value)}
              multiline
              minRows={4}
              placeholder="Paste a token with Customer, Admin, or Manager role"
            />
          </Stack>
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setSettingsOpen(false)}>Done</Button>
        </DialogActions>
      </Dialog>
    </Box>
  );
}

function NavIcon({ icon, label, active, onClick }: { icon: ReactNode; label: string; active?: boolean; onClick?: () => void }) {
  return (
    <Tooltip title={label} placement="right">
      <IconButton
        onClick={onClick}
        sx={{
          width: 44,
          height: 44,
          color: active ? 'white' : 'rgba(255,255,255,0.58)',
          bgcolor: active ? 'rgba(37,99,235,0.95)' : 'transparent',
          border: '1px solid',
          borderColor: active ? 'rgba(147,197,253,0.45)' : 'transparent',
          '&:hover': {
            bgcolor: active ? 'rgba(37,99,235,0.95)' : 'rgba(255,255,255,0.08)',
            color: 'white'
          }
        }}
      >
        {icon}
      </IconButton>
    </Tooltip>
  );
}

function SystemPill({ icon, label, value }: { icon: ReactNode; label: string; value: string }) {
  return (
    <Paper
      variant="outlined"
      sx={{
        p: 1.25,
        flex: 1,
        minWidth: 0,
        bgcolor: '#f8fafc',
        borderColor: 'rgba(37,99,235,0.14)'
      }}
    >
      <Stack direction="row" spacing={1} alignItems="center">
        <Box sx={{ width: 32, height: 32, borderRadius: 1.5, display: 'grid', placeItems: 'center', color: '#2563eb', bgcolor: 'rgba(37,99,235,0.08)' }}>
          {icon}
        </Box>
        <Box sx={{ minWidth: 0 }}>
          <Typography fontSize={11} fontWeight={900} color="text.secondary" sx={{ textTransform: 'uppercase' }}>
            {label}
          </Typography>
          <Typography fontSize={13} fontWeight={850} noWrap>
            {value}
          </Typography>
        </Box>
      </Stack>
    </Paper>
  );
}

function Metric({ icon, label, value, color, caption }: { icon: ReactNode; label: string; value: string; color: string; caption: string }) {
  return (
    <Grid item xs={12} sm={6} lg={3}>
      <Paper
        variant="outlined"
        sx={{
          p: 2,
          height: 124,
          position: 'relative',
          overflow: 'hidden',
          borderColor: alpha(color, 0.18),
          boxShadow: '0 14px 40px rgba(15, 23, 42, 0.06)',
          '&:before': {
            content: '""',
            position: 'absolute',
            inset: '0 auto 0 0',
            width: 4,
            bgcolor: color
          }
        }}
      >
        <Stack direction="row" spacing={1.5} alignItems="center" sx={{ height: '100%' }}>
          <Box sx={{ width: 42, height: 42, borderRadius: 2, display: 'grid', placeItems: 'center', color, bgcolor: alpha(color, 0.1) }}>
            {icon}
          </Box>
          <Box sx={{ minWidth: 0 }}>
            <Typography color="text.secondary" fontSize={13} fontWeight={700}>
              {label}
            </Typography>
            <Typography variant="h2" sx={{ mt: 0.5 }}>
              {value}
            </Typography>
            <Typography color="text.secondary" fontSize={12} sx={{ mt: 0.2 }}>
              {caption}
            </Typography>
          </Box>
        </Stack>
      </Paper>
    </Grid>
  );
}

function OrderDrawer({
  order,
  reason,
  setReason,
  onClose,
  onUpdate,
  onCancel
}: {
  order: OrderResponse | null;
  reason: string;
  setReason: (value: string) => void;
  onClose: () => void;
  onUpdate: (order: OrderResponse, status: OrderStatus) => void;
  onCancel: (order: OrderResponse) => void;
}) {
  return (
    <Drawer
      anchor="right"
      open={Boolean(order)}
      onClose={onClose}
      PaperProps={{ sx: { width: { xs: '100%', sm: 560 } }, 'data-testid': 'order-detail-drawer' } as never}
    >
      {order && (
        <Box sx={{ p: 3 }}>
          <Stack direction="row" alignItems="flex-start" justifyContent="space-between" spacing={2}>
            <Box>
              <Typography variant="h2">{order.orderNumber}</Typography>
              <Typography color="text.secondary" sx={{ mt: 0.5 }}>
                {order.id}
              </Typography>
            </Box>
            <StatusChip status={order.status} />
          </Stack>

          <Divider sx={{ my: 2 }} />

          <Grid container spacing={2}>
            <Grid item xs={6}>
              <Typography color="text.secondary" fontSize={13}>Total</Typography>
              <Typography fontWeight={800}>{formatMoney(order.totalAmount, order.currency)}</Typography>
            </Grid>
            <Grid item xs={6}>
              <Typography color="text.secondary" fontSize={13}>Created</Typography>
              <Typography fontWeight={800}>{formatDate(order.createdAt)}</Typography>
            </Grid>
          </Grid>

          <Typography variant="h3" sx={{ mt: 3, mb: 1 }}>Items</Typography>
          <Stack spacing={1}>
            {order.items.map((item) => (
              <Paper key={item.id} variant="outlined" sx={{ p: 1.5 }}>
                <Stack direction="row" justifyContent="space-between" spacing={2}>
                  <Box sx={{ minWidth: 0 }}>
                    <Typography fontWeight={800}>{item.productName}</Typography>
                    <Typography color="text.secondary" fontSize={13}>{item.productSku} · Qty {item.quantity}</Typography>
                  </Box>
                  <Typography fontWeight={800}>{formatMoney(item.lineTotal, item.currency)}</Typography>
                </Stack>
              </Paper>
            ))}
          </Stack>

          <Typography variant="h3" sx={{ mt: 3, mb: 1 }}>Status Controls</Typography>
          <TextField
            label="Reason"
            value={reason}
            onChange={(event) => setReason(event.target.value)}
            fullWidth
            size="small"
            sx={{ mb: 1.5 }}
          />
          <Stack direction="row" spacing={1} flexWrap="wrap" useFlexGap>
            {(nextStatuses[order.status] || []).map((status) => (
              <Button key={status} variant="contained" onClick={() => onUpdate(order, status)}>
                Move to {status}
              </Button>
            ))}
            {order.status === 'Pending' && (
              <Button color="error" variant="outlined" startIcon={<Ban size={16} />} onClick={() => onCancel(order)}>
                Cancel
              </Button>
            )}
          </Stack>

          <Typography variant="h3" sx={{ mt: 3, mb: 1 }}>History</Typography>
          <Stack spacing={1}>
            {order.statusHistory.map((history, index) => (
              <Paper key={`${history.changedAt}-${index}`} variant="outlined" sx={{ p: 1.5 }}>
                <Stack direction="row" justifyContent="space-between" spacing={2}>
                  <Box>
                    <Typography fontWeight={800}>{history.fromStatus} to {history.toStatus}</Typography>
                    <Typography color="text.secondary" fontSize={13}>{history.reason}</Typography>
                  </Box>
                  <Typography color="text.secondary" fontSize={12}>{formatDate(history.changedAt)}</Typography>
                </Stack>
              </Paper>
            ))}
          </Stack>
        </Box>
      )}
    </Drawer>
  );
}

function CreateOrderDialog({
  open,
  order,
  setOrder,
  busy,
  onClose,
  onSubmit
}: {
  open: boolean;
  order: CreateOrderRequest;
  setOrder: (order: CreateOrderRequest) => void;
  busy: boolean;
  onClose: () => void;
  onSubmit: () => void;
}) {
  const item = order.items[0];

  function updateAddress(key: keyof CreateOrderRequest['shippingAddress'], value: string) {
    setOrder({ ...order, shippingAddress: { ...order.shippingAddress, [key]: value } });
  }

  function updateItem(key: keyof typeof item, value: string | number) {
    setOrder({ ...order, items: [{ ...item, [key]: value }] });
  }

  return (
    <Dialog open={open} onClose={onClose} fullWidth maxWidth="md" PaperProps={{ 'data-testid': 'create-order-dialog' } as never}>
      <DialogTitle>Create Test Order</DialogTitle>
      <DialogContent>
        <Grid container spacing={2} sx={{ pt: 1 }}>
          <Grid item xs={12}>
            <TextField label="Customer Id" value={order.customerId} onChange={(event) => setOrder({ ...order, customerId: event.target.value })} fullWidth />
          </Grid>
          <Grid item xs={12} md={8}>
            <TextField label="Address line 1" value={order.shippingAddress.line1} onChange={(event) => updateAddress('line1', event.target.value)} fullWidth />
          </Grid>
          <Grid item xs={12} md={4}>
            <TextField label="Address line 2" value={order.shippingAddress.line2 || ''} onChange={(event) => updateAddress('line2', event.target.value)} fullWidth />
          </Grid>
          <Grid item xs={12} md={3}>
            <TextField label="City" value={order.shippingAddress.city} onChange={(event) => updateAddress('city', event.target.value)} fullWidth />
          </Grid>
          <Grid item xs={12} md={3}>
            <TextField label="State" value={order.shippingAddress.state} onChange={(event) => updateAddress('state', event.target.value)} fullWidth />
          </Grid>
          <Grid item xs={12} md={3}>
            <TextField label="Postal code" value={order.shippingAddress.postalCode} onChange={(event) => updateAddress('postalCode', event.target.value)} fullWidth />
          </Grid>
          <Grid item xs={12} md={3}>
            <TextField label="Country" value={order.shippingAddress.country} onChange={(event) => updateAddress('country', event.target.value)} fullWidth />
          </Grid>
          <Grid item xs={12}>
            <Divider sx={{ my: 1 }} />
          </Grid>
          <Grid item xs={12} md={4}>
            <TextField label="Product Id" value={item.productId} onChange={(event) => updateItem('productId', event.target.value)} fullWidth />
          </Grid>
          <Grid item xs={12} md={3}>
            <TextField label="SKU" value={item.productSku} onChange={(event) => updateItem('productSku', event.target.value)} fullWidth />
          </Grid>
          <Grid item xs={12} md={5}>
            <TextField label="Product name" value={item.productName} onChange={(event) => updateItem('productName', event.target.value)} fullWidth />
          </Grid>
          <Grid item xs={12} md={4}>
            <TextField label="Quantity" type="number" value={item.quantity} onChange={(event) => updateItem('quantity', Number(event.target.value))} fullWidth />
          </Grid>
          <Grid item xs={12} md={4}>
            <TextField label="Unit price" type="number" value={item.unitPrice} onChange={(event) => updateItem('unitPrice', Number(event.target.value))} fullWidth />
          </Grid>
          <Grid item xs={12} md={4}>
            <TextField label="Currency" value={item.currency} onChange={(event) => updateItem('currency', event.target.value.toUpperCase())} fullWidth />
          </Grid>
        </Grid>
      </DialogContent>
      <DialogActions>
        <Button onClick={onClose}>Cancel</Button>
        <Button variant="contained" startIcon={<Plus size={16} />} onClick={onSubmit} disabled={busy}>
          Create
        </Button>
      </DialogActions>
    </Dialog>
  );
}

function getErrorMessage(error: unknown) {
  if (error instanceof Error) {
    return error.message;
  }

  return 'Unexpected UI error.';
}

function formatMoney(amount: number, currency: string) {
  return new Intl.NumberFormat('en-US', { style: 'currency', currency }).format(amount || 0);
}

function formatDate(value: string) {
  return new Intl.DateTimeFormat('en-US', {
    month: 'short',
    day: '2-digit',
    hour: '2-digit',
    minute: '2-digit'
  }).format(new Date(value));
}
