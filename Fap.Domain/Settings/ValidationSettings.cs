namespace Fap.Domain.Settings
{
    public class ValidationSettings
    {
        /// <summary>
        /// Determines whether teachers must take attendance on the exact slot date.
        /// Defaults to true to protect business logic, but can be toggled at runtime.
        /// </summary>
        public bool EnforceAttendanceDateValidation { get; set; } = true;
    }
}
