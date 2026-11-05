-- Dental Clinic Management System
-- Patients, Appointments, Dentists, Treatments, Invoices

CREATE TABLE patients (
  patient_id INT AUTO_INCREMENT PRIMARY KEY,
  first_name VARCHAR(100) NOT NULL,
  last_name VARCHAR(100) NOT NULL,
  email VARCHAR(200),
  phone VARCHAR(50),
  date_of_birth DATE,
  address TEXT,
  emergency_contact_name VARCHAR(150),
  emergency_contact_phone VARCHAR(50),
  insurance_provider VARCHAR(150),
  insurance_member_id VARCHAR(100),
  insurance_group_number VARCHAR(100),
  notes TEXT,
  status VARCHAR(30) NOT NULL DEFAULT 'active',
  createdBy VARCHAR(255) NOT NULL,
  modifiedBy VARCHAR(255) NOT NULL,
  createdAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  modifiedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  isActive TINYINT(1) NOT NULL DEFAULT '1'
);

CREATE TABLE dentists (
  dentist_id INT AUTO_INCREMENT PRIMARY KEY,
  first_name VARCHAR(100) NOT NULL,
  last_name VARCHAR(100) NOT NULL,
  email VARCHAR(200) NOT NULL,
  phone VARCHAR(50),
  specialty VARCHAR(100),
  license_number VARCHAR(50),
  hire_date DATE,
  status VARCHAR(30) NOT NULL DEFAULT 'active',
  createdBy VARCHAR(255) NOT NULL,
  modifiedBy VARCHAR(255) NOT NULL,
  createdAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  modifiedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  isActive TINYINT(1) NOT NULL DEFAULT '1'
);

CREATE TABLE appointments (
  appointment_id INT AUTO_INCREMENT PRIMARY KEY,
  patient_id INT NOT NULL,
  dentist_id INT NOT NULL,
  appointment_date DATETIME NOT NULL,
  duration_minutes INT NOT NULL DEFAULT 30,
  status VARCHAR(30) NOT NULL DEFAULT 'scheduled',
  appointment_type VARCHAR(50) NOT NULL DEFAULT 'checkup',
  reason TEXT,
  notes TEXT,
  createdBy VARCHAR(255) NOT NULL,
  modifiedBy VARCHAR(255) NOT NULL,
  createdAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  modifiedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  isActive TINYINT(1) NOT NULL DEFAULT '1',
  FOREIGN KEY (patient_id) REFERENCES patients(patient_id),
  FOREIGN KEY (dentist_id) REFERENCES dentists(dentist_id)
);

CREATE TABLE treatments (
  treatment_id INT AUTO_INCREMENT PRIMARY KEY,
  appointment_id INT NOT NULL,
  patient_id INT NOT NULL,
  dentist_id INT NOT NULL,
  treatment_code VARCHAR(30) NOT NULL,
  treatment_name VARCHAR(150) NOT NULL,
  description TEXT,
  tooth_numbers VARCHAR(100),
  surface VARCHAR(50),
  status VARCHAR(30) NOT NULL DEFAULT 'planned',
  fee DECIMAL(10,2) NOT NULL DEFAULT 0.00,
  insurance_covered DECIMAL(10,2) NOT NULL DEFAULT 0.00,
  patient_pay DECIMAL(10,2) NOT NULL DEFAULT 0.00,
  started_at DATETIME,
  completed_at DATETIME,
  notes TEXT,
  createdBy VARCHAR(255) NOT NULL,
  modifiedBy VARCHAR(255) NOT NULL,
  createdAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  modifiedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  isActive TINYINT(1) NOT NULL DEFAULT '1',
  FOREIGN KEY (appointment_id) REFERENCES appointments(appointment_id),
  FOREIGN KEY (patient_id) REFERENCES patients(patient_id),
  FOREIGN KEY (dentist_id) REFERENCES dentists(dentist_id)
);

