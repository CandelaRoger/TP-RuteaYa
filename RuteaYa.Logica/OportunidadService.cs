using Microsoft.EntityFrameworkCore;
using RuteaYa.DTOs;
using RuteaYa.Entidades;
using RuteaYa.Logica.Rutas;

namespace RuteaYa.Logica
{
    public class OportunidadService : IOportunidadService
    {
        private const string EstadoDisponible = "Buscando conductor";
        private const string EstadoAsignado = "Conductor asignado";
        private const double FactorMargen = 2.0;      
        private const int MaxCandidatosAHere = 15;

        private const int MaxSeleccion = 20;   // tope de envíos por selección (limita llamadas al mapa)

        private readonly RuteaYaDbContext _context;
        private readonly IRutaService _rutaService;

        public OportunidadService(RuteaYaDbContext context, IRutaService rutaService)
        {
            _context = context;
            _rutaService = rutaService;
        }

        public async Task<List<OportunidadDto>> ObtenerCandidatosAsync(
            int viajeId,
            int usuarioId,
            string? orden = "ganancia")
        {
            var viaje = await _context.Viajes
                .AsNoTracking()
                .Include(v => v.DiasSemana)
                .Include(v => v.Paradas)
                .FirstOrDefaultAsync(v =>
                    v.Id == viajeId &&
                    v.UsuarioId == usuarioId &&
                    v.Activo);

            if (viaje == null)
            {
                throw new KeyNotFoundException(
                    "El viaje no existe, está inactivo o no te pertenece.");
            }

            var asignados = await _context.Envios
                .AsNoTracking()
                .Where(e => e.ViajeId == viajeId && e.Estado != "Cancelado")
                .Select(e => new { e.Peso, e.DesvioKm })
                .ToListAsync();

            int paquetesLibres = viaje.MaxPaquetes - asignados.Count;
            decimal pesoLibre = viaje.PesoMaxKg - asignados.Sum(a => a.Peso);
            double desvioRestante = (double)(viaje.DesvioMaxKm - asignados.Sum(a => a.DesvioKm));

            if (paquetesLibres <= 0 || pesoLibre <= 0 || desvioRestante <= 0)
                return new List<OportunidadDto>();

            var envios = await _context.Envios
                .AsNoTracking()
                .Where(e =>
                    e.Estado == EstadoDisponible &&
                    e.ViajeId == null &&
                    e.UsuarioId != usuarioId &&
                    e.Fecha >= DateTime.Today &&
                    e.Peso <= pesoLibre)
                .ToListAsync();

            var diasPermitidos = viaje.DiasSemana
                .Select(d => (int)d.DiaSemana)
                .ToHashSet();

            var candidatos = envios
                .Where(e => diasPermitidos.Contains(ADiaDelViaje(e.Fecha)))
                .ToList();

            var puntosHabituales = ArmarPuntos(viaje, soloHabituales: true);

            var preseleccionados = candidatos
                .Select(e =>
                {
                    var (retiro, entrega) = PuntosDelEnvio(e);
                    var ruta = PlanificadorRuta.MejorInsercion(puntosHabituales, retiro, entrega);
                    var costo = PlanificadorRuta.CostoExtraKm(puntosHabituales, ruta);
                    return new { Envio = e, Ruta = ruta, CostoEstimadoKm = costo };
                })
                .Where(x => x.CostoEstimadoKm <= desvioRestante * FactorMargen)
                .OrderBy(x => x.CostoEstimadoKm)
                .Take(MaxCandidatosAHere)
                .ToList();

            var oportunidades = new List<OportunidadDto>();
            int fallos = 0;

            foreach (var c in preseleccionados)
            {
                try
                {
                    var evaluacion = await EvaluarAsync(viaje, c.Envio, c.Ruta);

                    if (evaluacion != null && evaluacion.DesvioKm <= desvioRestante)
                    {
                        evaluacion.TiempoHabitualMin = viaje.DuracionMin;

                        // Capacidad que quedaría después de aceptar este envío
                        evaluacion.CapacidadRestanteKg =
                            Math.Max(0m, pesoLibre - c.Envio.Peso);

                        evaluacion.PaquetesRestantes =
                            Math.Max(0, paquetesLibres - 1);

                        oportunidades.Add(evaluacion);
                    }
                }
                catch (HttpRequestException)
                {
                    fallos++; 
                }
            }

            if (preseleccionados.Count > 0 && fallos == preseleccionados.Count)
            {
                throw new InvalidOperationException(
                    "No se pudo conectar con el servicio de mapas. Intentá nuevamente en unos minutos.");
            }

            return Ordenar(oportunidades, orden);
        }


