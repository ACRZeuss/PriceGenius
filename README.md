# PriceGenius ⚡

**PriceGenius**, e-ticaret satıcıları için geliştirilmiş, mikroservis mimarisine sahip **otonom ve yapay zeka destekli bir fiyatlandırma (repricing) platformudur**. 

Sistem, piyasadaki rakip fiyatlarını ve stok durumlarını sürekli olarak (gerçek zamanlı) tarar. Bir değişiklik tespit ettiğinde, bu verileri RabbitMQ üzerinden Python tabanlı yapay zeka ajanı olan "PriceEngine"e gönderir. AI Ajanı (Google Gemini destekli), satıcının kar marjını koruyacak şekilde piyasadaki en optimum stratejiyi (örneğin: *opportunistic*, *undercut*, *premium*, *match*, *hold*) belirler ve ürün fiyatını otomatik olarak günceller. Tüm bu akış, SignalR üzerinden saniyesinde React tabanlı modern bir Dashboard'a canlı log olarak düşer.

## 🌟 Öne Çıkan Özellikler

- **Otonom Piyasa Taraması:** MockCompetitorService ile rakiplerin stok ve fiyatlarındaki değişimlerin düzenli simülasyonu.
- **Yapay Zeka Karar Motoru (AI Agent):** Google Gemini modellerini kullanarak bağlamsal fiyat stratejileri geliştirme.
- **Kesintisiz İletişim:** RabbitMQ ile servisler arası dayanıklı (durable) ve asenkron (async) mesaj kuyruğu altyapısı.
- **Gerçek Zamanlı UI (Real-time Dashboard):** C# SignalR ve React entegrasyonu ile sayfayı yenilemeye gerek kalmadan anlık fiyat değişikliklerini, yapay zeka kararlarını ve sistem loglarını izleme.
- **Modern ve Premium Tasarım:** Glassmorphism tarzı, koyu tema (dark mode) odaklı, hızlı ve duyarlı (responsive) Vite + React arayüzü.
- **Güvenlik (Safety Fallback):** Yapay zekanın hata yapması veya API sınırına takılması durumunda devreye giren "Fallback" kural tabanlı stratejiler ve minimum kar marjı koruması.

## 🏗️ Mimari ve Teknoloji Yığını

Proje, birbirinden bağımsız çalışan ve RabbitMQ üzerinden haberleşen mikroservis parçalarından oluşur:

1. **Frontend (Dashboard):** `React`, `Vite`, `SignalR Client`, `Vanilla CSS` (Port: 5173)
2. **Backend API (Core):** `C# .NET 8 Web API`, `Entity Framework Core`, `SignalR` (Port: 5000)
3. **AI Worker (PriceEngine):** `Python 3`, `pika` (RabbitMQ Client), `google-generativeai`
4. **Veritabanı:** `PostgreSQL` (Docker)
5. **Message Broker:** `RabbitMQ` (Docker)

---

## 🚀 Kurulum ve Çalıştırma Rehberi

Sistemi lokal ortamınızda çalıştırmak için aşağıdaki adımları sırasıyla uygulayın.

### Gereksinimler
- Docker ve Docker Compose
- .NET 8 SDK
- Node.js (v18+) ve npm
- Python 3.10+
- (Opsiyonel) Google Gemini API Key

### 1. Altyapıyı Ayağa Kaldırma (Docker)
Öncelikle veritabanı (PostgreSQL) ve Message Broker (RabbitMQ) servislerini başlatın.

```bash
cd PriceGenius
docker-compose up -d
```
*(Bu adım, `5432` portunda veritabanını, `5672` ve `15672` portlarında RabbitMQ'yu başlatır.)*

### 2. Backend API (.NET 8)
C# Core API'sini başlatarak piyasa tarama servislerini ve WebSocket (SignalR) bağlantısını aktif edin.

```bash
cd src/PriceGenius.API
dotnet restore
dotnet run
```
*(API, `http://localhost:5000` adresinde çalışacaktır. Swagger dokümantasyonu için `http://localhost:5000/swagger` adresine gidebilirsiniz.)*

### 3. AI Agent (Python)
Yapay zeka motorunu başlatın. Eğer Gemini API Key'iniz yoksa sistem **kural tabanlı otomatik Fallback stratejisi** ile simülasyona devam edecektir.

```bash
cd src/ai-agent
# Windows için sanal ortam (venv) kurulumu:
python -m venv .venv
.\.venv\Scripts\Activate

# Bağımlılıkları yükleyin
pip install -r requirements.txt

# Çevre değişkenlerini ayarlayın (Opsiyonel: .env dosyasına GEMINI_API_KEY ekleyin)
cp .env.example .env

# Worker servisini başlatın
python main.py
```

### 4. Frontend Dashboard (React)
Kullanıcı arayüzünü başlatın.

```bash
cd src/frontend
npm install
npm run dev
```
*(Uygulama `http://localhost:5173` adresinde çalışacaktır.)*

---

## 🧠 Nasıl Çalışır? (Sistem Akışı)

1. **Tarama (MarketScanner):** C# tarafındaki `MarketScannerService` her 30 saniyede bir veritabanındaki ürünleri kontrol eder.
2. **Simülasyon (MockCompetitor):** Rakiplerin stok ve fiyatlarında yapay oynamalar yapılır. Eğer bir değişim tespit edilirse, `market_analysis_queue` adlı RabbitMQ kuyruğuna mesaj bırakılır.
3. **Analiz (AI Agent):** Python servisi bu kuyruğu okur. Eski fiyat, rakip durumu ve maliyet verilerini yapay zekaya (Gemini) göndererek bir karar ister.
4. **Karar (PriceUpdate):** Yapay zeka kararı `price_update_queue` kuyruğuna atılır.
5. **Uygulama ve Canlı Bildirim:** C# API bu kararı okur. Sistemin belirlediği % minimum kar marjı sınırlarına uyuyorsa veritabanını günceller. Aynı anda SignalR üzerinden React arayüzüne bildirim gönderir. Canlı log ekranında anında görünür!
