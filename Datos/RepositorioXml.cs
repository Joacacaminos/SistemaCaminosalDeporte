using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Serialization;

namespace SistemaCaminosalDeporte.Datos
{
    public class RepositorioXml<T> where T : class, new()
    {
        private readonly string rutaArchivo;
        private readonly Func<T, int> obtenerId;
        private readonly Action<T, int> asignarId;
        private readonly object sincronizacion = new object();
        private List<T> registros;

        protected RepositorioXml(
            string nombreArchivo,
            Func<T, int> obtenerId,
            Action<T, int> asignarId,
            IEnumerable<T> datosIniciales = null)
        {
            if (string.IsNullOrWhiteSpace(nombreArchivo))
                throw new ArgumentException("El nombre del archivo es obligatorio.", nameof(nombreArchivo));
            if (obtenerId == null)
                throw new ArgumentNullException(nameof(obtenerId));
            if (asignarId == null)
                throw new ArgumentNullException(nameof(asignarId));

            var carpetaDatos = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Datos");
            Directory.CreateDirectory(carpetaDatos);
            rutaArchivo = Path.Combine(carpetaDatos, nombreArchivo);
            this.obtenerId = obtenerId;
            this.asignarId = asignarId;

            if (File.Exists(rutaArchivo))
            {
                registros = LeerArchivo();
            }
            else
            {
                registros = datosIniciales == null
                    ? new List<T>()
                    : new List<T>(datosIniciales);
                Guardar();
            }

            NormalizarIds();
        }

        private void NormalizarIds()
        {
            lock (sincronizacion)
            {
                var idsUsados = new HashSet<int>(registros.Where(x => obtenerId(x) > 0).Select(obtenerId));
                var siguienteId = idsUsados.Count == 0 ? 1 : idsUsados.Max() + 1;
                var huboCambios = false;

                foreach (var registro in registros)
                {
                    if (obtenerId(registro) > 0 && registros.Count(x => obtenerId(x) == obtenerId(registro)) == 1)
                        continue;

                    while (idsUsados.Contains(siguienteId))
                        siguienteId++;

                    asignarId(registro, siguienteId++);
                    idsUsados.Add(obtenerId(registro));
                    huboCambios = true;
                }

                if (huboCambios)
                    GuardarInternamente();
            }
        }

        public List<T> ObtenerTodos()
        {
            lock (sincronizacion)
            {
                return new List<T>(registros);
            }
        }

        public T ObtenerPorId(int id)
        {
            lock (sincronizacion)
            {
                return registros.FirstOrDefault(x => obtenerId(x) == id);
            }
        }

        public T Agregar(T entidad)
        {
            if (entidad == null)
                throw new ArgumentNullException(nameof(entidad));

            lock (sincronizacion)
            {
                var siguienteId = registros.Count == 0 ? 1 : registros.Max(obtenerId) + 1;
                asignarId(entidad, siguienteId);
                registros.Add(entidad);
                Guardar();
                return entidad;
            }
        }

        public void Guardar()
        {
            lock (sincronizacion)
            {
                GuardarInternamente();
            }
        }

        public void ReemplazarTodos(IEnumerable<T> nuevosRegistros)
        {
            if (nuevosRegistros == null)
                throw new ArgumentNullException(nameof(nuevosRegistros));

            lock (sincronizacion)
            {
                registros = new List<T>(nuevosRegistros);
                GuardarInternamente();
            }
        }

        private List<T> LeerArchivo()
        {
            var serializer = new XmlSerializer(typeof(List<T>));
            using (var stream = File.OpenRead(rutaArchivo))
            {
                return (List<T>)serializer.Deserialize(stream);
            }
        }

        private void GuardarInternamente()
        {
            var archivoTemporal = rutaArchivo + ".tmp";
            var serializer = new XmlSerializer(typeof(List<T>));

            try
            {
                using (var stream = new FileStream(
                    archivoTemporal,
                    FileMode.Create,
                    FileAccess.Write,
                    FileShare.None))
                {
                    serializer.Serialize(stream, registros);
                    stream.Flush(true);
                }

                if (File.Exists(rutaArchivo))
                    File.Replace(archivoTemporal, rutaArchivo, null);
                else
                    File.Move(archivoTemporal, rutaArchivo);
            }
            finally
            {
                if (File.Exists(archivoTemporal))
                    File.Delete(archivoTemporal);
            }
        }
    }
}
