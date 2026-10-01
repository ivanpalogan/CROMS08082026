-- 75_seed_causes_of_death.sql
-- Starter list of common causes of death for the Death Registration cause
-- combos (immediate / antecedent / underlying). Idempotent; ASCII only.
-- Editable later in Master Files. Not an ICD-10 import.
USE `croms`;

INSERT INTO `causes_of_death` (`name`)
SELECT v FROM (
  SELECT 'Cardiopulmonary arrest' v UNION ALL SELECT 'Cardiac arrest' UNION ALL
  SELECT 'Cardiorespiratory arrest' UNION ALL SELECT 'Respiratory failure' UNION ALL
  SELECT 'Acute respiratory distress syndrome' UNION ALL SELECT 'Acute myocardial infarction' UNION ALL
  SELECT 'Myocardial infarction' UNION ALL SELECT 'Congestive heart failure' UNION ALL
  SELECT 'Coronary artery disease' UNION ALL SELECT 'Hypertensive cardiovascular disease' UNION ALL
  SELECT 'Hypertension' UNION ALL SELECT 'Cerebrovascular accident' UNION ALL
  SELECT 'Cerebrovascular disease' UNION ALL SELECT 'Intracerebral hemorrhage' UNION ALL
  SELECT 'Ischemic stroke' UNION ALL SELECT 'Hemorrhagic stroke' UNION ALL
  SELECT 'Pneumonia' UNION ALL SELECT 'Community-acquired pneumonia' UNION ALL
  SELECT 'Aspiration pneumonia' UNION ALL SELECT 'Pulmonary tuberculosis' UNION ALL
  SELECT 'Chronic obstructive pulmonary disease' UNION ALL SELECT 'Bronchial asthma' UNION ALL
  SELECT 'Pulmonary embolism' UNION ALL SELECT 'COVID-19' UNION ALL
  SELECT 'Sepsis' UNION ALL SELECT 'Septic shock' UNION ALL
  SELECT 'Hypovolemic shock' UNION ALL SELECT 'Multiple organ failure' UNION ALL
  SELECT 'Acute renal failure' UNION ALL SELECT 'Chronic kidney disease' UNION ALL
  SELECT 'End-stage renal disease' UNION ALL SELECT 'Diabetes mellitus' UNION ALL
  SELECT 'Diabetes mellitus type 2' UNION ALL SELECT 'Diabetic ketoacidosis' UNION ALL
  SELECT 'Liver cirrhosis' UNION ALL SELECT 'Hepatocellular carcinoma' UNION ALL
  SELECT 'Upper gastrointestinal bleeding' UNION ALL SELECT 'Malignant neoplasm' UNION ALL
  SELECT 'Breast cancer' UNION ALL SELECT 'Lung cancer' UNION ALL
  SELECT 'Colon cancer' UNION ALL SELECT 'Cervical cancer' UNION ALL
  SELECT 'Prostate cancer' UNION ALL SELECT 'Leukemia' UNION ALL
  SELECT 'Dengue hemorrhagic fever' UNION ALL SELECT 'Typhoid fever' UNION ALL
  SELECT 'Acute gastroenteritis' UNION ALL SELECT 'HIV/AIDS' UNION ALL
  SELECT 'Meningitis' UNION ALL SELECT 'Head injury' UNION ALL
  SELECT 'Multiple trauma' UNION ALL SELECT 'Vehicular accident' UNION ALL
  SELECT 'Drowning' UNION ALL SELECT 'Gunshot wound' UNION ALL
  SELECT 'Stab wound' UNION ALL SELECT 'Burns' UNION ALL
  SELECT 'Electrocution' UNION ALL SELECT 'Poisoning' UNION ALL
  SELECT 'Hanging' UNION ALL SELECT 'Post-partum hemorrhage' UNION ALL
  SELECT 'Eclampsia' UNION ALL SELECT 'Prematurity' UNION ALL
  SELECT 'Neonatal sepsis' UNION ALL SELECT 'Birth asphyxia' UNION ALL
  SELECT 'Congenital heart disease' UNION ALL SELECT 'Old age' UNION ALL
  SELECT 'Senility'
) d WHERE NOT EXISTS (SELECT 1 FROM `causes_of_death` x WHERE x.`name` = d.v);
