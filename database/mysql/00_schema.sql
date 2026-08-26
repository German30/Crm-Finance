-- =============================================================================
-- crm_finance — Esquema completo (MySQL 8)
-- -----------------------------------------------------------------------------
-- Traducido desde el esquema de SQL Server, que fue la base original.
-- Los datos de los catalogos van aparte, en 00_seed.sql. Ejecuta primero este.
--
-- Idempotente: todo va en CREATE TABLE IF NOT EXISTS, con sus claves, indices y
-- restricciones EN LINEA. En MySQL es la unica forma limpia de conseguirlo, porque
-- no existe ADD CONSTRAINT IF NOT EXISTS ni CREATE INDEX IF NOT EXISTS. Por eso
-- las tablas van en orden topologico: cada una despues de las que referencia.
--
-- Diferencias que impone MySQL frente al original en SQL Server:
--
--   * VARCHAR(MAX)  -> TEXT
--   * IDENTITY(1,1) -> AUTO_INCREMENT
--   * GETDATE()     -> CURRENT_TIMESTAMP
--   * El indice unico de interbank_code ya NO necesita ser filtrado: MySQL trata
--     los NULL como distintos entre si, asi que un UNIQUE normal ya permite varios
--     contratos sin codigo. En SQL Server hacia falta un indice filtrado.
--   * Intercalacion utf8mb4_0900_ai_ci: insensible a mayusculas Y A ACENTOS. La
--     original (Modern_Spanish_CI_AS) era sensible a acentos. En la practica hace
--     las busquedas mas permisivas, que para un CRM en espanol es preferible.
-- =============================================================================

-- Fija la codificacion de la SESION. Sin esto el resultado depende de como se
-- haya invocado el cliente: si negocia latin1, los acentos de este fichero (UTF-8)
-- se guardan doblemente codificados y "Deposito" acaba como "DepAsito". Ademas de
-- verse mal, rompe OperationService, que deduce el signo de cada movimiento del
-- nombre de su transaction_type y con el texto corrupto deja de reconocerlos.
SET NAMES utf8mb4;

CREATE DATABASE IF NOT EXISTS `crm_finance`
    CHARACTER SET utf8mb4
    COLLATE utf8mb4_0900_ai_ci;

USE `crm_finance`;