        public async Task<List<OportunidadDto>> ObtenerCandidatosPorFechaAsync(
            int viajeId,
            int usuarioId,
            DateTime fecha,
            string? orden = "ganancia")
        {
            var fechaAnalizada = fecha.Date;

            var viaje = await _context.Viajes
                .AsNoTracking()
                .Include(v => v.DiasSemana)
                .Include(v => v.Paradas)
                .FirstOrDefaultAsync(v =>
                    v.Id == viajeId &&
                    v.UsuarioId == usuarioId &&
                    v.Activo);

            if (viaje == null)
            {
                throw new KeyNotFoundException(
                    "El viaje no existe, está inactivo o no te pertenece.");
            }

            int dia = ADiaDelViaje(fechaAnalizada);

            if (!viaje.DiasSemana.Any(d => (int)d.DiaSemana == dia))
                return new List<OportunidadDto>();

            if (fechaAnalizada < DateTime.Today)
                return new List<OportunidadDto>();

            var asignados = await _context.Envios
                .AsNoTracking()
                .Where(e =>
                    e.ViajeId == viajeId &&
                    e.Fecha.Date == fechaAnalizada &&
                    e.Estado != "Cancelado")
                .Select(e => new { e.Peso, e.DesvioKm })
                .ToListAsync();

            int paquetesLibres = viaje.MaxPaquetes - asignados.Count;
            decimal pesoLibre = viaje.PesoMaxKg - asignados.Sum(a => a.Peso);
            double desvioRestante =
                (double)(viaje.DesvioMaxKm - asignados.Sum(a => a.DesvioKm));

            if (paquetesLibres <= 0 || pesoLibre <= 0 || desvioRestante <= 0)
                return new List<OportunidadDto>();

            var candidatos = await _context.Envios
                .AsNoTracking()
                .Where(e =>
                    e.Estado == EstadoDisponible &&
                    e.ViajeId == null &&
                    e.UsuarioId != usuarioId &&
                    e.Fecha >= fechaAnalizada &&
                    e.Fecha < fechaAnalizada.AddDays(1) &&
                    e.Peso <= pesoLibre)
                .ToListAsync();

            var puntosHabituales = ArmarPuntos(viaje, soloHabituales: true);

            var preseleccionados = candidatos
                .Select(e =>
                {
                    var (retiro, entrega) = PuntosDelEnvio(e);
                    var ruta = PlanificadorRuta.MejorInsercion(
                        puntosHabituales, retiro, entrega);

                    var costo = PlanificadorRuta.CostoExtraKm(
                        puntosHabituales, ruta);

                    return new
                    {
                        Envio = e,
                        Ruta = ruta,
                        CostoEstimadoKm = costo
                    };
                })
                .Where(x => x.CostoEstimadoKm <= desvioRestante * FactorMargen)
                .OrderBy(x => x.CostoEstimadoKm)
                .Take(MaxCandidatosAHere)
                .ToList();

            var oportunidades = new List<OportunidadDto>();
            int fallos = 0;

            foreach (var candidato in preseleccionados)
            {
                try
                {
                    var evaluacion = await EvaluarAsync(
                        viaje, candidato.Envio, candidato.Ruta);

                    if (evaluacion != null &&
                        evaluacion.DesvioKm <= desvioRestante)
                    {
                        evaluacion.TiempoHabitualMin = viaje.DuracionMin;

                        evaluacion.CapacidadRestanteKg =
                            Math.Max(0m, pesoLibre - candidato.Envio.Peso);

                        evaluacion.PaquetesRestantes =
                            Math.Max(0, paquetesLibres - 1);

                        oportunidades.Add(evaluacion);
                    }
                }
                catch (HttpRequestException)
                {
                    fallos++;
                }
            }

            if (preseleccionados.Count > 0 &&
                fallos == preseleccionados.Count)
            {
                throw new InvalidOperationException(
                    "No se pudo conectar con el servicio de mapas. Intentá nuevamente.");
            }

            return Ordenar(oportunidades, orden);
        }

