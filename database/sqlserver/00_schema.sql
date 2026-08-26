/* =============================================================================
   crm_finance — Esquema completo
   -----------------------------------------------------------------------------
   Generado desde la base de datos viva. Recrea el esquema entero desde cero:
   tablas, identidades, valores por defecto, claves primarias y foraneas,
   restricciones UNIQUE y CHECK, e indices.

   Los datos de los catalogos van aparte, en 00_seed.sql. Ejecuta primero este.

   Idempotente: cada objeto se crea solo si no existe, asi que se puede volver a
   ejecutar sobre una base ya montada. Es lo que permite que el servicio db-init
   del docker-compose corra en cada arranque sin fallar.
   OJO: solo CREA lo que falta; no modifica una tabla que ya exista. Los cambios
   sobre tablas existentes van en scripts incrementales aparte (01_..., 02_...).

   A diferencia de la BD original, aqui todas las restricciones llevan nombre
   explicito (PK_tabla, FK_hija_padre, UQ_tabla_columna) en vez de los nombres
   autogenerados tipo PK__area__985D6D6BCCD2D166, que cambian en cada creacion.
   ============================================================================= */

IF DB_ID('crm_finance') IS NULL
    CREATE DATABASE [crm_finance] COLLATE Modern_Spanish_CI_AS;
GO

USE [crm_finance];
GO

SET XACT_ABORT ON;

-- ANSI_NULLS y QUOTED_IDENTIFIER deben estar en ON para poder crear los indices
-- FILTRADOS del final. SSMS y ADO.NET los traen asi por defecto, pero sqlcmd NO:
-- sin esto, ejecutar el script por linea de comandos falla con el error 1934.
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

/* -----------------------------------------------------------------------------
   Tablas (23)
   -------------------------------------------------------------------------- */

IF OBJECT_ID('dbo.area', 'U') IS NULL
CREATE TABLE dbo.[area] (
    [area_id]       int          IDENTITY(1,1) NOT NULL,
    [area_name]     varchar(255) NOT NULL,
    [description]   varchar(MAX) NULL,
    [date_creation] datetime     NULL CONSTRAINT [DF_area_date_creation] DEFAULT (getdate()),
    CONSTRAINT [PK_area] PRIMARY KEY ([area_id]),
    CONSTRAINT [UQ_area_area_name] UNIQUE ([area_name])
);
GO

IF OBJECT_ID('dbo.bank_contract', 'U') IS NULL
CREATE TABLE dbo.[bank_contract] (
    [contract_id]          int           NOT NULL,
    [interbank_code]       varchar(18)   NULL,
    [balance_actual]       decimal(15,2) NOT NULL CONSTRAINT [DF_bank_contract_balance_actual] DEFAULT ((0.00)),
    [loan_amount_granted]  decimal(15,2) NULL CONSTRAINT [DF_bank_contract_loan_amount_granted] DEFAULT ((0.00)),
    [agreed_interest_rate] decimal(5,2)  NOT NULL,
    [monthly_cutoff_day]   int           NULL CONSTRAINT [DF_bank_contract_monthly_cutoff_day] DEFAULT ((1)),
    CONSTRAINT [PK_bank_contract] PRIMARY KEY ([contract_id])
);
GO

IF OBJECT_ID('dbo.bank_transaction', 'U') IS NULL
CREATE TABLE dbo.[bank_transaction] (
    [transaction_id]      int           IDENTITY(1,1) NOT NULL,
    [contract_id]         int           NOT NULL,
    [transaction_type_id] int           NOT NULL,
    [amount]              decimal(15,2) NOT NULL,
    [date_transaction]    datetime      NULL CONSTRAINT [DF_bank_transaction_date_transaction] DEFAULT (getdate()),
    [description]         varchar(255)  NULL,
    CONSTRAINT [PK_bank_transaction] PRIMARY KEY ([transaction_id])
);
GO

IF OBJECT_ID('dbo.civil_state', 'U') IS NULL
CREATE TABLE dbo.[civil_state] (
    [civil_state_id]   int          IDENTITY(1,1) NOT NULL,
    [civil_state_name] varchar(100) NULL,
    [register_date]    datetime     NULL CONSTRAINT [DF_civil_state_register_date] DEFAULT (getdate()),
    CONSTRAINT [PK_civil_state] PRIMARY KEY ([civil_state_id])
);
GO

