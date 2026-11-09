-- Drop existing procedures before re-running
DROP PROCEDURE IF EXISTS get_organizer_dashboard;
DROP PROCEDURE IF EXISTS create_event;
DROP PROCEDURE IF EXISTS get_event_for_edit;
DROP PROCEDURE IF EXISTS edit_event;
DROP PROCEDURE IF EXISTS delete_event;
DROP PROCEDURE IF EXISTS get_event_sales;

DELIMITER //

-- Organizer dashboard metrics (owned events only)
CREATE PROCEDURE get_organizer_dashboard(IN current_user_id INT)
BEGIN
    SELECT 
        e.id,
        e.title,
        e.location,
        e.event_date,
        e.price,
        e.total_tickets,
        e.available_tickets,
        (e.total_tickets - e.available_tickets) AS tickets_booked,
        (SELECT COUNT(*) FROM tickets tk WHERE tk.event_id = e.id AND tk.status = 'active') AS tickets_sold,
        (SELECT COUNT(*) FROM tickets tk WHERE tk.event_id = e.id AND tk.status = 'reserved') AS tickets_reserved,
        (SELECT IFNULL(SUM(tr.total_price), 0.00) FROM transactions tr WHERE tr.event_id = e.id AND tr.payment_status = 'paid') AS total_revenue
    FROM events e
    WHERE e.organizer_id = current_user_id
    ORDER BY e.event_date ASC;
END //

-- Create event form handler
CREATE PROCEDURE create_event(
    IN current_user_id INT,
    IN event_title VARCHAR(120),
    IN event_desc TEXT,
    IN event_loc VARCHAR(150),
    IN event_dt DATETIME,
    IN ticket_price DECIMAL(8, 2),
    IN ticket_qty INT
)
BEGIN
    IF EXISTS (SELECT 1 FROM users WHERE id = current_user_id AND role = 'organizer' AND is_suspended = FALSE) THEN
        IF ticket_qty > 0 AND ticket_price >= 0 THEN
            INSERT INTO events (organizer_id, title, description, location, event_date, price, total_tickets, available_tickets)
            VALUES (current_user_id, event_title, event_desc, event_loc, event_dt, ticket_price, ticket_qty, ticket_qty);
        END IF;
    ELSE
        INSERT INTO security_logs (user_id, action, details)
        VALUES (current_user_id, 'AUTH_FAIL', CONCAT('User ', current_user_id, ' tried to create an event without organizer permissions'));
    END IF;
END //

-- Load event details for the edit form (verifies event.organizer_id == current_user_id)
CREATE PROCEDURE get_event_for_edit(
    IN current_user_id INT,
    IN target_event_id INT
)
BEGIN
    DECLARE owner_id INT DEFAULT NULL;

    SELECT organizer_id INTO owner_id
    FROM events
    WHERE id = target_event_id;

    IF owner_id = current_user_id THEN
        SELECT id, title, description, location, event_date, price, total_tickets, available_tickets
        FROM events
        WHERE id = target_event_id AND organizer_id = current_user_id;
    ELSEIF owner_id IS NOT NULL THEN
        INSERT INTO security_logs (user_id, action, details)
        VALUES (current_user_id, 'AUTH_FAIL', CONCAT('User ', current_user_id, ' tried to load edit form for event ', target_event_id, ' owned by user ', owner_id));
    END IF;
END //

