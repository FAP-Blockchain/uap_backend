namespace Fap.Domain.Constants
{
    public static class ActionLogActions
    {
        // Credential
        public const string IssueCredential = "ISSUE_CREDENTIAL";
        public const string VerifyCredential = "VERIFY_CREDENTIAL";
        public const string RevokeCredential = "REVOKE_CREDENTIAL";
        public const string BlockchainStore = "BLOCKCHAIN_STORE";
        public const string CredentialOnChainSync = "CREDENTIAL_ONCHAIN_SYNC";

        // Grades
        public const string SubmitGrade = "SUBMIT_GRADE";
        public const string UpdateGrade = "UPDATE_GRADE";
        public const string DeleteGrade = "DELETE_GRADE";
        public const string GradeOnChainSync = "GRADE_ONCHAIN_SYNC";

        // Users
        public const string UserLogin = "USER_LOGIN";
        public const string UserLogout = "USER_LOGOUT";
        public const string PasswordReset = "PASSWORD_RESET";
        public const string UserCreated = "USER_CREATED";
        public const string UserOnChainSync = "USER_ONCHAIN_SYNC";
        public const string UserWalletOnChainUpdate = "USER_WALLET_ONCHAIN_UPDATE";

        // Attendance
        public const string AttendanceOnChainSync = "ATTENDANCE_ONCHAIN_SYNC";

        // Generic blockchain audit
        public const string ChainTx = "CHAIN_TX";
        public const string ChainEvent = "CHAIN_EVENT";

        // Classes / Schedule
        public const string CreateClass = "CREATE_CLASS";
        public const string UpdateSchedule = "UPDATE_SCHEDULE";
        public const string CancelSlot = "CANCEL_SLOT";
    }
}
