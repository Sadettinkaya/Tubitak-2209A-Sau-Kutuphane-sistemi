-- Clear existing identity data (preserve structure)
DELETE FROM "AspNetUserRoles";
DELETE FROM "AspNetUserClaims";
DELETE FROM "AspNetUserLogins";
DELETE FROM "AspNetUserTokens";
DELETE FROM "AspNetRoleClaims";
DELETE FROM "AspNetUsers";
DELETE FROM "AspNetRoles";

-- Insert Roles
INSERT INTO "AspNetRoles" ("Id", "Name", "NormalizedName", "ConcurrencyStamp") VALUES
('36171106-6ddf-48ab-913a-08ffb405d1da', 'admin', 'ADMIN', 'd1057be7-0079-46b4-a344-6d3f6bfc7980'),
('8fd73eca-94d5-44e4-893a-c77c557de752', 'student', 'STUDENT', '4c16a564-3e96-4deb-ac81-05c59e90a677'),
('4570edab-1714-414a-9650-6abe474329ac', 'service', 'SERVICE', '55b98856-af02-4686-96b9-9380180c1588');

-- Insert Users
INSERT INTO "AspNetUsers" ("Id", "FullName", "StudentNumber", "AcademicLevel", "UserName", "NormalizedUserName", "Email", "NormalizedEmail", "EmailConfirmed", "PasswordHash", "SecurityStamp", "ConcurrencyStamp", "PhoneNumber", "PhoneNumberConfirmed", "TwoFactorEnabled", "LockoutEnd", "LockoutEnabled", "AccessFailedCount") VALUES
('2c46cd58-c8be-4b1f-a859-5084dc484032', 'Sistem Yöneticisi', 'admin', 'Admin', 'admin', 'ADMIN', 'admin@library.local', 'ADMIN@LIBRARY.LOCAL', true, 'AQAAAAEAACcQAAAAEG27yf8RF7j2RE9cZGShTIz68+rFUVDGuLqz9oufh7njFq8J2gyB7LkMEQAvj6ROnA==', 'P6EGF63CAYOZJX7WNSMRYGKDRIR4IKCB', '95ec5f66-ad1d-4df5-946a-7bc1823736b7', NULL, false, false, NULL, true, 0),
('08be85dc-6e60-4dc0-898e-d4a25e9319ea', 'Test Öğrenci', '123456', 'Lisans', '123456', '123456', 'ogrenci@school.edu', 'OGRENCI@SCHOOL.EDU', true, 'AQAAAAEAACcQAAAAEBKE7vrjG8/1b4d5zrnoCDVLaFV+/BcB3jxACKYKgOx56/e4dvsFfQ6MhUCq0/igiQ==', 'X2MVNW5SORTHPMHWOBJ7UAPCEWBC56KM', '8e097e18-2344-45b2-986f-5ac7492a2dd9', NULL, false, false, NULL, true, 0),
('45d14021-05a5-4ed2-b552-7ad4891ea8e6', 'Turnstile Servis', 'turnstile-bot', 'Service', 'turnstile-bot', 'TURNSTILE-BOT', 'turnstile@library.local', 'TURNSTILE@LIBRARY.LOCAL', true, 'AQAAAAEAACcQAAAAEKWNkvvnamL9aHuNcjw3XDUYRDOU46dzPfa19t4rl0mowtMXWr3rZFfHhYRM8ml0ew==', 'MGPSCEE5X3GBNUI6Y44KET4R7GOOMKTV', 'cbf3e4cc-0564-45d8-ae36-62ca21c4d1d6', NULL, false, false, NULL, true, 0),
('99121ee9-aae0-4d3a-bcda-f5a110a652f2', 'sadettin', '12345', 'Lisans', '12345', '12345', 'sadettn@gmail.com', 'SADETTN@GMAIL.COM', true, 'AQAAAAEAACcQAAAAECHZ5U+MJiyusrY+v55ce0axj67kNzt+0oQ9daPUcpXi8++xNDAiJXeUtu+PJLElDw==', 'UPJCJN5XFT375PUG5XOZRCG7H3WLOFPL', 'b5676acb-c4fb-489a-a31a-c71febb23d24', NULL, false, false, NULL, true, 0),
('eb51af90-12d9-4215-8026-0b17c48c78e9', 'dr', '777', 'Doktora', '777', '777', 'dr@gmail.com', 'DR@GMAIL.COM', true, 'AQAAAAEAACcQAAAAEIljm0PG1JreHkIRUfurT7nt1Sk+P9Xz4SSu4Atj5/vfFJn2qPA+Kx5X/F9j1lWxdQ==', 'LCZCZ3J4LPXJXGVIX6AWTLU5NILCZCIE', 'c4817116-afb8-4964-a869-698f7d207948', NULL, false, false, NULL, true, 0),
('acfdd536-6e2f-4382-9df6-50d46dae6056', 'ceza', '1111', 'Lisans', '1111', '1111', 'ceza@gmail.com', 'CEZA@GMAIL.COM', true, 'AQAAAAEAACcQAAAAEIKZI+8vKkBpRCgtm8SKiLddgfgRUFSZsupXFXwSoS8yeLU3K+LNVIYMq77jdJNKIA==', '6B7RDDQEHFZQ2JAETDDE7T54TWVD7VLR', '5d5fd0b2-a3a2-43a0-bf71-c97ed2437cfb', NULL, false, false, NULL, true, 0);

-- Insert User-Role mappings
INSERT INTO "AspNetUserRoles" ("UserId", "RoleId") VALUES
('2c46cd58-c8be-4b1f-a859-5084dc484032', '36171106-6ddf-48ab-913a-08ffb405d1da'),
('08be85dc-6e60-4dc0-898e-d4a25e9319ea', '8fd73eca-94d5-44e4-893a-c77c557de752'),
('45d14021-05a5-4ed2-b552-7ad4891ea8e6', '4570edab-1714-414a-9650-6abe474329ac'),
('99121ee9-aae0-4d3a-bcda-f5a110a652f2', '8fd73eca-94d5-44e4-893a-c77c557de752'),
('eb51af90-12d9-4215-8026-0b17c48c78e9', '8fd73eca-94d5-44e4-893a-c77c557de752'),
('acfdd536-6e2f-4382-9df6-50d46dae6056', '8fd73eca-94d5-44e4-893a-c77c557de752');
