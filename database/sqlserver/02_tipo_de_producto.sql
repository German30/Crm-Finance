/* =============================================================================
   crm_finance — Bloque 2: catalogo de tipo de producto
   -----------------------------------------------------------------------------
   PROPUESTA. No se ha ejecutado nada.

   *** ESTE BLOQUE REQUIERE UN CAMBIO EN EL BACKEND. No lo ejecutes suelto. ***

   Resuelve la unica suposicion que queda en la logica de negocio:
   `bank_contract.balance_actual` es saldo TOTAL en una cuenta de debito y saldo
   DEUDOR en una de credito, y eso invierte el signo de casi todos los movimientos.
   Como ningun campo distingue una de otra, hoy OperationService lo deduce del
   NOMBRE del producto ("credito", "tarjeta"). Funciona con los 24 productos
   sembrados, pero un producto de credito que no lleve esas palabras en el nombre
   se trataria como cuenta de debito y moveria el dinero al reves.

   Con este catalogo la clasificacion pasa a ser un dato explicito.
   ============================================================================= */

USE crm_finance;
GO

SET XACT_ABORT ON;
BEGIN TRANSACTION;
GO

/* -----------------------------------------------------------------------------
   A. El catalogo
   -----------------------------------------------------------------------------
   Mismo patron que el resto de catalogos del esquema (gender, civil_state, stage…):
   id identity, nombre unico, descripcion y fecha de alta.
   ----------------------------------------------------------------------------- */

IF OBJECT_ID('dbo.product_kind') IS NULL
BEGIN
    CREATE TABLE dbo.product_kind (
        product_kind_id   INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_product_kind PRIMARY KEY,
        product_kind_name VARCHAR(50)  NOT NULL CONSTRAINT UQ_product_kind_name UNIQUE,
        description       VARCHAR(255) NULL,
        date_creation     DATETIME     NULL CONSTRAINT DF_product_kind_date_creation DEFAULT (GETUTCDATE())
    );

    INSERT INTO dbo.product_kind (product_kind_name, description) VALUES
        ('Debito',    'Cuenta a la vista: balance_actual es saldo total a favor del cliente'),
        ('Credito',   'Linea de credito: balance_actual es saldo deudor'),
        ('Inversion', 'Ahorro a plazo o fondo: balance_actual es saldo total'),
        ('Servicio',  'Producto sin balance asociado (caja de seguridad, transferencias)'),
        ('Seguro',    'Producto del area de Seguros: no usa bank_contract');
END
GO

/* -----------------------------------------------------------------------------
   B. La columna en finance_products
   ----------------------------------------------------------------------------- */

IF COL_LENGTH('dbo.finance_products', 'product_kind_id') IS NULL
    ALTER TABLE dbo.finance_products ADD product_kind_id INT NULL;
GO

/* -----------------------------------------------------------------------------
   C. Clasificacion inicial de los 24 productos sembrados
   -----------------------------------------------------------------------------
   Reproduce el criterio que hoy aplica el backend por nombre, para que el
   comportamiento no cambie al activar esto. COLLATE ..._AI para que funcione
   tanto si los nombres estan acentuados como si no.

   REVISA EL RESULTADO (la consulta del final lo muestra) y corrige a mano lo que
   no encaje: a partir de aqui manda el dato, no el nombre.
   ----------------------------------------------------------------------------- */

UPDATE fp
SET product_kind_id = k.product_kind_id
FROM dbo.finance_products fp
JOIN dbo.area a ON a.area_id = fp.area_id
CROSS APPLY (
    SELECT CASE
        WHEN a.area_name COLLATE Latin1_General_CI_AI = 'Seguros'                      THEN 'Seguro'
        WHEN fp.product_name COLLATE Latin1_General_CI_AI LIKE '%credito%'
          OR fp.product_name COLLATE Latin1_General_CI_AI LIKE '%tarjeta%'             THEN 'Credito'
        WHEN fp.product_name COLLATE Latin1_General_CI_AI LIKE '%pagare%'
          OR fp.product_name COLLATE Latin1_General_CI_AI LIKE '%inversion%'           THEN 'Inversion'
        WHEN fp.product_name COLLATE Latin1_General_CI_AI LIKE '%caja de seguridad%'
          OR fp.product_name COLLATE Latin1_General_CI_AI LIKE '%transferencia%'       THEN 'Servicio'
        ELSE 'Debito'
    END AS nombre
) c
JOIN dbo.product_kind k ON k.product_kind_name = c.nombre
WHERE fp.product_kind_id IS NULL;
GO

/* -----------------------------------------------------------------------------
   D. Cerrar: obligatoria, con FK e indice
   ----------------------------------------------------------------------------- */

IF EXISTS (SELECT 1 FROM dbo.finance_products WHERE product_kind_id IS NULL)
BEGIN
    RAISERROR('Quedan productos sin clasificar: revisa el paso C antes de continuar.', 16, 1);
    ROLLBACK TRANSACTION;
    RETURN;
END

ALTER TABLE dbo.finance_products ALTER COLUMN product_kind_id INT NOT NULL;

IF OBJECT_ID('FK_finance_products_product_kind') IS NULL
    ALTER TABLE dbo.finance_products WITH CHECK ADD CONSTRAINT FK_finance_products_product_kind
        FOREIGN KEY (product_kind_id) REFERENCES dbo.product_kind(product_kind_id);

DROP INDEX IF EXISTS IX_finance_products_product_kind_id ON dbo.finance_products;
CREATE INDEX IX_finance_products_product_kind_id ON dbo.finance_products(product_kind_id);
GO

COMMIT TRANSACTION;
GO

/* -----------------------------------------------------------------------------
   Verificacion: revisa esta clasificacion antes de dar el paso por bueno
   ----------------------------------------------------------------------------- */

SELECT a.area_name AS area, k.product_kind_name AS tipo, fp.product_id, fp.product_name
FROM dbo.finance_products fp
JOIN dbo.area a ON a.area_id = fp.area_id
JOIN dbo.product_kind k ON k.product_kind_id = fp.product_kind_id
ORDER BY a.area_name, k.product_kind_name, fp.product_id;
GO

/* =============================================================================
   CAMBIO PENDIENTE EN EL BACKEND (sin el, esto no sirve de nada)
   -----------------------------------------------------------------------------
   1. Models/FinanceProduct.cs      -> propiedad ProductKindId + navegacion ProductKind
      Models/ProductKind.cs         -> entidad nueva del catalogo
      Data/ApplicationDbContext.cs  -> DbSet + FK con DeleteBehavior.Restrict
   2. Services/OperationService.cs  -> sustituir EsProductoDeCredito(nombre) por la
      lectura de product_kind_name; se borran las listas ProductosDeCredito.
   3. Services/CatalogService.cs    -> GET /api/Catalog/product-kinds
   4. DTOs/Product/                 -> ProductKindId en create/update y el nombre en
      FinanceProductResponseDto (esto SI cambia el contrato con el frontend).
   ============================================================================= */
