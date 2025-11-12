using Nethereum.Web3;
using Nethereum.RPC.Eth.DTOs;

namespace Fap.Api.Interfaces
{
    /// <summary>
    /// Core blockchain service interface
    /// </summary>
    public interface IBlockchainService
    {
        /// <summary>
        /// Get Web3 instance for direct blockchain interaction
        /// </summary>
        Web3 GetWeb3();

        /// <summary>
        /// Get current account address
        /// </summary>
        string GetAccountAddress();

        /// <summary>
        /// Send transaction to smart contract (WRITE operation)
        /// </summary>
        Task<string> SendTransactionAsync(
            string contractAddress,
            string abi,
            string functionName,
            params object[] parameters);

        /// <summary>
        /// Call smart contract function (READ operation - no gas)
        /// </summary>
        Task<T> CallFunctionAsync<T>(
            string contractAddress,
            string abi,
            string functionName,
            params object[] parameters);

        /// <summary>
        /// Get transaction receipt
        /// </summary>
        Task<TransactionReceipt?> GetTransactionReceiptAsync(string txHash);

        /// <summary>
        /// Wait for transaction to be mined
        /// </summary>
        Task<TransactionReceipt> WaitForTransactionReceiptAsync(
            string txHash,
            int timeoutSeconds = 60);

        /// <summary>
        /// Get current block number
        /// </summary>
        Task<ulong> GetBlockNumberAsync();

        /// <summary>
        /// Check if contract exists at address
        /// </summary>
        Task<bool> IsContractDeployedAsync(string contractAddress);
    }

    /// <summary>
    /// Credential data retrieved from blockchain
    /// </summary>
    public class CredentialBlockchainData
    {
        public Guid CredentialId { get; set; }
        public string StudentCode { get; set; } = string.Empty;
        public string CertificateHash { get; set; } = string.Empty;
        public DateTime IssuedAt { get; set; }
        public string IssuerAddress { get; set; } = string.Empty;
        public bool IsRevoked { get; set; }
    }
}
