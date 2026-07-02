import {
  Alert,
  AppBar,
  Box,
  Button,
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
  AlertCircle,
  Ban,
  CheckCircle2,
  ClipboardList,
  Clock3,
  Eye,
  PackageCheck,
  Plus,
  RefreshCcw,
  Search,
  Settings,
  ShieldCheck,
  Truck
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
    message: 'Connect the API and add a JWT token to begin testing protected order endpoints.'
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
    <Box sx={{ minHeight: '100vh', bgcolor: 'background.default' }}>
      <AppBar position="sticky" elevation={0} sx={{ bgcolor: '#111827', borderBottom: '1px solid rgba(255,255,255,0.12)' }}>
        <Toolbar sx={{ gap: 2, minHeight: 68 }}>
          <Box sx={{ width: 40, height: 40, borderRadius: 2, display: 'grid', placeItems: 'center', bgcolor: '#2563eb' }}>
            <PackageCheck size={22} />
          </Box>
          <Box sx={{ flex: 1, minWidth: 0 }}>
            <Typography variant="h2" sx={{ color: 'white', fontSize: 20 }}>
              Order Operations Console
            </Typography>
            <Typography sx={{ color: 'rgba(255,255,255,0.68)', fontSize: 13 }}>
              Enterprise order testing workspace
            </Typography>
          </Box>
          <Tooltip title="API settings">
            <IconButton color="inherit" onClick={() => setSettingsOpen(true)}>
              <Settings size={20} />
            </IconButton>
          </Tooltip>
          <Button color="inherit" variant="outlined" startIcon={<RefreshCcw size={16} />} onClick={() => loadOrders()} disabled={busy}>
            Refresh
          </Button>
          <Button variant="contained" startIcon={<Plus size={16} />} onClick={() => setCreateOpen(true)}>
            New Order
          </Button>
        </Toolbar>
        {busy && <LinearProgress color="secondary" />}
      </AppBar>

      <Container maxWidth="xl" sx={{ py: 3 }}>
        {notice && (
          <Alert severity={notice.type} icon={notice.type === 'success' ? <CheckCircle2 size={18} /> : <AlertCircle size={18} />} sx={{ mb: 2 }}>
            {notice.message}
          </Alert>
        )}

        <Grid container spacing={2} sx={{ mb: 2 }}>
          <Metric icon={<ClipboardList size={20} />} label="Orders" value={String(metrics.total)} color="#1d4ed8" />
          <Metric icon={<Clock3 size={20} />} label="Pending on page" value={String(metrics.pending)} color="#b45309" />
          <Metric icon={<Truck size={20} />} label="Processing on page" value={String(metrics.processing)} color="#0f766e" />
          <Metric icon={<ShieldCheck size={20} />} label="Page value" value={formatMoney(metrics.revenue, 'USD')} color="#6d28d9" />
        </Grid>

        <Paper variant="outlined" sx={{ mb: 2, p: 2 }}>
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

        <TableContainer component={Paper} variant="outlined">
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
                <TableRow key={order.id} hover>
                  <TableCell>
                    <Typography fontWeight={800}>{order.orderNumber}</Typography>
                    <Typography color="text.secondary" fontSize={13}>
                      {order.id}
                    </Typography>
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
                      <IconButton onClick={() => openOrderDetails(order.id)}>
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

function Metric({ icon, label, value, color }: { icon: ReactNode; label: string; value: string; color: string }) {
  return (
    <Grid item xs={12} sm={6} lg={3}>
      <Paper variant="outlined" sx={{ p: 2, height: 112 }}>
        <Stack direction="row" spacing={1.5} alignItems="center">
          <Box sx={{ width: 38, height: 38, borderRadius: 2, display: 'grid', placeItems: 'center', color, bgcolor: alpha(color, 0.1) }}>
            {icon}
          </Box>
          <Box sx={{ minWidth: 0 }}>
            <Typography color="text.secondary" fontSize={13} fontWeight={700}>
              {label}
            </Typography>
            <Typography variant="h2" sx={{ mt: 0.5 }}>
              {value}
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
    <Drawer anchor="right" open={Boolean(order)} onClose={onClose} PaperProps={{ sx: { width: { xs: '100%', sm: 560 } } }}>
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
    <Dialog open={open} onClose={onClose} fullWidth maxWidth="md">
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
