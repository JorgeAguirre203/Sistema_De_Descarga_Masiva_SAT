using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace sw.descargamasiva.InsertarBD
{
    internal class SolicitudPendiente
    {
        public int Id { get; set; }

        public string IdSolicitudSat { get; set; }

        public string Tipo { get; set; }
        public string RFC { get; set; }
    }
}