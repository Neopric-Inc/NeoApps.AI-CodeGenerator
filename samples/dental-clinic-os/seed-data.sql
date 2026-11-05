-- Seed data for Dental Clinic OS
-- Run against the dental_clinic_db database

USE dental_clinic_db;

-- Clear existing data (optional - comment out if you want to keep existing)
-- SET FOREIGN_KEY_CHECKS = 0;
-- TRUNCATE TABLE treatments;
-- TRUNCATE TABLE invoices;
-- TRUNCATE TABLE appointments;
-- TRUNCATE TABLE patients;
-- TRUNCATE TABLE dentists;
-- TRUNCATE TABLE clinic_policies;
-- SET FOREIGN_KEY_CHECKS = 1;

-- Insert Dentists
INSERT INTO dentists (first_name, last_name, email, phone, specialty, license_number, hire_date, status, createdBy, modifiedBy) VALUES
('Sarah', 'Mitchell', 'sarah.mitchell@dentalclinic.com', '555-0101', 'General Dentistry', 'DDS-12345', '2020-01-15', 'active', 'system', 'system'),
('James', 'Chen', 'james.chen@dentalclinic.com', '555-0102', 'Orthodontics', 'DDS-12346', '2021-03-22', 'active', 'system', 'system'),
('Maria', 'Rodriguez', 'maria.rodriguez@dentalclinic.com', '555-0103', 'Periodontics', 'DDS-12347', '2019-07-10', 'active', 'system', 'system'),
('David', 'Thompson', 'david.thompson@dentalclinic.com', '555-0104', 'Endodontics', 'DDS-12348', '2022-02-01', 'active', 'system', 'system'),
('Lisa', 'Park', 'lisa.park@dentalclinic.com', '555-0105', 'Pediatric Dentistry', 'DDS-12349', '2021-11-15', 'active', 'system', 'system');

-- Insert Patients
INSERT INTO patients (first_name, last_name, email, phone, date_of_birth, address, emergency_contact_name, emergency_contact_phone, insurance_provider, insurance_member_id, insurance_group_number, notes, status, createdBy, modifiedBy) VALUES
('John', 'Anderson', 'john.anderson@email.com', '555-1001', '1985-03-12', '123 Maple St, Springfield, IL 62701', 'Jane Anderson', '555-1002', 'Delta Dental', 'DD-789456', 'GRP-1001', 'Prefers morning appointments. Mild dental anxiety.', 'active', 'system', 'system'),
('Maria', 'Garcia', 'maria.garcia@email.com', '555-1003', '1992-07-24', '456 Oak Ave, Springfield, IL 62702', 'Carlos Garcia', '555-1004', 'Cigna', 'CG-234567', 'GRP-1002', 'Spanish speaker. Needs translator for complex procedures.', 'active', 'system', 'system'),
('Robert', 'Kim', 'robert.kim@email.com', '555-1005', '1978-11-05', '789 Pine Rd, Springfield, IL 62703', 'Susan Kim', '555-1006', 'MetLife', 'ML-345678', 'GRP-1003', 'High blood pressure - check BP before procedures.', 'active', 'system', 'system'),
('Emily', 'Johnson', 'emily.johnson@email.com', '555-1007', '2005-04-18', '321 Elm St, Springfield, IL 62704', 'Michael Johnson', '555-1008', 'Delta Dental', 'DD-456789', 'GRP-1001', 'Pediatric patient. Parent must be present.', 'active', 'system', 'system'),
('William', 'Brown', 'william.brown@email.com', '555-1009', '1965-09-30', '654 Cedar Ln, Springfield, IL 62705', 'Patricia Brown', '555-1010', 'Aetna', 'AE-567890', 'GRP-1004', 'On blood thinners - consult physician for extractions.', 'active', 'system', 'system'),
('Jennifer', 'Davis', 'jennifer.davis@email.com', '555-1011', '1990-01-22', '987 Birch Dr, Springfield, IL 62706', 'Thomas Davis', '555-1012', 'Guardian', 'GD-678901', 'GRP-1005', 'Pregnant (2nd trimester) - avoid X-rays unless emergency.', 'active', 'system', 'system'),
('Michael', 'Wilson', 'michael.wilson@email.com', '555-1013', '1982-06-14', '147 Spruce St, Springfield, IL 62707', 'Laura Wilson', '555-1014', 'Delta Dental', 'DD-789012', 'GRP-1001', 'Grinds teeth at night - needs night guard.', 'active', 'system', 'system'),
('Sarah', 'Taylor', 'sarah.taylor@email.com', '555-1015', '2010-08-09', '258 Walnut Ave, Springfield, IL 62708', 'David Taylor', '555-1016', 'Cigna', 'CG-890123', 'GRP-1002', 'First dental visit. Very nervous.', 'active', 'system', 'system'),
('David', 'Martinez', 'david.martinez@email.com', '555-1017', '1975-12-03', '369 Chestnut Rd, Springfield, IL 62709', 'Ana Martinez', '555-1018', 'MetLife', 'ML-901234', 'GRP-1003', 'Diabetic - monitor healing after procedures.', 'active', 'system', 'system'),
('Lisa', 'Anderson', 'lisa.anderson@email.com', '555-1019', '1998-02-27', '741 Fir St, Springfield, IL 62710', 'James Anderson', '555-1020', 'Aetna', 'AE-012345', 'GRP-1004', 'Wisdom teeth coming in - needs evaluation.', 'active', 'system', 'system');

