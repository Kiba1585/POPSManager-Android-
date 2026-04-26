namespace POPSManager.Logic.Automation
{
    /// <summary>
    /// Comportamiento específico para cada acción automatizable.
    /// Este enum es usado por AutomationEngine para decidir
    /// si una acción se ejecuta automáticamente, se pregunta al usuario
    /// o se bloquea.
    /// </summary>
    public enum AutoBehavior
    {
        /// <summary>
        /// Ejecutar siempre sin preguntar.
        /// </summary>
        Automatico = 0,

        /// <summary>
        /// Preguntar al usuario antes de ejecutar.
        /// </summary>
        Preguntar = 1,

        /// <summary>
        /// No ejecutar nunca.
        /// </summary>
        Manual = 2
    }
}