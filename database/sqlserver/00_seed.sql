/* =============================================================================
   crm_finance — Catalogos y productos
   -----------------------------------------------------------------------------
   Ejecutar DESPUES de 00_schema.sql.

   Contiene los catalogos que el backend da por sembrados y los productos
   financieros. Los ids se insertan explicitamente (IDENTITY_INSERT) porque el
   codigo depende de algunos: ClientService usa type_person 1 = Fisica y
   2 = Moral, UserService usa status 1 = Activo y 2 = Inactivo, y FinanceProduct
   arranca en finance_status_product 1.

   Idempotente: cada bloque solo siembra si su tabla esta vacia, asi que se puede
   volver a ejecutar sin duplicar nada.

   Las fechas de alta se dejan al DEFAULT de cada tabla en vez de copiarlas.
   ============================================================================= */

USE [crm_finance];
GO

SET XACT_ABORT ON;
BEGIN TRANSACTION;
GO

/* area — 3 filas */
IF NOT EXISTS (SELECT 1 FROM dbo.[area])
BEGIN
    SET IDENTITY_INSERT dbo.[area] ON;
    INSERT INTO dbo.[area] ([area_id], [area_name], [description])
        SELECT 1, 'General', 'Área global aplicable a toda la organización (Dirección General, TI, Administradores Maestros).'
    UNION ALL
        SELECT 2, 'Seguros', 'División encargada de la comercialización, suscripción y gestión de pólizas y siniestros.'
    UNION ALL
        SELECT 3, 'Banca', 'División enfocada en servicios bancarios, colocación de créditos, captación y operaciones en ventanilla.'
    ;
    SET IDENTITY_INSERT dbo.[area] OFF;
END
GO

/* role — 19 filas */
IF NOT EXISTS (SELECT 1 FROM dbo.[role])
BEGIN
    SET IDENTITY_INSERT dbo.[role] ON;
    INSERT INTO dbo.[role] ([role_id], [area_id], [role_name], [category], [description])
        SELECT 1, 1, 'Administrador', 'Administración Global', 'Usuario maestro con acceso total al sistema y gestión de usuarios.'
    UNION ALL
        SELECT 2, 2, 'Director o Gerente de Sucursal (Seguros)', 'Dirección y Administración', 'Lidera la oficina, establece estrategias de venta locales y supervisa metas.'
    UNION ALL
        SELECT 3, 2, 'Coordinador Administrativo / RRHH (Seguros)', 'Dirección y Administración', 'Gestiona presupuesto local, pago de nóminas e instalaciones.'
    UNION ALL
        SELECT 4, 2, 'Auxiliar Administrativo / Recepción (Seguros)', 'Dirección y Administración', 'Atención presencial, correspondencia, archivo y control de caja chica.'
    UNION ALL
        SELECT 5, 2, 'Líder Comercial / Gerente de Ventas (Seguros)', 'Área Comercial y Ventas', 'Supervisa la fuerza de ventas e instruye en ramos de autos, vida y daños.'
    UNION ALL
        SELECT 6, 2, 'Ejecutivo de Cuenta o Asesor (Seguros)', 'Área Comercial y Ventas', 'Asesora clientes, cotiza pólizas y da seguimiento a renovaciones.'
    UNION ALL
        SELECT 7, 2, 'Agente de Seguros / Corredor', 'Área Comercial y Ventas', 'Agente de planta dedicado a la venta directa y prospección.'
    UNION ALL
        SELECT 8, 2, 'Especialista en Suscripción', 'Operaciones, Servicio y Siniestros', 'Evalúa el riesgo de solicitudes antes de emitir pólizas y determina primas.'
    UNION ALL
        SELECT 9, 2, 'Gestor de Siniestros / Reclamos', 'Operaciones, Servicio y Siniestros', 'Primer contacto en incidentes, recaba documentación y autoriza pagos/reparaciones.'
    UNION ALL
        SELECT 10, 2, 'Ejecutivo de Atención al Cliente (Seguros)', 'Operaciones, Servicio y Siniestros', 'Resuelve dudas sobre coberturas, apoya en renovaciones y canaliza quejas.'
    UNION ALL
        SELECT 11, 3, 'Gerente de Sucursal / Director de Agencia', 'Dirección y Gestión', 'Máxima autoridad. Responsable de metas comerciales, presupuesto y cumplimiento normativo.'
    UNION ALL
        SELECT 12, 3, 'Subgerente de Sucursal', 'Dirección y Gestión', 'Apoya en la administración diaria y supervisa directamente al equipo operativo.'
    UNION ALL
        SELECT 13, 3, 'Ejecutivo de Atención al Cliente (Banca)', 'Área Comercial y Asesoría', 'Venta de productos financieros (cuentas, tarjetas, seguros) y resolución de incidencias.'
    UNION ALL
        SELECT 14, 3, 'Asesor Financiero / Ejecutivo Pyme', 'Área Comercial y Asesoría', 'Especialista en perfiles de alto valor/empresas. Asesora inversiones y créditos.'
    UNION ALL
        SELECT 15, 3, 'Ejecutivo Hipotecarios', 'Área Comercial y Asesoría', 'Promoción, evaluación y gestión exclusiva de créditos para vivienda.'
    UNION ALL
        SELECT 16, 3, 'Cajero Principal / Jefe de Caja', 'Área de Operaciones y Ventanilla', 'Supervisa flujo de efectivo, gestiona bóvedas y cuadra cierres diarios.'
    UNION ALL
        SELECT 17, 3, 'Cajero / Operador de Ventanilla', 'Área de Operaciones y Ventanilla', 'Primer punto de contacto para depósitos, retiros, pagos y cambio de divisas.'
    UNION ALL
        SELECT 18, 3, 'Coordinador / Analista de Riesgo y Crédito', 'Soporte y Control', 'Evalúa viabilidad de préstamos bajo las políticas de riesgo del banco.'
    UNION ALL
        SELECT 19, 3, 'Asistente Administrativo / Recepcionista (Banca)', 'Soporte y Control', 'Recepción de clientes, canalización y control de documentación interna.'
    ;
    SET IDENTITY_INSERT dbo.[role] OFF;
