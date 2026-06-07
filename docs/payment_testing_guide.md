# Hướng dẫn Kiểm thử tính năng Payment bằng Postman

Tài liệu này hướng dẫn chi tiết cách cấu hình và gọi các API liên quan đến thanh toán (Payment) trong hệ thống Eshop Modular Monolith thông qua Postman.

---

## 1. Chuẩn bị môi trường

1. **Khởi động server**:
   Đảm bảo toàn bộ hệ thống (Database, RabbitMQ, Keycloak, API Gateway) đã được khởi chạy với code mới của module Payment bằng lệnh:
   ```bash
   docker compose down
   ```
   ```bash
   docker compose up --build -d
   ```

2. **Cổng dịch vụ (Port)**:
   - **API Gateway (chính)**: `http://localhost:6000` (HTTP) hoặc `https://localhost:6060` (HTTPS)
   - **RabbitMQ Dashboard**: `http://localhost:15672` (Tài khoản: `guest`/`guest`)
   - **Seq Log Dashboard**: `http://localhost:9091`

---

## 2. Kịch bản 1: Test trực tiếp API Payment (Không cần Token)

API này gọi trực tiếp đến Endpoint xử lý thanh toán độc lập nằm trong module Payment.

* **HTTP Method**: `POST`
* **URL**: `http://localhost:6000/payments`
* **Headers**:
  - `Content-Type`: `application/json`
* **Body** (chọn `raw` -> `JSON`):
  ```json
  {
    "payment": {
      "orderId": "5f1e31e5-bd42-45e0-81f1-3312c8b84d4b",
      "amount": 199.98,
      "cardName": "NGUYEN VAN A",
      "cardNumber": "1234567890123456",
      "expiration": "12/28",
      "cvv": "123",
      "paymentMethod": 1
    }
  }
  ```

* **Kết quả trả về mong đợi (Status: `200 OK`)**:
  ```json
  {
    "paymentId": "d3b07384-d113-4956-a5b2-38e5e6e3afcd",
    "isSuccess": true
  }
  ```

---

## 3. Kịch bản 2: Đặt hàng tích hợp thông tin thanh toán (Create Order)

Kịch bản này kiểm tra luồng tạo đơn hàng trực tiếp và lưu trữ thông tin thanh toán vào Module Ordering.

* **HTTP Method**: `POST`
* **URL**: `http://localhost:6000/orders`
* **Headers**:
  - `Content-Type`: `application/json`
* **Body** (chọn `raw` -> `JSON`):
  ```json
  {
    "order": {
      "customerId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
      "orderName": "Order #1002",
      "shippingAddress": {
        "firstName": "Nguyen",
        "lastName": "Loan",
        "emailAddress": "loan@example.com",
        "addressLine": "123 Main St",
        "country": "Vietnam",
        "state": "Hanoi",
        "zipCode": "10000"
      },
      "billingAddress": {
        "firstName": "Nguyen",
        "lastName": "Loan",
        "emailAddress": "loan@example.com",
        "addressLine": "123 Main St",
        "country": "Vietnam",
        "state": "Hanoi",
        "zipCode": "10000"
      },
      "payment": {
        "cardName": "NGUYEN VAN A",
        "cardNumber": "1234567890123456",
        "expiration": "12/28",
        "cvv": "123",
        "paymentMethod": 1
      },
      "items": [
        {
          "productId": "9a38f321-df39-4b68-98e3-0c4a45155f9a",
          "quantity": 2,
          "price": 99.99
        }
      ]
    }
  }
  ```

* **Kết quả trả về mong đợi (Status: `201 Created`)**:
  ```json
  {
    "id": "e9c12df4-727b-402a-9e12-421734bc1a15"
  }
  ```

---

## 4. Kịch bản 3: Luồng đặt hàng từ Giỏ hàng (Basket Checkout)

*Lưu ý: Endpoint này yêu cầu xác thực JWT Token từ Keycloak.*

### Bước A: Lấy Token từ Keycloak (Authentication)
* **HTTP Method**: `POST`
* **URL**: `http://localhost:9090/realms/eshop-realm/protocol/openid-connect/token`
* **Headers**:
  - `Content-Type`: `application/x-www-form-urlencoded`
* **Body** (chọn `x-www-form-urlencoded`):
  - `client_id`: `eshop-client`
  - `grant_type`: `password`
  - `username`: `<tên_đăng_nhập_của_bạn>`
  - `password`: `<mật_khẩu_của_bạn>`
  
*Lấy giá trị `access_token` từ response để sử dụng làm Bearer Token.*

### Bước B: Gửi Request Checkout
* **HTTP Method**: `POST`
* **URL**: `http://localhost:6000/basket/checkout`
* **Headers**:
  - `Content-Type`: `application/json`
  - `Authorization`: `Bearer <token_đã_lấy_ở_bước_A>`
* **Body** (chọn `raw` -> `JSON`):
  ```json
  {
    "basketCheckout": {
      "userName": "user1",
      "customerId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
      "totalPrice": 199.98,
      "firstName": "Nguyen",
      "lastName": "Loan",
      "emailAddress": "loan@example.com",
      "addressLine": "123 Main St",
      "country": "Vietnam",
      "state": "Hanoi",
      "zipCode": "10000",
      "cardName": "NGUYEN VAN A",
      "cardNumber": "1234567890123456",
      "expiration": "12/28",
      "cvv": "123",
      "paymentMethod": 1
    }
  }
  ```

* **Kết quả trả về mong đợi (Status: `200 OK`)**:
  ```json
  {
    "isSuccess": true
  }
  ```

---

## 5. Kiểm tra dữ liệu trong Database

Bạn có thể kết nối vào Database Postgres (`eshopdb`) trên cổng `5432` (User: `postgres` / Pass: `postgres`) để kiểm tra dữ liệu:

* **Kiểm tra đơn hàng vừa được tạo (luồng Order/Checkout)**:
  ```sql
  SELECT * FROM ordering."Orders";
  ```
  Bạn sẽ nhìn thấy thông tin thanh toán đã được nhúng trực tiếp vào các cột:
  - `Payment_CardName`
  - `Payment_CardNumber`
  - `Payment_Expiration`
  - `Payment_CVV`