        public async Task<ResultadoOperacion<bool>> AceptarEnvioAsync(
            int viajeId,
            int envioId,
            int usuarioId)
        {
            await using var transaccion = await _context.Database
                .BeginTransactionAsync(System.Data.IsolationLevel.Serializable);

            var viaje = await _context.Viajes
                .Include(v => v.DiasSemana)
                .Include(v => v.Paradas)
                .FirstOrDefaultAsync(v =>
                    v.Id == viajeId &&
                    v.UsuarioId == usuarioId &&
                    v.Activo);

            if (viaje == null)
                return ResultadoOperacion<bool>.Falla(
                    "El viaje no existe, está inactivo o no te pertenece.");

            var envio = await _context.Envios
                .FirstOrDefaultAsync(e =>
                    e.Id == envioId &&
                    e.Estado == EstadoDisponible &&
                    e.ViajeId == null);

            if (envio == null)
                return ResultadoOperacion<bool>.Falla("El envío ya no está disponible.");

            if (envio.UsuarioId == usuarioId)
                return ResultadoOperacion<bool>.Falla(
                    "No podés aceptar un envío creado por vos mismo.");

            if (envio.Fecha.Date < DateTime.Today)
                return ResultadoOperacion<bool>.Falla(
                    "No se pueden aceptar envíos con fechas anteriores.");

            int dia = ADiaDelViaje(envio.Fecha);

            if (!viaje.DiasSemana.Any(d => (int)d.DiaSemana == dia))
                return ResultadoOperacion<bool>.Falla(
                    "El viaje no está disponible para ese día.");

            var enviosAsignados = await _context.Envios
                .Where(e => e.ViajeId == viajeId && e.Estado != "Cancelado")
                .ToListAsync();

            if (enviosAsignados.Count + 1 > viaje.MaxPaquetes)
                return ResultadoOperacion<bool>.Falla(
                    "El viaje alcanzó la cantidad máxima de paquetes.");

            if (enviosAsignados.Sum(e => e.Peso) + envio.Peso > viaje.PesoMaxKg)
                return ResultadoOperacion<bool>.Falla(
                    "El envío supera el peso disponible del viaje.");

            decimal desvioAsignado = enviosAsignados.Sum(e => e.DesvioKm);

            var puntosHabituales = ArmarPuntos(viaje, soloHabituales: true);
            var (retiro, entrega) = PuntosDelEnvio(envio);
            var rutaIndividual = PlanificadorRuta.MejorInsercion(puntosHabituales, retiro, entrega);

            OportunidadDto? evaluacion;
            try
            {
                evaluacion = await EvaluarAsync(viaje, envio, rutaIndividual);
            }
            catch (HttpRequestException)
            {
                return ResultadoOperacion<bool>.Falla(
                    "No se pudo conectar con el servicio de mapas.");
            }

            if (evaluacion == null)
                return ResultadoOperacion<bool>.Falla(
                    "No se pudo calcular el recorrido con este envío.");

            if ((decimal)evaluacion.DesvioKm + desvioAsignado > viaje.DesvioMaxKm)
                return ResultadoOperacion<bool>.Falla(
                    $"El envío supera el desvío máximo del viaje (ya se usan {desvioAsignado:0.0} de {viaje.DesvioMaxKm:0.0} km).");

            envio.ViajeId = viajeId;
            envio.Estado = EstadoAsignado;
            envio.DesvioKm = (decimal)evaluacion.DesvioKm;

            var paradaRetiro = new Parada
            {
                ViajeId = viajeId,
                EnvioId = envio.Id,
                Tipo = "Retiro",
                Direccion = envio.OrigenDir,
                Lat = envio.OrigenLat,
                Lng = envio.OrigenLng
            };

            var paradaEntrega = new Parada
            {
                ViajeId = viajeId,
                EnvioId = envio.Id,
                Tipo = "Entrega",
                Direccion = envio.DestinoDir,
                Lat = envio.DestinoLat,
                Lng = envio.DestinoLng
            };

            
            var (retiroTotal, entregaTotal) = PuntosDelEnvio(envio);
            var rutaCompleta = PlanificadorRuta.MejorInsercion(
                ArmarPuntos(viaje, soloHabituales: false), retiroTotal, entregaTotal);

            int posRetiro = rutaCompleta.IndexOf(retiroTotal);
            int posEntrega = rutaCompleta.IndexOf(entregaTotal);

            var ordenadas = viaje.Paradas.OrderBy(p => p.Orden).ToList();
            ordenadas.Insert(posRetiro - 1, paradaRetiro);
            ordenadas.Insert(posEntrega - 1, paradaEntrega);

            for (int i = 0; i < ordenadas.Count; i++)
                ordenadas[i].Orden = i + 1;

            _context.Paradas.AddRange(paradaRetiro, paradaEntrega);

            await _context.SaveChangesAsync();
            await transaccion.CommitAsync();

            return ResultadoOperacion<bool>.Ok(true);
        }

