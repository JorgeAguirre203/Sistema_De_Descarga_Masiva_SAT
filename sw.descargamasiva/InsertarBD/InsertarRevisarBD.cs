using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using sw.descargamasiva.Services;



namespace sw.descargamasiva.InsertarBD

{
    internal class InsertarRevisarBD
    {

        //Esta funcion es la que se encarga de revisar el estado de la solicitud y descargar los paquetes si están disponibles
        public static void RevisarYDescargarSolicitud(
           X509Certificate2 certificate,
           string authorization,
           string idSolicitud,
           string tipo)

        {
            if (string.IsNullOrWhiteSpace(idSolicitud))
            {
                Console.WriteLine("No se proporcionó un IdSolicitud válido.");        //si el id de la solicitud no es valido o esta vacio este mensaje se mostrara en consola y el programa terminara
                return;
            }
            Console.WriteLine();
            Console.WriteLine(
               "verificando solicitud"
                );

            Console.WriteLine(idSolicitud);

            VerificaSolicitud resultado = Orquestador.ValidarSolicitud(      //se obtiene resultado (xml de salida del sat) el cual contiene la informacion denuetsra peticion al sat
                certificate,
                authorization,
                idSolicitud);

            actsolibd.ActualizarSolicitudEnBD(
                idSolicitud,
                     resultado);

            Console.WriteLine();
            Console.WriteLine(
                "Estado de la solicitud: " + resultado.EstadoSolicitud); //se le extrae el estado de la solicitud a resultado

            Console.WriteLine("Código de estado: " + resultado.CodigoEstadoSolicitud);         //se imprime en la consola el codigo de estado de el resultado (la solicitud)
            if (resultado.EstadoSolicitud == "1")  // si es 1 es que aun no la esta procesando el sat 
            {
                Console.WriteLine(
                    "La solicitud fue aceptada, " +
                    "pero todavía no está procesándose.");

                return;
            }


            if (resultado.EstadoSolicitud == "2")
            {  // un codigo de salida 2 es que esta en proceso y la solicitud debe esperar

                Console.WriteLine("Solicitud en proceso");
                Console.WriteLine("verifica de nuevo utilizando este id " + idSolicitud);



            }

            if (resultado.EstadoSolicitud != "3")      //cuando el sat devuelve codigo de salida = a 3 ya esta listo para descargar los paquetes,
                                                       //si no es 3 entonces se muestra el mensaje de solicitud no disponible y termina el programa
            {
                Console.WriteLine("Solicitud no disponible");
                return;



            }

            if (resultado.IdsPaquetes.Count == 0)      //aqui se lee de la variable resultado el conteo de paquetes mediante ids.paquetes.count el cual de ser 0 
                                                       //quiere decir que la consulta no contenia CFDIS es decir no habia paquetes
            {
                Console.WriteLine("Solicitud sin paquetes que descargar");
                return;
            }

            //esta seccion de codigo inferior es lo que procede de haber paquetes...
            Console.WriteLine();
            Console.WriteLine(
                "Paquetes encontrados: " +
                resultado.IdsPaquetes.Count);

            foreach (
                string idPaquete
                in resultado.IdsPaquetes)
            {
                GuardarpaqenBD.GuardarPaqueteEnBD(
                    idSolicitud,
                    idPaquete);
                try
                {
                    Console.WriteLine();
                    Console.WriteLine(
                        "Descargando paquete:");

                    Console.WriteLine(idPaquete);

                    string descargaResponse =      //se llama a la funcion de descarga 
                        SolicDescarga.DescargarSolicitud(
                            certificate,
                            authorization,
                            idPaquete);

                    // Guarda GZIP, descomprime, mueve los XML a su carpeta final y devuelve la ruta del GZIP
                    string rutaArchivo =
                        GuardaryDescomprimirPQ.GuardarSolicitud(
                            idPaquete,
                            descargaResponse,
                            tipo,
                            CertificadoRfc.Obtener(certificate));


                    ActualizarPQEnBD.ActualizarPaqueteEnBD(
                        idPaquete,
                        true,
                        true,
                        rutaArchivo,
                        null);

                    Console.WriteLine(
                        "Paquete procesado correctamente.");
                }
                catch (Exception ex)
                {
                    ActualizarPQEnBD.ActualizarPaqueteEnBD(
                        idPaquete,
                        false,
                        false,
                        null,
                        ex.Message);

                    Console.WriteLine(
                        "ERROR procesando paquete " +
                        idPaquete);

                    Console.WriteLine(
                        ex.Message);
                }
            }
        }

        public static List<SolicitudPendiente> ObtenerSolicitudesPendientes(string rfc)
        {
            List<SolicitudPendiente> solicitudes =
                new List<SolicitudPendiente>();

            string query = @"
        SELECT
            id,
            idsolicitudsat,
            tipo,
            RFC
        FROM SolicitudesSAT
        WHERE estado IN (1, 2)
          AND RFC = @rfc
          AND proximarevision IS NOT NULL
          AND proximarevision <= SYSDATETIME()
        ORDER BY proximarevision ASC;
    ";

            using (SqlConnection conn =
                new SqlConnection(Guardarenbased.connectionString))
            using (SqlCommand cmd =
                new SqlCommand(query, conn))
            {
                cmd.Parameters.Add("@rfc", SqlDbType.NVarChar, 15).Value = rfc;

                conn.Open();

                using (SqlDataReader reader =
                    cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        SolicitudPendiente solicitud =
                            new SolicitudPendiente();

                        solicitud.Id =
                            Convert.ToInt32(reader["id"]);

                        solicitud.IdSolicitudSat =
                            reader["idsolicitudsat"].ToString();

                        solicitud.Tipo =
                            reader["tipo"].ToString();

                        solicitud.RFC =
                            reader["RFC"].ToString();

                        solicitudes.Add(solicitud);
                    }
                }
            }