-- Insert Appointments (mix of past, today, future)
INSERT INTO appointments (patient_id, dentist_id, appointment_date, duration_minutes, status, appointment_type, reason, notes, createdBy, modifiedBy) VALUES
-- Completed appointments (for testing workflows)
(1, 1, '2025-12-15 09:00:00', 60, 'completed', 'checkup', 'Routine checkup and cleaning', 'Patient needs crown on #19 (tooth 30). Recommended D2740. Also needs prophylaxis D1110.', 'system', 'system'),
(2, 2, '2025-12-18 10:30:00', 90, 'completed', 'consultation', 'Orthodontic consultation', 'Patient interested in Invisalign. Examined - mild crowding. Recommended comprehensive ortho eval.', 'system', 'system'),
(3, 3, '2025-12-20 14:00:00', 60, 'completed', 'cleaning', 'Periodontal maintenance', 'Deep cleaning needed - scaling and root planing D4341 quadrants 1 and 2. 4mm pockets.', 'system', 'system'),
(4, 5, '2025-12-22 11:00:00', 30, 'completed', 'checkup', 'Pediatric checkup', 'First visit. Good cooperation. No caries. Fluoride varnish applied.', 'system', 'system'),
(5, 4, '2025-12-19 09:30:00', 90, 'completed', 'root_canal', 'Root canal #14', 'Root canal D3310 on tooth 14 completed. Crown recommended (D2740).', 'system', 'system'),
(6, 1, '2025-12-16 13:00:00', 45, 'completed', 'filling', 'Composite filling #3', 'D2330 on tooth 3 (surface MO). Patient comfortable throughout.', 'system', 'system'),

-- Future appointments
(1, 1, '2026-01-15 09:00:00', 60, 'scheduled', 'crown', 'Crown preparation #19', 'Prep for D2740 on tooth 30. Patient pre-medicated.', 'system', 'system'),
(2, 2, '2026-01-16 10:00:00', 60, 'scheduled', 'consultation', 'Invisalign records appointment', 'Photos, impressions, x-rays for Invisalign treatment plan.', 'system', 'system'),
(3, 3, '2026-01-17 14:00:00', 60, 'scheduled', 'cleaning', 'Periodontal maintenance Q2', 'SRP quadrants 3 and 4 (D4341).', 'system', 'system'),
(7, 1, '2026-01-18 11:00:00', 30, 'scheduled', 'checkup', '6-month checkup', 'Routine exam and cleaning. Check night guard fit.', 'system', 'system'),
(8, 5, '2026-01-19 09:00:00', 30, 'scheduled', 'checkup', 'Pediatric checkup', '6-month recall. Sealants on #3 and #14 if erupted.', 'system', 'system'),
(9, 4, '2026-01-20 10:30:00', 60, 'scheduled', 'consultation', 'Root canal eval #19', 'Tooth 19 symptomatic. Evaluate for RCT vs extraction.', 'system', 'system'),
(10, 1, '2026-01-21 13:00:00', 45, 'scheduled', 'extraction', 'Wisdom tooth extraction #1', 'D7140 tooth 1. Patient on blood thinners - medical clearance obtained.', 'system', 'system'),

