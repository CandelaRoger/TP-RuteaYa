namespace RuteaYa.Logica.Rutas
{
    public static class PlanificadorRuta
    {
        // puntosViaje = origen, paradas (en su orden), destino.
        // a = donde se retira el paquete, b = donde se entrega.
        // Devuelve la ruta con A y B intercalados en el mejor lugar.
        public static List<Coordenadas> MejorInsercion(
            List<Coordenadas> puntosViaje, Coordenadas a, Coordenadas b)
        {
            List<Coordenadas>? mejorRuta = null;
            double mejorLongitud = double.MaxValue;

            // El hueco i está entre puntosViaje[i] y puntosViaje[i + 1]
            for (int i = 0; i < puntosViaje.Count - 1; i++)
            {
                for (int j = i; j < puntosViaje.Count - 1; j++) // j >= i: A siempre antes que B
                {
                    var candidata = ArmarRuta(puntosViaje, i, j, a, b);
                    double longitud = LongitudKm(candidata);

                    if (longitud < mejorLongitud)
                    {
                        mejorLongitud = longitud;
                        mejorRuta = candidata;
                    }
                }
            }

            return mejorRuta!;
        }

        // Cuántos km agrega el paquete: ruta con el envío menos ruta original.
        public static double CostoExtraKm(
            List<Coordenadas> puntosViaje, List<Coordenadas> rutaConEnvio)
        {
            return LongitudKm(rutaConEnvio) - LongitudKm(puntosViaje);
        }

        // Longitud en línea recta de un recorrido: suma de tramos consecutivos.
        public static double LongitudKm(List<Coordenadas> puntos)
        {
            double total = 0;

            for (int k = 0; k < puntos.Count - 1; k++)
                total += Geografia.DistanciaKm(puntos[k], puntos[k + 1]);

            return total;
        }

        // Arma la lista con A después del punto i y B después del punto j.
        private static List<Coordenadas> ArmarRuta(
            List<Coordenadas> puntos, int i, int j, Coordenadas a, Coordenadas b)
        {
            var ruta = new List<Coordenadas>();

            for (int k = 0; k < puntos.Count; k++)
            {
                ruta.Add(puntos[k]);
                if (k == i) ruta.Add(a);
                if (k == j) ruta.Add(b); // si i == j, queda A y luego B
            }

            return ruta;
        }
    }
}