        public async Task<List<OportunidadDto>> ObtenerAsignadosAsync(
            int viajeId,
            int usuarioId)
        {
            var existeViaje = await _context.Viajes
                .AsNoTracking()
                .AnyAsync(v =>
                    v.Id == viajeId &&
                    v.UsuarioId == usuarioId);

            if (!existeViaje)
            {
                throw new KeyNotFoundException(
                    "El viaje no existe o no te pertenece.");
            }

            var envios = await _context.Envios
                .AsNoTracking()
                .Where(e =>
                    e.ViajeId == viajeId &&
                    e.Estado != "Cancelado")
                .OrderBy(e => e.Fecha)
                .Select(e => new OportunidadDto
                {
                    EnvioId = e.Id,
                    OrigenDir = e.OrigenDir,
                    DestinoDir = e.DestinoDir,
                    Fecha = e.Fecha,
                    Peso = e.Peso,
                    Tamanio = e.Tamanio,
                    Descripcion = e.Descripcion,
                    MontoOfrecido = e.MontoOfrecido,
                    DesvioKm = (double)e.DesvioKm
                })
                .ToListAsync();

            return envios;
        }

        public async Task<CapacidadViajeDto> ObtenerCapacidadAsync(
            int viajeId,
            int usuarioId)
        {
            var viaje = await _context.Viajes
                .AsNoTracking()
                .FirstOrDefaultAsync(v =>
                    v.Id == viajeId &&
                    v.UsuarioId == usuarioId);

            if (viaje == null)
            {
                throw new KeyNotFoundException(
                    "El viaje no existe o no te pertenece.");
            }

            var ocupacion = await _context.Envios
                .AsNoTracking()
                .Where(e =>
                    e.ViajeId == viajeId &&
                    e.Estado != "Cancelado")
                .GroupBy(e => e.ViajeId)
                .Select(g => new
                {
                    Cantidad = g.Count(),
                    PesoTotal = g.Sum(e => e.Peso)
                })
                .FirstOrDefaultAsync();

            int paquetesOcupados = ocupacion?.Cantidad ?? 0;
            decimal pesoOcupado = ocupacion?.PesoTotal ?? 0m;

            return new CapacidadViajeDto
            {
                ViajeId = viaje.Id,

                MaxPaquetes = viaje.MaxPaquetes,
                PaquetesOcupados = paquetesOcupados,
                PaquetesDisponibles = Math.Max(
                    0, viaje.MaxPaquetes - paquetesOcupados),

                PesoMaxKg = viaje.PesoMaxKg,
                PesoOcupadoKg = pesoOcupado,
                PesoDisponibleKg = Math.Max(
                    0m, viaje.PesoMaxKg - pesoOcupado)
            };
        }

