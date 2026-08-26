/* =============================================================================
   crm_finance — Marcha atras del bloque 1
   -----------------------------------------------------------------------------
   Deja el esquema exactamente como estaba antes de 01_indices_y_restricciones.sql:
   borra los 24 indices y los 7 CHECK, y devuelve interbank_code a su UNIQUE normal.

   No toca datos.
   ============================================================================= */

USE crm_finance;
GO

SET XACT_ABORT ON;
BEGIN TRANSACTION;
GO

/* A. CHECK constraints */
IF OBJECT_ID('CK_products_contract_fechas') IS NOT NULL
    ALTER TABLE dbo.products_contract DROP CONSTRAINT CK_products_contract_fechas;
IF OBJECT_ID('CK_bank_contract_dia_corte') IS NOT NULL
    ALTER TABLE dbo.bank_contract DROP CONSTRAINT CK_bank_contract_dia_corte;
IF OBJECT_ID('CK_phisic_person_client_nacimiento') IS NOT NULL
    ALTER TABLE dbo.phisic_person_client DROP CONSTRAINT CK_phisic_person_client_nacimiento;
IF OBJECT_ID('CK_moral_person_client_constitucion') IS NOT NULL
    ALTER TABLE dbo.moral_person_client DROP CONSTRAINT CK_moral_person_client_constitucion;
IF OBJECT_ID('CK_insurance_claims_montos') IS NOT NULL
    ALTER TABLE dbo.insurance_claims DROP CONSTRAINT CK_insurance_claims_montos;
IF OBJECT_ID('CK_bank_transaction_importe') IS NOT NULL
    ALTER TABLE dbo.bank_transaction DROP CONSTRAINT CK_bank_transaction_importe;
IF OBJECT_ID('CK_comercial_oportunities_probabilidad') IS NOT NULL
    ALTER TABLE dbo.comercial_oportunities DROP CONSTRAINT CK_comercial_oportunities_probabilidad;
GO

/* B. Indices de claves foraneas */
DROP INDEX IF EXISTS IX_products_contract_client_id ON dbo.products_contract;
DROP INDEX IF EXISTS IX_products_contract_product_id ON dbo.products_contract;
DROP INDEX IF EXISTS IX_products_contract_user_id ON dbo.products_contract;
DROP INDEX IF EXISTS IX_products_contract_contract_status_id ON dbo.products_contract;
DROP INDEX IF EXISTS IX_bank_transaction_contract_id ON dbo.bank_transaction;
DROP INDEX IF EXISTS IX_bank_transaction_transaction_type_id ON dbo.bank_transaction;
DROP INDEX IF EXISTS IX_insurance_claims_contract_id ON dbo.insurance_claims;
DROP INDEX IF EXISTS IX_insurance_claims_disaster_state_id ON dbo.insurance_claims;
DROP INDEX IF EXISTS IX_insurance_contranct_pay_form_id ON dbo.insurance_contranct;
DROP INDEX IF EXISTS IX_comercial_oportunities_client_id ON dbo.comercial_oportunities;
DROP INDEX IF EXISTS IX_comercial_oportunities_product_id ON dbo.comercial_oportunities;
DROP INDEX IF EXISTS IX_comercial_oportunities_user_id ON dbo.comercial_oportunities;
DROP INDEX IF EXISTS IX_comercial_oportunities_stage_id ON dbo.comercial_oportunities;
DROP INDEX IF EXISTS IX_clients_assigned_user_id ON dbo.clients;
DROP INDEX IF EXISTS IX_clients_type_person_id ON dbo.clients;
DROP INDEX IF EXISTS IX_finance_products_area_id ON dbo.finance_products;
DROP INDEX IF EXISTS IX_finance_products_finance_status_product_id ON dbo.finance_products;
DROP INDEX IF EXISTS IX_users_role_id ON dbo.users;
DROP INDEX IF EXISTS IX_users_status_id ON dbo.users;
DROP INDEX IF EXISTS IX_role_area_id ON dbo.role;
DROP INDEX IF EXISTS IX_phisic_person_client_gender_id ON dbo.phisic_person_client;
DROP INDEX IF EXISTS IX_phisic_person_client_civil_state_id ON dbo.phisic_person_client;
GO

/* C. clients.fiscal_id vuelve a admitir duplicados */
DROP INDEX IF EXISTS UX_clients_fiscal_id ON dbo.clients;
GO

/* D. interbank_code vuelve al UNIQUE normal (solo un NULL permitido) */
DROP INDEX IF EXISTS UX_bank_contract_interbank_code ON dbo.bank_contract;

IF NOT EXISTS (SELECT 1 FROM sys.key_constraints
               WHERE parent_object_id = OBJECT_ID('dbo.bank_contract') AND type = 'UQ')
    ALTER TABLE dbo.bank_contract ADD CONSTRAINT UQ_bank_contract_interbank_code UNIQUE (interbank_code);
GO

COMMIT TRANSACTION;
GO

SELECT 'indices restantes' AS que, COUNT(*) AS cuantos
FROM sys.indexes WHERE name LIKE 'IX[_]%' OR name LIKE 'UX[_]%'
UNION ALL
SELECT 'checks restantes', COUNT(*) FROM sys.check_constraints;
GO