IF OBJECT_ID('dbo.clients', 'U') IS NULL
CREATE TABLE dbo.[clients] (
    [client_id]        int          IDENTITY(1,1) NOT NULL,
    [type_person_id]   int          NOT NULL,
    [fiscal_id]        varchar(50)  NULL,
    [email]            varchar(100) NULL,
    [phone]            varchar(20)  NULL,
    [address_fiscal]   varchar(MAX) NULL,
    [assigned_user_id] int          NULL,
    [register_date]    datetime     NULL CONSTRAINT [DF_clients_register_date] DEFAULT (getdate()),
    CONSTRAINT [PK_clients] PRIMARY KEY ([client_id])
);
GO

IF OBJECT_ID('dbo.comercial_oportunities', 'U') IS NULL
CREATE TABLE dbo.[comercial_oportunities] (
    [oportunity_id]        int           IDENTITY(1,1) NOT NULL,
    [client_id]            int           NOT NULL,
    [product_id]           int           NOT NULL,
    [user_id]              int           NOT NULL,
    [estimated_mont]       decimal(15,2) NULL,
    [stage_id]             int           NOT NULL,
    [success_probability]  int           NULL CONSTRAINT [DF_comercial_oportunities_success_probability] DEFAULT ((10)),
    [date_estimated_close] date          NULL,
    [date_register]        datetime      NULL CONSTRAINT [DF_comercial_oportunities_date_register] DEFAULT (getdate()),
    CONSTRAINT [PK_comercial_oportunities] PRIMARY KEY ([oportunity_id])
);
GO

IF OBJECT_ID('dbo.contract_status', 'U') IS NULL
CREATE TABLE dbo.[contract_status] (
    [contract_status_id]   int         IDENTITY(1,1) NOT NULL,
    [contract_status_name] varchar(50) NULL,
    [date_creation]        datetime    NULL CONSTRAINT [DF_contract_status_date_creation] DEFAULT (getdate()),
    CONSTRAINT [PK_contract_status] PRIMARY KEY ([contract_status_id])
);
GO

IF OBJECT_ID('dbo.disaster_state', 'U') IS NULL
CREATE TABLE dbo.[disaster_state] (
    [disaster_state_id]   int         IDENTITY(1,1) NOT NULL,
    [disaster_state_name] varchar(50) NULL,
    [date_created]        datetime    NULL CONSTRAINT [DF_disaster_state_date_created] DEFAULT (getdate()),
    CONSTRAINT [PK_disaster_state] PRIMARY KEY ([disaster_state_id])
);
GO

IF OBJECT_ID('dbo.finace_status_product', 'U') IS NULL
CREATE TABLE dbo.[finace_status_product] (
    [finance_status_product_id]   int         IDENTITY(1,1) NOT NULL,
    [finance_status_product_name] varchar(50) NULL,
    [register_date]               datetime    NULL CONSTRAINT [DF_finace_status_product_register_date] DEFAULT (getdate()),
    CONSTRAINT [PK_finace_status_product] PRIMARY KEY ([finance_status_product_id])
);
GO

IF OBJECT_ID('dbo.finance_products', 'U') IS NULL
CREATE TABLE dbo.[finance_products] (
    [product_id]                int          IDENTITY(1,1) NOT NULL,
    [area_id]                   int          NOT NULL,
    [product_name]              varchar(100) NOT NULL,
    [description]               varchar(MAX) NULL,
    [tasa_interes_o_prima_base] decimal(5,2) NULL CONSTRAINT [DF_finance_products_tasa_interes_o_prima_base] DEFAULT ((0.00)),
    [finance_status_product_id] int          NULL CONSTRAINT [DF_finance_products_finance_status_product_id] DEFAULT ((1)),
    [date_creation]             datetime     NULL CONSTRAINT [DF_finance_products_date_creation] DEFAULT (getdate()),
    CONSTRAINT [PK_finance_products] PRIMARY KEY ([product_id])
);
GO

IF OBJECT_ID('dbo.gender', 'U') IS NULL
CREATE TABLE dbo.[gender] (
    [gender_id]     int         IDENTITY(1,1) NOT NULL,
    [gender_name]   varchar(50) NULL,
    [register_date] datetime    NULL CONSTRAINT [DF_gender_register_date] DEFAULT (getdate()),
    CONSTRAINT [PK_gender] PRIMARY KEY ([gender_id])
);
GO