        public async Task<ResultadoOperacion<bool>> QuitarEnvioAsync(
            int viajeId,
            int envioId,
            int usuarioId)
        {
            await using var transaccion = await _context.Database
                .BeginTransactionAsync(
                    System.Data.IsolationLevel.Serializable);

            var viaje = await _context.Viajes
                .FirstOrDefaultAsync(v =>
                    v.Id == viajeId &&
                    v.UsuarioId == usuarioId);

            if (viaje == null)
                return ResultadoOperacion<bool>.Falla(
                    "El viaje no existe o no te pertenece.");

            var envio = await _context.Envios
                .FirstOrDefaultAsync(e =>
                    e.Id == envioId &&
                    e.ViajeId == viajeId &&
                    e.Estado == EstadoAsignado);

            if (envio == null)
                return ResultadoOperacion<bool>.Falla(
                    "El envío no está asignado a este viaje.");

            var paradas = await _context.Paradas
                .Where(p =>
                    p.ViajeId == viajeId &&
                    p.EnvioId == envioId)
                .ToListAsync();

            _context.Paradas.RemoveRange(paradas);

            envio.ViajeId = null;
            envio.DesvioKm = 0;
            envio.Estado = EstadoDisponible;

            await _context.SaveChangesAsync();
            await transaccion.CommitAsync();

            return ResultadoOperacion<bool>.Ok(true);
        }

        // ================= PANTALLA 11: CONFIRMACIÓN DE SELECCIÓN =================

        public async Task<ResultadoOperacion<ResumenSeleccionDto>> PrevisualizarSeleccionAsync(
            int viajeId, List<int> envioIds, int usuarioId)
        {
            var resultado = await EvaluarSeleccionAsync(viajeId, envioIds, usuarioId);

            if (!resultado.Exitoso)
                return Propagar<SeleccionEvaluada, ResumenSeleccionDto>(resultado);

            var s = resultado.Datos!;

            return ResultadoOperacion<ResumenSeleccionDto>.Ok(new ResumenSeleccionDto
            {
                ViajeId = s.Viaje.Id,
                ViajeOrigenDir = s.Viaje.OrigenDir,
                ViajeDestinoDir = s.Viaje.DestinoDir,
                Envios = s.Items.Select(i => i.Evaluacion).ToList(),
                GananciaTotal = s.Items.Sum(i => i.Envio.MontoOfrecido),
                PesoTotal = s.Items.Sum(i => i.Envio.Peso),
                DesvioTotalKm = Math.Round(s.Items.Sum(i => i.Evaluacion.DesvioKm), 2),
                CantidadPaquetes = s.Items.Count,
                Conflictos = s.Conflictos,
                Errores = s.Errores
            });
        }

