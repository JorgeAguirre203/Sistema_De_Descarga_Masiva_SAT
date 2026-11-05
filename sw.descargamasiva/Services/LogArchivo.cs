using System;
using System.IO;
using System.Text;

namespace sw.descargamasiva.Services
{
    // Redirige Console.WriteLine a un archivo por día, para cuando el programa corre como windows service

 
    // Carpeta: C:\ProgramData\DescargaMasivaSAT\logs\
    internal static class LogArchivo
    {
        const int DiasAConservar = 30;

        public static void Iniciar()
        {
            string carpeta =
                Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
                    "DescargaMasivaSAT",
                    "logs");

            Directory.CreateDirectory(carpeta);

            BorrarAntiguos(carpeta);

            var escritor = new LogDiario(carpeta);

            Console.SetOut(escritor);
            Console.SetError(escritor);
        }

        // Borra logs de más de 30 días para que el disco no se llene
        static void BorrarAntiguos(string carpeta)
        {
            try
            {
                foreach (string archivo in Directory.GetFiles(carpeta, "log-*.txt"))
                {
                    if (File.GetLastWriteTime(archivo) < DateTime.Now.AddDays(-DiasAConservar))
                        File.Delete(archivo);
                }
            }
            catch
            {
                // Limpiar logs nunca debe impedir que el servicio arranque
            }
        }

        // Escribe en el archivo del día actual; al cambiar de día abre uno nuevo solo.
        class LogDiario : TextWriter
        {
            readonly string carpeta;
            readonly object candado = new object();

            public LogDiario(string carpeta)
            {
                this.carpeta = carpeta;
            }

            public override Encoding Encoding
            {
                get { return Encoding.UTF8; }
            }

            public override void Write(char value)
            {
                Escribir(value.ToString());
            }

            public override void Write(string value)
            {
                Escribir(value);
            }

            public override void WriteLine()
            {
                Escribir(Environment.NewLine);
            }

            public override void WriteLine(string value)
            {
                Escribir(DateTime.Now.ToString("HH:mm:ss") + "  " + value + Environment.NewLine);
            }

            void Escribir(string texto)
            {
                if (string.IsNullOrEmpty(texto))
                    return;

                lock (candado)
                {
                    try
                    {
                        string ruta =
                            Path.Combine(
                                carpeta,
                                "log-" + DateTime.Now.ToString("yyyyMMdd") + ".txt");

                        File.AppendAllText(ruta, texto, Encoding.UTF8);
                    }
                    catch
                    {
                        // Un fallo al escribir el log no debe tumbar el servicio
                    }
                }
            }
        }
    }
}