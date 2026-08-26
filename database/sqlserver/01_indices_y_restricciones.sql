/* =============================================================================
   crm_finance — Bloque 1: indices y restricciones
   -----------------------------------------------------------------------------
   PROPUESTA. No se ha ejecutado nada. Revisar antes de aplicar.

   No requiere ningun cambio en el backend: solo respalda en la BD reglas que hoy
   el codigo ya valida, y quita el unico defecto de esquema que obliga al backend
   a compensar (el UNIQUE sobre una columna nullable).

   Probado contra SQL Server 2025 Express. Idempotente: se puede correr varias veces.
   ============================================================================= */

USE crm_finance;
GO

SET XACT_ABORT ON;
BEGIN TRANSACTION;
GO

/* -----------------------------------------------------------------------------
   A. bank_contract.interbank_code — UNIQUE sobre columna nullable
   -----------------------------------------------------------------------------
   SQL Server considera iguales los NULL en un indice unico, asi que hoy solo cabe
   UN contrato bancario sin codigo interbancario: el segundo alta sin codigo choca
   contra la clave. Un indice unico FILTRADO mantiene la unicidad de los codigos
   reales y deja de contar los nulos.

   Con esto se puede simplificar ContractService.ValidateInterbankCodeIsFreeAsync:
   el mensaje sobre "solo cabe uno sin codigo" deja de tener sentido.
   ----------------------------------------------------------------------------- */

DECLARE @uq sysname = (
    SELECT kc.name
    FROM sys.key_constraints kc
    JOIN sys.index_columns ic ON ic.object_id = kc.parent_object_id AND ic.index_id = kc.unique_index_id
    JOIN sys.columns c ON c.object_id = ic.object_id AND c.column_id = ic.column_id
    WHERE kc.parent_object_id = OBJECT_ID('dbo.bank_contract')
      AND kc.type = 'UQ'
      AND c.name = 'interbank_code');

IF @uq IS NOT NULL
BEGIN
    DECLARE @sql nvarchar(max) = N'ALTER TABLE dbo.bank_contract DROP CONSTRAINT ' + QUOTENAME(@uq);
    EXEC sp_executesql @sql;
END
GO

DROP INDEX IF EXISTS UX_bank_contract_interbank_code ON dbo.bank_contract;
CREATE UNIQUE INDEX UX_bank_contract_interbank_code
    ON dbo.bank_contract(interbank_code)
    WHERE interbank_code IS NOT NULL;
GO

/* -----------------------------------------------------------------------------
   B. clients.fiscal_id — unicidad que hoy solo vive en el backend
   -----------------------------------------------------------------------------
   ClientService la impone como regla de negocio, pero la BD la permite duplicada.
   Filtrado tambien, porque la columna es nullable.
   Comprobado: 0 duplicados actuales, el indice se crea sin limpiar datos.
   ----------------------------------------------------------------------------- */

DROP INDEX IF EXISTS UX_clients_fiscal_id ON dbo.clients;
CREATE UNIQUE INDEX UX_clients_fiscal_id
    ON dbo.clients(fiscal_id)
    WHERE fiscal_id IS NOT NULL;
GO

/* -----------------------------------------------------------------------------
   C. Indices sobre las claves foraneas
   -----------------------------------------------------------------------------
   Hoy NINGUNA de las 22 FK tiene indice de apoyo: todo filtro por client_id,
   product_id, user_id o contract_id hace recorrido de tabla, y cada borrado en la
   tabla padre tiene que recorrer entera la hija para comprobar la integridad.

   C.1 — Las que consultan los endpoints (mayor impacto)
   ----------------------------------------------------------------------------- */

DROP INDEX IF EXISTS IX_products_contract_client_id ON dbo.products_contract;
CREATE INDEX IX_products_contract_client_id ON dbo.products_contract(client_id);

DROP INDEX IF EXISTS IX_products_contract_product_id ON dbo.products_contract;
CREATE INDEX IX_products_contract_product_id ON dbo.products_contract(product_id);

DROP INDEX IF EXISTS IX_products_contract_user_id ON dbo.products_contract;
CREATE INDEX IX_products_contract_user_id ON dbo.products_contract(user_id);

