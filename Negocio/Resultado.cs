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
