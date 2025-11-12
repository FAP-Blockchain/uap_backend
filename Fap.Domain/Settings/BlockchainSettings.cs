namespace Fap.Domain.Settings
{
    /// <summary>
    /// Blockchain configuration settings
    /// </summary>
    public class BlockchainSettings
    {
        /// <summary>
        /// Hardhat node URL (e.g., http://127.0.0.1:8545)
        /// </summary>
        public string NetworkUrl { get; set; } = string.Empty;

        /// <summary>
        /// Chain ID (Hardhat local: 31337, Sepolia: 11155111)
        /// </summary>
        public int ChainId { get; set; } = 31337;

        /// <summary>
        /// Private key c?a account dùng ?? sign transactions
        /// ?? KHÔNG commit vào Git trong production!
        /// </summary>
        public string PrivateKey { get; set; } = string.Empty;

        /// <summary>
        /// Smart contract addresses
        /// </summary>
        public ContractAddresses Contracts { get; set; } = new();

        /// <summary>
        /// Gas limit cho transactions (default: 3,000,000)
        /// </summary>
        public long GasLimit { get; set; } = 3000000;

        /// <summary>
        /// Gas price in Wei (default: 20 Gwei = 20,000,000,000 Wei)
        /// </summary>
        public long GasPrice { get; set; } = 20000000000;

        /// <summary>
        /// Timeout cho transaction confirmation (seconds)
        /// </summary>
        public int TransactionTimeout { get; set; } = 60;
    }

    /// <summary>
    /// Smart contract addresses for different modules
    /// </summary>
    public class ContractAddresses
    {
        public string UniversityManagement { get; set; } = string.Empty;
        public string CredentialManagement { get; set; } = string.Empty;
        public string AttendanceManagement { get; set; } = string.Empty;
        public string GradeManagement { get; set; } = string.Empty;
        public string ClassManagement { get; set; } = string.Empty;
    }
}