DROP INDEX IF EXISTS IX_products_contract_contract_status_id ON dbo.products_contract;
CREATE INDEX IX_products_contract_contract_status_id ON dbo.products_contract(contract_status_id);

DROP INDEX IF EXISTS IX_bank_transaction_contract_id ON dbo.bank_transaction;
CREATE INDEX IX_bank_transaction_contract_id ON dbo.bank_transaction(contract_id);

DROP INDEX IF EXISTS IX_insurance_claims_contract_id ON dbo.insurance_claims;
CREATE INDEX IX_insurance_claims_contract_id ON dbo.insurance_claims(contract_id);

DROP INDEX IF EXISTS IX_comercial_oportunities_client_id ON dbo.comercial_oportunities;
CREATE INDEX IX_comercial_oportunities_client_id ON dbo.comercial_oportunities(client_id);

DROP INDEX IF EXISTS IX_comercial_oportunities_product_id ON dbo.comercial_oportunities;
CREATE INDEX IX_comercial_oportunities_product_id ON dbo.comercial_oportunities(product_id);

DROP INDEX IF EXISTS IX_comercial_oportunities_user_id ON dbo.comercial_oportunities;
CREATE INDEX IX_comercial_oportunities_user_id ON dbo.comercial_oportunities(user_id);

DROP INDEX IF EXISTS IX_comercial_oportunities_stage_id ON dbo.comercial_oportunities;
CREATE INDEX IX_comercial_oportunities_stage_id ON dbo.comercial_oportunities(stage_id);

DROP INDEX IF EXISTS IX_clients_assigned_user_id ON dbo.clients;
CREATE INDEX IX_clients_assigned_user_id ON dbo.clients(assigned_user_id);

DROP INDEX IF EXISTS IX_clients_type_person_id ON dbo.clients;
CREATE INDEX IX_clients_type_person_id ON dbo.clients(type_person_id);

DROP INDEX IF EXISTS IX_finance_products_area_id ON dbo.finance_products;
CREATE INDEX IX_finance_products_area_id ON dbo.finance_products(area_id);

DROP INDEX IF EXISTS IX_finance_products_finance_status_product_id ON dbo.finance_products;
CREATE INDEX IX_finance_products_finance_status_product_id ON dbo.finance_products(finance_status_product_id);
GO

/* C.2 — Las que apuntan a catalogos pequenos.
   Aportan poco a las consultas, pero evitan el recorrido de la tabla hija cuando
   se borra o modifica una fila del catalogo. */

DROP INDEX IF EXISTS IX_users_role_id ON dbo.users;
CREATE INDEX IX_users_role_id ON dbo.users(role_id);

DROP INDEX IF EXISTS IX_users_status_id ON dbo.users;
CREATE INDEX IX_users_status_id ON dbo.users(status_id);

DROP INDEX IF EXISTS IX_role_area_id ON dbo.role;
CREATE INDEX IX_role_area_id ON dbo.role(area_id);

DROP INDEX IF EXISTS IX_phisic_person_client_gender_id ON dbo.phisic_person_client;
CREATE INDEX IX_phisic_person_client_gender_id ON dbo.phisic_person_client(gender_id);

DROP INDEX IF EXISTS IX_phisic_person_client_civil_state_id ON dbo.phisic_person_client;
CREATE INDEX IX_phisic_person_client_civil_state_id ON dbo.phisic_person_client(civil_state_id);

DROP INDEX IF EXISTS IX_bank_transaction_transaction_type_id ON dbo.bank_transaction;
CREATE INDEX IX_bank_transaction_transaction_type_id ON dbo.bank_transaction(transaction_type_id);

DROP INDEX IF EXISTS IX_insurance_claims_disaster_state_id ON dbo.insurance_claims;
CREATE INDEX IX_insurance_claims_disaster_state_id ON dbo.insurance_claims(disaster_state_id);

DROP INDEX IF EXISTS IX_insurance_contranct_pay_form_id ON dbo.insurance_contranct;
CREATE INDEX IX_insurance_contranct_pay_form_id ON dbo.insurance_contranct(pay_form_id);
GO

