# Docker ile Sistem Kurulumu

## 🚀 Hızlı Başlangıç

### 1. Sistemi Başlatma

```powershell
docker-compose up -d
```

Bu komut:
- PostgreSQL veritabanını başlatır
- Tüm backend servislerini (Identity, Reservation, Turnstile, Feedback, API Gateway) başlatır
- Frontend Angular uygulamasını başlatır
- **Veritabanını otomatik olarak başlangıç verileriyle doldurur**

### 2. Servislere Erişim

- **Frontend**: http://localhost:4200
- **API Gateway**: http://localhost:5010
- **Identity Service**: http://localhost:5001
- **Reservation Service**: http://localhost:5002
- **Turnstile Service**: http://localhost:5003
- **Feedback Service**: http://localhost:5004
- **PostgreSQL**: localhost:5432

### 3. Varsayılan Kullanıcılar

#### Admin Kullanıcısı
- **Kullanıcı Adı**: admin
- **Şifre**: Admin123!

#### Test Öğrencileri
- **123456** - Test Öğrenci (Şifre: Student123!)
- **12345** - Sadettin (Şifre: Student123!)
- **777** - Dr (Doktora - Şifre: Student123!)
- **1111** - Ceza testi (Şifre: Student123!)

### 4. Başlangıç Verileri

Sistem ilk başlatıldığında otomatik olarak şu veriler yüklenir:
- ✅ Kullanıcı hesapları ve roller
- ✅ Öğrenci profilleri
- ✅ Kütüphane masaları (30 masa, 3 kat)
- ✅ Örnek rezervasyonlar

## 🔄 Sistemi Yeniden Başlatma

### Servisleri Durdurma
```powershell
docker-compose down
```

### Servisleri Yeniden Başlatma (veriler korunur)
```powershell
docker-compose up -d
```

### Tamamen Temiz Kurulum (tüm verileri sil)
```powershell
docker-compose down -v
docker-compose up -d
```

⚠️ **Dikkat**: `-v` parametresi tüm veritabanı verilerini siler ve sistem başlangıç verileriyle yeniden kurulur.

### 💾 Veritabanı Yedeği Alma ve Geri Yükleme

Yeni eklenen verilerinizi korumak için:

#### Yedek Alma
```powershell
# Çalışan sistemden yedek alın
docker exec library_postgres pg_dump -U postgres -d LibraryReservation > my_backup_$(date +%Y%m%d_%H%M%S).sql
```

#### Yedek Geri Yükleme
```powershell
# Sistemi durdurun
docker-compose down

# Yedeği geri yükleyin
docker exec -i library_postgres psql -U postgres -d LibraryReservation < my_backup_20251213_143000.sql

# Sistemi yeniden başlatın
docker-compose up -d
```

#### Örnek Kullanım
```powershell
# Yedek alma
docker exec library_postgres pg_dump -U postgres -d LibraryReservation > library_backup.sql

# Sistemi yeniden başlatma sonrası geri yükleme
docker-compose down
docker exec -i library_postgres psql -U postgres -d LibraryReservation < library_backup.sql
docker-compose up -d
```

## 📊 Logları Görüntüleme

### Tüm servislerin logları
```powershell
docker-compose logs -f
```

### Belirli bir servisin logları
```powershell
docker logs identity_service -f
docker logs reservation_service -f
docker logs library_postgres -f
```

### Veritabanı başlatma logları
```powershell
docker logs library_db_init
```

## 🔍 Veritabanı Kontrolü

### PostgreSQL'e bağlanma
```powershell
docker exec -it library_postgres psql -U postgres -d LibraryReservation
```

### Hızlı sorgular
```powershell
# Kullanıcıları listele
docker exec library_postgres psql -U postgres -d LibraryReservation -c 'SELECT "UserName", "Email" FROM "AspNetUsers";'

# Rezervasyonları listele
docker exec library_postgres psql -U postgres -d LibraryReservation -c 'SELECT * FROM "Reservations";'

# Masaları listele
docker exec library_postgres psql -U postgres -d LibraryReservation -c 'SELECT * FROM "Tables";'
```

## 🛠️ Sorun Giderme

### Problem: Servisler başlamıyor
```powershell
# Container'ların durumunu kontrol et
docker-compose ps

# Hatalı servislerin loglarını kontrol et
docker-compose logs
```

### Problem: Veritabanı bağlantısı yok
```powershell
# PostgreSQL'in sağlık durumunu kontrol et
docker exec library_postgres pg_isready -U postgres

# PostgreSQL loglarını kontrol et
docker logs library_postgres
```

### Problem: Veriler yüklenmemiş
```powershell
# DB init container'ının başarılı çalıştığını kontrol et
docker logs library_db_init

# Yeniden yükle
docker-compose down -v
docker-compose up -d
```

## 📝 Yapılan Değişiklikler

Bu Docker kurulumu şunları içerir:

1. **Otomatik Veritabanı Migration**: Servisler başladığında migration'lar otomatik çalışır
2. **Otomatik Veri Yükleme**: `db-init` container'ı migration'lardan sonra başlangıç verilerini yükler
3. **Sıralı Başlatma**: Servisler doğru sırayla başlar (PostgreSQL → Services → Init → Frontend)
4. **Kalıcı Veri**: Volume'ler sayesinde veriler container'lar yeniden başlatılsa bile korunur

## 🌐 Üretim Ortamına Geçiş

Üretim ortamında kullanmadan önce:
- [ ] PostgreSQL şifresini değiştir
- [ ] Admin şifresini değiştir
- [ ] JWT secret key'lerini güncelle
- [ ] HTTPS/SSL sertifikaları ekle
- [ ] Environment değişkenlerini production'a çevir
