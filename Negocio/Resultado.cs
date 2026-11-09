using System;
using System.IO;

namespace SistemaCaminosalDeporte.Negocio
{
    public class Resultado
    {
        public bool Exito { get; private set; }
        public string Mensaje { get; private set; }

        private Resultado(bool exito, string mensaje)
        {
            Exito = exito;
            Mensaje = mensaje;
        }

        public static Resultado Ok(string mensaje = "Operación realizada correctamente.")
        {
            return new Resultado(true, mensaje);
        }

        public static Resultado Error(string mensaje)
        {
            return new Resultado(false, mensaje);
        }
    }

    public class CopiaSeguridadNegocio
    {
        public Resultado<string> CrearCopia(string carpetaDestino)
        {
            if (string.IsNullOrWhiteSpace(carpetaDestino))
                return Resultado<string>.Error("La carpeta de destino es obligatoria.");

            try
            {
                var carpetaDatos = ObtenerCarpetaDatos();
                if (!Directory.Exists(carpetaDatos))
                    return Resultado<string>.Error("No existe la carpeta de datos para copiar.");

                Directory.CreateDirectory(carpetaDestino);
                var nombre = "Copia_" + DateTime.Now.ToString("yyyyMMdd_HHmmss");
                var carpetaCopia = Path.Combine(carpetaDestino, nombre);
                var sufijo = 1;
                while (Directory.Exists(carpetaCopia))
                    carpetaCopia = Path.Combine(carpetaDestino, nombre + "_" + sufijo++);

                Directory.CreateDirectory(carpetaCopia);
                var archivos = Directory.GetFiles(carpetaDatos, "*.xml");
                foreach (var archivo in archivos)
                    File.Copy(archivo, Path.Combine(carpetaCopia, Path.GetFileName(archivo)));

                return Resultado<string>.Ok(carpetaCopia, "Copia creada correctamente en: " + carpetaCopia);
            }
            catch (Exception ex)
            {
                return Resultado<string>.Error("No se pudo crear la copia: " + ex.Message);
            }
        }

        public Resultado RestaurarCopia(string carpetaCopia)
        {
            if (string.IsNullOrWhiteSpace(carpetaCopia) || !Directory.Exists(carpetaCopia))
                return Resultado.Error("La carpeta de copia no existe.");

            try
            {
                var archivos = Directory.GetFiles(carpetaCopia, "*.xml");
                if (archivos.Length == 0)
                    return Resultado.Error("La carpeta seleccionada no contiene archivos XML.");

                var carpetaDatos = ObtenerCarpetaDatos();
                Directory.CreateDirectory(carpetaDatos);
                foreach (var archivo in archivos)
                    File.Copy(archivo, Path.Combine(carpetaDatos, Path.GetFileName(archivo)), true);

                return Resultado.Ok("Copia restaurada correctamente. Reiniciá la aplicación para recargar los datos.");
            }
            catch (Exception ex)
            {
                return Resultado.Error("No se pudo restaurar la copia: " + ex.Message);
            }
        }

        private static string ObtenerCarpetaDatos()
        {
            return System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Datos");
        }
    }

    public class Resultado<T>
    {
        public bool Exito { get; private set; }
        public string Mensaje { get; private set; }
        public T Valor { get; private set; }

        private Resultado(bool exito, string mensaje, T valor)
        {
            Exito = exito;
            Mensaje = mensaje;
            Valor = valor;
        }

        public static Resultado<T> Ok(T valor, string mensaje = "Operación realizada correctamente.")
        {
            return new Resultado<T>(true, mensaje, valor);
        }

        public static Resultado<T> Error(string mensaje)
        {
            return new Resultado<T>(false, mensaje, default(T));
        }
    }
}