        public async Task<ResultadoOperacion<bool>> ConfirmarSeleccionAsync(
            int viajeId, List<int> envioIds, int usuarioId)
        {
            await using var transaccion = await _context.Database
                .BeginTransactionAsync(System.Data.IsolationLevel.Serializable);

            // 1) Se valida TODO antes de modificar nada (así el "todo o nada" es seguro)
            var resultado = await EvaluarSeleccionAsync(viajeId, envioIds, usuarioId);

            if (!resultado.Exitoso)
                return Propagar<SeleccionEvaluada, bool>(resultado);

            var seleccion = resultado.Datos!;
            var viaje = seleccion.Viaje;

            // Si otro usuario tomó alguno, se informa el conflicto y no se reserva ninguno
            if (seleccion.Conflictos.Count > 0)
                return ResultadoOperacion<bool>.Conflicto(
                    string.Join(" ", seleccion.Conflictos.Select(c => c.Mensaje)));

            if (seleccion.Errores.Count > 0)
                return ResultadoOperacion<bool>.Falla(string.Join(" ", seleccion.Errores));

            // 2) Recién ahora se asigna, uno por uno
            foreach (var (envio, evaluacion) in seleccion.Items)
            {
                envio.ViajeId = viajeId;
                envio.Estado = EstadoAsignado;
                envio.DesvioKm = (decimal)evaluacion.DesvioKm;

                var paradaRetiro = new Parada
                {
                    ViajeId = viajeId,
                    EnvioId = envio.Id,
                    Tipo = "Retiro",
                    Direccion = envio.OrigenDir,
                    Lat = envio.OrigenLat,
                    Lng = envio.OrigenLng
                };

                var paradaEntrega = new Parada
                {
                    ViajeId = viajeId,
                    EnvioId = envio.Id,
                    Tipo = "Entrega",
                    Direccion = envio.DestinoDir,
                    Lat = envio.DestinoLat,
                    Lng = envio.DestinoLng
                };

                // Retiro y entrega van en la mejor posición sobre TODAS las paradas actuales
                var (retiro, entrega) = PuntosDelEnvio(envio);
                var rutaCompleta = PlanificadorRuta.MejorInsercion(
                    ArmarPuntos(viaje, soloHabituales: false), retiro, entrega);

                int posRetiro = rutaCompleta.IndexOf(retiro);
                int posEntrega = rutaCompleta.IndexOf(entrega);

                var ordenadas = viaje.Paradas.OrderBy(p => p.Orden).ToList();
                ordenadas.Insert(posRetiro - 1, paradaRetiro);
                ordenadas.Insert(posEntrega - 1, paradaEntrega);

                for (int i = 0; i < ordenadas.Count; i++)
                    ordenadas[i].Orden = i + 1;

                // Se agregan a la colección para que el siguiente envío de la selección las vea
                viaje.Paradas.Add(paradaRetiro);
                viaje.Paradas.Add(paradaEntrega);
            }

            try
            {
                await _context.SaveChangesAsync();
                await transaccion.CommitAsync();
            }
            catch (DbUpdateException)
            {
                return ResultadoOperacion<bool>.Conflicto(
                    "Otro usuario modificó alguno de los envíos mientras confirmabas. Revisá la selección e intentá de nuevo.");
            }

            return ResultadoOperacion<bool>.Ok(true);
        }

        // ---------- Núcleo compartido por previsualizar y confirmar ----------

        private sealed class SeleccionEvaluada
        {
            public Viaje Viaje { get; set; } = null!;
            public List<(Envio Envio, OportunidadDto Evaluacion)> Items { get; } = new();
            public List<ProblemaSeleccionDto> Conflictos { get; } = new();
            public List<string> Errores { get; } = new();
        }

