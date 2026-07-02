import { Box, Chip } from '@mui/material';
import { alpha, useTheme } from '@mui/material/styles';
import type { OrderStatus } from '../types/orders';

const colors: Record<OrderStatus, string> = {
  Pending: '#b45309',
  Processing: '#2563eb',
  Shipped: '#7c3aed',
  Delivered: '#15803d',
  Cancelled: '#b91c1c'
};

export function StatusChip({ status }: { status: OrderStatus }) {
  const theme = useTheme();
  const color = colors[status];

  return (
    <Chip
      size="small"
      label={status}
      icon={<Box sx={{ width: 8, height: 8, borderRadius: '50%', bgcolor: color }} />}
      sx={{
        height: 28,
        borderColor: alpha(color, 0.28),
        bgcolor: alpha(color, 0.08),
        color,
        fontWeight: 800,
        '& .MuiChip-icon': {
          ml: 1,
          color: theme.palette.common.white
        }
      }}
      variant="outlined"
    />
  );
}