END
GO

/* user_status — 2 filas */
IF NOT EXISTS (SELECT 1 FROM dbo.[user_status])
BEGIN
    SET IDENTITY_INSERT dbo.[user_status] ON;
    INSERT INTO dbo.[user_status] ([status_id], [status_name], [description])
        SELECT 1, 'Activo', 'Usuario operativo con permisos de acceso normales en el sistema.'
    UNION ALL
        SELECT 2, 'Inactivo', 'Usuario deshabilitado que no puede iniciar sesión ni realizar acciones.'
    ;
    SET IDENTITY_INSERT dbo.[user_status] OFF;
END
GO

/* type_person — 2 filas */
IF NOT EXISTS (SELECT 1 FROM dbo.[type_person])
BEGIN
    SET IDENTITY_INSERT dbo.[type_person] ON;
    INSERT INTO dbo.[type_person] ([type_person_id], [type_name], [description])
        SELECT 1, 'Física', 'Individuo miembro de la población con derechos y obligaciones fiscales individuales.'
    UNION ALL
        SELECT 2, 'Moral', 'Entidad jurídica o empresa constituida por una o más personas con una razón social.'
    ;
    SET IDENTITY_INSERT dbo.[type_person] OFF;
END
GO

/* gender — 3 filas */
IF NOT EXISTS (SELECT 1 FROM dbo.[gender])
BEGIN
    SET IDENTITY_INSERT dbo.[gender] ON;
    INSERT INTO dbo.[gender] ([gender_id], [gender_name])
        SELECT 1, 'Masculino'
    UNION ALL
        SELECT 2, 'Femenino'
    UNION ALL
        SELECT 3, 'No Especificado'
    ;
    SET IDENTITY_INSERT dbo.[gender] OFF;
END
GO