        private async Task<ResultadoOperacion<SeleccionEvaluada>> EvaluarSeleccionAsync(
            int viajeId, List<int>? envioIds, int usuarioId)
        {
            var ids = (envioIds ?? new List<int>()).Distinct().ToList();

            if (ids.Count == 0)
                return ResultadoOperacion<SeleccionEvaluada>.Falla("Seleccioná al menos un envío.");

            if (ids.Count > MaxSeleccion)
                return ResultadoOperacion<SeleccionEvaluada>.Falla(
                    $"Podés seleccionar hasta {MaxSeleccion} envíos a la vez.");

            var viaje = await _context.Viajes
                .Include(v => v.DiasSemana)
                .Include(v => v.Paradas)
                .FirstOrDefaultAsync(v =>
                    v.Id == viajeId &&
                    v.UsuarioId == usuarioId &&
                    v.Activo);

            if (viaje == null)
                return ResultadoOperacion<SeleccionEvaluada>.NoEncontrado(
                    "El viaje no existe, está inactivo o no te pertenece.");

            var seleccion = new SeleccionEvaluada { Viaje = viaje };

            var envios = await _context.Envios
                .Where(x => ids.Contains(x.Id))
                .ToListAsync();

            var diasPermitidos = viaje.DiasSemana.Select(d => (int)d.DiaSemana).ToHashSet();
            var aEvaluar = new List<Envio>();

            // Reglas individuales: un envío que falla se descarta, y se sigue con el resto
            foreach (var id in ids)
            {
                var envio = envios.FirstOrDefault(x => x.Id == id);

                // Conflicto: otro conductor lo tomó, se canceló o ya no existe
                if (envio == null || envio.Estado != EstadoDisponible || envio.ViajeId != null)
                {
                    seleccion.Conflictos.Add(new ProblemaSeleccionDto
                    {
                        EnvioId = id,
                        Mensaje = $"El envío {Describir(envio, id)} ya no está disponible."
                    });
                    continue;
                }

                if (envio.UsuarioId == usuarioId)
                {
                    seleccion.Errores.Add(
                        $"El envío {Describir(envio, id)} lo creaste vos: no podés transportarlo.");
                    continue;
                }

                if (envio.Fecha.Date < DateTime.Today)
                {
                    seleccion.Errores.Add(
                        $"El envío {Describir(envio, id)} tiene una fecha anterior a hoy.");
                    continue;
                }

                if (!diasPermitidos.Contains(ADiaDelViaje(envio.Fecha)))
                {
                    seleccion.Errores.Add(
                        $"El viaje no sale el día del envío {Describir(envio, id)}.");
                    continue;
                }

                aEvaluar.Add(envio);
            }

            // Desvío individual de cada envío, sobre el recorrido habitual (una llamada al mapa por envío)
            var puntosHabituales = ArmarPuntos(viaje, soloHabituales: true);

            foreach (var envio in aEvaluar)
            {
                var (retiro, entrega) = PuntosDelEnvio(envio);
                var ruta = PlanificadorRuta.MejorInsercion(puntosHabituales, retiro, entrega);

                OportunidadDto? evaluacion;
                try
                {
                    evaluacion = await EvaluarAsync(viaje, envio, ruta);
                }
                catch (HttpRequestException)
                {
                    throw new InvalidOperationException(
                        "No se pudo conectar con el servicio de mapas. Intentá nuevamente en unos minutos.");
                }

                if (evaluacion == null)
                {
                    seleccion.Errores.Add(
                        $"No se pudo calcular el recorrido para {Describir(envio, envio.Id)}.");
                    continue;
                }

                seleccion.Items.Add((envio, evaluacion));
            }

            // Reglas del conjunto, contando lo que ya tiene asignado el viaje
            if (seleccion.Items.Count > 0)
            {
                var asignados = await _context.Envios
                    .Where(x => x.ViajeId == viajeId && x.Estado != "Cancelado")
                    .Select(x => new { x.Peso, x.DesvioKm })
                    .ToListAsync();

                int paquetes = asignados.Count + seleccion.Items.Count;
                if (paquetes > viaje.MaxPaquetes)
                    seleccion.Errores.Add(
                        $"Superás la cantidad máxima de paquetes del viaje ({paquetes} de {viaje.MaxPaquetes}).");

                decimal peso = asignados.Sum(a => a.Peso) + seleccion.Items.Sum(i => i.Envio.Peso);
                if (peso > viaje.PesoMaxKg)
                    seleccion.Errores.Add(
                        $"Superás el peso máximo del viaje ({peso:0.##} de {viaje.PesoMaxKg:0.##} kg).");

                decimal desvio = asignados.Sum(a => a.DesvioKm)
                               + (decimal)seleccion.Items.Sum(i => i.Evaluacion.DesvioKm);
                if (desvio > viaje.DesvioMaxKm)
                    seleccion.Errores.Add(
                        $"Superás el desvío máximo del viaje ({desvio:0.##} de {viaje.DesvioMaxKm:0.##} km).");
            }

            return ResultadoOperacion<SeleccionEvaluada>.Ok(seleccion);
        }

        private static string Describir(Envio? envio, int id) =>
            envio != null ? $"{envio.OrigenDir} → {envio.DestinoDir}" : $"#{id}";

        // Copia el error (y sus indicadores) a un resultado de otro tipo
        private static ResultadoOperacion<TDestino> Propagar<TOrigen, TDestino>(
            ResultadoOperacion<TOrigen> r) =>
            new()
            {
                Exitoso = false,
                EsNoEncontrado = r.EsNoEncontrado,
                EsConflicto = r.EsConflicto,
                MensajeError = r.MensajeError
            };