IF OBJECT_ID('dbo.insurance_claims', 'U') IS NULL
CREATE TABLE dbo.[insurance_claims] (
    [insurance_id]      int           IDENTITY(1,1) NOT NULL,
    [contract_id]       int           NOT NULL,
    [report_number]     varchar(50)   NOT NULL,
    [date_occurrence]   date          NOT NULL,
    [amount_claimed]    decimal(15,2) NOT NULL,
    [amount_paid]       decimal(15,2) NULL CONSTRAINT [DF_insurance_claims_amount_paid] DEFAULT ((0.00)),
    [disaster_state_id] int           NOT NULL CONSTRAINT [DF_insurance_claims_disaster_state_id] DEFAULT ((1)),
    [report_details]    varchar(MAX)  NULL,
    [date_register]     datetime      NULL CONSTRAINT [DF_insurance_claims_date_register] DEFAULT (getdate()),
    CONSTRAINT [PK_insurance_claims] PRIMARY KEY ([insurance_id]),
    CONSTRAINT [UQ_insurance_claims_report_number] UNIQUE ([report_number])
);
GO

IF OBJECT_ID('dbo.insurance_contranct', 'U') IS NULL
CREATE TABLE dbo.[insurance_contranct] (
    [contract_id]          int           NOT NULL,
    [insurance_sume_total] decimal(15,2) NOT NULL,
    [total_annual_premium] decimal(15,2) NOT NULL,
    [pay_form_id]          int           NOT NULL CONSTRAINT [DF_insurance_contranct_pay_form_id] DEFAULT ((4)),
    [porcent_deductible]   decimal(4,2)  NULL CONSTRAINT [DF_insurance_contranct_porcent_deductible] DEFAULT ((0.00)),
    [beneficiary_name]     varchar(MAX)  NULL,
    CONSTRAINT [PK_insurance_contranct] PRIMARY KEY ([contract_id])
);
GO

IF OBJECT_ID('dbo.moral_person_client', 'U') IS NULL
CREATE TABLE dbo.[moral_person_client] (
    [client_id]                 int          NOT NULL,
    [social_razon]              varchar(200) NOT NULL,
    [comercial_name]            varchar(150) NULL,
    [date_constitucion]         date         NOT NULL,
    [comercial_activity]        varchar(150) NULL,
    [representative_legal_name] varchar(150) NOT NULL,
    [representative_id]         varchar(50)  NULL,
    CONSTRAINT [PK_moral_person_client] PRIMARY KEY ([client_id])
);
GO

IF OBJECT_ID('dbo.pay_form', 'U') IS NULL
CREATE TABLE dbo.[pay_form] (
    [pay_form_id]   int         IDENTITY(1,1) NOT NULL,
    [pay_form_name] varchar(50) NULL,
    [date_created]  datetime    NULL CONSTRAINT [DF_pay_form_date_created] DEFAULT (getdate()),
    CONSTRAINT [PK_pay_form] PRIMARY KEY ([pay_form_id])
);
GO

IF OBJECT_ID('dbo.phisic_person_client', 'U') IS NULL
CREATE TABLE dbo.[phisic_person_client] (
    [client_id]       int          NOT NULL,
    [names]           varchar(100) NOT NULL,
    [father_lastname] varchar(50)  NOT NULL,
    [mother_lastname] varchar(50)  NOT NULL,
    [birth_date]      date         NOT NULL,
    [gender_id]       int          NOT NULL,
    [civil_state_id]  int          NOT NULL,
    CONSTRAINT [PK_phisic_person_client] PRIMARY KEY ([client_id])
);
GO

IF OBJECT_ID('dbo.products_contract', 'U') IS NULL
CREATE TABLE dbo.[products_contract] (
    [contract_id]        int         IDENTITY(1,1) NOT NULL,
    [client_id]          int         NOT NULL,
    [product_id]         int         NOT NULL,
    [user_id]            int         NOT NULL,
    [reference_number]   varchar(50) NOT NULL,
    [date_opening_issue] date        NOT NULL,
    [date_end]           date        NULL,
    [contract_status_id] int         NOT NULL,
    [date_register]      datetime    NULL CONSTRAINT [DF_products_contract_date_register] DEFAULT (getdate()),
    CONSTRAINT [PK_products_contract] PRIMARY KEY ([contract_id]),
    CONSTRAINT [UQ_products_contract_reference_number] UNIQUE ([reference_number])
);
GO