CREATE TABLE IF NOT EXISTS `area` (
    `area_id`        INT          NOT NULL AUTO_INCREMENT,
    `area_name`      VARCHAR(255) NOT NULL,
    `description`    TEXT         NULL,
    `date_creation`  DATETIME     NULL DEFAULT CURRENT_TIMESTAMP,
    PRIMARY KEY (`area_id`),
    CONSTRAINT `UQ_area_area_name` UNIQUE (`area_name`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

CREATE TABLE IF NOT EXISTS `user_status` (
    `status_id`      INT          NOT NULL AUTO_INCREMENT,
    `status_name`    VARCHAR(30)  NOT NULL,
    `description`    VARCHAR(255) NULL,
    `date_creation`  DATETIME     NULL DEFAULT CURRENT_TIMESTAMP,
    PRIMARY KEY (`status_id`),
    CONSTRAINT `UQ_user_status_status_name` UNIQUE (`status_name`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

CREATE TABLE IF NOT EXISTS `type_person` (
    `type_person_id`  INT          NOT NULL AUTO_INCREMENT,
    `type_name`       VARCHAR(50)  NOT NULL,
    `description`     VARCHAR(255) NULL,
    `date_creation`   DATETIME     NULL DEFAULT CURRENT_TIMESTAMP,
    PRIMARY KEY (`type_person_id`),
    CONSTRAINT `UQ_type_person_type_name` UNIQUE (`type_name`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

CREATE TABLE IF NOT EXISTS `gender` (
    `gender_id`      INT         NOT NULL AUTO_INCREMENT,
    `gender_name`    VARCHAR(50) NULL,
    `register_date`  DATETIME    NULL DEFAULT CURRENT_TIMESTAMP,
    PRIMARY KEY (`gender_id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

CREATE TABLE IF NOT EXISTS `civil_state` (
    `civil_state_id`    INT          NOT NULL AUTO_INCREMENT,
    `civil_state_name`  VARCHAR(100) NULL,
    `register_date`     DATETIME     NULL DEFAULT CURRENT_TIMESTAMP,
    PRIMARY KEY (`civil_state_id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

CREATE TABLE IF NOT EXISTS `finace_status_product` (
    `finance_status_product_id`    INT         NOT NULL AUTO_INCREMENT,
    `finance_status_product_name`  VARCHAR(50) NULL,
    `register_date`                DATETIME    NULL DEFAULT CURRENT_TIMESTAMP,
    PRIMARY KEY (`finance_status_product_id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

CREATE TABLE IF NOT EXISTS `contract_status` (
    `contract_status_id`    INT         NOT NULL AUTO_INCREMENT,
    `contract_status_name`  VARCHAR(50) NULL,
    `date_creation`         DATETIME    NULL DEFAULT CURRENT_TIMESTAMP,
    PRIMARY KEY (`contract_status_id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

CREATE TABLE IF NOT EXISTS `pay_form` (
    `pay_form_id`    INT         NOT NULL AUTO_INCREMENT,
    `pay_form_name`  VARCHAR(50) NULL,
    `date_created`   DATETIME    NULL DEFAULT CURRENT_TIMESTAMP,
    PRIMARY KEY (`pay_form_id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

CREATE TABLE IF NOT EXISTS `stage` (
    `stage_id`      INT         NOT NULL AUTO_INCREMENT,
    `stage_name`    VARCHAR(50) NULL,
    `date_created`  DATETIME    NULL DEFAULT CURRENT_TIMESTAMP,
    PRIMARY KEY (`stage_id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

CREATE TABLE IF NOT EXISTS `transaction_type` (
    `transaction_type_id`    INT         NOT NULL AUTO_INCREMENT,
    `transaction_type_name`  VARCHAR(50) NULL,
    `data_created`           DATETIME    NULL DEFAULT CURRENT_TIMESTAMP,
    PRIMARY KEY (`transaction_type_id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

CREATE TABLE IF NOT EXISTS `disaster_state` (
    `disaster_state_id`    INT         NOT NULL AUTO_INCREMENT,
    `disaster_state_name`  VARCHAR(50) NULL,
    `date_created`         DATETIME    NULL DEFAULT CURRENT_TIMESTAMP,
    PRIMARY KEY (`disaster_state_id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

CREATE TABLE IF NOT EXISTS `role` (
    `role_id`       INT          NOT NULL AUTO_INCREMENT,
    `area_id`       INT          NOT NULL,
    `role_name`     VARCHAR(100) NOT NULL,
    `category`      VARCHAR(100) NOT NULL,
    `description`   TEXT         NULL,
    `date_created`  DATETIME     NULL DEFAULT CURRENT_TIMESTAMP,
    PRIMARY KEY (`role_id`),
    CONSTRAINT `UQ_role_role_name` UNIQUE (`role_name`),
    KEY `IX_role_area_id` (`area_id`),
    CONSTRAINT `FK_role_area_id` FOREIGN KEY (`area_id`) REFERENCES `area` (`area_id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

CREATE TABLE IF NOT EXISTS `finance_products` (
    `product_id`                 INT          NOT NULL AUTO_INCREMENT,
    `area_id`                    INT          NOT NULL,
    `product_name`               VARCHAR(100) NOT NULL,
    `description`                TEXT         NULL,
    `tasa_interes_o_prima_base`  DECIMAL(5,2) NULL DEFAULT 0.00,
    `finance_status_product_id`  INT          NULL DEFAULT 1,
    `date_creation`              DATETIME     NULL DEFAULT CURRENT_TIMESTAMP,
    PRIMARY KEY (`product_id`),
    KEY `IX_finance_products_area_id` (`area_id`),
    KEY `IX_finance_products_finance_status_product_id` (`finance_status_product_id`),
    CONSTRAINT `FK_finance_products_area_id` FOREIGN KEY (`area_id`) REFERENCES `area` (`area_id`),
    CONSTRAINT `FK_finance_products_finance_status_product_id` FOREIGN KEY (`finance_status_product_id`) REFERENCES `finace_status_product` (`finance_status_product_id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

CREATE TABLE IF NOT EXISTS `users` (
    `user_id`        INT          NOT NULL AUTO_INCREMENT,
    `role_id`        INT          NOT NULL,
    `status_id`      INT          NOT NULL DEFAULT 1,
    `name`           VARCHAR(100) NOT NULL,
    `email`          VARCHAR(100) NOT NULL,
    `password_hash`  VARCHAR(255) NOT NULL,
    `creation_date`  DATETIME     NULL DEFAULT CURRENT_TIMESTAMP,
    PRIMARY KEY (`user_id`),
    CONSTRAINT `UQ_users_email` UNIQUE (`email`),
    KEY `IX_users_role_id` (`role_id`),
    KEY `IX_users_status_id` (`status_id`),
    CONSTRAINT `FK_users_role_id` FOREIGN KEY (`role_id`) REFERENCES `role` (`role_id`),
    CONSTRAINT `FK_users_status_id` FOREIGN KEY (`status_id`) REFERENCES `user_status` (`status_id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

CREATE TABLE IF NOT EXISTS `clients` (
    `client_id`         INT          NOT NULL AUTO_INCREMENT,
    `type_person_id`    INT          NOT NULL,
    `fiscal_id`         VARCHAR(50)  NULL,
    `email`             VARCHAR(100) NULL,
    `phone`             VARCHAR(20)  NULL,
    `address_fiscal`    TEXT         NULL,
    `assigned_user_id`  INT          NULL,
    `register_date`     DATETIME     NULL DEFAULT CURRENT_TIMESTAMP,
    PRIMARY KEY (`client_id`),
    KEY `IX_clients_assigned_user_id` (`assigned_user_id`),
    KEY `IX_clients_type_person_id` (`type_person_id`),
    CONSTRAINT `UX_clients_fiscal_id` UNIQUE (`fiscal_id`),
    CONSTRAINT `FK_clients_assigned_user_id` FOREIGN KEY (`assigned_user_id`) REFERENCES `users` (`user_id`) ON DELETE SET NULL,
    CONSTRAINT `FK_clients_type_person_id` FOREIGN KEY (`type_person_id`) REFERENCES `type_person` (`type_person_id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

CREATE TABLE IF NOT EXISTS `phisic_person_client` (
    `client_id`        INT          NOT NULL,
    `names`            VARCHAR(100) NOT NULL,
    `father_lastname`  VARCHAR(50)  NOT NULL,
    `mother_lastname`  VARCHAR(50)  NOT NULL,
    `birth_date`       DATE         NOT NULL,
    `gender_id`        INT          NOT NULL,
    `civil_state_id`   INT          NOT NULL,
    PRIMARY KEY (`client_id`),
    KEY `IX_phisic_person_client_civil_state_id` (`civil_state_id`),
    KEY `IX_phisic_person_client_gender_id` (`gender_id`),
    CONSTRAINT `FK_phisic_person_client_civil_state_id` FOREIGN KEY (`civil_state_id`) REFERENCES `civil_state` (`civil_state_id`),
    CONSTRAINT `FK_phisic_person_client_client_id` FOREIGN KEY (`client_id`) REFERENCES `clients` (`client_id`) ON DELETE CASCADE,
    CONSTRAINT `FK_phisic_person_client_gender_id` FOREIGN KEY (`gender_id`) REFERENCES `gender` (`gender_id`),
    CONSTRAINT `CK_phisic_person_client_nacimiento` CHECK (`birth_date`>='1753-01-01')
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

CREATE TABLE IF NOT EXISTS `moral_person_client` (
    `client_id`                  INT          NOT NULL,
    `social_razon`               VARCHAR(200) NOT NULL,
    `comercial_name`             VARCHAR(150) NULL,
    `date_constitucion`          DATE         NOT NULL,
    `comercial_activity`         VARCHAR(150) NULL,
    `representative_legal_name`  VARCHAR(150) NOT NULL,
    `representative_id`          VARCHAR(50)  NULL,
    PRIMARY KEY (`client_id`),
    CONSTRAINT `FK_moral_person_client_client_id` FOREIGN KEY (`client_id`) REFERENCES `clients` (`client_id`) ON DELETE CASCADE,
    CONSTRAINT `CK_moral_person_client_constitucion` CHECK (`date_constitucion`>='1753-01-01')
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

CREATE TABLE IF NOT EXISTS `products_contract` (
    `contract_id`         INT         NOT NULL AUTO_INCREMENT,
    `client_id`           INT         NOT NULL,
    `product_id`          INT         NOT NULL,
    `user_id`             INT         NOT NULL,
    `reference_number`    VARCHAR(50) NOT NULL,
    `date_opening_issue`  DATE        NOT NULL,
    `date_end`            DATE        NULL,
    `contract_status_id`  INT         NOT NULL,
    `date_register`       DATETIME    NULL DEFAULT CURRENT_TIMESTAMP,
    PRIMARY KEY (`contract_id`),
    CONSTRAINT `UQ_products_contract_reference_number` UNIQUE (`reference_number`),
    KEY `IX_products_contract_client_id` (`client_id`),
    KEY `IX_products_contract_contract_status_id` (`contract_status_id`),
    KEY `IX_products_contract_product_id` (`product_id`),
    KEY `IX_products_contract_user_id` (`user_id`),
    CONSTRAINT `FK_products_contract_client_id` FOREIGN KEY (`client_id`) REFERENCES `clients` (`client_id`),
    CONSTRAINT `FK_products_contract_contract_status_id` FOREIGN KEY (`contract_status_id`) REFERENCES `contract_status` (`contract_status_id`),
    CONSTRAINT `FK_products_contract_product_id` FOREIGN KEY (`product_id`) REFERENCES `finance_products` (`product_id`),
    CONSTRAINT `FK_products_contract_user_id` FOREIGN KEY (`user_id`) REFERENCES `users` (`user_id`),
    CONSTRAINT `CK_products_contract_fechas` CHECK (`date_opening_issue`>='1753-01-01' AND (`date_end` IS NULL OR `date_end`>=`date_opening_issue`))
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

CREATE TABLE IF NOT EXISTS `comercial_oportunities` (
    `oportunity_id`         INT           NOT NULL AUTO_INCREMENT,
    `client_id`             INT           NOT NULL,
    `product_id`            INT           NOT NULL,
    `user_id`               INT           NOT NULL,
    `estimated_mont`        DECIMAL(15,2) NULL,
    `stage_id`              INT           NOT NULL,
    `success_probability`   INT           NULL DEFAULT 10,
    `date_estimated_close`  DATE          NULL,
    `date_register`         DATETIME      NULL DEFAULT CURRENT_TIMESTAMP,
    PRIMARY KEY (`oportunity_id`),
    KEY `IX_comercial_oportunities_client_id` (`client_id`),
    KEY `IX_comercial_oportunities_product_id` (`product_id`),
    KEY `IX_comercial_oportunities_stage_id` (`stage_id`),
    KEY `IX_comercial_oportunities_user_id` (`user_id`),
    CONSTRAINT `FK_comercial_oportunities_client_id` FOREIGN KEY (`client_id`) REFERENCES `clients` (`client_id`) ON DELETE CASCADE,
    CONSTRAINT `FK_comercial_oportunities_product_id` FOREIGN KEY (`product_id`) REFERENCES `finance_products` (`product_id`),
    CONSTRAINT `FK_comercial_oportunities_stage_id` FOREIGN KEY (`stage_id`) REFERENCES `stage` (`stage_id`),
    CONSTRAINT `FK_comercial_oportunities_user_id` FOREIGN KEY (`user_id`) REFERENCES `users` (`user_id`),
    CONSTRAINT `CK_comercial_oportunities_probabilidad` CHECK (`success_probability`>=(0) AND `success_probability`<=(100) AND (`estimated_mont` IS NULL OR `estimated_mont`>=(0)))
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

CREATE TABLE IF NOT EXISTS `bank_contract` (
    `contract_id`           INT           NOT NULL,
    `interbank_code`        VARCHAR(18)   NULL,
    `balance_actual`        DECIMAL(15,2) NOT NULL DEFAULT 0.00,
    `loan_amount_granted`   DECIMAL(15,2) NULL DEFAULT 0.00,
    `agreed_interest_rate`  DECIMAL(5,2)  NOT NULL,
    `monthly_cutoff_day`    INT           NULL DEFAULT 1,
    PRIMARY KEY (`contract_id`),
    CONSTRAINT `UX_bank_contract_interbank_code` UNIQUE (`interbank_code`),
    CONSTRAINT `FK_bank_contract_contract_id` FOREIGN KEY (`contract_id`) REFERENCES `products_contract` (`contract_id`) ON DELETE CASCADE,
    CONSTRAINT `CK_bank_contract_dia_corte` CHECK (`monthly_cutoff_day`>=(1) AND `monthly_cutoff_day`<=(28))
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

CREATE TABLE IF NOT EXISTS `insurance_contranct` (
    `contract_id`           INT           NOT NULL,
    `insurance_sume_total`  DECIMAL(15,2) NOT NULL,
    `total_annual_premium`  DECIMAL(15,2) NOT NULL,
    `pay_form_id`           INT           NOT NULL DEFAULT 4,
    `porcent_deductible`    DECIMAL(4,2)  NULL DEFAULT 0.00,
    `beneficiary_name`      TEXT          NULL,
    PRIMARY KEY (`contract_id`),
    KEY `IX_insurance_contranct_pay_form_id` (`pay_form_id`),
    CONSTRAINT `FK_insurance_contranct_contract_id` FOREIGN KEY (`contract_id`) REFERENCES `products_contract` (`contract_id`) ON DELETE CASCADE,
    CONSTRAINT `FK_insurance_contranct_pay_form_id` FOREIGN KEY (`pay_form_id`) REFERENCES `pay_form` (`pay_form_id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

CREATE TABLE IF NOT EXISTS `bank_transaction` (
    `transaction_id`       INT           NOT NULL AUTO_INCREMENT,
    `contract_id`          INT           NOT NULL,
    `transaction_type_id`  INT           NOT NULL,
    `amount`               DECIMAL(15,2) NOT NULL,
    `date_transaction`     DATETIME      NULL DEFAULT CURRENT_TIMESTAMP,
    `description`          VARCHAR(255)  NULL,
    PRIMARY KEY (`transaction_id`),
    KEY `IX_bank_transaction_contract_id` (`contract_id`),
    KEY `IX_bank_transaction_transaction_type_id` (`transaction_type_id`),
    CONSTRAINT `FK_bank_transaction_contract_id` FOREIGN KEY (`contract_id`) REFERENCES `products_contract` (`contract_id`),
    CONSTRAINT `FK_bank_transaction_transaction_type_id` FOREIGN KEY (`transaction_type_id`) REFERENCES `transaction_type` (`transaction_type_id`),
    CONSTRAINT `CK_bank_transaction_importe` CHECK (`amount`>(0))
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

CREATE TABLE IF NOT EXISTS `insurance_claims` (
    `insurance_id`       INT           NOT NULL AUTO_INCREMENT,
    `contract_id`        INT           NOT NULL,
    `report_number`      VARCHAR(50)   NOT NULL,
    `date_occurrence`    DATE          NOT NULL,
    `amount_claimed`     DECIMAL(15,2) NOT NULL,
    `amount_paid`        DECIMAL(15,2) NULL DEFAULT 0.00,
    `disaster_state_id`  INT           NOT NULL DEFAULT 1,
    `report_details`     TEXT          NULL,
    `date_register`      DATETIME      NULL DEFAULT CURRENT_TIMESTAMP,
    PRIMARY KEY (`insurance_id`),
    CONSTRAINT `UQ_insurance_claims_report_number` UNIQUE (`report_number`),
    KEY `IX_insurance_claims_contract_id` (`contract_id`),
    KEY `IX_insurance_claims_disaster_state_id` (`disaster_state_id`),
    CONSTRAINT `FK_insurance_claims_contract_id` FOREIGN KEY (`contract_id`) REFERENCES `insurance_contranct` (`contract_id`),
    CONSTRAINT `FK_insurance_claims_disaster_state_id` FOREIGN KEY (`disaster_state_id`) REFERENCES `disaster_state` (`disaster_state_id`),
    CONSTRAINT `CK_insurance_claims_montos` CHECK (`amount_claimed`>(0) AND (`amount_paid` IS NULL OR `amount_paid`>=(0) AND `amount_paid`<=`amount_claimed`) AND `date_occurrence`>='1753-01-01')
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

-- Resumen
SELECT 'tablas' AS objeto, COUNT(*) AS total FROM information_schema.TABLES WHERE TABLE_SCHEMA = 'crm_finance'
UNION ALL SELECT 'claves foraneas', COUNT(DISTINCT CONSTRAINT_NAME) FROM information_schema.TABLE_CONSTRAINTS WHERE CONSTRAINT_SCHEMA = 'crm_finance' AND CONSTRAINT_TYPE = 'FOREIGN KEY'
UNION ALL SELECT 'restricciones CHECK', COUNT(*) FROM information_schema.CHECK_CONSTRAINTS WHERE CONSTRAINT_SCHEMA = 'crm_finance'
UNION ALL SELECT 'indices', COUNT(DISTINCT CONCAT(TABLE_NAME,'.',INDEX_NAME)) FROM information_schema.STATISTICS WHERE TABLE_SCHEMA = 'crm_finance' AND INDEX_NAME <> 'PRIMARY';
