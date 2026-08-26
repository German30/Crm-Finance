-- =============================================================================
-- crm_finance — Catalogos y productos (MySQL 8)
-- -----------------------------------------------------------------------------
-- Ejecutar DESPUES de 00_schema.sql.
--
-- Los ids van explicitos porque el codigo depende de algunos: ClientService usa
-- type_person 1 = Fisica y 2 = Moral, UserService usa status 1 = Activo y
-- 2 = Inactivo, y FinanceProduct arranca en finance_status_product 1.
--
-- Idempotente via INSERT IGNORE: las filas cuya clave primaria ya existe se saltan,
-- asi que se puede volver a ejecutar sin duplicar nada.
--
-- Las fechas de alta se dejan al DEFAULT de cada tabla en vez de copiarlas.
--
-- >>> CARGALO SIEMPRE CON --default-character-set=utf8mb4 <<<
--
--   mysql -u root -p --default-character-set=utf8mb4 < 00_seed.sql
--
-- Sin esa opcion el cliente negocia latin1, los acentos del fichero (que es UTF-8) se
-- guardan doblemente codificados y "Deposito" acaba almacenado como "DepÃ³sito". Ademas
-- de verse mal, rompe OperationService: el signo de cada movimiento se deduce del nombre
-- del transaction_type, y con el texto corrupto deja de reconocerlos y el balance no se
-- mueve.
-- =============================================================================

-- Fija la codificacion de la SESION. Sin esto el resultado depende de como se
-- haya invocado el cliente: si negocia latin1, los acentos de este fichero (UTF-8)
-- se guardan doblemente codificados y "Deposito" acaba como "DepAsito". Ademas de
-- verse mal, rompe OperationService, que deduce el signo de cada movimiento del
-- nombre de su transaction_type y con el texto corrupto deja de reconocerlos.
SET NAMES utf8mb4;

USE `crm_finance`;

-- area : 3 filas
INSERT IGNORE INTO `area` (`area_id`, `area_name`, `description`) VALUES
    (1, 'General', 'Área global aplicable a toda la organización (Dirección General, TI, Administradores Maestros).'),
    (2, 'Seguros', 'División encargada de la comercialización, suscripción y gestión de pólizas y siniestros.'),
    (3, 'Banca', 'División enfocada en servicios bancarios, colocación de créditos, captación y operaciones en ventanilla.');

