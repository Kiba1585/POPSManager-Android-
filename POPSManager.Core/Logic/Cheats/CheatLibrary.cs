using System.Collections.Generic;

namespace POPSManager.Logic.Cheats
{
    /// <summary>
    /// Biblioteca de cheats oficiales de POPStarter.
    /// </summary>
    public static class CheatLibrary
    {
        /// <summary>
        /// Obtiene la lista de cheats oficiales.
        /// </summary>
        public static IReadOnlyList<CheatDefinition> GetOfficialCheats()
        {
            return new List<CheatDefinition>
            {
                new()
                {
                    Code = "VMODE=NTSC",
                    Name = "Forzar NTSC",
                    Category = "Video",
                    Description = "Fuerza salida NTSC para algunos juegos PAL.",
                    IsOfficial = true
                },
                new()
                {
                    Code = "VMODE=PAL",
                    Name = "Forzar PAL",
                    Category = "Video",
                    Description = "Fuerza salida PAL.",
                    IsOfficial = true
                },
                new()
                {
                    Code = "BORDER=OFF",
                    Name = "Quitar bordes",
                    Category = "Video",
                    Description = "Desactiva los bordes negros.",
                    IsOfficial = true
                },
                new()
                {
                    Code = "CENTER=ON",
                    Name = "Centrar imagen",
                    Category = "Video",
                    Description = "Centra la imagen en pantallas modernas.",
                    IsOfficial = true
                },
                new()
                {
                    Code = "SKIPVIDEOS=ON",
                    Name = "Saltar FMVs",
                    Category = "Compatibilidad",
                    Description = "Evita cuelgues en cinemáticas dañadas.",
                    IsOfficial = true
                },
                new()
                {
                    Code = "FORCEVIDEO=1",
                    Name = "PAL60 automático",
                    Category = "Video",
                    Description = "Fuerza modo PAL60 para mejorar compatibilidad.",
                    IsOfficial = true
                },
                new()
                {
                    Code = "WIDESCREEN=ON",
                    Name = "Forzar 16:9",
                    Category = "Video",
                    Description = "Fuerza modo panorámico (no todos los juegos lo soportan).",
                    IsOfficial = true
                },
                new()
                {
                    Code = "ANALOG=ON",
                    Name = "Modo analógico",
                    Category = "Entrada",
                    Description = "Activa modo analógico en juegos compatibles.",
                    IsOfficial = true
                },
                new()
                {
                    Code = "DEBUG=ON",
                    Name = "Modo debug",
                    Category = "Debug",
                    Description = "Activa salida de depuración.",
                    IsOfficial = true
                },
                new()
                {
                    Code = "LOG=ON",
                    Name = "Log de POPStarter",
                    Category = "Debug",
                    Description = "Genera logs internos de POPStarter.",
                    IsOfficial = true
                }
            };
        }
    }
}