IF OBJECT_ID('dbo.role', 'U') IS NULL
CREATE TABLE dbo.[role] (
    [role_id]      int          IDENTITY(1,1) NOT NULL,
    [area_id]      int          NOT NULL,
    [role_name]    varchar(100) NOT NULL,
    [category]     varchar(100) NOT NULL,
    [description]  varchar(MAX) NULL,
    [date_created] datetime     NULL CONSTRAINT [DF_role_date_created] DEFAULT (getdate()),
    CONSTRAINT [PK_role] PRIMARY KEY ([role_id]),
    CONSTRAINT [UQ_role_role_name] UNIQUE ([role_name])
);
GO

IF OBJECT_ID('dbo.stage', 'U') IS NULL
CREATE TABLE dbo.[stage] (
    [stage_id]     int         IDENTITY(1,1) NOT NULL,
    [stage_name]   varchar(50) NULL,
    [date_created] datetime    NULL CONSTRAINT [DF_stage_date_created] DEFAULT (getdate()),
    CONSTRAINT [PK_stage] PRIMARY KEY ([stage_id])
);
GO

IF OBJECT_ID('dbo.transaction_type', 'U') IS NULL
CREATE TABLE dbo.[transaction_type] (
    [transaction_type_id]   int         IDENTITY(1,1) NOT NULL,
    [transaction_type_name] varchar(50) NULL,
    [data_created]          datetime    NULL CONSTRAINT [DF_transaction_type_data_created] DEFAULT (getdate()),
    CONSTRAINT [PK_transaction_type] PRIMARY KEY ([transaction_type_id])
);
GO

IF OBJECT_ID('dbo.type_person', 'U') IS NULL
CREATE TABLE dbo.[type_person] (
    [type_person_id] int          IDENTITY(1,1) NOT NULL,
    [type_name]      varchar(50)  NOT NULL,
    [description]    varchar(255) NULL,
    [date_creation]  datetime     NULL CONSTRAINT [DF_type_person_date_creation] DEFAULT (getdate()),
    CONSTRAINT [PK_type_person] PRIMARY KEY ([type_person_id]),
    CONSTRAINT [UQ_type_person_type_name] UNIQUE ([type_name])
);
GO

IF OBJECT_ID('dbo.user_status', 'U') IS NULL
CREATE TABLE dbo.[user_status] (
    [status_id]     int          IDENTITY(1,1) NOT NULL,
    [status_name]   varchar(30)  NOT NULL,
    [description]   varchar(255) NULL,
    [date_creation] datetime     NULL CONSTRAINT [DF_user_status_date_creation] DEFAULT (getdate()),
    CONSTRAINT [PK_user_status] PRIMARY KEY ([status_id]),
    CONSTRAINT [UQ_user_status_status_name] UNIQUE ([status_name])
);
GO

IF OBJECT_ID('dbo.users', 'U') IS NULL
CREATE TABLE dbo.[users] (
    [user_id]       int          IDENTITY(1,1) NOT NULL,
    [role_id]       int          NOT NULL,
    [status_id]     int          NOT NULL CONSTRAINT [DF_users_status_id] DEFAULT ((1)),
    [name]          varchar(100) NOT NULL,
    [email]         varchar(100) NOT NULL,
    [password_hash] varchar(255) NOT NULL,
    [creation_date] datetime     NULL CONSTRAINT [DF_users_creation_date] DEFAULT (getdate()),
    CONSTRAINT [PK_users] PRIMARY KEY ([user_id]),
    CONSTRAINT [UQ_users_email] UNIQUE ([email])
);
GO

/* -----------------------------------------------------------------------------
   Claves foraneas
   -----------------------------------------------------------------------------
   Se crean despues de todas las tablas para no depender del orden de creacion.
   Las relaciones 1:1 (herencia table-per-type) van en CASCADE; el resto en
   NO ACTION, que es lo que el backend mapea como DeleteBehavior.Restrict.
   -------------------------------------------------------------------------- */

IF OBJECT_ID('FK_bank_contract_contract_id', 'F') IS NULL
ALTER TABLE dbo.[bank_contract] WITH CHECK ADD CONSTRAINT [FK_bank_contract_contract_id]
    FOREIGN KEY ([contract_id]) REFERENCES dbo.[products_contract] ([contract_id]) ON DELETE CASCADE;