-- role : 19 filas
INSERT IGNORE INTO `role` (`role_id`, `area_id`, `role_name`, `category`, `description`) VALUES
    (1, 1, 'Administrador', 'Administración Global', 'Usuario maestro con acceso total al sistema y gestión de usuarios.'),
    (2, 2, 'Director o Gerente de Sucursal (Seguros)', 'Dirección y Administración', 'Lidera la oficina, establece estrategias de venta locales y supervisa metas.'),
    (3, 2, 'Coordinador Administrativo / RRHH (Seguros)', 'Dirección y Administración', 'Gestiona presupuesto local, pago de nóminas e instalaciones.'),
    (4, 2, 'Auxiliar Administrativo / Recepción (Seguros)', 'Dirección y Administración', 'Atención presencial, correspondencia, archivo y control de caja chica.'),
    (5, 2, 'Líder Comercial / Gerente de Ventas (Seguros)', 'Área Comercial y Ventas', 'Supervisa la fuerza de ventas e instruye en ramos de autos, vida y daños.'),
    (6, 2, 'Ejecutivo de Cuenta o Asesor (Seguros)', 'Área Comercial y Ventas', 'Asesora clientes, cotiza pólizas y da seguimiento a renovaciones.'),
    (7, 2, 'Agente de Seguros / Corredor', 'Área Comercial y Ventas', 'Agente de planta dedicado a la venta directa y prospección.'),
    (8, 2, 'Especialista en Suscripción', 'Operaciones, Servicio y Siniestros', 'Evalúa el riesgo de solicitudes antes de emitir pólizas y determina primas.'),
    (9, 2, 'Gestor de Siniestros / Reclamos', 'Operaciones, Servicio y Siniestros', 'Primer contacto en incidentes, recaba documentación y autoriza pagos/reparaciones.'),
    (10, 2, 'Ejecutivo de Atención al Cliente (Seguros)', 'Operaciones, Servicio y Siniestros', 'Resuelve dudas sobre coberturas, apoya en renovaciones y canaliza quejas.'),
    (11, 3, 'Gerente de Sucursal / Director de Agencia', 'Dirección y Gestión', 'Máxima autoridad. Responsable de metas comerciales, presupuesto y cumplimiento normativo.'),
    (12, 3, 'Subgerente de Sucursal', 'Dirección y Gestión', 'Apoya en la administración diaria y supervisa directamente al equipo operativo.'),
    (13, 3, 'Ejecutivo de Atención al Cliente (Banca)', 'Área Comercial y Asesoría', 'Venta de productos financieros (cuentas, tarjetas, seguros) y resolución de incidencias.'),
    (14, 3, 'Asesor Financiero / Ejecutivo Pyme', 'Área Comercial y Asesoría', 'Especialista en perfiles de alto valor/empresas. Asesora inversiones y créditos.'),
    (15, 3, 'Ejecutivo Hipotecarios', 'Área Comercial y Asesoría', 'Promoción, evaluación y gestión exclusiva de créditos para vivienda.'),
    (16, 3, 'Cajero Principal / Jefe de Caja', 'Área de Operaciones y Ventanilla', 'Supervisa flujo de efectivo, gestiona bóvedas y cuadra cierres diarios.'),
    (17, 3, 'Cajero / Operador de Ventanilla', 'Área de Operaciones y Ventanilla', 'Primer punto de contacto para depósitos, retiros, pagos y cambio de divisas.'),
    (18, 3, 'Coordinador / Analista de Riesgo y Crédito', 'Soporte y Control', 'Evalúa viabilidad de préstamos bajo las políticas de riesgo del banco.'),
    (19, 3, 'Asistente Administrativo / Recepcionista (Banca)', 'Soporte y Control', 'Recepción de clientes, canalización y control de documentación interna.');

-- user_status : 2 filas
INSERT IGNORE INTO `user_status` (`status_id`, `status_name`, `description`) VALUES
    (1, 'Activo', 'Usuario operativo con permisos de acceso normales en el sistema.'),
    (2, 'Inactivo', 'Usuario deshabilitado que no puede iniciar sesión ni realizar acciones.');

-- type_person : 2 filas
INSERT IGNORE INTO `type_person` (`type_person_id`, `type_name`, `description`) VALUES
    (1, 'Física', 'Individuo miembro de la población con derechos y obligaciones fiscales individuales.'),
    (2, 'Moral', 'Entidad jurídica o empresa constituida por una o más personas con una razón social.');

-- gender : 3 filas
INSERT IGNORE INTO `gender` (`gender_id`, `gender_name`) VALUES
    (1, 'Masculino'),
    (2, 'Femenino'),
    (3, 'No Especificado');

-- civil_state : 5 filas
INSERT IGNORE INTO `civil_state` (`civil_state_id`, `civil_state_name`) VALUES
    (1, 'Soltero/a'),
    (2, 'Casado/a'),
    (3, 'Divorciado/a'),
    (4, 'Viudo/a'),
    (5, 'Unión Libre');

-- finace_status_product : 2 filas
INSERT IGNORE INTO `finace_status_product` (`finance_status_product_id`, `finance_status_product_name`) VALUES
    (1, 'Activo'),
    (2, 'Descontinuado');

-- contract_status : 5 filas
INSERT IGNORE INTO `contract_status` (`contract_status_id`, `contract_status_name`) VALUES
    (1, 'Vigente'),
    (2, 'Inactivo'),
    (3, 'Cancelado'),
    (4, 'Vencido'),
    (5, 'En Reclamación');