/* -----------------------------------------------------------------------------
   D. CHECK constraints — red de seguridad para las validaciones del backend
   -----------------------------------------------------------------------------
   Hoy la BD no tiene ni un CHECK. Estos replican reglas que el backend ya aplica;
   sirven para que un bug futuro, un script manual o cualquier otro cliente no
   puedan meter datos imposibles.

   El limite de 1753 en las fechas es el mismo que usa RequiredDateAttribute, para
   que backend y BD no discrepen. Ataja el caso real que se guardaba en silencio:
   un POST sin fecha se almacenaba como 0001-01-01, porque las columnas de negocio
   son `date` (rango desde el ano 1) y ahi DateTime.MinValue no desborda.
   ----------------------------------------------------------------------------- */

IF OBJECT_ID('CK_products_contract_fechas') IS NOT NULL
    ALTER TABLE dbo.products_contract DROP CONSTRAINT CK_products_contract_fechas;
ALTER TABLE dbo.products_contract WITH CHECK ADD CONSTRAINT CK_products_contract_fechas
    CHECK (date_opening_issue >= '1753-01-01'
       AND (date_end IS NULL OR date_end >= date_opening_issue));

IF OBJECT_ID('CK_bank_contract_dia_corte') IS NOT NULL
    ALTER TABLE dbo.bank_contract DROP CONSTRAINT CK_bank_contract_dia_corte;
ALTER TABLE dbo.bank_contract WITH CHECK ADD CONSTRAINT CK_bank_contract_dia_corte
    CHECK (monthly_cutoff_day BETWEEN 1 AND 28);

IF OBJECT_ID('CK_phisic_person_client_nacimiento') IS NOT NULL
    ALTER TABLE dbo.phisic_person_client DROP CONSTRAINT CK_phisic_person_client_nacimiento;
ALTER TABLE dbo.phisic_person_client WITH CHECK ADD CONSTRAINT CK_phisic_person_client_nacimiento
    CHECK (birth_date >= '1753-01-01');

IF OBJECT_ID('CK_moral_person_client_constitucion') IS NOT NULL
    ALTER TABLE dbo.moral_person_client DROP CONSTRAINT CK_moral_person_client_constitucion;
ALTER TABLE dbo.moral_person_client WITH CHECK ADD CONSTRAINT CK_moral_person_client_constitucion
    CHECK (date_constitucion >= '1753-01-01');

IF OBJECT_ID('CK_insurance_claims_montos') IS NOT NULL
    ALTER TABLE dbo.insurance_claims DROP CONSTRAINT CK_insurance_claims_montos;
ALTER TABLE dbo.insurance_claims WITH CHECK ADD CONSTRAINT CK_insurance_claims_montos
    CHECK (amount_claimed > 0
       AND (amount_paid IS NULL OR (amount_paid >= 0 AND amount_paid <= amount_claimed))
       AND date_occurrence >= '1753-01-01');

IF OBJECT_ID('CK_bank_transaction_importe') IS NOT NULL
    ALTER TABLE dbo.bank_transaction DROP CONSTRAINT CK_bank_transaction_importe;
ALTER TABLE dbo.bank_transaction WITH CHECK ADD CONSTRAINT CK_bank_transaction_importe
    CHECK (amount > 0);

IF OBJECT_ID('CK_comercial_oportunities_probabilidad') IS NOT NULL
    ALTER TABLE dbo.comercial_oportunities DROP CONSTRAINT CK_comercial_oportunities_probabilidad;
ALTER TABLE dbo.comercial_oportunities WITH CHECK ADD CONSTRAINT CK_comercial_oportunities_probabilidad
    CHECK (success_probability BETWEEN 0 AND 100
       AND (estimated_mont IS NULL OR estimated_mont >= 0));
GO

COMMIT TRANSACTION;
GO

/* -----------------------------------------------------------------------------
   Verificacion
   ----------------------------------------------------------------------------- */

SELECT t.name AS tabla, i.name AS indice, i.is_unique, i.filter_definition
FROM sys.indexes i
JOIN sys.tables t ON t.object_id = i.object_id
WHERE i.name LIKE 'IX[_]%' OR i.name LIKE 'UX[_]%'
ORDER BY t.name, i.name;

SELECT OBJECT_NAME(parent_object_id) AS tabla, name AS restriccion, definition
FROM sys.check_constraints
ORDER BY tabla, name;
GO