IF OBJECT_ID('FK_bank_transaction_contract_id', 'F') IS NULL
ALTER TABLE dbo.[bank_transaction] WITH CHECK ADD CONSTRAINT [FK_bank_transaction_contract_id]
    FOREIGN KEY ([contract_id]) REFERENCES dbo.[products_contract] ([contract_id]);
IF OBJECT_ID('FK_bank_transaction_transaction_type_id', 'F') IS NULL
ALTER TABLE dbo.[bank_transaction] WITH CHECK ADD CONSTRAINT [FK_bank_transaction_transaction_type_id]
    FOREIGN KEY ([transaction_type_id]) REFERENCES dbo.[transaction_type] ([transaction_type_id]);
IF OBJECT_ID('FK_clients_assigned_user_id', 'F') IS NULL
ALTER TABLE dbo.[clients] WITH CHECK ADD CONSTRAINT [FK_clients_assigned_user_id]
    FOREIGN KEY ([assigned_user_id]) REFERENCES dbo.[users] ([user_id]) ON DELETE SET NULL;
IF OBJECT_ID('FK_clients_type_person_id', 'F') IS NULL
ALTER TABLE dbo.[clients] WITH CHECK ADD CONSTRAINT [FK_clients_type_person_id]
    FOREIGN KEY ([type_person_id]) REFERENCES dbo.[type_person] ([type_person_id]);
IF OBJECT_ID('FK_comercial_oportunities_client_id', 'F') IS NULL
ALTER TABLE dbo.[comercial_oportunities] WITH CHECK ADD CONSTRAINT [FK_comercial_oportunities_client_id]
    FOREIGN KEY ([client_id]) REFERENCES dbo.[clients] ([client_id]) ON DELETE CASCADE;
IF OBJECT_ID('FK_comercial_oportunities_product_id', 'F') IS NULL
ALTER TABLE dbo.[comercial_oportunities] WITH CHECK ADD CONSTRAINT [FK_comercial_oportunities_product_id]
    FOREIGN KEY ([product_id]) REFERENCES dbo.[finance_products] ([product_id]);
IF OBJECT_ID('FK_comercial_oportunities_stage_id', 'F') IS NULL
ALTER TABLE dbo.[comercial_oportunities] WITH CHECK ADD CONSTRAINT [FK_comercial_oportunities_stage_id]
    FOREIGN KEY ([stage_id]) REFERENCES dbo.[stage] ([stage_id]);
IF OBJECT_ID('FK_comercial_oportunities_user_id', 'F') IS NULL
ALTER TABLE dbo.[comercial_oportunities] WITH CHECK ADD CONSTRAINT [FK_comercial_oportunities_user_id]
    FOREIGN KEY ([user_id]) REFERENCES dbo.[users] ([user_id]);
IF OBJECT_ID('FK_finance_products_area_id', 'F') IS NULL
ALTER TABLE dbo.[finance_products] WITH CHECK ADD CONSTRAINT [FK_finance_products_area_id]
    FOREIGN KEY ([area_id]) REFERENCES dbo.[area] ([area_id]);
IF OBJECT_ID('FK_finance_products_finance_status_product_id', 'F') IS NULL
ALTER TABLE dbo.[finance_products] WITH CHECK ADD CONSTRAINT [FK_finance_products_finance_status_product_id]
    FOREIGN KEY ([finance_status_product_id]) REFERENCES dbo.[finace_status_product] ([finance_status_product_id]);
IF OBJECT_ID('FK_insurance_contranct_contract_id', 'F') IS NULL
ALTER TABLE dbo.[insurance_contranct] WITH CHECK ADD CONSTRAINT [FK_insurance_contranct_contract_id]
    FOREIGN KEY ([contract_id]) REFERENCES dbo.[products_contract] ([contract_id]) ON DELETE CASCADE;
IF OBJECT_ID('FK_insurance_claims_contract_id', 'F') IS NULL
ALTER TABLE dbo.[insurance_claims] WITH CHECK ADD CONSTRAINT [FK_insurance_claims_contract_id]
    FOREIGN KEY ([contract_id]) REFERENCES dbo.[insurance_contranct] ([contract_id]);
IF OBJECT_ID('FK_insurance_claims_disaster_state_id', 'F') IS NULL
ALTER TABLE dbo.[insurance_claims] WITH CHECK ADD CONSTRAINT [FK_insurance_claims_disaster_state_id]
    FOREIGN KEY ([disaster_state_id]) REFERENCES dbo.[disaster_state] ([disaster_state_id]);
