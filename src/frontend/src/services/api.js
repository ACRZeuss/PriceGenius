const API_BASE = '/api';

export async function fetchDashboardSummary() {
  const res = await fetch(`${API_BASE}/dashboard/summary`);
  if (!res.ok) throw new Error('Dashboard summary fetch failed');
  return res.json();
}

export async function fetchDecisions(take = 20) {
  const res = await fetch(`${API_BASE}/dashboard/decisions?take=${take}`);
  if (!res.ok) throw new Error('Decisions fetch failed');
  return res.json();
}

export async function fetchPriceHistory(take = 30) {
  const res = await fetch(`${API_BASE}/dashboard/price-history?take=${take}`);
  if (!res.ok) throw new Error('Price history fetch failed');
  return res.json();
}

export async function fetchProducts() {
  const res = await fetch(`${API_BASE}/products`);
  if (!res.ok) throw new Error('Products fetch failed');
  return res.json();
}

export async function fetchProductHistory(productId) {
  const res = await fetch(`${API_BASE}/products/${productId}/history`);
  if (!res.ok) throw new Error('Product history fetch failed');
  return res.json();
}

export async function createProduct(productData) {
  const res = await fetch(`${API_BASE}/products`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(productData),
  });
  if (!res.ok) throw new Error('Product creation failed');
  return res.json();
}

export async function deleteProduct(productId) {
  const res = await fetch(`${API_BASE}/products/${productId}`, {
    method: 'DELETE',
  });
  if (!res.ok) throw new Error('Product deletion failed');
  return true;
}

export async function fetchSellers() {
  const res = await fetch(`${API_BASE}/sellers`);
  if (!res.ok) throw new Error('Sellers fetch failed');
  return res.json();
}
