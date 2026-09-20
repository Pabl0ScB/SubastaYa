// URL base de la API. Si cambia el puerto en que corre la API, se cambia aca.
const API_BASE = 'http://localhost:5093/api/v1';

// El hub no cuelga de /api/v1: SignalR tiene su propia ruta.
const HUB_SUBASTAS = 'http://localhost:5093/hubs/subastas';
