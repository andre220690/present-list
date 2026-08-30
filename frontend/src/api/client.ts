export type PublicGift = {
  id: string;
  name: string;
  imageUrl: string;
  isReserved: boolean;
  isReservedByCurrentVisitor: boolean;
};

export type GiftDetails = PublicGift & {
  description?: string | null;
  productUrl: string;
};

export type AdminGift = Omit<GiftDetails, 'isReservedByCurrentVisitor'> & {
  reservedByName?: string | null;
  createdAt: string;
};

export type ApiError = {
  code: string;
  message: string;
};

async function request<T>(url: string, init?: RequestInit): Promise<T> {
  const response = await fetch(url, {
    credentials: 'include',
    headers: init?.body instanceof FormData ? undefined : { 'Content-Type': 'application/json' },
    ...init
  });

  if (!response.ok) {
    let error: ApiError = { code: 'request_failed', message: 'Не удалось выполнить запрос.' };
    try {
      error = await response.json();
    } catch {
      if (response.status === 401) error.message = 'Нужно войти как администратор.';
      if (response.status === 403) error.message = 'Недостаточно прав для этого действия.';
    }
    throw error;
  }

  if (response.status === 204) {
    return undefined as T;
  }

  const text = await response.text();
  if (!text) {
    return undefined as T;
  }

  return JSON.parse(text);
}

export const api = {
  getGifts: () => request<PublicGift[]>('/api/gifts'),
  getGift: (id: string) => request<GiftDetails>(`/api/gifts/${id}`),
  reserveGift: (id: string, name: string) =>
    request<void>(`/api/gifts/${id}/reservations`, {
      method: 'POST',
      body: JSON.stringify({ name })
    }),
  cancelReservation: (id: string) =>
    request<void>(`/api/gifts/${id}/reservation`, { method: 'DELETE' }),
  login: (username: string, password: string) =>
    request<{ username: string }>('/api/admin/login', {
      method: 'POST',
      body: JSON.stringify({ username, password })
    }),
  logout: () => request<void>('/api/admin/logout', { method: 'POST' }),
  session: () => request<{ username: string }>('/api/admin/session'),
  adminGifts: () => request<AdminGift[]>('/api/admin/gifts'),
  addGift: (form: FormData) =>
    request<AdminGift>('/api/admin/gifts', { method: 'POST', body: form }),
  deleteGift: (id: string) =>
    request<void>(`/api/admin/gifts/${id}`, { method: 'DELETE' }),
  clearReservation: (id: string) =>
    request<void>(`/api/admin/gifts/${id}/reservation`, { method: 'DELETE' })
};
