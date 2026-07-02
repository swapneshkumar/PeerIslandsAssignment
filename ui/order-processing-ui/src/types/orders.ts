export type OrderStatus = 'Pending' | 'Processing' | 'Shipped' | 'Delivered' | 'Cancelled';

export type ApiResponse<T> = {
  success: boolean;
  message: string;
  data: T;
  errors: string[];
  traceId: string;
  timestamp: string;
};

export type PagedResult<T> = {
  items: T[];
  pageNumber: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
};

export type AddressDto = {
  line1: string;
  line2?: string | null;
  city: string;
  state: string;
  postalCode: string;
  country: string;
};

export type CreateOrderItemRequest = {
  productId: string;
  productSku: string;
  productName: string;
  quantity: number;
  unitPrice: number;
  currency: string;
};

export type CreateOrderRequest = {
  customerId: string;
  shippingAddress: AddressDto;
  items: CreateOrderItemRequest[];
};

export type OrderItemResponse = {
  id: string;
  productId: string;
  productSku: string;
  productName: string;
  quantity: number;
  unitPrice: number;
  lineTotal: number;
  currency: string;
};

export type OrderStatusHistoryResponse = {
  fromStatus: OrderStatus;
  toStatus: OrderStatus;
  reason: string;
  changedBy: string;
  changedAt: string;
};

export type OrderResponse = {
  id: string;
  orderNumber: string;
  customerId: string;
  status: OrderStatus;
  totalAmount: number;
  currency: string;
  shippingAddress: AddressDto;
  createdAt: string;
  items: OrderItemResponse[];
  statusHistory: OrderStatusHistoryResponse[];
};
