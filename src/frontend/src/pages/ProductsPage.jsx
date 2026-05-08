import { useState, useEffect } from 'react';
import { fetchProducts, createProduct, deleteProduct, fetchSellers } from '../services/api';

function formatPrice(val) {
  return Number(val).toLocaleString('tr-TR', { minimumFractionDigits: 2, maximumFractionDigits: 2 });
}

export default function ProductsPage({ realtimePriceUpdates }) {
  const [products, setProducts] = useState([]);
  const [sellers, setSellers] = useState([]);
  const [loading, setLoading] = useState(true);
  const [expandedId, setExpandedId] = useState(null);
  
  // Modal state
  const [isModalOpen, setIsModalOpen] = useState(false);
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [formData, setFormData] = useState({
    name: '', sku: '', sellerId: '', costPrice: '', currentPrice: '', minPrice: '', maxPrice: '', stockQuantity: ''
  });

  useEffect(() => {
    loadData();
    const interval = setInterval(() => loadProducts(false), 20000);
    return () => clearInterval(interval);
  }, []);

  useEffect(() => {
    if (realtimePriceUpdates.length > 0) {
      const latest = realtimePriceUpdates[0];
      setProducts(prev => prev.map(p =>
        p.id === latest.productId ? { ...p, currentPrice: latest.newPrice } : p
      ));
    }
  }, [realtimePriceUpdates]);

  async function loadData() {
    await Promise.all([loadProducts(true), loadSellers()]);
  }

  async function loadProducts(showLoader = false) {
    if (showLoader) setLoading(true);
    try {
      const data = await fetchProducts();
      setProducts(data);
    } catch (err) { console.warn('Products fetch failed:', err); }
    finally { if (showLoader) setLoading(false); }
  }

  async function loadSellers() {
    try {
      const data = await fetchSellers();
      setSellers(data);
      if (data.length > 0) setFormData(prev => ({ ...prev, sellerId: data[0].id }));
    } catch (err) { console.warn('Sellers fetch failed:', err); }
  }

  const handleInputChange = (e) => {
    const { name, value } = e.target;
    setFormData(prev => ({ ...prev, [name]: value }));
  };

  const handleAddProduct = async (e) => {
    e.preventDefault();
    setIsSubmitting(true);
    try {
      const payload = {
        name: formData.name,
        sku: formData.sku,
        sellerId: parseInt(formData.sellerId),
        costPrice: parseFloat(formData.costPrice),
        currentPrice: parseFloat(formData.currentPrice),
        minPrice: parseFloat(formData.minPrice),
        maxPrice: parseFloat(formData.maxPrice),
        stockQuantity: parseInt(formData.stockQuantity)
      };
      const newProduct = await createProduct(payload);
      setProducts(prev => [...prev, newProduct]);
      setIsModalOpen(false);
      // Reset form
      setFormData({
        name: '', sku: '', sellerId: sellers.length > 0 ? sellers[0].id : '', costPrice: '', currentPrice: '', minPrice: '', maxPrice: '', stockQuantity: ''
      });
    } catch (err) {
      console.error('Failed to create product', err);
      alert('Ürün eklenirken bir hata oluştu.');
    } finally {
      setIsSubmitting(false);
    }
  };

  const handleDeleteProduct = async (e, id, name) => {
    e.stopPropagation(); // Prevent row expansion
    if (window.confirm(`"${name}" adlı ürünü silmek istediğinize emin misiniz?`)) {
      try {
        await deleteProduct(id);
        setProducts(prev => prev.filter(p => p.id !== id));
      } catch (err) {
        console.error('Failed to delete product', err);
        alert('Ürün silinirken bir hata oluştu.');
      }
    }
  };

  if (loading) return <div className="animate-in" style={{ textAlign: 'center', padding: '60px', color: 'var(--text-muted)' }}>⏳ Ürünler yükleniyor...</div>;

  return (
    <div className="animate-in">
      <div className="page-header" style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
        <div>
          <h1>Ürünler</h1>
          <p>Tüm ürünlerin fiyat ve rekabet durumu</p>
        </div>
        <button className="btn btn-primary" onClick={() => setIsModalOpen(true)}>
          <span style={{ fontSize: '1.2rem' }}>+</span> Yeni Ürün Ekle
        </button>
      </div>
      <div className="card">
        <div className="table-container">
          <table>
            <thead>
              <tr>
                <th>Ürün</th><th>SKU</th><th>Satıcı</th><th>Maliyet</th>
                <th>Satış Fiyatı</th><th>Kar Marjı</th><th>Stok</th><th>Rakipler</th><th>İşlemler</th>
              </tr>
            </thead>
            <tbody>
              {products.map((p) => {
                const margin = p.profitMargin;
                const isExpanded = expandedId === p.id;
                const marginClass = margin >= 30 ? 'price-up' : margin >= 15 ? 'price-neutral' : 'price-down';
                return (
                  <tr key={p.id} onClick={() => setExpandedId(isExpanded ? null : p.id)} style={{ cursor: 'pointer' }}>
                    <td style={{ fontWeight: 700 }}>{p.name}</td>
                    <td><span className="badge neutral">{p.sku}</span></td>
                    <td>{p.sellerName}</td>
                    <td>{formatPrice(p.costPrice)} ₺</td>
                    <td style={{ fontWeight: 700, color: 'var(--accent-indigo-light)' }}>{formatPrice(p.currentPrice)} ₺</td>
                    <td><span className={marginClass} style={{ fontWeight: 600 }}>%{margin}</span></td>
                    <td><span className={`badge ${p.stockQuantity > 50 ? 'success' : p.stockQuantity > 10 ? 'warning' : 'danger'}`}>{p.stockQuantity}</span></td>
                    <td>{p.competitors?.length || 0} rakip</td>
                    <td>
                      <button className="btn btn-ghost" style={{ color: 'var(--accent-red)', padding: '4px 8px', borderColor: 'transparent' }} 
                              onClick={(e) => handleDeleteProduct(e, p.id, p.name)} title="Ürünü Sil">
                        🗑️
                      </button>
                    </td>
                  </tr>
                );
              })}
            </tbody>
          </table>
        </div>
      </div>

      {/* Modal overlay */}
      {isModalOpen && (
        <div style={{
          position: 'fixed', top: 0, left: 0, right: 0, bottom: 0,
          background: 'rgba(10, 14, 26, 0.8)', backdropFilter: 'blur(4px)',
          display: 'flex', alignItems: 'center', justifyContent: 'center', zIndex: 1000
        }}>
          <div className="card" style={{ width: '100%', maxWidth: '600px', margin: '20px' }}>
            <div className="card-header" style={{ marginBottom: '24px' }}>
              <span className="card-title" style={{ fontSize: '1.2rem' }}>Yeni Ürün Ekle</span>
              <button className="btn btn-ghost" style={{ padding: '4px 8px' }} onClick={() => setIsModalOpen(false)}>✕</button>
            </div>
            
            <form onSubmit={handleAddProduct} style={{ display: 'grid', gap: '16px' }}>
              <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '16px' }}>
                <div>
                  <label style={{ display: 'block', marginBottom: '8px', color: 'var(--text-secondary)' }}>Ürün Adı</label>
                  <input required name="name" value={formData.name} onChange={handleInputChange}
                         style={{ width: '100%', padding: '10px 14px', borderRadius: 'var(--radius-sm)', border: '1px solid var(--border-subtle)', background: 'var(--bg-input)', color: 'white' }} />
                </div>
                <div>
                  <label style={{ display: 'block', marginBottom: '8px', color: 'var(--text-secondary)' }}>SKU</label>
                  <input required name="sku" value={formData.sku} onChange={handleInputChange}
                         style={{ width: '100%', padding: '10px 14px', borderRadius: 'var(--radius-sm)', border: '1px solid var(--border-subtle)', background: 'var(--bg-input)', color: 'white' }} />
                </div>
              </div>
              
              <div>
                <label style={{ display: 'block', marginBottom: '8px', color: 'var(--text-secondary)' }}>Satıcı</label>
                <select required name="sellerId" value={formData.sellerId} onChange={handleInputChange}
                        style={{ width: '100%', padding: '10px 14px', borderRadius: 'var(--radius-sm)', border: '1px solid var(--border-subtle)', background: 'var(--bg-input)', color: 'white' }}>
                  {sellers.map(s => <option key={s.id} value={s.id}>{s.name} (Min %{s.minProfitMargin} Kar)</option>)}
                </select>
              </div>

              <div style={{ display: 'grid', gridTemplateColumns: 'repeat(4, 1fr)', gap: '16px' }}>
                <div>
                  <label style={{ display: 'block', marginBottom: '8px', color: 'var(--text-secondary)', fontSize: '0.85rem' }}>Maliyet (₺)</label>
                  <input required type="number" step="0.01" min="0" name="costPrice" value={formData.costPrice} onChange={handleInputChange}
                         style={{ width: '100%', padding: '10px 10px', borderRadius: 'var(--radius-sm)', border: '1px solid var(--border-subtle)', background: 'var(--bg-input)', color: 'white' }} />
                </div>
                <div>
                  <label style={{ display: 'block', marginBottom: '8px', color: 'var(--text-secondary)', fontSize: '0.85rem' }}>Satış Fiyatı (₺)</label>
                  <input required type="number" step="0.01" min="0" name="currentPrice" value={formData.currentPrice} onChange={handleInputChange}
                         style={{ width: '100%', padding: '10px 10px', borderRadius: 'var(--radius-sm)', border: '1px solid var(--border-subtle)', background: 'var(--bg-input)', color: 'white' }} />
                </div>
                <div>
                  <label style={{ display: 'block', marginBottom: '8px', color: 'var(--text-secondary)', fontSize: '0.85rem' }}>Min Fiyat (₺)</label>
                  <input required type="number" step="0.01" min="0" name="minPrice" value={formData.minPrice} onChange={handleInputChange}
                         style={{ width: '100%', padding: '10px 10px', borderRadius: 'var(--radius-sm)', border: '1px solid var(--border-subtle)', background: 'var(--bg-input)', color: 'white' }} />
                </div>
                <div>
                  <label style={{ display: 'block', marginBottom: '8px', color: 'var(--text-secondary)', fontSize: '0.85rem' }}>Maks Fiyat (₺)</label>
                  <input required type="number" step="0.01" min="0" name="maxPrice" value={formData.maxPrice} onChange={handleInputChange}
                         style={{ width: '100%', padding: '10px 10px', borderRadius: 'var(--radius-sm)', border: '1px solid var(--border-subtle)', background: 'var(--bg-input)', color: 'white' }} />
                </div>
              </div>
              
              <div>
                <label style={{ display: 'block', marginBottom: '8px', color: 'var(--text-secondary)' }}>Stok Miktarı</label>
                <input required type="number" min="0" name="stockQuantity" value={formData.stockQuantity} onChange={handleInputChange}
                       style={{ width: '100%', padding: '10px 14px', borderRadius: 'var(--radius-sm)', border: '1px solid var(--border-subtle)', background: 'var(--bg-input)', color: 'white' }} />
              </div>

              <div style={{ display: 'flex', justifyContent: 'flex-end', gap: '12px', marginTop: '16px' }}>
                <button type="button" className="btn btn-ghost" onClick={() => setIsModalOpen(false)}>İptal</button>
                <button type="submit" className="btn btn-primary" disabled={isSubmitting}>
                  {isSubmitting ? 'Ekleniyor...' : 'Kaydet'}
                </button>
              </div>
            </form>
          </div>
        </div>
      )}
    </div>
  );
}
