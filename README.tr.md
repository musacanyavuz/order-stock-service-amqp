# E-Ticaret Mikroservis Mimarisi Case Study

![.NET](https://img.shields.io/badge/.NET-10.0-512BD4?style=flat&logo=dotnet)
![RabbitMQ](https://img.shields.io/badge/RabbitMQ-3.13-FF6600?style=flat&logo=rabbitmq)
![PostgreSQL](https://img.shields.io/badge/PostgreSQL-16-336791?style=flat&logo=postgresql)
![Docker](https://img.shields.io/badge/Docker-Compose-2496ED?style=flat&logo=docker)

.NET Core ile geliştirilmiş, dağıtık sistemlerdeki karmaşık tutarlılık ve güvenilirlik sorunlarına "Senior" seviyesinde çözümler sunan, production-grade bir e-ticaret altyapı çalışmasıdır.

## 🚀 Özellikler ve Mimari Desenler

*   **Mikroservis Mimarisi**: `Sipariş`, `Stok` ve `Bildirim` süreçleri için ayrıştırılmış, bağımsız servisler.
*   **Olay Güdümlü (Event-Driven) İletişim**: **RabbitMQ** ve **MassTransit** kütüphanesi ile asenkron haberleşme.
*   **Veri Tutarlılığı (Data Consistency)**:
    *   **Outbox Pattern**: "Dual Write" problemini çözen, veritabanı ve mesaj kuyruğu atomisitesi.
    *   **Optimistic Concurrency**: "Overselling" (Stoktan fazla satış) riskini engelleyen versiyonlama (RowVersion) stratejisi.
    *   **Idempotency**: Tekrarlayan mesajların (Duplicate Messages) sistemi bozmasını engelleyen filtreler.
    *   **Otomatik Global Loglama**: MassTransit Filtreleri (`MongoLogPublishFilter`, `MongoLogConsumeFilter`) sayesinde akan her mesaj otomatik loglanır, manuel loglama hatası ortadan kalkar.
    *   **Retry Policy (Yeniden Deneme)**: Stok çakışmalarını (Optimistic Concurrency) yönetmek için Stock.API üzerinde "Exponential Backoff" stratejisi.
    *   **Gelişmiş İzlenebilirlik (Observability)**: **Grafana**, **Prometheus** ve **OpenTelemetry** ile tam sistem görünürlüğü. Dashboard; dağıtık izleme, RabbitMQ Backpressure takibi ve canlı iş metriklerini içerir.

## 🛠 Teknoloji Yığını

*   **Backend**: .NET 10.0 Web API
*   **Mesaj Kuyruğu**: RabbitMQ (MassTransit Abstraction layer ile)
*   **Veritabanı**: PostgreSQL (Entity Framework Core), MongoDB (Loglar)
*   **İzleme**: Prometheus, Grafana, OpenTelemetry
*   **Konteyner**: Docker & Docker Compose
*   **Test**: xUnit, Moq (Unit ve Entegrasyon Testleri)

## 🏃 Kurulum ve Çalıştırma

### Gereksinimler
*   [Docker Desktop](https://www.docker.com/products/docker-desktop)
*   [.NET 10.0 SDK](https://dotnet.microsoft.com/download) (Lokal geliştirme ve debug için)

### Projeyi Ayağa Kaldırma (Kolay Yöntem)

Proje, tüm altyapıyı ve servisleri tek komutla başlatmak için bir script içerir. Port çakışmalarını önlemek için önce temizlik yapılması önerilir.

1.  **Repoyu Klonlayın**
    ```bash
    git clone https://github.com/kullanici-adiniz/beymen-case-study.git
    cd beymen-case-study
    ```

2.  **Başlatma Scriptini Çalıştırın**
    Terminal üzerinden şu komutu girin:
    ```bash
    chmod +x run_services.sh kill_ports.sh
    ./kill_ports.sh && ./run_services.sh
    ```
    *Bu script şunları yapar:*
    *   Docker konteynerlerini (Postgres, Mongo, RabbitMQ) kaldırır.
    *   Order, Stock ve Notification API'lerini başlatır.
    *   React Client uygulamasını başlatır.

3.  **Uygulamaya Gidin**
    *   Tarayıcınızda **[http://localhost:5173](http://localhost:5173)** adresine gidin.

### 🔌 Uç Noktalar (Endpoints)

| Servis | Port | Swagger UI | Açıklama |
| :--- | :--- | :--- | :--- |
| **Order API** | `5001` | [http://localhost:5001/swagger](http://localhost:5001/swagger) | Sipariş oluşturma (POST `/api/orders`). |
| **Stock API** | `5002` | [http://localhost:5002/swagger](http://localhost:5002/swagger) | Stok işlemleri (Consumer ağırlıklı). |
| **Notification API** | `5003` | [http://localhost:5003/swagger](http://localhost:5003/swagger) | SignalR bildirimleri. |
| **Client App** | `5173` | [http://localhost:5173](http://localhost:5173) | Manuel test arayüzü. |
| **Grafana** | `3000` | [http://localhost:3000](http://localhost:3000) | Sistem Paneli (Kullanıcı: admin / Şifre: admin). |
| **RabbitMQ Mgmt** | `15672` | [http://localhost:15672](http://localhost:15672) | Kuyruk Yönetimi (Kullanıcı: guest / Şifre: guest). |
| **Prometheus** | `9091` | [http://localhost:9091](http://localhost:9091) | Ham Metrikler. |

### 4. Manuel Başlatma (Alternatif)
Script kullanmak istemezseniz:
    ```bash
    docker-compose up -d
    dotnet run --project src/Order.API --urls "http://localhost:5001"
    dotnet run --project src/Stock.API --urls "http://localhost:5002"
    dotnet run --project src/Notification.API --urls "http://localhost:5003"
    cd src/client && npm run dev
    ```

### 5. İstemci Uygulaması (Client - Manuel Test)
Projeyle birlikte, işlemleri manuel olarak yönetmek ve test etmek için hazırlanmış bir ön yüz (frontend) uygulaması bulunur:
```bash
cd src/client
npm install
npm run dev
```
> **Not**: Client uygulaması, API'ler ile manuel etkileşime girmek, kullanıcı davranışlarını ve uç durumları (edge cases) simüle etmek amacıyla tasarlanmıştır.

### 🧪 Testleri Çalıştırma
Sistem mantığını ve kritik stok tutarlılık kurallarını doğrulamak için:
```bash
dotnet test
```

## 🗺 Yol Haritası (Roadmap)

- [x] **Temel Servisler**: Order, Stock, Notification API implementasyonları.
- [x] **Güvenilirlik**: Outbox, Idempotency ve Retry mekanizmaları.
- [x] **Test**: Stok rezervasyon mantığı için Unit Testler.
- [ ] **API Gateway**: Ocelot veya YARP ile tek bir giriş noktası sağlanması.
- [ ] **Identity Server**: Merkezi kimlik doğrulama ve yetkilendirme.
- [x] **İzleme (Monitoring)**: Prometheus ve Grafana entegrasyonu.

## 📊 İzleme ve Gözlemlenebilirlik (Yeni)

Proje, "Senior Developer" seviyesinde bir izleme ortamı sunar:

1.  **İş Metrikleri**: Anlık `Toplam Sipariş` sayısı ve hata oranları.
2.  **Mimari Akış**: Bir isteğin yolculuğunun görselleştirilmesi: `Order API (Producer)` -> `RabbitMQ (Kuyruk)` -> `Stock/Notification (Consumer)`.
3.  **Backpressure İzleme**: RabbitMQ üzerindeki `Throughput (Giriş/Çıkış)` hızlarını takip ederek, yük altındaki performans ve kuyruk derinliği (`Queue Depth`) analizi.
4.  **Performans**: Tüm servisler için Latency (P95) takibi.

**Dashboard Erişimi:** [http://localhost:3000](http://localhost:3000) -> *Dashboards* -> *Beymen Senior Case Study*

## 🤖 AI Katkıda Bulunanlar

*   **ChatGPT**
*   **Antigravity**
*   **Cursor**

## 📄 Lisans
Bu proje MIT lisansı altında açık kaynaklanmıştır.
