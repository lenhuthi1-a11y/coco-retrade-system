-- =============================================================================
-- DỰ ÁN: COCO RETRADE - SPRINT 3 DATABASE MIGRATION
-- =============================================================================

-- 1. BẢNG TÀI KHOẢN NGƯỜI DÙNG (D1 - users)
CREATE TABLE users (
    user_id VARCHAR(20) PRIMARY KEY,
    full_name VARCHAR(100) NOT NULL,
    email VARCHAR(255) UNIQUE,
    phone VARCHAR(15) UNIQUE NOT NULL,
    password_hash VARCHAR(255) NOT NULL,
    role VARCHAR(20) NOT NULL DEFAULT 'user',
    account_status VARCHAR(20) NOT NULL DEFAULT 'ACTIVE',
    trust_score INT NOT NULL DEFAULT 100,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- 2. BẢNG HỒ SƠ KÝ GỬI (D2 - consignment_profiles)
CREATE TABLE consignment_profiles (
    consignment_id VARCHAR(20) PRIMARY KEY,
    consignor_id VARCHAR(20) NOT NULL REFERENCES users(user_id),
    declared_info TEXT,
    dropoff_code VARCHAR(30) UNIQUE NOT NULL,
    start_tracking_date DATE,
    consignment_period INT NOT NULL DEFAULT 60,
    status VARCHAR(30) NOT NULL DEFAULT 'PENDING',
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- 3. BẢNG BIÊN BẢN KIỂM ĐỊNH TẠI TIỆM (D3 - inspection_results)
CREATE TABLE inspection_results (
    inspection_id VARCHAR(20) PRIMARY KEY,
    consignment_id VARCHAR(20) UNIQUE NOT NULL REFERENCES consignment_profiles(consignment_id),
    actual_evaluation TEXT NOT NULL,
    inspection_report VARCHAR(500),
    result VARCHAR(20) NOT NULL,
    reject_reason TEXT,
    inspected_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- 4. BẢNG ẢNH BỔ SUNG KIỂM ĐỊNH (inspection_images)
CREATE TABLE inspection_images (
    image_id VARCHAR(20) PRIMARY KEY,
    inspection_id VARCHAR(20) NOT NULL REFERENCES inspection_results(inspection_id),
    image_url VARCHAR(500) NOT NULL
);

-- 5. BẢNG SẢN PHẨM & TEM SKU/QR (D4 - products)
CREATE TABLE products (
    sku VARCHAR(30) PRIMARY KEY,
    consignment_id VARCHAR(20) NOT NULL REFERENCES consignment_profiles(consignment_id),
    product_name VARCHAR(255) NOT NULL,
    category VARCHAR(100) NOT NULL,
    brand VARCHAR(100),
    size VARCHAR(20) NOT NULL,
    condition_rate VARCHAR(50) NOT NULL,
    description TEXT,
    listing_price DECIMAL(18,2) NOT NULL DEFAULT 0,
    status VARCHAR(30) NOT NULL DEFAULT 'ACTIVE',
    published_at TIMESTAMP
);

-- 6. BẢNG THƯƠNG LƯỢNG TRẢ GIÁ (D5 - price_offers)
CREATE TABLE price_offers (
    offer_id VARCHAR(20) PRIMARY KEY,
    buyer_id VARCHAR(20) NOT NULL REFERENCES users(user_id),
    sku VARCHAR(30) NOT NULL REFERENCES products(sku),
    proposed_price DECIMAL(18,2) NOT NULL,
    counter_price DECIMAL(18,2),
    final_price DECIMAL(18,2),
    check_rate DECIMAL(5,2) NOT NULL DEFAULT 80.00,
    attempt_count INT NOT NULL DEFAULT 1,
    status VARCHAR(30) NOT NULL DEFAULT 'PENDING',
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    expires_at TIMESTAMP
);

-- 7. BẢNG GIỎ HÀNG / YÊU THÍCH (D6 - carts)
CREATE TABLE carts (
    cart_id VARCHAR(20) PRIMARY KEY,
    user_id VARCHAR(20) NOT NULL REFERENCES users(user_id),
    item_type VARCHAR(20) NOT NULL DEFAULT 'CART',
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- 8. BẢNG TRUNG GIAN GIỎ HÀNG - SẢN PHẨM (cart_items / Bảng CHỨA)
CREATE TABLE cart_items (
    cart_id VARCHAR(20) NOT NULL REFERENCES carts(cart_id),
    sku VARCHAR(30) NOT NULL REFERENCES products(sku),
    added_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    PRIMARY KEY (cart_id, sku)
);

-- 9. BẢNG ĐƠN HÀNG & TẠM GIỮ (D7 - orders)
CREATE TABLE orders (
    order_id VARCHAR(20) PRIMARY KEY,
    buyer_id VARCHAR(20) NOT NULL REFERENCES users(user_id),
    total_amount DECIMAL(18,2) NOT NULL,
    shipping_fee DECIMAL(18,2) NOT NULL DEFAULT 0,
    fulfillment_type VARCHAR(30) NOT NULL DEFAULT 'PICKUP',
    recipient_name VARCHAR(100) NOT NULL,
    recipient_phone VARCHAR(15) NOT NULL,
    shipping_address TEXT,
    order_status VARCHAR(30) NOT NULL DEFAULT 'PENDING',
    hold_deadline TIMESTAMP,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- 10. BẢNG CHI TIẾT ĐƠN HÀNG (order_items / Bảng GỒM)
CREATE TABLE order_items (
    order_id VARCHAR(20) NOT NULL REFERENCES orders(order_id),
    sku VARCHAR(30) NOT NULL REFERENCES products(sku),
    quantity INT NOT NULL DEFAULT 1,
    unit_price DECIMAL(18,2) NOT NULL,
    PRIMARY KEY (order_id, sku)
);

-- 11. BẢNG GIAO NHẬN (D8 - deliveries)
CREATE TABLE deliveries (
    delivery_id VARCHAR(20) PRIMARY KEY,
    order_id VARCHAR(20) UNIQUE NOT NULL REFERENCES orders(order_id),
    tracking_code VARCHAR(50),
    delivery_status VARCHAR(30) NOT NULL DEFAULT 'DELIVERING',
    shipping_address TEXT,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- 12. BẢNG LỊCH SỬ GIAO NHẬN (delivery_history)
CREATE TABLE delivery_history (
    history_id VARCHAR(20) PRIMARY KEY,
    delivery_id VARCHAR(20) NOT NULL REFERENCES deliveries(delivery_id),
    status_note VARCHAR(255) NOT NULL,
    recorded_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- 13. BẢNG THANH TOÁN & ĐỐI SOÁT (D9 - payments)
CREATE TABLE payments (
    payment_id VARCHAR(20) PRIMARY KEY,
    order_id VARCHAR(20) UNIQUE NOT NULL REFERENCES orders(order_id),
    payment_method VARCHAR(30) NOT NULL DEFAULT 'COD',
    amount DECIMAL(18,2) NOT NULL,
    payment_status VARCHAR(30) NOT NULL DEFAULT 'UNPAID',
    reconciliation_status VARCHAR(30) NOT NULL DEFAULT 'PENDING',
    paid_at TIMESTAMP
);

-- 14. BẢNG BÚT TOÁN HOA HỒNG & VÍ CÁ NHÂN (D10 - wallet_transactions)
CREATE TABLE wallet_transactions (
    transaction_id VARCHAR(20) PRIMARY KEY,
    user_id VARCHAR(20) NOT NULL REFERENCES users(user_id),
    type VARCHAR(30) NOT NULL,
    amount DECIMAL(18,2) NOT NULL,
    allocation_rate DECIMAL(5,2) NOT NULL DEFAULT 80.00,
    balance_before DECIMAL(18,2) NOT NULL,
    balance_after DECIMAL(18,2) NOT NULL,
    reference_id VARCHAR(50),
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- 15. BẢNG YÊU CẦU RÚT TIỀN NGÂN HÀNG (D11 - withdrawal_requests)
CREATE TABLE withdrawal_requests (
    withdrawal_id VARCHAR(20) PRIMARY KEY,
    user_id VARCHAR(20) NOT NULL REFERENCES users(user_id),
    amount DECIMAL(18,2) NOT NULL,
    bank_name VARCHAR(100) NOT NULL,
    bank_account_number VARCHAR(50) NOT NULL,
    bank_account_name VARCHAR(100) NOT NULL,
    status VARCHAR(30) NOT NULL DEFAULT 'PENDING',
    transfer_ref_code VARCHAR(50),
    requested_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    processed_at TIMESTAMP
);

-- 16. BẢNG THÔNG BÁO NGHIỆP VỤ (D12 - notifications)
CREATE TABLE notifications (
    notification_id VARCHAR(20) PRIMARY KEY,
    user_id VARCHAR(20) NOT NULL REFERENCES users(user_id),
    type VARCHAR(30) NOT NULL,
    title VARCHAR(255) NOT NULL,
    content TEXT NOT NULL,
    is_read BOOLEAN NOT NULL DEFAULT FALSE,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- DỮ LIỆU MẪU KIỂM THỬ (SEED DATA)
INSERT INTO users (user_id, full_name, email, phone, password_hash, role) VALUES
('USR001', 'CoCo Admin', 'admin@cocoretrade.vn', '0905123456', 'hash_admin_123', 'admin'),
('USR002', 'Lê Quỳnh Như', 'nhu.buyer@gmail.com', '0905333444', 'hash_pass_123', 'user'),
('USR003', 'Lương Thùy Trinh', 'trinh.seller@gmail.com', '0905111222', 'hash_pass_123', 'user');
