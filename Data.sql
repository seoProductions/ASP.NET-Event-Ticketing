-- Clear existing tables before re-running
DROP TABLE IF EXISTS security_logs;
DROP TABLE IF EXISTS disputes;
DROP TABLE IF EXISTS tickets;
DROP TABLE IF EXISTS transactions;
DROP TABLE IF EXISTS events;
DROP TABLE IF EXISTS users;

-- Users table (Customer, Organizer, Admin)
CREATE TABLE users (
    id INT AUTO_INCREMENT PRIMARY KEY,
    username VARCHAR(50) NOT NULL UNIQUE,
    email VARCHAR(100) NOT NULL UNIQUE,
    password_hash VARCHAR(255) NOT NULL,
    role ENUM('customer', 'organizer', 'admin') NOT NULL DEFAULT 'customer',
    is_suspended BOOLEAN NOT NULL DEFAULT FALSE,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- Events table (owned by an organizer)
CREATE TABLE events (
    id INT AUTO_INCREMENT PRIMARY KEY,
    organizer_id INT NOT NULL,
    title VARCHAR(120) NOT NULL,
    description TEXT,
    location VARCHAR(150) NOT NULL,
    event_date DATETIME NOT NULL,
    price DECIMAL(8, 2) NOT NULL,
    total_tickets INT NOT NULL,
    available_tickets INT NOT NULL,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (organizer_id) REFERENCES users(id) ON DELETE CASCADE
);

-- Transactions table (tracks orders and mock payment state)
CREATE TABLE transactions (
    id INT AUTO_INCREMENT PRIMARY KEY,
    user_id INT NOT NULL,
    event_id INT NOT NULL,
    quantity INT NOT NULL,
    total_price DECIMAL(8, 2) NOT NULL,
    payment_status ENUM('pending', 'paid', 'failed', 'cancelled') NOT NULL DEFAULT 'pending',
    payment_ref VARCHAR(64),
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (user_id) REFERENCES users(id) ON DELETE CASCADE,
    FOREIGN KEY (event_id) REFERENCES events(id) ON DELETE CASCADE
);

-- Tickets table (individual tickets tied to a transaction)
CREATE TABLE tickets (
    id INT AUTO_INCREMENT PRIMARY KEY,
    transaction_id INT NOT NULL,
    event_id INT NOT NULL,
    user_id INT NOT NULL,
    ticket_code VARCHAR(40) NOT NULL UNIQUE,
    status ENUM('reserved', 'active', 'cancelled') NOT NULL DEFAULT 'reserved',
    FOREIGN KEY (transaction_id) REFERENCES transactions(id) ON DELETE CASCADE,
    FOREIGN KEY (event_id) REFERENCES events(id) ON DELETE CASCADE,
    FOREIGN KEY (user_id) REFERENCES users(id) ON DELETE CASCADE
);

-- Disputes table (for admin cancellation/dispute handling)
CREATE TABLE disputes (
    id INT AUTO_INCREMENT PRIMARY KEY,
    transaction_id INT NOT NULL,
    user_id INT NOT NULL,
    reason VARCHAR(255) NOT NULL,
    status ENUM('open', 'resolved', 'rejected') NOT NULL DEFAULT 'open',
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (transaction_id) REFERENCES transactions(id) ON DELETE CASCADE,
    FOREIGN KEY (user_id) REFERENCES users(id) ON DELETE CASCADE
);

-- Security audit log
CREATE TABLE security_logs (
    id INT AUTO_INCREMENT PRIMARY KEY,
    user_id INT NULL,
    action VARCHAR(50) NOT NULL,
    details VARCHAR(255) NOT NULL,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (user_id) REFERENCES users(id) ON DELETE SET NULL
);

-- Demo accounts (sample bcrypt hashes for testing)
INSERT INTO users (username, email, password_hash, role, is_suspended) VALUES
('admin1', 'admin@test.com', '$2b$12$eImiTXuWVxfM37uY4JANjQ==', 'admin', FALSE),
('org_smith', 'smith@test.com', '$2b$12$LQv3c1yqBWVHxkd0LHAkCOYz6TtxMQJqhN8/LewdBPj4h/j9.1K2m', 'organizer', FALSE),
('org_jones', 'jones@test.com', '$2b$12$92IXUNpkjO0rOQ5byMi.Ye4oKoEa3Ro9llC/.og/at2.uheWG/igi', 'organizer', FALSE),
('alice', 'alice@test.com', '$2b$12$K4G5f8vL1pM9qR2sT3uV4wX5yZ6aB7cD8eF9gH0iJ1kL2mN3oP4qR', 'customer', FALSE),
('bob', 'bob@test.com', '$2b$12$M1n2B3v4C5x6Z7l8K9j0H1g2F3d4S5a6P7o8I9u0Y1t2R3e4W5q6E', 'customer', FALSE);

-- Sample events
INSERT INTO events (organizer_id, title, description, location, event_date, price, total_tickets, available_tickets) VALUES
(2, 'Spring Coding Workshop', 'Introductory workshop on web security.', 'Room 204', '2026-11-15 14:00:00', 20.00, 50, 48),
(2, 'Tech Career Fair', 'Networking event with local employers.', 'Student Union', '2026-11-22 10:00:00', 10.00, 100, 99),
(3, 'Campus Acoustic Night', 'Live music at the student center.', 'Main Hall', '2026-12-05 19:00:00', 15.00, 80, 79);

-- Sample transactions
INSERT INTO transactions (user_id, event_id, quantity, total_price, payment_status, payment_ref) VALUES
(4, 1, 2, 40.00, 'paid', 'SIM-1001'),
(4, 2, 1, 10.00, 'pending', 'SIM-1002'),
(5, 3, 1, 15.00, 'paid', 'SIM-1003');

-- Sample tickets
INSERT INTO tickets (transaction_id, event_id, user_id, ticket_code, status) VALUES
(1, 1, 4, 'TKT-101', 'active'),
(1, 1, 4, 'TKT-102', 'active'),
(2, 2, 4, 'TKT-201', 'reserved'),
(3, 3, 5, 'TKT-301', 'active');

-- Sample dispute
INSERT INTO disputes (transaction_id, user_id, reason, status) VALUES
(3, 5, 'Booked the wrong date, requesting refund.', 'open');

-- Sample security logs
INSERT INTO security_logs (user_id, action, details) VALUES
(4, 'LOGIN_SUCCESS', 'User alice logged in'),
(NULL, 'LOGIN_FAIL', 'Invalid password for username admin1'),
(3, 'AUTH_FAIL', 'User 3 tried to edit event 1 owned by user 2');

-- Verify tables loaded
SELECT id, username, role FROM users;