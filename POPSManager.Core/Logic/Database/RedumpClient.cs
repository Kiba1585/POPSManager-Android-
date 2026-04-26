using POPSManager.Models;

namespace POPSManager.Logic.Database
{
    /// <summary>
    /// Cliente para obtener información de Redump.
    /// Actualmente es un placeholder para futura implementación.
    /// </summary>
    public static class RedumpClient
    {
        /// <summary>
        /// Busca información de un juego por su ID.
        /// </summary>
        /// <param name="gameId">ID del juego (ej: SLUS_12345).</param>
        /// <returns>Información del juego o null si no se encuentra.</returns>
        public static GameInfo? Lookup(string gameId)
        {
            // TODO: Implementar API o scraping de Redump
            return null;
        }
    }
}