namespace Fap.Domain.Enums
{
    public enum BlockchainCredentialStatus : byte
    {
        Pending = 0,
        Active = 1,
        Revoked = 2,
        Expired = 3
    }
}
