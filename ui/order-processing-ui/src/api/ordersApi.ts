import type { ApiResponse, CreateOrderRequest, OrderResponse, OrderStatus, PagedResult } from '../types/orders';

const defaultApiBaseUrl = '/api';

export type OrdersQuery = {
  pageNumber: number;
  pageSize: number;
  status?: OrderStatus | 'All';
  customerId?: string;
};

export class ApiError extends Error {
  constructor(
    message: string,
    public readonly status: number,
    public readonly errors: string[] = []
  ) {
    super(message);
  }
}

export type ApiClientOptions = {
  baseUrl: string;
  token: string;
};

export function createOrdersApi(options: Partial<ApiClientOptions>) {
  const baseUrl = options.baseUrl || defaultApiBaseUrl;

  async function request<T>(path: string, init?: RequestInit): Promise<T> {
    const headers = new Headers(init?.headers);
    headers.set('Accept', 'application/json');

    if (init?.body) {
      headers.set('Content-Type', 'application/json');
    }

    if (options.token) {
      headers.set('Authorization', `Bearer ${options.token}`);
    }

    const response = await fetch(`${baseUrl}${path}`, { ...init, headers });
    if (!response.ok) {
      const problem = await readJsonSafely<{ title?: string; detail?: string; errors?: string[] }>(response);
      throw new ApiError(
        problem?.detail || problem?.title || `Request failed with status ${response.status}`,
        response.status,
        problem?.errors || []
      );
    }

    if (response.status === 204) {
      return undefined as T;
    }

    const payload = await response.json() as ApiResponse<T>;
    if (!payload.success) {
      throw new ApiError(payload.message, response.status, payload.errors);
    }

    return payload.data;
  }

  return {
    getOrders(query: OrdersQuery) {
      const search = new URLSearchParams({
        pageNumber: String(query.pageNumber),
        pageSize: String(query.pageSize)
      });

      if (query.status && query.status !== 'All') {
        search.set('status', query.status);
      }

      if (query.customerId) {
        search.set('customerId', query.customerId);
      }

      return request<PagedResult<OrderResponse>>(`/orders?${search.toString()}`);
    },
    getOrder(id: string) {
      return request<OrderResponse>(`/orders/${id}`);
    },
    createOrder(payload: CreateOrderRequest) {
      return request<OrderResponse>('/orders', {
        method: 'POST',
        body: JSON.stringify(payload)
      });
    },
    updateStatus(id: string, status: OrderStatus, reason: string) {
      return request<OrderResponse>(`/orders/${id}/status`, {
        method: 'PATCH',
        body: JSON.stringify({ status, reason })
      });
    },
    cancelOrder(id: string, reason: string) {
      return request<void>(`/orders/${id}`, {
        method: 'DELETE',
        body: JSON.stringify({ reason })
      });
    }
  };
}

async function readJsonSafely<T>(response: Response): Promise<T | null> {
  try {
    return await response.json() as T;
  } catch {
    return null;
  }
}
