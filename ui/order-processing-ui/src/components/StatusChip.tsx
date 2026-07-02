import { Chip } from '@mui/material';
import type { ChipProps } from '@mui/material/Chip';
import type { OrderStatus } from '../types/orders';

const colors: Record<OrderStatus, ChipProps['color']> = {
  Pending: 'warning',
  Processing: 'info',
  Shipped: 'secondary',
  Delivered: 'success',
  Cancelled: 'error'
};

export function StatusChip({ status }: { status: OrderStatus }) {
  return <Chip size="small" color={colors[status]} label={status} variant="outlined" />;
}
