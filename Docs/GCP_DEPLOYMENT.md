# Deploy Fap backend + SQL Server lên Google Cloud

Tài liệu này đưa ra 2 hướng triển khai phổ biến:

- **Option A (Khuyến nghị):** API chạy trên **Cloud Run**, DB dùng **Cloud SQL for SQL Server**.
- **Option B (Dễ nhất nếu muốn “SQL Server container”):** Chạy **Compute Engine VM** và dùng **Docker Compose** để chạy cả API + SQL Server.

> Lưu ý: `appsettings.json` trong repo hiện có **rất nhiều secrets** (JWT key, email password, Pinata, Cloudinary…). Khi deploy thật, **đừng commit secrets**; hãy dùng biến môi trường / Secret Manager.

---

## Option A (Khuyến nghị): Cloud Run + Cloud SQL (SQL Server)

### Sơ đồ nhanh
- **Cloud Run** chạy container từ `Dockerfile`.
- **Cloud SQL for SQL Server** làm DB managed.
- Cloud Run kết nối Cloud SQL qua 1 trong 2 cách:
  - **(Khuyến nghị)** Private IP + **Serverless VPC Access Connector** (an toàn, không public DB)
  - Public IP + Authorized networks (nhanh để test, kém an toàn hơn)

> Với SQL Server trên Cloud SQL, bạn sẽ dùng **địa chỉ IP của instance** (private/public) trong connection string.
> Cloud Run hiện không “gắn Cloud SQL instance” theo cơ chế unix-socket như MySQL/Postgres; vì vậy bạn kết nối bằng TCP và kiểm soát network bằng VPC/authorized networks.

### 1) Tạo Cloud SQL for SQL Server
1. Vào **Google Cloud Console → SQL → Create instance → SQL Server**.
2. Chọn region gần nhất.
3. Đặt root/sa password.
4. Tạo database `FapDb`.

Khuyến nghị:
- Bật **Private IP**.
- Tắt/không dùng public IP nếu không cần.
- Nếu cần public IP giai đoạn đầu, chỉ allow IP dev của bạn trong **Authorized networks**.

### 2) Kết nối Cloud Run ↔ Cloud SQL (Private IP - khuyến nghị)
1. Tạo **VPC Network** (nếu chưa có) và tạo **Serverless VPC Access Connector** cùng region với Cloud Run.
2. Đảm bảo Cloud SQL instance có **Private IP** trong cùng VPC.
3. Trong Cloud Run service:
   - VPC connector: chọn connector ở bước (1)
   - Egress: **All traffic** (để truy cập private IP)
4. Mở firewall rule nội bộ (nếu cần) cho TCP:1433 giữa connector range và Cloud SQL.

> Nếu bạn muốn triển khai nhanh để demo: dùng Public IP + Authorized networks. Nhưng production nên quay về Private IP.

### 3) Build & push container image (Artifact Registry + Cloud Build)
1. Bật API:
   - Cloud Run API
   - Cloud Build API
   - Artifact Registry API
   - Cloud SQL Admin API
2. Tạo Artifact Registry repo (Docker) (ví dụ: `uap-backend`).
3. Build image bằng Cloud Build từ source repo.

Bạn có thể làm trong Cloud Shell hoặc local (gcloud). Các bước/commands cụ thể tuỳ vào pipeline của bạn.

### 4) Deploy Cloud Run
Trong Cloud Run:
- Set **Container port**: `8080` (Dockerfile đã expose).
- Set env var (khuyến nghị dùng Secret Manager để inject):
  - `ConnectionStrings__DefaultConnection`
  - các secrets khác.

#### Template connection string
- Nếu dùng **Private IP** của Cloud SQL (khuyến nghị):
  - `Server=<CLOUD_SQL_PRIVATE_IP>,1433;Database=FapDb;User Id=<sql_user>;Password=<password>;TrustServerCertificate=True;Encrypt=False;`
- Nếu dùng **Public IP** (dev/demo):
  - `Server=<CLOUD_SQL_PUBLIC_IP>,1433;Database=FapDb;User Id=<sql_user>;Password=<password>;TrustServerCertificate=True;Encrypt=False;`

> Lưu ý: `Encrypt=False` đang phù hợp demo. Production nên cân nhắc bật TLS và cấu hình certificate phù hợp.

#### Map cấu hình hiện tại sang env var
Ứng dụng ASP.NET tự map `Section:Key` sang env var kiểu `Section__Key`.
Ví dụ:
- `Jwt:Key` → `Jwt__Key`
- `EmailSettings:Password` → `EmailSettings__Password`
- `CloudinarySettings:ApiSecret` → `CloudinarySettings__ApiSecret`
- `IpfsSettings:ApiSecret` → `IpfsSettings__ApiSecret`

### 5) Migration/seed
App đã gọi `db.Database.MigrateAsync()` khi startup. Khi deploy lần đầu, chỉ cần service start thành công là tự migrate.

Khuyến nghị production:
- Tắt reset DB: `DatabaseSettings:AutoResetOnStartup=false`
- Không dùng `--force-seed` ở Cloud Run

---

## Option B (Dễ nhất): Compute Engine VM + Docker Compose (API + SQL Server)

Kịch bản: tạo 1 VM Linux, cài Docker, chạy `docker-compose.yml` để lên cả 2 container.

### 1) Tạo VM
- Compute Engine → Create instance
- Machine type tùy tải (vd e2-medium)
- Bật firewall nếu cần (HTTP/HTTPS)

### 2) Cài Docker + Compose
Theo hướng dẫn chính thức (Ubuntu/Debian).

### 3) Copy source hoặc chỉ copy các file cần thiết
Các file bạn cần tối thiểu:
- `Dockerfile`
- `docker-compose.yml`
- Source code (để build image trên VM) hoặc build image trước rồi push registry.

### 4) Chạy service
- Set `MSSQL_SA_PASSWORD` an toàn.
- Set các secrets (JWT_KEY, EMAIL_PASSWORD, …)

`docker-compose.yml` trong repo đã map:
- API: `http://<VM_PUBLIC_IP>:8080`
- SQL Server: port 1433 (không khuyến nghị public)

### 5) Best practices
- Không expose 1433 ra internet; dùng VPC/firewall chỉ allow IP nội bộ.
- Dùng persistent disk (compose volume đã cấu hình `mssql_data`).

---

## Checklist cấu hình (cần làm trước khi deploy thật)
- [ ] Đưa secrets ra biến môi trường/Secret Manager (xóa khỏi `appsettings.json`).
- [ ] Đổi `sa` password mạnh.
- [ ] Cấu hình CORS đúng domain production.
- [ ] Bật HTTPS / proxy headers nếu dùng load balancer.

### Gợi ý cấu hình secrets (khuyến nghị)
1. **Không commit secrets** trong `Fap.Api/appsettings.json`.
2. Tạo các secrets trong **Secret Manager**:
  - `jwt-key`
  - `smtp-password`
  - `pinata-api-secret`
  - `cloudinary-api-secret`
  - `sql-password`
3. Khi deploy Cloud Run, map secrets → env vars:
  - `Jwt__Key`
  - `EmailSettings__Password`
  - `IpfsSettings__ApiSecret`
  - `CloudinarySettings__ApiSecret`
  - `ConnectionStrings__DefaultConnection`

> Bạn có thể giữ `appsettings.Development.json` cho local (không commit secrets lên remote), còn production dùng env vars.

