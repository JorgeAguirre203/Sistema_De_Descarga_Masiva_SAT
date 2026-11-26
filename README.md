# Software Automatizado de Descarga-Masiva-CFDI-SAT

El siguiente es un software contable que permite la descarga automatizada
de facturas emitidas por el SAT mediante 4 workers principales.

El programa permite agregar mas de un solo contribuyente, configurar intervalos de tiempo entre otros y ademas
permite agregar sus datos FIEL desde la aplicación así como una configuración bastante completa del mismo programa y sus intervalos.

-Importante: El programa requiere SW.DESCARGAMASIVA esta pensado y diseñado para funcionar como servicio de Windows por lo que
en caso de querer usarlo se le debera dar de alta en el sistema manualmente.

<img width="1904" height="941" alt="image" src="https://github.com/user-attachments/assets/746cc3b7-04ca-4972-b081-d6f9d85cc183" />

## Requisitos
-Windows con .NET Framework 4.8, y IIS o IIS Express (IIS Express si se abre desde Visual Studio). Es la base del equipo.


-Acceso a internet, para llegar a los servicios del SAT y a los paquetes NuGet o fuentes del panel.


-SQL Server con la base DescargaMasivaAutonoma, ya creada con sus tablas.


-Modificar la cadena de conexión en el Web.config para que apunte a tu instancia (hoy usa localhost\SQLEXPRESS01).


-IIS para PanelCFDI: publicar el panel como sitio web.


-Dar a la identidad del grupo de aplicaciones de IIS un login en SQL Server, con permisos sobre la base. Sin esto el panel no puede leer ni escribir datos.


-Dar permiso de escritura en la carpeta de la FIEL a esa misma identidad, para que el panel pueda guardar los .cer y .key.


-Contar con la FIEL de el o los contribuyentes que se vayan a registrar, y cargarla desde el panel. Debe estar vigente. Los archivos los puedes ir juntando desde antes, pero hasta este punto ya tienes dónde subirlos.


-Dar de alta el servicio de Windows.