CREATE TABLE invoices (
  invoice_id INT AUTO_INCREMENT PRIMARY KEY,
  patient_id INT NOT NULL,
  appointment_id INT,
  invoice_number VARCHAR(30) NOT NULL,
  invoice_date DATE NOT NULL,
  due_date DATE NOT NULL,
  status VARCHAR(30) NOT NULL DEFAULT 'draft',
  subtotal DECIMAL(10,2) NOT NULL DEFAULT 0.00,
  tax_amount DECIMAL(10,2) NOT NULL DEFAULT 0.00,
  discount_amount DECIMAL(10,2) NOT NULL DEFAULT 0.00,
  total_amount DECIMAL(10,2) NOT NULL DEFAULT 0.00,
  amount_paid DECIMAL(10,2) NOT NULL DEFAULT 0.00,
  balance_due DECIMAL(10,2) NOT NULL DEFAULT 0.00,
  payment_method VARCHAR(50),
  payment_reference VARCHAR(100),
  paid_at DATETIME,
  notes TEXT,
  createdBy VARCHAR(255) NOT NULL,
  modifiedBy VARCHAR(255) NOT NULL,
  createdAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  modifiedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  isActive TINYINT(1) NOT NULL DEFAULT '1',
  FOREIGN KEY (patient_id) REFERENCES patients(patient_id),
  FOREIGN KEY (appointment_id) REFERENCES appointments(appointment_id)
);

CREATE TABLE clinic_policies (
  policy_id INT AUTO_INCREMENT PRIMARY KEY,
  title VARCHAR(200) NOT NULL,
  category VARCHAR(50) NOT NULL,
  version VARCHAR(20) NOT NULL DEFAULT '1.0',
  effective_date DATE NOT NULL,
  expiry_date DATE,
  content LONGTEXT NOT NULL,
  file_url VARCHAR(500),
  status VARCHAR(30) NOT NULL DEFAULT 'active',
  createdBy VARCHAR(255) NOT NULL,
  modifiedBy VARCHAR(255) NOT NULL,
  createdAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  modifiedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  isActive TINYINT(1) NOT NULL DEFAULT '1'
);

-- Seed data for treatment codes (common CDT codes)
INSERT INTO treatments (treatment_id, appointment_id, patient_id, dentist_id, treatment_code, treatment_name, description, fee, createdBy, modifiedBy, createdAt, modifiedAt, isActive) VALUES
(1, 0, 0, 0, 'D0120', 'Periodic Oral Evaluation', 'Routine checkup examination', 85.00, 'system', 'system', NOW(), NOW(), 0),
(2, 0, 0, 0, 'D0150', 'Comprehensive Oral Evaluation', 'New patient comprehensive exam', 120.00, 'system', 'system', NOW(), NOW(), 0),
(3, 0, 0, 0, 'D1110', 'Prophylaxis - Adult', 'Adult dental cleaning', 110.00, 'system', 'system', NOW(), NOW(), 0),
(4, 0, 0, 0, 'D1120', 'Prophylaxis - Child', 'Child dental cleaning', 85.00, 'system', 'system', NOW(), NOW(), 0),
(5, 0, 0, 0, 'D2140', 'Amalgam - One Surface', 'Silver filling one surface', 150.00, 'system', 'system', NOW(), NOW(), 0),
(6, 0, 0, 0, 'D2330', 'Resin Composite - One Surface', 'White filling one surface anterior', 180.00, 'system', 'system', NOW(), NOW(), 0),
(7, 0, 0, 0, 'D2740', 'Crown - Porcelain/Ceramic', 'Porcelain crown', 1200.00, 'system', 'system', NOW(), NOW(), 0),
(8, 0, 0, 0, 'D3310', 'Root Canal - Anterior', 'Anterior root canal therapy', 850.00, 'system', 'system', NOW(), NOW(), 0),
(9, 0, 0, 0, 'D4341', 'Scaling and Root Planing', 'Deep cleaning per quadrant', 280.00, 'system', 'system', NOW(), NOW(), 0),
(10, 0, 0, 0, 'D7140', 'Extraction - Simple', 'Simple tooth extraction', 200.00, 'system', 'system', NOW(), NOW(), 0);