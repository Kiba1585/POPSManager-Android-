namespace POPSManager.Logic.Automation
{
    /// <summary>
    /// Modo global de automatización.
    /// </summary>
    public enum AutomationMode
    {
        /// <summary>El usuario decide cada acción manualmente.</summary>
        Manual = 0,

        /// <summary>El sistema pregunta antes de ejecutar acciones importantes.</summary>
        Asistido = 1,

        /// <summary>El sistema ejecuta todo automáticamente.</summary>
        Automatico = 2
    }
}