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

        // ============ Credential Management (CredentialManagement.sol) ============

        /// <summary>
        /// Issue credential on CredentialManagement contract
        /// </summary>
        Task<(long BlockchainCredentialId, string TransactionHash)> IssueCredentialOnChainAsync(
            string studentWalletAddress,
            string credentialType,
            string credentialDataJson,
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
        Task<BlockchainCredentialOnChain> GetCredentialFromChainAsync(long blockchainCredentialId);

        /// <summary>
        /// Get total credential count from contract
        /// </summary>
        Task<long> GetCredentialCountAsync();
    }

    /// <summary>
    /// DTO for credential data retrieved from CredentialManagement contract
    /// </summary>
    public class BlockchainCredentialOnChain
    {
        public System.Numerics.BigInteger CredentialId { get; set; }
        public string StudentAddress { get; set; } = string.Empty;
        public string CredentialType { get; set; } = string.Empty;
        public string CredentialData { get; set; } = string.Empty;
        public int Status { get; set; } // 0 = ACTIVE, 1 = REVOKED
        public string IssuedBy { get; set; } = string.Empty;
        public System.Numerics.BigInteger IssuedAt { get; set; }
        public System.Numerics.BigInteger ExpiresAt { get; set; }
    }
}