            return solicitudes;
        }
        public static void ProcesarSolicitudesPendientes(
    X509Certificate2 certificate,
    string authorization)
        {
            // Solo las solicitudes del RFC dueño de este certificado
            string rfc = CertificadoRfc.Obtener(certificate);

            List<SolicitudPendiente> solicitudes =
                ObtenerSolicitudesPendientes(rfc);

            if (solicitudes.Count == 0)
            {
                Console.WriteLine();
                Console.WriteLine(
                    "No hay solicitudes pendientes de revisión.");

                return;
            }

            Console.WriteLine();
            Console.WriteLine(
                "Solicitudes pendientes: " +
                solicitudes.Count);

            foreach (SolicitudPendiente solicitud
                in solicitudes)
            {
                Console.WriteLine();
                Console.WriteLine(
                    "====================================");

                Console.WriteLine(
                    "Solicitud: " +
                    solicitud.IdSolicitudSat);

                Console.WriteLine(
                    "Tipo: " +
                    solicitud.Tipo);

                Console.WriteLine(
                    "====================================");

                try
                {
                    // Preguntar al SAT el estado actual
                    VerificaSolicitud resultado =
                        Orquestador.ValidarSolicitud(
                            certificate,
                            authorization,
                            solicitud.IdSolicitudSat);

                    // Guardar el nuevo estado en SQL Server
                    actsolibd.ActualizarSolicitudEnBD(
                        solicitud.IdSolicitudSat,
                        resultado);

                    Console.WriteLine();
                    Console.WriteLine(
                        "Estado actualizado en BD.");

                    Console.WriteLine(
                        "Estado SAT: " +
                        resultado.EstadoSolicitud);

                    Console.WriteLine(
                        "Código SAT: " +
                        resultado.CodigoEstadoSolicitud);

                    Console.WriteLine(
                        "CFDI: " +
                        resultado.NumeroCFDIs);

                    // Si todavía está procesándose,
                    // no hacemos nada más.
                    if (resultado.EstadoSolicitud == "1")
                    {
                        Console.WriteLine(
                            "La solicitud todavía no está siendo procesada.");

                        continue;
                    }

                    if (resultado.EstadoSolicitud == "2")
                    {
                        Console.WriteLine(
                            "La solicitud continúa en proceso.");

                        continue;
                    }

                    // Si no es 3, no está lista para descargar
                    if (resultado.EstadoSolicitud != "3")
                    {
                        Console.WriteLine(
                            "La solicitud no está disponible para descarga.");

                        continue;
                    }

                    // Estado 3 = terminada
                    if (resultado.IdsPaquetes.Count == 0)
                    {
                        Console.WriteLine(
                            "La solicitud terminó pero no contiene paquetes.");

                        continue;
                    }

                    Console.WriteLine();
                    Console.WriteLine(
                        "Paquetes encontrados: " +
                        resultado.IdsPaquetes.Count);

                    foreach (string idPaquete
                        in resultado.IdsPaquetes)
                    {
                        try
                        {
                            Console.WriteLine();
                            Console.WriteLine(
                                "Procesando paquete:");

                            Console.WriteLine(idPaquete);

                            // Registrar paquete en BD
                            GuardarpaqenBD.GuardarPaqueteEnBD(
                                solicitud.IdSolicitudSat,
                                idPaquete);

                            // Descargar paquete del SAT
                            string descargaResponse =
                                SolicDescarga.DescargarSolicitud(
                                    certificate,
                                    authorization,
                                    idPaquete);

                            // Guardar GZIP, descomprimir y guardar XML
                            // Guarda GZIP, descomprime, mueve los XML a su carpeta final y devuelve la ruta del GZIP
                            string rutaArchivo =
                                GuardaryDescomprimirPQ.GuardarSolicitud(
                                    idPaquete,
                                    descargaResponse,
                                    solicitud.Tipo,
                                    rfc);

                            // Marcar paquete como descargado
                            ActualizarPQEnBD.ActualizarPaqueteEnBD(
                                idPaquete,
                                true,
                                true,
                                rutaArchivo,
                                null);

                            Console.WriteLine();
                            Console.WriteLine(
                                "Paquete procesado correctamente.");
                        }
                        catch (Exception ex)
                        {
                            ActualizarPQEnBD.ActualizarPaqueteEnBD(
                                idPaquete,
                                false,
                                false,
                                null,
                                ex.Message);

                            Console.WriteLine();
                            Console.WriteLine(
                                "ERROR procesando paquete:");

                            Console.WriteLine(idPaquete);

                            Console.WriteLine(
                                ex.Message);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine();
                    Console.WriteLine(
                        "ERROR procesando solicitud:");

                    Console.WriteLine(
                        solicitud.IdSolicitudSat);

                    Console.WriteLine(
                        ex.Message);
                }
            }
        }

    }
}