IF OBJECT_ID('FK_insurance_contranct_pay_form_id', 'F') IS NULL
ALTER TABLE dbo.[insurance_contranct] WITH CHECK ADD CONSTRAINT [FK_insurance_contranct_pay_form_id]
    FOREIGN KEY ([pay_form_id]) REFERENCES dbo.[pay_form] ([pay_form_id]);
IF OBJECT_ID('FK_moral_person_client_client_id', 'F') IS NULL
ALTER TABLE dbo.[moral_person_client] WITH CHECK ADD CONSTRAINT [FK_moral_person_client_client_id]
    FOREIGN KEY ([client_id]) REFERENCES dbo.[clients] ([client_id]) ON DELETE CASCADE;
IF OBJECT_ID('FK_phisic_person_client_civil_state_id', 'F') IS NULL
ALTER TABLE dbo.[phisic_person_client] WITH CHECK ADD CONSTRAINT [FK_phisic_person_client_civil_state_id]
    FOREIGN KEY ([civil_state_id]) REFERENCES dbo.[civil_state] ([civil_state_id]);
IF OBJECT_ID('FK_phisic_person_client_client_id', 'F') IS NULL
ALTER TABLE dbo.[phisic_person_client] WITH CHECK ADD CONSTRAINT [FK_phisic_person_client_client_id]
    FOREIGN KEY ([client_id]) REFERENCES dbo.[clients] ([client_id]) ON DELETE CASCADE;
IF OBJECT_ID('FK_phisic_person_client_gender_id', 'F') IS NULL
ALTER TABLE dbo.[phisic_person_client] WITH CHECK ADD CONSTRAINT [FK_phisic_person_client_gender_id]
    FOREIGN KEY ([gender_id]) REFERENCES dbo.[gender] ([gender_id]);
IF OBJECT_ID('FK_products_contract_client_id', 'F') IS NULL
ALTER TABLE dbo.[products_contract] WITH CHECK ADD CONSTRAINT [FK_products_contract_client_id]
    FOREIGN KEY ([client_id]) REFERENCES dbo.[clients] ([client_id]);
IF OBJECT_ID('FK_products_contract_contract_status_id', 'F') IS NULL
ALTER TABLE dbo.[products_contract] WITH CHECK ADD CONSTRAINT [FK_products_contract_contract_status_id]
    FOREIGN KEY ([contract_status_id]) REFERENCES dbo.[contract_status] ([contract_status_id]);
IF OBJECT_ID('FK_products_contract_product_id', 'F') IS NULL
ALTER TABLE dbo.[products_contract] WITH CHECK ADD CONSTRAINT [FK_products_contract_product_id]
    FOREIGN KEY ([product_id]) REFERENCES dbo.[finance_products] ([product_id]);
IF OBJECT_ID('FK_products_contract_user_id', 'F') IS NULL
ALTER TABLE dbo.[products_contract] WITH CHECK ADD CONSTRAINT [FK_products_contract_user_id]
    FOREIGN KEY ([user_id]) REFERENCES dbo.[users] ([user_id]);
IF OBJECT_ID('FK_role_area_id', 'F') IS NULL
ALTER TABLE dbo.[role] WITH CHECK ADD CONSTRAINT [FK_role_area_id]
    FOREIGN KEY ([area_id]) REFERENCES dbo.[area] ([area_id]);
IF OBJECT_ID('FK_users_role_id', 'F') IS NULL
ALTER TABLE dbo.[users] WITH CHECK ADD CONSTRAINT [FK_users_role_id]
    FOREIGN KEY ([role_id]) REFERENCES dbo.[role] ([role_id]);
IF OBJECT_ID('FK_users_status_id', 'F') IS NULL
ALTER TABLE dbo.[users] WITH CHECK ADD CONSTRAINT [FK_users_status_id]
    FOREIGN KEY ([status_id]) REFERENCES dbo.[user_status] ([status_id]);
GO

/* -----------------------------------------------------------------------------
   Restricciones CHECK
   -----------------------------------------------------------------------------
   Replican en la BD reglas que el backend ya valida, para que ningun otro
   cliente ni un script manual puedan meter datos imposibles.
   -------------------------------------------------------------------------- */

