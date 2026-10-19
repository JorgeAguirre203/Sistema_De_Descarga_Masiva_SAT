SELECT * FROM DatosXML WHERE id_factura IS NOT NULL;

select * from PaquetesSAT;

select * from SolicitudesSAT;

Select * from ControlDescargaSAT;

select * from ControlRFCSAT

select * from ControlConsultasSAT 

select * from DatosXML;

SELECT * FROM dbo.EstadoCfdiSAT

SELECT name, definition
FROM sys.check_constraints
WHERE name = 'CK_SolicitudTipo';
