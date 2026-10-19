using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace sw.descargamasiva.InsertarBD
{
    internal class actsolibd
    {

        public static void ActualizarSolicitudEnBD(
string idSolicitudSat,
VerificaSolicitud resultado)
        {
            int estado = 0;

            int.TryParse(
                resultado.EstadoSolicitud, //SE PARSEA EL RESULTADO QUE VENDRIA A SER EL XML QUE DEVUELVE EL SAT CON LA INFORMACION DE LA PETICION EN ESTE CASO SE SOLICITA EL estado.solicitud
                out estado);

            string query = @"
        UPDATE SolicitudesSAT  

        SET
            estado = @estado,
            codigoestado = @codigoestado,
            numerocfdis = @numerocfdis,
            mensaje = @mensaje,
            fechaultimarevision =
                SYSDATETIME(),

            proximarevision =
                CASE
                    WHEN @estado IN (1, 2) --si el estado es 1 o 2 entonces se le asigna a la variable proximarevision la fecha y hora actual mas 5 minutos, de lo contrario se le asigna null
                    THEN DATEADD(
                        MINUTE,
                        5,
                        SYSDATETIME()
                    )
                    ELSE NULL
                END,

            intentos = intentos + 1

        WHERE
            idsolicitudsat =
                @idsolicitudsat;
    ";

            using (SqlConnection conn =
                new SqlConnection(Guardarenbased.connectionString))
            using (SqlCommand cmd =
                new SqlCommand(query, conn))
            {
                cmd.Parameters.Add(
                    "@idsolicitudsat",
                    SqlDbType.VarChar, //le asignamos el valor a la variable idsolicitudsat que es el id de la solicitud que se le asigna a la variable idSolicitudSat
                    100).Value =
                        idSolicitudSat;

                cmd.Parameters.Add(
                    "@estado",
                    SqlDbType.Int).Value = // se jala el valor de estado anteriormente parseado 
                        estado;

                cmd.Parameters.Add(
                    "@codigoestado",
                    SqlDbType.VarChar,
                    20).Value =
                        (object)resultado  // se obtiene de resultado el codigo de estado d ela solicitud
                            .CodigoEstadoSolicitud
                        ?? DBNull.Value;

                cmd.Parameters.Add(
                    "@numerocfdis",
                    SqlDbType.Int).Value =  //se obtiene de resultado el numero de cfdis 
                        resultado.NumeroCFDIs;

                cmd.Parameters.Add(
                    "@mensaje",
                    SqlDbType.NVarChar,
                    500).Value =
                        (object)resultado.Mensaje // se obtiene de resultado el mensaje de la solicitud
                        ?? DBNull.Value;

                conn.Open();

                cmd.ExecuteNonQuery();
            }
        }
    }
}


