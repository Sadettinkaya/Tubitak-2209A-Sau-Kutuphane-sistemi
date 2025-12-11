-- Clear existing data
DELETE FROM "Reservations";
DELETE FROM "StudentProfiles";
DELETE FROM "Tables";

-- Insert old Reservations
INSERT INTO "Reservations" ("Id", "TableId", "StudentNumber", "ReservationDate", "StartTime", "EndTime", "IsAttended", "PenaltyProcessed", "StudentType") VALUES
(1, 9, '12345', '2025-12-11', '15:15:00', '16:22:00', true, false, 'Lisans'),
(2, 8, '123456', '2025-12-11', '15:26:00', '16:58:00', true, false, 'Lisans'),
(3, 9, '777', '2025-12-11', '16:28:00', '18:00:00', true, false, 'Doktora'),
(4, 10, '1111', '2025-12-11', '16:45:00', '17:45:00', true, false, 'Lisans');

-- Insert old StudentProfiles
INSERT INTO "StudentProfiles" ("Id", "StudentNumber", "StudentType", "PenaltyPoints", "BanUntil", "LastNoShowProcessedAt", "BanReason") VALUES
(1, 'admin', 'Lisans', 0, NULL, NULL, NULL),
(2, '12345', 'Lisans', 0, NULL, NULL, NULL),
(3, '123456', 'Lisans', 0, NULL, NULL, NULL),
(4, '777', 'Doktora', 0, NULL, NULL, NULL),
(5, '1111', 'Lisans', 0, NULL, NULL, NULL);

-- Insert old Tables
INSERT INTO "Tables" ("Id", "TableNumber", "FloorId") VALUES
(1, 'Masa 1-1', 1),
(2, 'Masa 1-2', 1),
(3, 'Masa 1-3', 1),
(4, 'Masa 1-4', 1),
(5, 'Masa 1-5', 1),
(6, 'Masa 1-6', 1),
(7, 'Masa 1-7', 1),
(8, 'Masa 1-8', 1),
(9, 'Masa 1-9', 1),
(10, 'Masa 1-10', 1),
(11, 'Masa 2-1', 2),
(12, 'Masa 2-2', 2),
(13, 'Masa 2-3', 2),
(14, 'Masa 2-4', 2),
(15, 'Masa 2-5', 2),
(16, 'Masa 2-6', 2),
(17, 'Masa 2-7', 2),
(18, 'Masa 2-8', 2),
(19, 'Masa 2-9', 2),
(20, 'Masa 2-10', 2),
(21, 'Masa 3-1', 3),
(22, 'Masa 3-2', 3),
(23, 'Masa 3-3', 3),
(24, 'Masa 3-4', 3),
(25, 'Masa 3-5', 3),
(26, 'Masa 3-6', 3),
(27, 'Masa 3-7', 3),
(28, 'Masa 3-8', 3),
(29, 'Masa 3-9', 3),
(30, 'Masa 3-10', 3);

-- Reset sequences
SELECT setval('"Reservations_Id_seq"', 4, true);
SELECT setval('"StudentProfiles_Id_seq"', 5, true);
SELECT setval('"Tables_Id_seq"', 30, true);