/* civil_state — 5 filas */
IF NOT EXISTS (SELECT 1 FROM dbo.[civil_state])
BEGIN
    SET IDENTITY_INSERT dbo.[civil_state] ON;
    INSERT INTO dbo.[civil_state] ([civil_state_id], [civil_state_name])
        SELECT 1, 'Soltero/a'
    UNION ALL
        SELECT 2, 'Casado/a'
    UNION ALL
        SELECT 3, 'Divorciado/a'
    UNION ALL
        SELECT 4, 'Viudo/a'
    UNION ALL
        SELECT 5, 'Unión Libre'
    ;
    SET IDENTITY_INSERT dbo.[civil_state] OFF;
END
GO

/* finace_status_product — 2 filas */
IF NOT EXISTS (SELECT 1 FROM dbo.[finace_status_product])
BEGIN
    SET IDENTITY_INSERT dbo.[finace_status_product] ON;
    INSERT INTO dbo.[finace_status_product] ([finance_status_product_id], [finance_status_product_name])
        SELECT 1, 'Activo'
    UNION ALL
        SELECT 2, 'Descontinuado'
    ;
    SET IDENTITY_INSERT dbo.[finace_status_product] OFF;
END
GO

/* contract_status — 5 filas */
IF NOT EXISTS (SELECT 1 FROM dbo.[contract_status])
BEGIN
    SET IDENTITY_INSERT dbo.[contract_status] ON;
    INSERT INTO dbo.[contract_status] ([contract_status_id], [contract_status_name])
        SELECT 1, 'Vigente'
    UNION ALL
        SELECT 2, 'Inactivo'
    UNION ALL
        SELECT 3, 'Cancelado'
    UNION ALL
        SELECT 4, 'Vencido'
    UNION ALL
        SELECT 5, 'En Reclamación'
    ;
    SET IDENTITY_INSERT dbo.[contract_status] OFF;
END
GO

/* pay_form — 4 filas */
IF NOT EXISTS (SELECT 1 FROM dbo.[pay_form])
BEGIN
    SET IDENTITY_INSERT dbo.[pay_form] ON;
    INSERT INTO dbo.[pay_form] ([pay_form_id], [pay_form_name])
        SELECT 1, 'Mensual'
    UNION ALL
        SELECT 2, 'Trimestral'
    UNION ALL
        SELECT 3, 'Semestral'
    UNION ALL
        SELECT 4, 'Anual'
    ;
    SET IDENTITY_INSERT dbo.[pay_form] OFF;
END
GO

/* stage — 6 filas */
IF NOT EXISTS (SELECT 1 FROM dbo.[stage])
BEGIN
    SET IDENTITY_INSERT dbo.[stage] ON;
    INSERT INTO dbo.[stage] ([stage_id], [stage_name])
        SELECT 1, 'Prospecto'
    UNION ALL
        SELECT 2, 'Cotización/Propuesta'
    UNION ALL
        SELECT 3, 'Análisis de Riesgo'
    UNION ALL
        SELECT 4, 'Negociación'
    UNION ALL
        SELECT 5, 'Ganada'
    UNION ALL
        SELECT 6, 'Perdida'
    ;
    SET IDENTITY_INSERT dbo.[stage] OFF;
END
GO

/* transaction_type — 5 filas */
IF NOT EXISTS (SELECT 1 FROM dbo.[transaction_type])
BEGIN
    SET IDENTITY_INSERT dbo.[transaction_type] ON;
    INSERT INTO dbo.[transaction_type] ([transaction_type_id], [transaction_type_name])
        SELECT 1, 'Depósito'
    UNION ALL
        SELECT 2, 'Retiro'
    UNION ALL
        SELECT 3, 'Pago de Crédito'
    UNION ALL
        SELECT 4, 'Cobro de Comisión'
    UNION ALL
        SELECT 5, 'Interés Generado'
    ;
    SET IDENTITY_INSERT dbo.[transaction_type] OFF;
END
GO

