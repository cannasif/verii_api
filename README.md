# V3RII API

V3RII ana web sitesi, ürün destek asistanı ve destek operasyonları için .NET 8 backend çekirdeği.

## Proje Yapısı

Repo adı: `verii_api`

Kod, CRM ekosistemindeki okunabilirliğe yakın olacak şekilde feature bazlı gruplanır:

```text
Modules/Identity/Api
Modules/Identity/Application/Dtos
Modules/Identity/Application/Services
Modules/Identity/Domain/Entities

Modules/SupportTickets/Api
Modules/SupportTickets/Application/Dtos
Modules/SupportTickets/Application/Services
Modules/SupportTickets/Domain/Entities

Modules/Knowledge
Modules/Chat
Modules/Analytics
Modules/MailOutbox
Modules/System

Shared/Common
Shared/Domain
Infrastructure
Data
Migrations
```

CRM projesindeki gibi root'ta tek `verii-api.csproj` ve tek `verii_api.sln` vardır.

## İçerik

- JWT login ve claim tabanlı permission sistemi
- Hangfire dashboard ve background job altyapısı
- Chatbot destek talebi oluşturma endpointi
- Ticket numarası, durum ve öncelik yönetimi
- Canlı destek/handoff kuyruğu alanları
- Yönetilebilir bilgi tabanı endpointleri
- Bilgi tabanı tabanlı RAG cevap endpointi
- Chatbot analitik eventleri ve özet raporu
- SMTP destekli mail outbox kuyruğu ve retry
- FluentValidation ile backend validasyon
- EF Core SQL Server migration

## Başlangıç

Development ayarları:

- API: `verii_api`
- DB: `V3RII_Dev`
- Swagger: `/swagger`
- Hangfire: `/hangfire`
- Seed admin: `admin@v3rii.com`
- Seed admin parola: `V3riiAdmin!2026`

```bash
dotnet restore verii_api.sln
dotnet build verii_api.sln
dotnet ef database update --project verii-api.csproj --startup-project verii-api.csproj
dotnet run --project verii-api.csproj
```

## Production Network Ayarı

Production `appsettings.json` varsayılan olarak dış yüzeyi kapalı getirir:

- Swagger kapalı: `NetworkSecurity:EnableSwagger=false`
- Hangfire dashboard kapalı: `NetworkSecurity:EnableHangfireDashboard=false`
- CORS sadece `https://v3rii.com` ve `https://www.v3rii.com`
- `AllowedHosts`: `v3rii.com;www.v3rii.com`
- Startup auto migration kapalı: `Database:AutoMigrate=false`

Jenkins veya sunucu ortamında değerleri environment variable ile geç:

```bash
ConnectionStrings__DefaultConnection="Server=...;Database=V3RII;User Id=...;Password=...;TrustServerCertificate=True"
Jwt__Secret="prod-uzun-random-secret"
Cors__AllowedOrigins__0="https://v3rii.com"
Cors__AllowedOrigins__1="https://www.v3rii.com"
NetworkSecurity__EnableHangfireDashboard="false"
NetworkSecurity__AdminIpAllowList__0="JENKINS_OR_OFFICE_IP"
```

## Migration

Oluşturulan migrationlar:

- `InitialV3RiiSchema`
- `AddSupportOperationsAndRag`
- `SeedB2BAndUtsKnowledge`

Uygulama komutu:

```bash
dotnet ef database update --project verii-api.csproj --startup-project verii-api.csproj
```

Jenkinsfile içinde `APPLY_MIGRATIONS=true` verilirse publish öncesi aynı komut çalışır.

## Ana Endpointler

- `POST /api/auth/login`
- `GET /api/auth/me`
- `GET /api/knowledge?product=Wms&query=netsis`
- `POST /api/knowledge`
- `POST /api/support/tickets`
- `GET /api/support/tickets`
- `GET /api/support/tickets/dashboard`
- `PATCH /api/support/tickets/{id}/status`
- `POST /api/chat/answer`
- `POST /api/analytics/events`
- `GET /api/analytics/summary`
- `GET /api/users`
- `POST /api/users`

## Sonraki Faz

- Canlı destek sağlayıcısı için handoff adapter ekle.
- Ürün dokümanlarını bilgi tabanına import eden job ekle.
- Bilgi tabanı doküman importu sonrası embedding/vector store katmanını ekle.
