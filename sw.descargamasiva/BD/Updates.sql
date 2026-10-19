
INSERT INTO ControlDescargaSAT
(
    tipo,
    ultimafechaprocesada
)
VALUES
('RECIBIDO', NULL),
('EMITIDO', NULL);


UPDATE ControlRFCSAT
SET RFC = ''
WHERE tipo = 'RECIBIDO';

INSERT INTO ControlConsultasSAT
(
    tipo,
    ultimaConsulta
)
VALUES
(
    'RECIBIDO',
    DATEADD(MINUTE, -10, SYSDATETIME())
);


INSERT INTO ControlConsultasSAT
(
    tipo,
    ultimaConsulta
)
VALUES
(
    'EMITIDO',
    DATEADD(MINUTE, -10, SYSDATETIME())
);


UPDATE ControlConsultasSAT
SET ultimaConsulta = '2025-03-10 00:00:00'
WHERE tipo = 'EMITIDO';


USE master;
  IF NOT EXISTS (SELECT 1 FROM sys.server_principals WHERE name = 'NT AUTHORITY\SYSTEM')
      CREATE LOGIN [NT AUTHORITY\SYSTEM] FROM WINDOWS;
  GO
  USE DescargaMasivaAutonoma;
  GO
  CREATE USER [NT AUTHORITY\SYSTEM] FOR LOGIN [NT AUTHORITY\SYSTEM];
  ALTER ROLE db_datareader ADD MEMBER [NT AUTHORITY\SYSTEM];
  ALTER ROLE db_datawriter ADD MEMBER [NT AUTHORITY\SYSTEM];