/* disaster_state — 4 filas */
IF NOT EXISTS (SELECT 1 FROM dbo.[disaster_state])
BEGIN
    SET IDENTITY_INSERT dbo.[disaster_state] ON;
    INSERT INTO dbo.[disaster_state] ([disaster_state_id], [disaster_state_name])
        SELECT 1, 'Reportado'
    UNION ALL
        SELECT 2, 'En Evaluación'
    UNION ALL
        SELECT 3, 'Aprobado/Pagado'
    UNION ALL
        SELECT 4, 'Rechazado'
    ;
    SET IDENTITY_INSERT dbo.[disaster_state] OFF;
END
GO

/* finance_products — 24 filas */
IF NOT EXISTS (SELECT 1 FROM dbo.[finance_products])
BEGIN
    SET IDENTITY_INSERT dbo.[finance_products] ON;
    INSERT INTO dbo.[finance_products] ([product_id], [area_id], [product_name], [description], [tasa_interes_o_prima_base], [finance_status_product_id])
        SELECT 1, 3, 'Cuenta de Ahorro', 'Diseñada para guardar dinero a la vista con una tasa de interés baja.', 1.50, 1
    UNION ALL
        SELECT 2, 3, 'Cuenta de Cheques / Corriente', 'Permite realizar pagos mediante chequeras y transferencias constantes.', 0.00, 1
    UNION ALL
        SELECT 3, 3, 'Cuenta de Nómina', 'Vinculada directamente al pago de salarios por parte de un empleador.', 0.00, 1
    UNION ALL
        SELECT 4, 3, 'Tarjeta de Crédito', 'Línea de crédito revolvente para compras, con opción a pagar a meses.', 35.00, 1
    UNION ALL
        SELECT 5, 3, 'Crédito Personal / de Nómina', 'Préstamo de libre destino para gastos imprevistos o liquidez.', 22.50, 1
    UNION ALL
        SELECT 6, 3, 'Crédito Hipotecario', 'Financiamiento a largo plazo para la compra, construcción o remodelación de vivienda.', 9.50, 1
    UNION ALL
        SELECT 7, 3, 'Crédito Automotriz', 'Específico para la adquisición de un vehículo nuevo o usado.', 12.00, 1
    UNION ALL
        SELECT 8, 3, 'Pagaré Bancario / Depósito a Plazo', 'Inversión segura con dinero bloqueado a tiempo definido y rendimiento garantizado.', 8.00, 1
    UNION ALL
        SELECT 9, 3, 'Fondo de Inversión', 'Conjunto de activos operados por expertos, ideales para diversificar capital.', 11.00, 1
    UNION ALL
        SELECT 10, 3, 'Caja de Seguridad', 'Espacio físico dentro de la sucursal para resguardar objetos de valor.', 0.00, 1
    UNION ALL
        SELECT 11, 3, 'Transferencias y Cambio de Divisas', 'Envío de remesas o compra-venta de moneda extranjera.', 0.00, 1
    UNION ALL
        SELECT 12, 2, 'Gastos Médicos Mayores', 'Cubre hospitalización, consultas, medicamentos y tratamientos por enfermedades graves.', 0.00, 1
    UNION ALL
        SELECT 13, 2, 'Seguro de Vida', 'Brinda una suma asegurada a los beneficiarios en caso de fallecimiento del titular.', 0.00, 1
    UNION ALL
        SELECT 14, 2, 'Accidentes Personales', 'Indemniza o cubre gastos médicos por lesiones o incapacidades derivadas de un accidente.', 0.00, 1
    UNION ALL
        SELECT 15, 2, 'Seguro de Salud / Dental', 'Plan preventivo o correctivo para consultas, revisiones y tratamientos específicos.', 0.00, 1
    UNION ALL
        SELECT 16, 2, 'Seguro de Automóvil', 'Coberturas desde Daños a Terceros hasta Daños Materiales, Robo Total y Gastos Médicos.', 0.00, 1
    UNION ALL
        SELECT 17, 2, 'Seguro de Casa Habitación', 'Protección de estructura y bienes internos contra robos, incendios o terremotos.', 0.00, 1
    UNION ALL
        SELECT 18, 2, 'Responsabilidad Civil (RC)', 'Cubre el pago de indemnizaciones por daños materiales o lesiones a terceros.', 0.00, 1
    UNION ALL
        SELECT 19, 2, 'Seguro de Flotillas', 'Protección para los vehículos de uso comercial o utilitario de la empresa.', 0.00, 1
    UNION ALL
        SELECT 20, 2, 'Seguro Empresarial', 'Cubre el local, inventario, maquinaria y protege contra interrupción de actividades.', 0.00, 1
    UNION ALL
        SELECT 21, 2, 'Responsabilidad Civil Profesional', 'Protege ante demandas por errores, omisiones o negligencias profesionales.', 0.00, 1
    UNION ALL
        SELECT 22, 2, 'Plan de Retiro', 'Fondos de inversión a largo plazo para asegurar un ingreso mensual en la jubilación.', 6.50, 1
    UNION ALL
        SELECT 23, 2, 'Seguro Educativo', 'Plan de ahorro con seguro de vida para garantizar los estudios universitarios de los hijos.', 5.00, 1
    UNION ALL
        SELECT 24, 2, 'Seguro Dotal', 'Combina protección por fallecimiento con ahorro; si sobrevive al plazo, recibe la suma.', 4.00, 1
    ;
    SET IDENTITY_INSERT dbo.[finance_products] OFF;
