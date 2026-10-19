
Create table DatosXML (
	  Id INT IDENTITY(1,1) PRIMARY KEY,
	  id_factura int not null,
	  Archivoxml xml not null,
	  fecha_guardado datetime




);


Create table CFDIPendientes (
	 Id INT IDENTITY(1,1) PRIMARY KEY,
	  id_factura_pendiente int not null,
	    fecha_guardado datetime

);

CREATE TABLE ConfiguracionDescarga (
    Id INT IDENTITY PRIMARY KEY,
    Tipo VARCHAR(15) NOT NULL,             
    Activo BIT NOT NULL DEFAULT 1,
    IntervaloBusquedaMinutos INT NOT NULL,  
    IntervaloConsultaMinutos INT NOT NULL,  
    UltimaFechaSolicitada DATETIME2 NULL    
);