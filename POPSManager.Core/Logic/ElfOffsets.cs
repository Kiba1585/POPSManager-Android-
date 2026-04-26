namespace POPSManager.Logic
{
    public static class ElfOffsets
    {
        // ------------------------------------------------------------
        // GAME ID (SCES_XXXXX)
        // POPStarter lo almacena en offset 0x2C
        // Longitud EXACTA: 10 bytes
        // Formato: XXXX_YYYYY (sin terminador 0x00)
        // ------------------------------------------------------------
        public const int GameId = 0x2C;
        public const int GameIdMaxLength = 10;

        // ------------------------------------------------------------
        // VCD PATH (ruta interna del VCD)
        // Offset real: 0x100
        // Longitud EXACTA: 128 bytes
        // POPStarter lee el bloque completo, relleno con 0x00
        // ------------------------------------------------------------
        public const int VcdPath = 0x100;
        public const int VcdPathMaxLength = 128;

        // ------------------------------------------------------------
        // TITLE (lo que OPL muestra)
        // Offset real: 0x220
        // Longitud EXACTA: 48 bytes
        // 47 caracteres + 0x00 final obligatorio
        // ------------------------------------------------------------
        public const int Title = 0x220;
        public const int TitleMaxLength = 48;

        // ------------------------------------------------------------
        // Tamaño mínimo del ELF base para validar integridad
        // POPSTARTER.ELF real mide ~700 KB
        // 64 KB es un límite seguro para detectar archivos corruptos
        // ------------------------------------------------------------
        public const int MinimumElfSize = 0x10000; // 64 KB
    }
}