-- Today's appointments
(4, 5, DATE_FORMAT(NOW(), '%Y-%m-%d 09:00:00'), 30, 'confirmed', 'checkup', 'Pediatric checkup', 'Morning appointment - parent present.', 'system', 'system'),
(7, 1, DATE_FORMAT(NOW(), '%Y-%m-%d 10:30:00'), 45, 'confirmed', 'cleaning', 'Prophylaxis and exam', 'Check night guard wear.', 'system', 'system'),
(3, 3, DATE_FORMAT(NOW(), '%Y-%m-%d 13:00:00'), 60, 'scheduled', 'cleaning', 'Periodontal maintenance', 'SRP follow-up.', 'system', 'system');

-- Insert Treatments (some completed, some planned)
INSERT INTO treatments (appointment_id, patient_id, dentist_id, treatment_code, treatment_name, description, tooth_numbers, surface, status, fee, insurance_covered, patient_pay, started_at, completed_at, notes, createdBy, modifiedBy) VALUES
-- Completed treatments for appointment 1 (John Anderson - crown #19 + cleaning)
(1, 1, 1, 'D2740', 'Crown - Porcelain/Ceramic', 'Porcelain crown on tooth 30', '30', 'full', 'completed', 1200.00, 960.00, 240.00, '2025-12-15 09:15:00', '2025-12-15 10:00:00', 'Prepped and temporary placed. Lab case sent.', 'system', 'system'),
(1, 1, 1, 'D1110', 'Prophylaxis - Adult', 'Adult dental cleaning', '', '', 'completed', 110.00, 88.00, 22.00, '2025-12-15 10:00:00', '2025-12-15 10:30:00', 'Heavy calculus removed. Oral hygiene instructions given.', 'system', 'system'),

-- Completed treatments for appointment 3 (Robert Kim - SRP)
(3, 3, 3, 'D4341', 'Scaling and Root Planing', 'Deep cleaning quadrant 1', '1,2,3,4,5,6,7,8', '', 'completed', 280.00, 224.00, 56.00, '2025-12-20 14:10:00', '2025-12-20 15:00:00', 'Quadrant 1 complete. 4-5mm pockets.', 'system', 'system'),
(3, 3, 3, 'D4341', 'Scaling and Root Planing', 'Deep cleaning quadrant 2', '9,10,11,12,13,14,15,16', '', 'completed', 280.00, 224.00, 56.00, '2025-12-20 15:00:00', '2025-12-20 15:45:00', 'Quadrant 2 complete. Patient tolerated well.', 'system', 'system'),

-- Completed treatment for appointment 5 (William Brown - root canal)
(5, 5, 4, 'D3310', 'Root Canal - Anterior', 'Root canal therapy on tooth 14', '14', '', 'completed', 850.00, 680.00, 170.00, '2025-12-19 09:45:00', '2025-12-19 11:00:00', 'RCT completed. Crown recommended.', 'system', 'system'),

-- Completed treatment for appointment 6 (Michael Wilson - filling)
(6, 7, 1, 'D2330', 'Resin Composite - One Surface', 'Composite filling tooth 3 MO', '3', 'MO', 'completed', 180.00, 144.00, 36.00, '2025-12-16 13:10:00', '2025-12-16 13:40:00', 'Shade A2. Good contact.', 'system', 'system'),

-- Planned treatments (for future appointments)
(7, 1, 1, 'D2740', 'Crown - Porcelain/Ceramic', 'Crown on tooth 30', '30', 'full', 'planned', 1200.00, 960.00, 240.00, NULL, NULL, 'Awaiting lab return from appointment 1.', 'system', 'system'),
(9, 3, 3, 'D4341', 'Scaling and Root Planing', 'SRP quadrant 3', '17,18,19,20,21,22,23,24', '', 'planned', 280.00, 224.00, 56.00, NULL, NULL, 'Quadrant 3.', 'system', 'system'),
(9, 3, 3, 'D4341', 'Scaling and Root Planing', 'SRP quadrant 4', '25,26,27,28,29,30,31,32', '', 'planned', 280.00, 224.00, 56.00, NULL, NULL, 'Quadrant 4.', 'system', 'system'),
(11, 9, 4, 'D3310', 'Root Canal - Anterior', 'RCT on tooth 19', '19', '', 'planned', 850.00, 680.00, 170.00, NULL, NULL, 'Eval first - may need extraction instead.', 'system', 'system'),
(12, 10, 1, 'D7140', 'Extraction - Simple', 'Wisdom tooth extraction', '1', '', 'planned', 200.00, 160.00, 40.00, NULL, NULL, 'Medical clearance on file.', 'system', 'system');

-- Insert Invoices
INSERT INTO invoices (patient_id, appointment_id, invoice_number, invoice_date, due_date, status, subtotal, tax_amount, discount_amount, total_amount, amount_paid, balance_due, payment_method, payment_reference, paid_at, notes, createdBy, modifiedBy) VALUES
(1, 1, 'INV-2025-00001', '2025-12-15', '2026-01-14', 'partial', 1310.00, 0.00, 0.00, 1310.00, 500.00, 810.00, 'credit_card', 'CC-12345', '2025-12-20 14:30:00', 'Crown + cleaning. $500 paid.', 'system', 'system'),
(3, 3, 'INV-2025-00002', '2025-12-20', '2026-01-19', 'sent', 560.00, 0.00, 0.00, 560.00, 0.00, 560.00, NULL, NULL, NULL, 'SRP quadrants 1 & 2.', 'system', 'system'),
(5, 5, 'INV-2025-00003', '2025-12-19', '2026-01-18', 'paid', 850.00, 0.00, 0.00, 850.00, 850.00, 0.00, 'insurance', 'INS-789', '2025-12-22 10:00:00', 'Root canal. Insurance paid in full.', 'system', 'system'),
(7, 6, 'INV-2025-00004', '2025-12-16', '2026-01-15', 'paid', 180.00, 0.00, 0.00, 180.00, 180.00, 0.00, 'credit_card', 'CC-67890', '2025-12-16 14:00:00', 'Composite filling.', 'system', 'system'),
(2, NULL, 'INV-2025-00005', '2025-12-18', '2026-01-17', 'draft', 0.00, 0.00, 0.00, 0.00, 0.00, 0.00, NULL, NULL, NULL, 'Orthodontic consultation - no charge.', 'system', 'system'),
(4, NULL, 'INV-2025-00006', '2025-12-22', '2026-01-21', 'draft', 0.00, 0.00, 0.00, 0.00, 0.00, 0.00, NULL, NULL, NULL, 'Pediatric checkup - covered by insurance.', 'system', 'system');

-- Insert Clinic Policies (for AI Assistant)
INSERT INTO clinic_policies (title, category, version, effective_date, expiry_date, content, file_url, status, createdBy, modifiedBy) VALUES
('Missed Appointment Policy', 'general', '2.0', '2025-01-01', NULL,
'## Missed Appointment Policy

**Effective:** January 1, 2025
**Version:** 2.0

### Policy
Patients who miss appointments without 24-hour notice will be charged a $50 fee. After 3 missed appointments, the patient may be dismissed from the practice.

### Procedure
1. Front desk confirms appointments 48 hours prior via text/email/phone.
2. If patient cancels with <24 hours notice, document in chart and apply $50 fee.
3. If patient no-shows, apply $50 fee and send written notice.
4. After 3 occurrences, dentist reviews for dismissal.

### Exceptions
- Medical emergencies (documentation required)
- Weather emergencies
- First offense may be waived at dentist discretion

### References
ADA Code of Ethics Section 5.A', NULL, 'active', 'system', 'system'),

('Infection Control - Operatory Turnover', 'infection_control', '3.1', '2025-01-01', NULL,
'## Infection Control: Operatory Turnover Procedure

**Effective:** January 1, 2025
**Version:** 3.1

### Purpose
Ensure consistent disinfection between patients per CDC and OSHA guidelines.

### Procedure
**Between Every Patient:**
1. Remove and dispose of barriers (light handles, chair covers, tray covers)
2. Discard single-use items (saliva ejector, HVE tip, air/water syringe tip, prophy angle)
3. Wipe all surfaces with EPA-registered hospital disinfectant (CaviCide or equivalent)
   - Dental chair (headrest, armrests, back)
   - Light handles and switches
   - Delivery unit (handpiece holders, air/water syringe)
   - Countertops and bracket table
   - Computer keyboard/mouse (if used)
4. Allow disinfectant contact time per manufacturer (typically 2-3 minutes)
5. Replace barriers for next patient

**End of Day:**
1. Flush water lines for 2 minutes
2. Run handpiece purge cycle
3. Clean and disinfect suction traps
4. Empty and clean amalgam separator (if applicable)
5. Restock supplies for next day

### Documentation
Log completion in daily infection control checklist. Retain 3 years.

### References
CDC Guidelines for Infection Control in Dental Health-Care Settings (2003)
OSHA Bloodborne Pathogens Standard (29 CFR 1910.1030)', NULL, 'active', 'system', 'system'),

('Insurance Claim Denial Handling', 'billing', '1.5', '2025-01-01', NULL,
'## Insurance Claim Denial Handling Procedure

**Effective:** January 1, 2025
**Version:** 1.5

### Policy
All denied claims must be reviewed and appealed within 30 days of denial date.

### Common Denial Reasons & Actions

| Denial Code | Reason | Action |
|-------------|--------|--------|
| CO-45 | Charge exceeds fee schedule | Adjust write-off, bill patient if allowed |
| CO-96 | Non-covered service | Verify eligibility, appeal with clinical notes |
| CO-16 | Missing information | Resubmit with corrected claim |
| CO-18 | Duplicate claim | Verify original payment status |
| CO-27 | Expired eligibility | Verify current coverage, update records |

### Appeal Process
1. **Day 1-3:** Billing specialist reviews EOB and clinical notes
2. **Day 4-7:** Prepare appeal letter with:
   - Patient info and claim number
   - Clinical rationale from treatment notes
   - Supporting documentation (x-rays, photos, narratives)
   - CDT code justification
3. **Day 8-14:** Submit appeal via portal/fax/mail with tracking
4. **Day 15-30:** Follow up weekly until resolution
5. **Day 30+:** Escalate to insurance commissioner if needed

### Documentation
All appeals logged in practice management system. Track denial rates monthly by payer.

### References
ADA Dental Claim Form Instructions
Individual payer provider manuals', NULL, 'active', 'system', 'system'),

('Medical Emergency Protocol', 'emergency', '2.0', '2025-01-01', NULL,
'## Medical Emergency Protocol

**Effective:** January 1, 2025
**Version:** 2.0

### Emergency Kit Location
- **Main kit:** Sterilization room, top cabinet (red label)
- **Portable kit:** Each operatory (under sink)
- **AED:** Reception area, wall-mounted

### Emergency Medications (Main Kit)
- Epinephrine 1:1000 (auto-injectors x2)
- Albuterol inhaler
- Nitroglycerin tablets
- Aspirin 325mg (chewable)
- Diphenhydramine 50mg (injectable)
- Glucose gel
- Ammonia inhalants

### Response Protocol: **P-C-A-B**

**P - Position**
- Syncope: Supine, legs elevated
- Respiratory distress: Semi-Fowler position
- Seizure: Clear area, protect head, do not restrain

**C - Circulation**
- Check pulse, BP
- Start CPR if no pulse
- Apply AED if available

**A - Airway**
- Head tilt-chin lift
- Suction if needed
- O2 at 10-15 L/min via non-rebreather

**B - Breathing**
- Assess respirations
- Assist ventilations if <10/min
- Administer emergency meds per protocol

### Specific Emergencies

**Syncope:** Position, O2, ammonia inhalant, monitor vitals, 911 if >2 min
**Allergic Reaction:** Epinephrine 0.3mg IM, O2, 911, monitor
**Asthma Attack:** Albuterol 2 puffs q5min x3, O2, 911 if no improvement
**Chest Pain:** Aspirin 325mg chewed, nitroglycerin if prescribed, O2, 911
**Seizure:** Protect, time, O2 post-ictal, 911 if >5 min or first seizure
**Hypoglycemia:** Glucose gel/Orange juice, reassess in 15 min

### Post-Emergency
1. Complete incident report within 24 hours
2. Debrief with team
3. Restock emergency kit
4. Follow up with patient next day

### Training
All staff: BLS certification current. Annual emergency drill.', NULL, 'active', 'system', 'system'),

('HIPAA Patient Communication Guidelines', 'hipaa', '1.0', '2025-01-01', NULL,
'## HIPAA Patient Communication Guidelines

**Effective:** January 1, 2025
**Version:** 1.0

### Minimum Necessary Standard
Only share PHI required for the specific purpose.

### Phone Communications
- **Verify identity:** Ask for DOB and address before discussing PHI
- **Voicemail:** Leave only name and callback number. No clinical info.
- **Family members:** Only if patient signed authorization form

### Electronic Communications
- **Email:** Use encrypted portal for PHI. Standard email for scheduling only.
- **Text:** Appointment reminders only. No clinical details.
- **Patient Portal:** Primary method for results, documents, messaging.

### In-Person
- **Reception area:** Speak quietly. No charts visible to other patients.
- **Treatment rooms:** Close doors during discussions.
- **Hallways:** No patient discussions.

### Documentation Requests
- **Patient requests:** Provide within 30 days (electronic preferred)
- **Third-party requests:** Require signed authorization
- **Fees:** Per state law (typically $0.75/page + clerical)

### Breach Response
1. Identify and contain immediately
2. Assess risk (type of PHI, who accessed, mitigation)
3. Notify Privacy Officer within 24 hours
4. Patient notification if required (60 days max)
5. HHS notification if >500 affected
6. Document all actions

### Training
Annual HIPAA training required for all staff. New hires within 30 days.', NULL, 'active', 'system', 'system'),

('Crown Procedure Documentation Standard', 'clinical', '1.2', '2025-01-01', NULL,
'## Crown Procedure Documentation Standard

**Effective:** January 1, 2025
**Version:** 1.2

### Required Documentation for Every Crown

**Pre-Op:**
- Diagnosis and indication (caries, fracture, endo, etc.)
- Pre-op radiographs (PA + BW)
- Tooth vitality testing results
- Periodontal probing depths
- Occlusal analysis notes
- Shade selection (photos recommended)
- Informed consent signed

**Preparation Appointment:**
- Anesthetic used (type, amount, carpule #)
- Reduction measurements (occlusal 1.5-2mm, axial 1-1.5mm)
- Margin type (chamfer, shoulder, feather)
- Gingival retraction method (cord, paste, laser)
- Impression material and technique
- Temporary crown: material, cement, occlusion check
- Post-op instructions given

**Delivery Appointment:**
- Try-in evaluation: marginal fit, contact, occlusion, shade
- Adjustments made (what, how much)
- Final cement: type, lot #
- Excess cement removal confirmed (floss, explorer, radiograph)
- Final occlusion check (MIP, excursive)
- Post-cement radiograph (BW)
- Post-op instructions given

**Photos Required (Minimum):**
1. Pre-op smile
2. Pre-op tooth close-up
3. Prep shade with shade tab
4. Temporary in place
5. Final crown on model (if lab)
6. Final crown in mouth
7. Post-op radiograph

### CDT Coding
- D2740: Crown - porcelain/ceramic
- D2750: Crown - porcelain fused to high noble metal
- D2790: Crown - full cast high noble metal
- D2950: Core buildup (if separate)
- D2952: Post and core (if separate)

### Lab Communication
Written prescription required with:
- Tooth #, material, shade, margin
- Stump shade (for all-ceramic)
- Contact/occlusion preferences
- Return date

### References
ADA Clinical Practice Guidelines
Crown and Bridge Prosthodontics standards', NULL, 'active', 'system', 'system'),

('Recall System Protocol', 'general', '1.0', '2025-01-01', NULL,
'## Recall System Protocol

**Effective:** January 1, 2025
**Version:** 1.0

### Recall Intervals

| Patient Type | Interval | Procedure |
|--------------|----------|-----------|
| Healthy adult | 6 months | Prophylaxis (D1110), exam, BW x-rays annually |
| Periodontal maintenance | 3-4 months | Perio maintenance (D4910), probing, exam |
| High caries risk | 3-4 months | Prophylaxis, fluoride, exam, BW annually |
| Pediatric | 6 months | Prophylaxis (D1120), fluoride, exam, sealants |
| Orthodontic | 6-8 weeks | Adjustment, hygiene check |

### Recall Workflow

**6 Weeks Before Due:**
1. System generates recall list
2. Front desk sends postcard/email/text (patient preference)
3. Document contact attempt in chart

**2 Weeks Before Due:**
1. Phone call to non-responders
2. Offer 3 appointment options
3. Schedule or document decline reason

**1 Week Overdue:**
1. Second phone call
2. Email with "We miss you" messaging
3. Flag for dentist review if 2+ cycles missed

**Dismissal Consideration:**
- 3 consecutive missed recall cycles
- Dentist reviews chart
- Certified letter if dismissing

### Special Populations

**Inactive Patients (12+ months):**
- Quarterly "Welcome back" campaign
- New patient exam if >3 years

**Insurance Changes:**
- Verify benefits at scheduling
- Update financial estimates

### Reports
Monthly: Recall compliance rate, overdue list, production from recall
Quarterly: Periodontal maintenance compliance', NULL, 'active', 'system', 'system');