IF OBJECT_ID('CK_bank_contract_dia_corte', 'C') IS NULL
ALTER TABLE dbo.[bank_contract] WITH CHECK ADD CONSTRAINT [CK_bank_contract_dia_corte]
    CHECK ([monthly_cutoff_day]>=(1) AND [monthly_cutoff_day]<=(28));
IF OBJECT_ID('CK_bank_transaction_importe', 'C') IS NULL
ALTER TABLE dbo.[bank_transaction] WITH CHECK ADD CONSTRAINT [CK_bank_transaction_importe]
    CHECK ([amount]>(0));
IF OBJECT_ID('CK_comercial_oportunities_probabilidad', 'C') IS NULL
ALTER TABLE dbo.[comercial_oportunities] WITH CHECK ADD CONSTRAINT [CK_comercial_oportunities_probabilidad]
    CHECK ([success_probability]>=(0) AND [success_probability]<=(100) AND ([estimated_mont] IS NULL OR [estimated_mont]>=(0)));
IF OBJECT_ID('CK_insurance_claims_montos', 'C') IS NULL
ALTER TABLE dbo.[insurance_claims] WITH CHECK ADD CONSTRAINT [CK_insurance_claims_montos]
    CHECK ([amount_claimed]>(0) AND ([amount_paid] IS NULL OR [amount_paid]>=(0) AND [amount_paid]<=[amount_claimed]) AND [date_occurrence]>='1753-01-01');
IF OBJECT_ID('CK_moral_person_client_constitucion', 'C') IS NULL
ALTER TABLE dbo.[moral_person_client] WITH CHECK ADD CONSTRAINT [CK_moral_person_client_constitucion]
    CHECK ([date_constitucion]>='1753-01-01');
IF OBJECT_ID('CK_phisic_person_client_nacimiento', 'C') IS NULL
ALTER TABLE dbo.[phisic_person_client] WITH CHECK ADD CONSTRAINT [CK_phisic_person_client_nacimiento]
    CHECK ([birth_date]>='1753-01-01');
IF OBJECT_ID('CK_products_contract_fechas', 'C') IS NULL
ALTER TABLE dbo.[products_contract] WITH CHECK ADD CONSTRAINT [CK_products_contract_fechas]
    CHECK ([date_opening_issue]>='1753-01-01' AND ([date_end] IS NULL OR [date_end]>=[date_opening_issue]));
GO