-- pay_form : 4 filas
INSERT IGNORE INTO `pay_form` (`pay_form_id`, `pay_form_name`) VALUES
    (1, 'Mensual'),
    (2, 'Trimestral'),
    (3, 'Semestral'),
    (4, 'Anual');

-- stage : 6 filas
INSERT IGNORE INTO `stage` (`stage_id`, `stage_name`) VALUES
    (1, 'Prospecto'),
    (2, 'Cotización/Propuesta'),
    (3, 'Análisis de Riesgo'),
    (4, 'Negociación'),
    (5, 'Ganada'),
    (6, 'Perdida');

-- transaction_type : 5 filas
INSERT IGNORE INTO `transaction_type` (`transaction_type_id`, `transaction_type_name`) VALUES
    (1, 'Depósito'),
    (2, 'Retiro'),
    (3, 'Pago de Crédito'),
    (4, 'Cobro de Comisión'),
    (5, 'Interés Generado');

-- disaster_state : 4 filas
INSERT IGNORE INTO `disaster_state` (`disaster_state_id`, `disaster_state_name`) VALUES
    (1, 'Reportado'),
    (2, 'En Evaluación'),
    (3, 'Aprobado/Pagado'),
    (4, 'Rechazado');

-- finance_products : 24 filas
INSERT IGNORE INTO `finance_products` (`product_id`, `area_id`, `product_name`, `description`, `tasa_interes_o_prima_base`, `finance_status_product_id`) VALUES
    (1, 3, 'Cuenta de Ahorro', 'Diseñada para guardar dinero a la vista con una tasa de interés baja.', 1.50, 1),
    (2, 3, 'Cuenta de Cheques / Corriente', 'Permite realizar pagos mediante chequeras y transferencias constantes.', 0.00, 1),
    (3, 3, 'Cuenta de Nómina', 'Vinculada directamente al pago de salarios por parte de un empleador.', 0.00, 1),
    (4, 3, 'Tarjeta de Crédito', 'Línea de crédito revolvente para compras, con opción a pagar a meses.', 35.00, 1),
    (5, 3, 'Crédito Personal / de Nómina', 'Préstamo de libre destino para gastos imprevistos o liquidez.', 22.50, 1),
    (6, 3, 'Crédito Hipotecario', 'Financiamiento a largo plazo para la compra, construcción o remodelación de vivienda.', 9.50, 1),
    (7, 3, 'Crédito Automotriz', 'Específico para la adquisición de un vehículo nuevo o usado.', 12.00, 1),
    (8, 3, 'Pagaré Bancario / Depósito a Plazo', 'Inversión segura con dinero bloqueado a tiempo definido y rendimiento garantizado.', 8.00, 1),
    (9, 3, 'Fondo de Inversión', 'Conjunto de activos operados por expertos, ideales para diversificar capital.', 11.00, 1),
    (10, 3, 'Caja de Seguridad', 'Espacio físico dentro de la sucursal para resguardar objetos de valor.', 0.00, 1),
    (11, 3, 'Transferencias y Cambio de Divisas', 'Envío de remesas o compra-venta de moneda extranjera.', 0.00, 1),
    (12, 2, 'Gastos Médicos Mayores', 'Cubre hospitalización, consultas, medicamentos y tratamientos por enfermedades graves.', 0.00, 1),
    (13, 2, 'Seguro de Vida', 'Brinda una suma asegurada a los beneficiarios en caso de fallecimiento del titular.', 0.00, 1),
    (14, 2, 'Accidentes Personales', 'Indemniza o cubre gastos médicos por lesiones o incapacidades derivadas de un accidente.', 0.00, 1),
    (15, 2, 'Seguro de Salud / Dental', 'Plan preventivo o correctivo para consultas, revisiones y tratamientos específicos.', 0.00, 1),
    (16, 2, 'Seguro de Automóvil', 'Coberturas desde Daños a Terceros hasta Daños Materiales, Robo Total y Gastos Médicos.', 0.00, 1),
    (17, 2, 'Seguro de Casa Habitación', 'Protección de estructura y bienes internos contra robos, incendios o terremotos.', 0.00, 1),
    (18, 2, 'Responsabilidad Civil (RC)', 'Cubre el pago de indemnizaciones por daños materiales o lesiones a terceros.', 0.00, 1),
    (19, 2, 'Seguro de Flotillas', 'Protección para los vehículos de uso comercial o utilitario de la empresa.', 0.00, 1),
    (20, 2, 'Seguro Empresarial', 'Cubre el local, inventario, maquinaria y protege contra interrupción de actividades.', 0.00, 1),
    (21, 2, 'Responsabilidad Civil Profesional', 'Protege ante demandas por errores, omisiones o negligencias profesionales.', 0.00, 1),
    (22, 2, 'Plan de Retiro', 'Fondos de inversión a largo plazo para asegurar un ingreso mensual en la jubilación.', 6.50, 1),
    (23, 2, 'Seguro Educativo', 'Plan de ahorro con seguro de vida para garantizar los estudios universitarios de los hijos.', 5.00, 1),
    (24, 2, 'Seguro Dotal', 'Combina protección por fallecimiento con ahorro; si sobrevive al plazo, recibe la suma.', 4.00, 1);

