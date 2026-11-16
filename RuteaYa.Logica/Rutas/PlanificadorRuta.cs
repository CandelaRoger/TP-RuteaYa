namespace RuteaYa.Logica.Rutas
{
    public static class PlanificadorRuta
    {
        
        public static List<Coordenadas> MejorInsercion(
            List<Coordenadas> puntosViaje, Coordenadas a, Coordenadas b)
        {
            List<Coordenadas>? mejorRuta = null;
            double mejorLongitud = double.MaxValue;

            for (int i = 0; i < puntosViaje.Count - 1; i++)
            {
                for (int j = i; j < puntosViaje.Count - 1; j++) 
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

        public static double CostoExtraKm(
            List<Coordenadas> puntosViaje, List<Coordenadas> rutaConEnvio)
        {
            return LongitudKm(rutaConEnvio) - LongitudKm(puntosViaje);
        }

        public static double LongitudKm(List<Coordenadas> puntos)
        {
            double total = 0;

            for (int k = 0; k < puntos.Count - 1; k++)
                total += Geografia.DistanciaKm(puntos[k], puntos[k + 1]);

            return total;
        }

        private static List<Coordenadas> ArmarRuta(
            List<Coordenadas> puntos, int i, int j, Coordenadas a, Coordenadas b)
        {
            var ruta = new List<Coordenadas>();

            for (int k = 0; k < puntos.Count; k++)
            {
                ruta.Add(puntos[k]);
                if (k == i) ruta.Add(a);
                if (k == j) ruta.Add(b); 
            }

            return ruta;
        }
    }
}