-- Update event route (verifies event.organizer_id == current_user_id)
CREATE PROCEDURE edit_event(
    IN current_user_id INT,
    IN target_event_id INT,
    IN new_title VARCHAR(120),
    IN new_desc TEXT,
    IN new_loc VARCHAR(150),
    IN new_dt DATETIME,
    IN new_price DECIMAL(8, 2),
    IN new_total_tickets INT
)
BEGIN
    DECLARE owner_id INT DEFAULT NULL;
    DECLARE tickets_taken INT DEFAULT 0;

    SELECT organizer_id, (total_tickets - available_tickets)
    INTO owner_id, tickets_taken
    FROM events
    WHERE id = target_event_id;

    IF owner_id = current_user_id THEN
        IF new_total_tickets >= tickets_taken AND new_total_tickets > 0 AND new_price >= 0 THEN
            UPDATE events
            SET title = new_title,
                description = new_desc,
                location = new_loc,
                event_date = new_dt,
                price = new_price,
                total_tickets = new_total_tickets,
                available_tickets = new_total_tickets - tickets_taken
            WHERE id = target_event_id AND organizer_id = current_user_id;
        END IF;
    ELSEIF owner_id IS NOT NULL THEN
        INSERT INTO security_logs (user_id, action, details)
        VALUES (current_user_id, 'AUTH_FAIL', CONCAT('User ', current_user_id, ' tried to edit event ', target_event_id, ' owned by user ', owner_id));
    END IF;
END //

-- Delete event route (verifies event.organizer_id == current_user_id)
CREATE PROCEDURE delete_event(
    IN current_user_id INT,
    IN target_event_id INT
)
BEGIN
    DECLARE owner_id INT DEFAULT NULL;

    SELECT organizer_id INTO owner_id
    FROM events
    WHERE id = target_event_id;

    IF owner_id = current_user_id THEN
        DELETE FROM events
        WHERE id = target_event_id AND organizer_id = current_user_id;
    ELSEIF owner_id IS NOT NULL THEN
        INSERT INTO security_logs (user_id, action, details)
        VALUES (current_user_id, 'AUTH_FAIL', CONCAT('User ', current_user_id, ' tried to delete event ', target_event_id, ' owned by user ', owner_id));
    END IF;
END //

-- View ticket sales and reservations for a specific owned event
CREATE PROCEDURE get_event_sales(
    IN current_user_id INT,
    IN target_event_id INT
)
BEGIN
    DECLARE owner_id INT DEFAULT NULL;

    SELECT organizer_id INTO owner_id
    FROM events
    WHERE id = target_event_id;

    IF owner_id = current_user_id THEN
        SELECT 
            tk.ticket_code,
            tk.status AS ticket_status,
            u.username AS customer,
            u.email,
            tr.quantity,
            tr.total_price,
            tr.payment_status,
            tr.payment_ref,
            tr.created_at
        FROM tickets tk
        JOIN transactions tr ON tk.transaction_id = tr.id
        JOIN events e ON tk.event_id = e.id
        JOIN users u ON tk.user_id = u.id
        WHERE e.id = target_event_id AND e.organizer_id = current_user_id
        ORDER BY tr.created_at DESC;
    ELSEIF owner_id IS NOT NULL THEN
        INSERT INTO security_logs (user_id, action, details)
        VALUES (current_user_id, 'AUTH_FAIL', CONCAT('User ', current_user_id, ' tried to view sales for event ', target_event_id, ' owned by user ', owner_id));
    END IF;
END //

DELIMITER ;

-- Test queries (org_smith = 2, org_jones = 3)
CALL get_organizer_dashboard(2);
CALL create_event(2, 'Hackathon 2026', '24-hour coding event.', 'Engineering Bldg', '2026-12-12 09:00:00', 15.00, 100);
CALL get_event_for_edit(2, 1);
CALL edit_event(2, 1, 'Spring Coding Workshop', 'Updated workshop description.', 'Room 204', '2026-11-15 14:00:00', 25.00, 60);
CALL get_event_sales(2, 1);

-- Test unauthorized update and delete by org_jones (user 3) on event 1 (owned by user 2)
CALL edit_event(3, 1, 'Fake Title', 'Should fail', 'Room 204', '2026-11-15 14:00:00', 5.00, 50);
CALL delete_event(3, 1);

-- Check updated events and security logs
SELECT * FROM events;
SELECT * FROM security_logs;