-- -----------------------------------------------------------------------------
-- Usuario administrador de arranque
-- -----------------------------------------------------------------------------
-- Hace falta uno para poder llamar a POST /api/User, que exige token de
-- administrador: sin este no hay forma de crear el primero.
--
-- La contrasena va EN TEXTO PLANO a proposito. AuthService acepta un hash heredado
-- en texto plano una unica vez y lo regraba como PBKDF2 en el primer login
-- correcto, asi que no hace falta calcular un hash a mano aqui.
--
-- >>> CAMBIA LA CONTRASENA EN CUANTO ENTRES. <<<
-- -----------------------------------------------------------------------------

INSERT IGNORE INTO `users` (`role_id`, `status_id`, `name`, `email`, `password_hash`)
SELECT r.role_id, 1, 'Administrador', 'admin.test@sistema.com', 'CambiaEstaClave1*'
FROM `role` r
WHERE r.role_name = 'Administrador'
  AND NOT EXISTS (SELECT 1 FROM (SELECT 1 FROM `users` WHERE email = 'admin.test@sistema.com') AS x);

-- Resumen
SELECT 'area' AS tabla, COUNT(*) AS filas FROM `area`
UNION ALL SELECT 'role' AS tabla, COUNT(*) AS filas FROM `role`
UNION ALL SELECT 'user_status' AS tabla, COUNT(*) AS filas FROM `user_status`
UNION ALL SELECT 'type_person' AS tabla, COUNT(*) AS filas FROM `type_person`
UNION ALL SELECT 'gender' AS tabla, COUNT(*) AS filas FROM `gender`
UNION ALL SELECT 'civil_state' AS tabla, COUNT(*) AS filas FROM `civil_state`
UNION ALL SELECT 'finace_status_product' AS tabla, COUNT(*) AS filas FROM `finace_status_product`
UNION ALL SELECT 'contract_status' AS tabla, COUNT(*) AS filas FROM `contract_status`
UNION ALL SELECT 'pay_form' AS tabla, COUNT(*) AS filas FROM `pay_form`
UNION ALL SELECT 'stage' AS tabla, COUNT(*) AS filas FROM `stage`
UNION ALL SELECT 'transaction_type' AS tabla, COUNT(*) AS filas FROM `transaction_type`
UNION ALL SELECT 'disaster_state' AS tabla, COUNT(*) AS filas FROM `disaster_state`
UNION ALL SELECT 'finance_products' AS tabla, COUNT(*) AS filas FROM `finance_products`
UNION ALL SELECT 'users', COUNT(*) FROM `users`;
