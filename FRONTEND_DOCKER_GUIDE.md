# Hướng dẫn triển khai Backend cho Frontend Team bằng Docker

Tài liệu này hướng dẫn cách nhanh chóng khởi chạy toàn bộ hệ thống backend (API + Database) trên bất kỳ máy tính nào chỉ với Docker.

## 1. Yêu cầu cần có

* **Cài đặt Docker Desktop**: Đảm bảo bạn đã cài đặt và đang chạy Docker Desktop trên máy tính của mình.

  * Tải về tại: [https://www.docker.com/products/docker-desktop/](https://www.docker.com/products/docker-desktop/)

## 2. Những gì bạn cần

* Bạn chỉ cần duy nhất tệp `docker-compose.yml`.
* **Không cần** tải xuống toàn bộ mã nguồn của backend.

## 3. Các bước thực hiện

1. **Tạo một thư mục mới**: Trên máy tính của bạn, tạo một thư mục trống để chứa tệp cấu hình, ví dụ: `fap-backend-env`.

2. **Sao chép tệp**: Đặt tệp `docker-compose.yml` vào bên trong thư mục bạn vừa tạo.

3. **Mở Terminal**: Mở một cửa sổ dòng lệnh (PowerShell, Command Prompt, hoặc Terminal) và điều hướng đến thư mục đó.

   ```sh
   # Ví dụ:
   cd C:\Users\YourUser\Desktop\fap-backend-env
   ```

4. **Khởi động Backend**: Chạy lệnh sau.

   ```sh
   docker compose up -d
   ```

**Chỉ một chút!** Lần đầu tiên chạy, Docker sẽ cần tải các "image" (bản đóng gói của API và SQL Server) từ trên mạng về. Quá trình này có thể mất vài phút. Những lần khởi động sau sẽ nhanh hơn nhiều.

Lệnh trên sẽ:

* Tải về và chạy container cho **API Backend**.
* Tải về và chạy container cho **SQL Server Database**.
* Cấu hình mạng để hai container có thể giao tiếp với nhau.

## 4. Kiểm tra hoạt động

Sau khi lệnh chạy xong, bạn có thể kiểm tra xem backend đã hoạt động đúng chưa:

* **API Endpoint**: Backend sẽ chạy tại địa chỉ `http://localhost:8080`.
* **Swagger UI (Tài liệu API)**: Mở trình duyệt và truy cập `http://localhost:8080/swagger`. Bạn sẽ thấy danh sách tất cả các API có sẵn để frontend có thể gọi.
* **Health Check**: Truy cập `http://localhost:8080/health` để đảm bảo API đang "khỏe mạnh".

## 5. Các lệnh Docker hữu ích khác

* **Dừng toàn bộ hệ thống backend**:

  ```sh
  docker compose down
  ```

* **Xem nhật ký (logs) của API nếu có lỗi**:

  ```sh
  docker compose logs -f api
  ```

* **Khởi động lại hệ thống**:

  ```sh
  docker compose restart
  ```