END
GO

/* -----------------------------------------------------------------------------
   Usuario administrador de arranque
   -----------------------------------------------------------------------------
   Hace falta uno para poder llamar a POST /api/User, que exige un token de
   administrador: sin este no hay forma de crear el primero.

   La contrasena va EN TEXTO PLANO a proposito. AuthService acepta un hash
   heredado en texto plano una unica vez y lo regraba como PBKDF2 en el primer
   login correcto, asi que no hace falta calcular un hash a mano aqui.

   >>> CAMBIA LA CONTRASENA EN CUANTO ENTRES. <<<
   -------------------------------------------------------------------------- */

IF NOT EXISTS (SELECT 1 FROM dbo.users WHERE email = 'admin.test@sistema.com')
BEGIN
    INSERT INTO dbo.users (role_id, status_id, name, email, password_hash)
    SELECT r.role_id, 1, 'Administrador', 'admin.test@sistema.com', 'CambiaEstaClave1*'
    FROM dbo.role r WHERE r.role_name = 'Administrador';
END
GO

COMMIT TRANSACTION;
GO

/* Resumen */
SELECT 'area' AS tabla, COUNT(*) AS filas FROM dbo.[area]
UNION ALL SELECT 'role' AS tabla, COUNT(*) AS filas FROM dbo.[role]
UNION ALL SELECT 'user_status' AS tabla, COUNT(*) AS filas FROM dbo.[user_status]
UNION ALL SELECT 'type_person' AS tabla, COUNT(*) AS filas FROM dbo.[type_person]
UNION ALL SELECT 'gender' AS tabla, COUNT(*) AS filas FROM dbo.[gender]
UNION ALL SELECT 'civil_state' AS tabla, COUNT(*) AS filas FROM dbo.[civil_state]
UNION ALL SELECT 'finace_status_product' AS tabla, COUNT(*) AS filas FROM dbo.[finace_status_product]
UNION ALL SELECT 'contract_status' AS tabla, COUNT(*) AS filas FROM dbo.[contract_status]
UNION ALL SELECT 'pay_form' AS tabla, COUNT(*) AS filas FROM dbo.[pay_form]
UNION ALL SELECT 'stage' AS tabla, COUNT(*) AS filas FROM dbo.[stage]
UNION ALL SELECT 'transaction_type' AS tabla, COUNT(*) AS filas FROM dbo.[transaction_type]
UNION ALL SELECT 'disaster_state' AS tabla, COUNT(*) AS filas FROM dbo.[disaster_state]
UNION ALL SELECT 'finance_products' AS tabla, COUNT(*) AS filas FROM dbo.[finance_products]
UNION ALL SELECT 'users', COUNT(*) FROM dbo.users;
GO
