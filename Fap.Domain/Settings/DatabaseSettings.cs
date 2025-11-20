namespace Fap.Domain.Settings
{
    public class DatabaseSettings
    {
        /// <summary>
        /// Automatically reset database on application startup (Development only)
      /// </summary>
        public bool AutoResetOnStartup { get; set; } = false;

        /// <summary>
        /// Allow Database Admin API endpoints (for manual reset via API)
      /// </summary>
        public bool AllowDatabaseAdminApi { get; set; } = false;
    }
}
