
CREATE TABLE SolicitudesSAT
(
    id INT IDENTITY(1,1) PRIMARY KEY,

    idsolicitudsat VARCHAR(100) NOT NULL,

    tipo VARCHAR(15) NOT NULL,

    fechainicial DATETIME2(0) NOT NULL,
    fechafinal DATETIME2(0) NOT NULL,

    estado INT NULL DEFAULT (0),

    codigoestado VARCHAR(20) NULL,

    numerocfdis INT NULL,

    mensaje NVARCHAR(500) NULL,

    fechasolicitud DATETIME2(0)
        NOT NULL DEFAULT SYSDATETIME(),

    fechaultimarevision DATETIME2(0) NULL,

    proximarevision DATETIME2(0) NULL,

    intentos INT NOT NULL DEFAULT 0,

    descargada BIT NOT NULL DEFAULT 0,

    fechadescarga DATETIME2(0) NULL,

    CONSTRAINT UQ_SolicitudesSAT
        UNIQUE(idsolicitudsat),

    CONSTRAINT CK_SolicitudTipo
        CHECK(tipo IN ('EMITIDO','RECIBIDO'))
);



CREATE TABLE PaquetesSAT
(
    id INT IDENTITY(1,1) PRIMARY KEY,

    solicitudid INT NOT NULL,

    idpaquetesat VARCHAR(150) NOT NULL,

    descargado BIT NOT NULL DEFAULT 0,

    descomprimido BIT NOT NULL DEFAULT 0,

    rutaarchivo NVARCHAR(500) NULL,

    fechadescarga DATETIME2(0) NULL,

    error NVARCHAR(1000) NULL,

    CONSTRAINT FK_Paquetes_Solicitudes
        FOREIGN KEY(solicitudid)
        REFERENCES SolicitudesSAT(id),

    CONSTRAINT UQ_PaquetesSAT
        UNIQUE(idpaquetesat)
);


CREATE TABLE ControlDescargaSAT
(
    id INT IDENTITY(1,1) PRIMARY KEY,

    tipo VARCHAR(15) NOT NULL,

    ultimafechaprocesada DATETIME2(0) NULL,

    fechaactualizacion DATETIME2(0)
        NOT NULL DEFAULT SYSDATETIME(),

    CONSTRAINT UQ_ControlDescargaSAT
        UNIQUE(tipo),

    CONSTRAINT CK_ControlDescargaTipo
        CHECK(tipo IN ('EMITIDO','RECIBIDO'))
);



CREATE TABLE ControlConsultasSAT
(
    tipo VARCHAR(15) NOT NULL PRIMARY KEY,

    ultimaConsulta DATETIME2(0) NOT NULL
);


  
CREATE TABLE ControlRFCSAT
(
    tipo varchar(15) not null primary key,
    RFC nvarchar(15)
); 


  CREATE TABLE dbo.EstadoCfdiSAT (
    id_factura            NVARCHAR(50)  NOT NULL PRIMARY KEY,   -- = DatosXML.id_factura (UUID)
    rfc_emisor            VARCHAR(13)   NULL,
    rfc_receptor          VARCHAR(13)   NULL,
    total                 DECIMAL(18,6) NULL,
 
    estado                VARCHAR(20)   NULL,    -- Vigente / Cancelado / No Encontrado
    es_cancelable         VARCHAR(60)   NULL,
    estatus_cancelacion   VARCHAR(60)   NULL,    -- En proceso, Plazo vencido, Cancelado sin aceptación...
    validacion_efos       VARCHAR(10)   NULL,
    codigo_estatus        VARCHAR(200)  NULL,
 
    estado_anterior       VARCHAR(20)   NULL,    -- el estado que tenía antes del último cambio
    fecha_cambio_estado   DATETIME2     NULL,    -- cuándo se detectó el cambio
 
    fecha_ultima_consulta DATETIME2     NULL,
    proxima_consulta      DATETIME2     NOT NULL DEFAULT SYSDATETIME(),
    consultas             INT           NOT NULL DEFAULT 0,
    errores               INT           NOT NULL DEFAULT 0,
    ultimo_error          NVARCHAR(400) NULL
);
GO
 
CREATE INDEX IX_EstadoCfdiSAT_proxima ON dbo.EstadoCfdiSAT (proxima_consulta);
GO
 
-- Si el descargador corre con NT AUTHORITY\SYSTEM y ya tiene db_datareader/db_datawriter,
-- no necesita más permisos. Para el panel (IIS):
-- GRANT SELECT ON dbo.EstadoCfdiSAT TO [IIS APPPOOL\PanelCFDI];