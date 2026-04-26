namespace POPSManager.Logic.Cheats
{
    /// <summary>
    /// Representa un cheat individual, ya sea oficial o definido por el usuario.
    /// </summary>
    public class CheatDefinition
    {
        /// <summary>Código del cheat (ej: "VMODE=NTSC").</summary>
        public string Code { get; set; } = "";

        /// <summary>Nombre legible del cheat.</summary>
        public string Name { get; set; } = "";

        /// <summary>Descripción detallada.</summary>
        public string Description { get; set; } = "";

        /// <summary>Categoría (Video, Compatibilidad, etc.).</summary>
        public string Category { get; set; } = "";

        /// <summary>Indica si es un cheat oficial de POPStarter.</summary>
        public bool IsOfficial { get; set; }

        /// <summary>Indica si fue creado por el usuario.</summary>
        public bool IsUserDefined { get; set; }
    }
}