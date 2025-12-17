using Nethereum.RPC.Eth.DTOs;
using Nethereum.Web3;

namespace Fap.Api.Interfaces
{
    /// <summary>
    /// Core blockchain service interface
    /// </summary>
    public interface IBlockchainService
    {
        // ============ Core ============

        Web3 GetWeb3();

        string GetAccountAddress();

        Task<string> SendTransactionAsync(
            string contractAddress,
            string abi,
            string functionName,
            params object[] parameters);

        Task<T> CallFunctionAsync<T>(
            string contractAddress,
            string abi,
            string functionName,
            params object[] parameters);

        Task<TransactionReceipt?> GetTransactionReceiptAsync(string txHash);

        Task<TransactionReceipt> WaitForTransactionReceiptAsync(
            string txHash,
            int timeoutSeconds = 60);

        Task<ulong> GetBlockNumberAsync();

        Task<bool> IsContractDeployedAsync(string contractAddress);

        /// <summary>
        /// Decode known contract events from a transaction receipt.
        /// Intended for audit logging (does not require the tx signer private key).
        /// </summary>
        Task<IReadOnlyList<(string EventName, string ContractAddress, string DetailJson)>> DecodeReceiptEventsAsync(string txHash);

        // ============ Credential Management (CredentialManagement.sol) ============

        /// <summary>
        /// Issue credential on CredentialManagement contract
        /// </summary>
        Task<(long BlockchainCredentialId, string TransactionHash)> IssueCredentialOnChainAsync(
            string studentWalletAddress,
            string credentialType,
            string credentialDataJson,
            string verificationHashBase64,
            ulong expiresAtUnixSeconds);

        /// <summary>
        /// Revoke credential on CredentialManagement contract
        /// </summary>
        Task<string> RevokeCredentialOnChainAsync(long blockchainCredentialId);

        /// <summary>
        /// Verify credential on-chain by its on-chain ID
        /// </summary>
        Task<bool> VerifyCredentialOnChainAsync(long blockchainCredentialId);

        /// <summary>
        /// Get credential data from chain
        /// </summary>
        Task<Services.BlockchainService.CredentialOnChainStructDto> GetCredentialFromChainAsync(long blockchainCredentialId);

        /// <summary>
        /// Debug helper to fetch raw ABI output for getCredential
        /// </summary>
        Task<string> DebugGetCredentialRawAsync(long blockchainCredentialId);

        /// <summary>
        /// Debug helper to decode credential output field-by-field without DTO mapping
        /// </summary>
        Task<object> DebugDecodeCredentialAsync(long blockchainCredentialId);

        /// <summary>
        /// Get total credential count from contract
        /// </summary>
        Task<long> GetCredentialCountAsync();

        // ============ Attendance Management (AttendanceManagement.sol) ============

        /// <summary>
        /// Mark attendance on AttendanceManagement contract
        /// </summary>
        Task<(long BlockchainRecordId, string TransactionHash)> MarkAttendanceOnChainAsync(
            ulong classId,
            string studentWalletAddress,
            ulong sessionDateUnixSeconds,
            byte status,
            string notes);

        /// <summary>
        /// Get attendance record data from chain
        /// </summary>
        Task<Services.BlockchainService.AttendanceOnChainStructDto> GetAttendanceFromChainAsync(long blockchainRecordId);
    }

    // DTO type is declared in BlockchainService to carry Nethereum attributes.
}