/* -----------------------------------------------------------------------------
   Indices (24)
   -----------------------------------------------------------------------------
   Los IX_ cubren las claves foraneas: sin ellos, todo filtro por client_id,
   product_id, user_id o contract_id hace recorrido de tabla.

   UX_bank_contract_interbank_code es FILTRADO a proposito: SQL Server considera
   iguales los NULL en un indice unico, asi que un UNIQUE normal solo dejaria
   existir UN contrato bancario sin codigo interbancario.
   -------------------------------------------------------------------------- */

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UX_bank_contract_interbank_code' AND object_id = OBJECT_ID('dbo.bank_contract'))
CREATE UNIQUE INDEX [UX_bank_contract_interbank_code] ON dbo.[bank_contract] ([interbank_code]) WHERE ([interbank_code] IS NOT NULL);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_bank_transaction_contract_id' AND object_id = OBJECT_ID('dbo.bank_transaction'))
CREATE INDEX [IX_bank_transaction_contract_id] ON dbo.[bank_transaction] ([contract_id]);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_bank_transaction_transaction_type_id' AND object_id = OBJECT_ID('dbo.bank_transaction'))
CREATE INDEX [IX_bank_transaction_transaction_type_id] ON dbo.[bank_transaction] ([transaction_type_id]);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_clients_assigned_user_id' AND object_id = OBJECT_ID('dbo.clients'))
CREATE INDEX [IX_clients_assigned_user_id] ON dbo.[clients] ([assigned_user_id]);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_clients_type_person_id' AND object_id = OBJECT_ID('dbo.clients'))
CREATE INDEX [IX_clients_type_person_id] ON dbo.[clients] ([type_person_id]);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UX_clients_fiscal_id' AND object_id = OBJECT_ID('dbo.clients'))
CREATE UNIQUE INDEX [UX_clients_fiscal_id] ON dbo.[clients] ([fiscal_id]) WHERE ([fiscal_id] IS NOT NULL);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_comercial_oportunities_client_id' AND object_id = OBJECT_ID('dbo.comercial_oportunities'))
CREATE INDEX [IX_comercial_oportunities_client_id] ON dbo.[comercial_oportunities] ([client_id]);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_comercial_oportunities_product_id' AND object_id = OBJECT_ID('dbo.comercial_oportunities'))
CREATE INDEX [IX_comercial_oportunities_product_id] ON dbo.[comercial_oportunities] ([product_id]);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_comercial_oportunities_stage_id' AND object_id = OBJECT_ID('dbo.comercial_oportunities'))
CREATE INDEX [IX_comercial_oportunities_stage_id] ON dbo.[comercial_oportunities] ([stage_id]);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_comercial_oportunities_user_id' AND object_id = OBJECT_ID('dbo.comercial_oportunities'))
CREATE INDEX [IX_comercial_oportunities_user_id] ON dbo.[comercial_oportunities] ([user_id]);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_finance_products_area_id' AND object_id = OBJECT_ID('dbo.finance_products'))
CREATE INDEX [IX_finance_products_area_id] ON dbo.[finance_products] ([area_id]);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_finance_products_finance_status_product_id' AND object_id = OBJECT_ID('dbo.finance_products'))
CREATE INDEX [IX_finance_products_finance_status_product_id] ON dbo.[finance_products] ([finance_status_product_id]);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_insurance_claims_contract_id' AND object_id = OBJECT_ID('dbo.insurance_claims'))
CREATE INDEX [IX_insurance_claims_contract_id] ON dbo.[insurance_claims] ([contract_id]);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_insurance_claims_disaster_state_id' AND object_id = OBJECT_ID('dbo.insurance_claims'))
CREATE INDEX [IX_insurance_claims_disaster_state_id] ON dbo.[insurance_claims] ([disaster_state_id]);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_insurance_contranct_pay_form_id' AND object_id = OBJECT_ID('dbo.insurance_contranct'))
CREATE INDEX [IX_insurance_contranct_pay_form_id] ON dbo.[insurance_contranct] ([pay_form_id]);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_phisic_person_client_civil_state_id' AND object_id = OBJECT_ID('dbo.phisic_person_client'))
CREATE INDEX [IX_phisic_person_client_civil_state_id] ON dbo.[phisic_person_client] ([civil_state_id]);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_phisic_person_client_gender_id' AND object_id = OBJECT_ID('dbo.phisic_person_client'))
CREATE INDEX [IX_phisic_person_client_gender_id] ON dbo.[phisic_person_client] ([gender_id]);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_products_contract_client_id' AND object_id = OBJECT_ID('dbo.products_contract'))
CREATE INDEX [IX_products_contract_client_id] ON dbo.[products_contract] ([client_id]);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_products_contract_contract_status_id' AND object_id = OBJECT_ID('dbo.products_contract'))
CREATE INDEX [IX_products_contract_contract_status_id] ON dbo.[products_contract] ([contract_status_id]);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_products_contract_product_id' AND object_id = OBJECT_ID('dbo.products_contract'))
CREATE INDEX [IX_products_contract_product_id] ON dbo.[products_contract] ([product_id]);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_products_contract_user_id' AND object_id = OBJECT_ID('dbo.products_contract'))
CREATE INDEX [IX_products_contract_user_id] ON dbo.[products_contract] ([user_id]);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_role_area_id' AND object_id = OBJECT_ID('dbo.role'))
CREATE INDEX [IX_role_area_id] ON dbo.[role] ([area_id]);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_users_role_id' AND object_id = OBJECT_ID('dbo.users'))
CREATE INDEX [IX_users_role_id] ON dbo.[users] ([role_id]);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_users_status_id' AND object_id = OBJECT_ID('dbo.users'))
CREATE INDEX [IX_users_status_id] ON dbo.[users] ([status_id]);
GO

/* -----------------------------------------------------------------------------
   Resumen
   -------------------------------------------------------------------------- */

SELECT 'tablas' AS objeto, COUNT(*) AS total FROM sys.tables
UNION ALL SELECT 'claves foraneas', COUNT(*) FROM sys.foreign_keys
UNION ALL SELECT 'restricciones CHECK', COUNT(*) FROM sys.check_constraints
UNION ALL SELECT 'indices', COUNT(*) FROM sys.indexes i JOIN sys.tables t ON t.object_id = i.object_id WHERE i.type > 0 AND i.is_primary_key = 0 AND i.is_unique_constraint = 0;
GO
