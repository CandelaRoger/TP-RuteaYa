namespace RuteaYa.Logica.Rutas
{
    public static class Geografia
    {
        private const double RadioTierraKm = 6371.0;

        // Distancia en línea recta entre dos puntos (fórmula de Haversine)
        public static double DistanciaKm(Coordenadas a, Coordenadas b)
        {
            double dLat = ARadianes(b.Latitud - a.Latitud);
            double dLng = ARadianes(b.Longitud - a.Longitud);

            double h = Math.Pow(Math.Sin(dLat / 2), 2) +
                       Math.Cos(ARadianes(a.Latitud)) *
                       Math.Cos(ARadianes(b.Latitud)) *
                       Math.Pow(Math.Sin(dLng / 2), 2);

            return 2 * RadioTierraKm * Math.Asin(Math.Sqrt(h));
        }

        private static double ARadianes(double grados) => grados * Math.PI / 180.0;
    }
}