        private async Task<OportunidadDto?> EvaluarAsync(
            Viaje viaje, Envio envio, List<Coordenadas> ruta)
        {
            var resultado = await _rutaService.CalcularRutaAsync(ruta);

            if (resultado == null)
                return null;

            double desvio = Math.Max(0, resultado.DistanciaKm - (double)viaje.DistanciaKm);
            double minutosExtra = Math.Max(0, resultado.DuracionMinutos - viaje.DuracionMin);

            return new OportunidadDto
            {
                EnvioId = envio.Id,
                OrigenDir = envio.OrigenDir,
                DestinoDir = envio.DestinoDir,
                Fecha = envio.Fecha,
                Peso = envio.Peso,
                Tamanio = envio.Tamanio,
                Descripcion = envio.Descripcion,
                MontoOfrecido = envio.MontoOfrecido,
                DistanciaHabitualKm = (double)viaje.DistanciaKm,
                DistanciaConEnvioKm = Math.Round(resultado.DistanciaKm, 2),
                DesvioKm = Math.Round(desvio, 2),
                TiempoAdicionalMin = Math.Round(minutosExtra, 0),
                Compatibilidad = Clasificar(desvio, (double)viaje.DesvioMaxKm)
            };
        }

        
        private static List<Coordenadas> ArmarPuntos(Viaje viaje, bool soloHabituales)
        {
            var puntos = new List<Coordenadas>
            {
                new() { Latitud = (double)viaje.OrigenLat, Longitud = (double)viaje.OrigenLng }
            };

            var paradas = viaje.Paradas.AsEnumerable();
            if (soloHabituales)
                paradas = paradas.Where(EsHabitual);

            puntos.AddRange(paradas
                .OrderBy(p => p.Orden)
                .Select(p => new Coordenadas { Latitud = (double)p.Lat, Longitud = (double)p.Lng }));

            puntos.Add(new Coordenadas
            {
                Latitud = (double)viaje.DestinoLat,
                Longitud = (double)viaje.DestinoLng
            });

            return puntos;
        }

        //private static bool EsHabitual(Parada p) => p.Tipo != "Retiro" && p.Tipo != "Entrega";

        private static bool EsHabitual(Parada p) => p.EnvioId == null;

        private static (Coordenadas Retiro, Coordenadas Entrega) PuntosDelEnvio(Envio e) => (
            new Coordenadas { Latitud = (double)e.OrigenLat, Longitud = (double)e.OrigenLng },
            new Coordenadas { Latitud = (double)e.DestinoLat, Longitud = (double)e.DestinoLng });

        private static int ADiaDelViaje(DateTime fecha)
        {
            int d = (int)fecha.DayOfWeek;
            return d == 0 ? 7 : d;
        }

        private static string Clasificar(double desvio, double desvioMax)
        {
            if (desvioMax <= 0) return "Mucho desvío";
            double ratio = desvio / desvioMax;

            if (ratio <= 0.25) return "Muy compatible";
            if (ratio <= 0.50) return "Compatible";
            return "Mucho desvío";
        }

        private static int Rango(string compatibilidad) => compatibilidad switch
        {
            "Muy compatible" => 0,
            "Compatible" => 1,
            _ => 2
        };

        private static List<OportunidadDto> Ordenar(List<OportunidadDto> lista, string? orden)
        {
            return (orden ?? "ganancia").ToLowerInvariant() switch
            {
                "desvio" => lista.OrderBy(o => o.DesvioKm)
                                 .ThenByDescending(o => o.MontoOfrecido).ToList(),

                "compatibilidad" => lista.OrderBy(o => Rango(o.Compatibilidad))
                                         .ThenByDescending(o => o.MontoOfrecido).ToList(),

                _ => lista.OrderByDescending(o => o.MontoOfrecido)
                          .ThenBy(o => o.DesvioKm).ToList()
            };
        }
    }
}