using System.Numerics;
using System.Linq;
using Fap.Api.Interfaces;
using Fap.Domain.Settings;
using Microsoft.Extensions.Options;
using Nethereum.ABI.FunctionEncoding.Attributes;
using Nethereum.Contracts;
using Nethereum.Hex.HexTypes;
using Nethereum.RPC.Eth.DTOs;
using Nethereum.Web3;
using Nethereum.Web3.Accounts;

namespace Fap.Api.Services
{
    public class BlockchainService : IBlockchainService
    {
        private readonly Web3 _web3;
        private readonly Account _account;
        private readonly BlockchainSettings _settings;
        private readonly ILogger<BlockchainService> _logger;

        public BlockchainService(IOptions<BlockchainSettings> settings, ILogger<BlockchainService> logger)
        {
            _settings = settings.Value;
            _logger = logger;
            _account = new Account(_settings.PrivateKey, _settings.ChainId);
            _web3 = new Web3(_account, _settings.NetworkUrl);
            _logger.LogInformation("Blockchain initialized: {Network}, ChainId: {ChainId}, Account: {Account}", _settings.NetworkUrl, _settings.ChainId, _account.Address);
        }

        public Web3 GetWeb3() => _web3;

        public string GetAccountAddress() => _account.Address;

        public async Task<string> SendTransactionAsync(string contractAddress, string abi, string functionName, params object[] parameters)
        {
            try
            {
                _logger.LogInformation("?? Sending transaction: {Function} to {Contract}", functionName, contractAddress);
                
                var contract = _web3.Eth.GetContract(abi, contractAddress);
                var function = contract.GetFunction(functionName);

                // 1?? Estimate Gas
                var estimatedGas = await function.EstimateGasAsync(_account.Address, null, null, parameters);
                _logger.LogDebug("? Estimated gas: {Gas}", estimatedGas.Value);

                // 2?? Use configured gas limit or estimated gas with buffer
                var gasLimit = _settings.GasLimit > 0 
                    ? new HexBigInteger(_settings.GasLimit) 
                    : new HexBigInteger(estimatedGas.Value * 120 / 100); // 20% buffer

                _logger.LogDebug("? Gas limit: {GasLimit}", gasLimit.Value);

                // 3?? Determine transaction type (Legacy vs EIP-1559)
                string txHash;
                bool useEIP1559 = _settings.MaxFeePerGas > 0 || _settings.MaxPriorityFeePerGas > 0;

                if (useEIP1559)
                {
                    // EIP-1559 Transaction (Type 2)
                    _logger.LogDebug("?? Using EIP-1559 transaction type");
                    
                    var maxFeePerGas = _settings.MaxFeePerGas > 0 
                        ? new HexBigInteger(_settings.MaxFeePerGas)
                        : await EstimateMaxFeePerGasAsync();

                    var maxPriorityFeePerGas = _settings.MaxPriorityFeePerGas > 0
                        ? new HexBigInteger(_settings.MaxPriorityFeePerGas)
                        : await EstimateMaxPriorityFeePerGasAsync();

                    _logger.LogDebug("? MaxFeePerGas: {MaxFeePerGas}, MaxPriorityFeePerGas: {MaxPriorityFeePerGas}", 
                        maxFeePerGas.Value, maxPriorityFeePerGas.Value);

                    // Create EIP-1559 transaction input
                    var transactionInput = function.CreateTransactionInput(
                        _account.Address,
                        gasLimit,
                        new HexBigInteger(0), // value
                        parameters
                    );

                    transactionInput.MaxFeePerGas = maxFeePerGas;
                    transactionInput.MaxPriorityFeePerGas = maxPriorityFeePerGas;
                    transactionInput.Type = new HexBigInteger(2); // EIP-1559 type

                    txHash = await _web3.Eth.TransactionManager.SendTransactionAsync(transactionInput);
                }
                else
                {
                    // Legacy Transaction (Type 0)
                    _logger.LogDebug("?? Using Legacy transaction type");
                    
                    var gasPrice = _settings.GasPrice > 0 
                        ? new HexBigInteger(_settings.GasPrice)
                        : await _web3.Eth.GasPrice.SendRequestAsync();

                    _logger.LogDebug("? Gas price: {GasPrice}", gasPrice.Value);

                    txHash = await function.SendTransactionAsync(
                        _account.Address, 
                        gasLimit, 
                        gasPrice, 
                        new HexBigInteger(0), // value
                        parameters
                    );
                }

                _logger.LogInformation("? Transaction sent: {TxHash}", txHash);

                // 4?? Wait for confirmation
                var receipt = await WaitForTransactionReceiptAsync(txHash, _settings.TransactionTimeout);
                
                if (receipt.Status?.Value != 1)
                {
                    _logger.LogError("? Transaction failed: {TxHash}, Status: {Status}", txHash, receipt.Status?.Value);
                    throw new Exception($"Transaction failed: {txHash}");
                }

                _logger.LogInformation("? Transaction confirmed in block {Block}. Gas used: {GasUsed}/{GasLimit}", 
                    receipt.BlockNumber.Value, receipt.GasUsed.Value, gasLimit.Value);

                return txHash;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "? Transaction failed");
                throw;
            }
        }

        /// <summary>
        /// Estimate MaxFeePerGas for EIP-1559 transactions
        /// </summary>
        private async Task<HexBigInteger> EstimateMaxFeePerGasAsync()
        {
            try
            {
                // Get base fee from latest block
                var latestBlock = await _web3.Eth.Blocks.GetBlockWithTransactionsByNumber.SendRequestAsync(BlockParameter.CreateLatest());
                var baseFee = latestBlock.BaseFeePerGas?.Value ?? 0;

                // MaxFeePerGas = (BaseFee * 2) + MaxPriorityFeePerGas
                var maxPriorityFee = (await EstimateMaxPriorityFeePerGasAsync()).Value;
                var maxFeePerGas = (baseFee * 2) + maxPriorityFee;

                _logger.LogDebug("?? Base fee: {BaseFee}, Calculated MaxFeePerGas: {MaxFeePerGas}", baseFee, maxFeePerGas);
                return new HexBigInteger(maxFeePerGas);
            }
            catch
            {
                // Fallback to gas price if base fee not available
                var gasPrice = await _web3.Eth.GasPrice.SendRequestAsync();
                _logger.LogWarning("?? Could not get base fee, using gas price: {GasPrice}", gasPrice.Value);
                return gasPrice;
            }
        }

        /// <summary>
        /// Estimate MaxPriorityFeePerGas (miner tip) for EIP-1559 transactions
        /// </summary>
        private async Task<HexBigInteger> EstimateMaxPriorityFeePerGasAsync()
        {
            try
            {
                // Try to get priority fee from node
                var priorityFee = await _web3.Eth.GasPrice.SendRequestAsync(); // Fallback
                _logger.LogDebug("?? Estimated MaxPriorityFeePerGas: {PriorityFee}", priorityFee.Value);
                return new HexBigInteger(priorityFee.Value / 10); // 10% of gas price as tip
            }
            catch
            {
                // Default: 1 Gwei
                _logger.LogWarning("?? Could not estimate priority fee, using default: 1 Gwei");
                return new HexBigInteger(1000000000); // 1 Gwei
            }
        }

        public async Task<T> CallFunctionAsync<T>(string contractAddress, string abi, string functionName, params object[] parameters)
        {
            try
            {
                var contract = _web3.Eth.GetContract(abi, contractAddress);
                var function = contract.GetFunction(functionName);
                var result = await function.CallAsync<T>(parameters);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Function call failed");
                throw;
            }
        }

        public async Task<TransactionReceipt?> GetTransactionReceiptAsync(string txHash)
        {
            return await _web3.Eth.Transactions.GetTransactionReceipt.SendRequestAsync(txHash);
        }

        public async Task<TransactionReceipt> WaitForTransactionReceiptAsync(string txHash, int timeoutSeconds = 60)
        {
            TransactionReceipt? receipt = null;
            var attempts = 0;
            while (receipt == null && attempts < timeoutSeconds)
            {
                receipt = await GetTransactionReceiptAsync(txHash);
                if (receipt == null)
                {
                    await Task.Delay(1000);
                    attempts++;
                }
            }
            if (receipt == null)
            {
                throw new TimeoutException($"Transaction {txHash} not confirmed after {timeoutSeconds} seconds");
            }
            return receipt;
        }

        public async Task<ulong> GetBlockNumberAsync()
        {
            var blockNumber = await _web3.Eth.Blocks.GetBlockNumber.SendRequestAsync();
            return (ulong)blockNumber.Value;
        }

        public async Task<bool> IsContractDeployedAsync(string contractAddress)
        {
            var code = await _web3.Eth.GetCode.SendRequestAsync(contractAddress);
            return code != "0x" && code != "0x0" && !string.IsNullOrEmpty(code);
        }

        #region Credential Management Contract Methods

        // Smart Contract ABI for CredentialManagement.sol (khớp với contract thực tế)
                private const string CredentialManagementAbi = @"[
                    {
                        'anonymous': false,
                        'inputs': [
                            { 'indexed': true,  'internalType': 'uint256', 'name': 'credentialId',   'type': 'uint256' },
                            { 'indexed': true,  'internalType': 'address', 'name': 'studentAddress', 'type': 'address' },
                            { 'indexed': true,  'internalType': 'address', 'name': 'issuedBy',       'type': 'address' },
                            { 'indexed': false, 'internalType': 'string',  'name': 'credentialType', 'type': 'string' }
                        ],
                        'name': 'CredentialIssued',
                        'type': 'event'
                    },
                    {
                        'inputs': [
                            { 'internalType': 'address', 'name': 'studentAddress', 'type': 'address' },
                            { 'internalType': 'string',  'name': 'credentialType', 'type': 'string' },
                            { 'internalType': 'string',  'name': 'credentialData', 'type': 'string' },
                            { 'internalType': 'uint256', 'name': 'expiresAt',      'type': 'uint256' }
                        ],
                        'name': 'issueCredential',
                        'outputs': [
                            { 'internalType': 'uint256', 'name': '', 'type': 'uint256' }
                        ],
                        'stateMutability': 'nonpayable',
                        'type': 'function'
                    },
                    {
                        'inputs': [
                            { 'internalType': 'uint256', 'name': 'credentialId', 'type': 'uint256' }
                        ],
                        'name': 'revokeCredential',
                        'outputs': [],
                        'stateMutability': 'nonpayable',
                        'type': 'function'
                    },
                    {
                        'inputs': [
                            { 'internalType': 'uint256', 'name': 'credentialId', 'type': 'uint256' }
                        ],
                        'name': 'verifyCredential',
                        'outputs': [
                            { 'internalType': 'bool', 'name': '', 'type': 'bool' }
                        ],
                        'stateMutability': 'view',
                        'type': 'function'
                    },
                    {
                        'inputs': [
                            { 'internalType': 'uint256', 'name': 'credentialId', 'type': 'uint256' }
                        ],
                        'name': 'getCredential',
                        'outputs': [
                            {
                                'components': [
                                    { 'internalType': 'uint256', 'name': 'credentialId',    'type': 'uint256' },
                                    { 'internalType': 'address', 'name': 'studentAddress',  'type': 'address' },
                                    { 'internalType': 'string',  'name': 'credentialType',  'type': 'string' },
                                    { 'internalType': 'string',  'name': 'credentialData',  'type': 'string' },
                                    { 'internalType': 'uint8',   'name': 'status',          'type': 'uint8' },
                                    { 'internalType': 'address', 'name': 'issuedBy',        'type': 'address' },
                                    { 'internalType': 'uint256', 'name': 'issuedAt',        'type': 'uint256' },
                                    { 'internalType': 'uint256', 'name': 'expiresAt',       'type': 'uint256' }
                                ],
                                'internalType': 'struct DataTypes.Credential',
                                'name': '',
                                'type': 'tuple'
                            }
                        ],
                        'stateMutability': 'view',
                        'type': 'function'
                    },
                    {
                        'inputs': [],
                        'name': 'credentialCount',
                        'outputs': [
                            { 'internalType': 'uint256', 'name': '', 'type': 'uint256' }
                        ],
                        'stateMutability': 'view',
                        'type': 'function'
                    }
                ]";

        /// <summary>
        /// Issue credential on blockchain
        /// </summary>
        public async Task<(long BlockchainCredentialId, string TransactionHash)> IssueCredentialOnChainAsync(
            string studentWalletAddress,
            string credentialType,
            string credentialDataJson,
            ulong expiresAtUnixSeconds)
        {
            try
            {
                _logger.LogInformation(
                    "Issuing credential on blockchain. Student: {Student}, Type: {Type}",
                    studentWalletAddress,
                    credentialType);

                // Call issueCredential: (address studentAddress, string credentialType, string credentialData, uint256 expiresAt)
                var txHash = await SendTransactionAsync(
                    _settings.CredentialContractAddress,
                    CredentialManagementAbi,
                    "issueCredential",
                    studentWalletAddress,
                    credentialType,
                    credentialDataJson,
                    (BigInteger)expiresAtUnixSeconds
                );

                // Wait for receipt to decode emitted events for deterministic credentialId retrieval
                var receipt = await WaitForTransactionReceiptAsync(txHash, _settings.TransactionTimeout);
                var issuedEvent = receipt
                    .DecodeAllEvents<CredentialIssuedEventDto>()
                    .Select(e => e.Event)
                    .FirstOrDefault(e =>
                        string.Equals(e.StudentAddress, studentWalletAddress, StringComparison.OrdinalIgnoreCase) &&
                        string.Equals(e.CredentialType, credentialType, StringComparison.Ordinal));

                long blockchainCredentialId;

                if (issuedEvent != null)
                {
                    blockchainCredentialId = (long)issuedEvent.CredentialId;
                    _logger.LogDebug(
                        "CredentialIssued event decoded. CredentialId: {CredentialId}, Student: {Student}, IssuedBy: {Issuer}",
                        blockchainCredentialId,
                        issuedEvent.StudentAddress,
                        issuedEvent.IssuedBy);
                }
                else
                {
                    _logger.LogWarning(
                        "CredentialIssued event not found, falling back to credentialCount. Student: {Student}, Type: {Type}",
                        studentWalletAddress,
                        credentialType);

                    var credentialCount = await GetCredentialCountAsync();
                    blockchainCredentialId = credentialCount;
                }

                _logger.LogInformation(
                    "Credential issued successfully. TxHash: {TxHash}, BlockchainId: {BlockchainId}",
                    txHash,
                    blockchainCredentialId);

                return (blockchainCredentialId, txHash);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to issue credential on blockchain");
                throw;
            }
        }

        /// <summary>
        /// Revoke credential on blockchain
        /// </summary>
        public async Task<string> RevokeCredentialOnChainAsync(long blockchainCredentialId)
        {
            try
            {
                _logger.LogInformation(
                    "Revoking credential on blockchain. BlockchainId: {BlockchainId}",
                    blockchainCredentialId);

                var txHash = await SendTransactionAsync(
                    _settings.CredentialContractAddress,
                    CredentialManagementAbi,
                    "revokeCredential",
                    (BigInteger)blockchainCredentialId
                );

                _logger.LogInformation(
                    "Credential revoked successfully. TxHash: {TxHash}, BlockchainId: {BlockchainId}",
                    txHash,
                    blockchainCredentialId);

                return txHash;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to revoke credential on blockchain. BlockchainId: {BlockchainId}", blockchainCredentialId);
                throw;
            }
        }

        /// <summary>
        /// Verify credential on blockchain (status/expiry only)
        /// </summary>
        public async Task<bool> VerifyCredentialOnChainAsync(long blockchainCredentialId)
        {
            try
            {
                _logger.LogInformation(
                    "Verifying credential on blockchain. BlockchainId: {BlockchainId}",
                    blockchainCredentialId);

                var isValid = await CallFunctionAsync<bool>(
                    _settings.CredentialContractAddress,
                    CredentialManagementAbi,
                    "verifyCredential",
                    (BigInteger)blockchainCredentialId
                );

                _logger.LogInformation(
                    "Credential verification completed. BlockchainId: {BlockchainId}, IsValid: {IsValid}",
                    blockchainCredentialId,
                    isValid);

                return isValid;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to verify credential on blockchain. BlockchainId: {BlockchainId}", blockchainCredentialId);
                throw;
            }
        }

        /// <summary>
        /// Get credential from blockchain
        /// </summary>
        public async Task<BlockchainCredentialOnChain> GetCredentialFromChainAsync(long blockchainCredentialId)
        {
            try
            {
                var contract = _web3.Eth.GetContract(CredentialManagementAbi, _settings.CredentialContractAddress);
                var getFunction = contract.GetFunction("getCredential");

                var result = await getFunction.CallDeserializingToObjectAsync<BlockchainCredentialOnChain>(
                    (BigInteger)blockchainCredentialId
                );

                _logger.LogInformation(
                    "Retrieved credential from blockchain. BlockchainId: {BlockchainId}, Status: {Status}",
                    blockchainCredentialId,
                    result.Status);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get credential from blockchain. BlockchainId: {BlockchainId}", blockchainCredentialId);
                throw;
            }
        }

        /// <summary>
        /// Get credential count from blockchain
        /// </summary>
        public async Task<long> GetCredentialCountAsync()
        {
            try
            {
                var count = await CallFunctionAsync<BigInteger>(
                    _settings.CredentialContractAddress,
                    CredentialManagementAbi,
                    "credentialCount");

                return (long)count;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get credential count from blockchain");
                throw;
            }
        }

        #endregion
        [Event("CredentialIssued")]
        private class CredentialIssuedEventDto : IEventDTO
        {
            [Parameter("uint256", "credentialId", 1, true)]
            public BigInteger CredentialId { get; set; }

            [Parameter("address", "studentAddress", 2, true)]
            public string StudentAddress { get; set; } = string.Empty;

            [Parameter("address", "issuedBy", 3, true)]
            public string IssuedBy { get; set; } = string.Empty;

            [Parameter("string", "credentialType", 4, false)]
            public string CredentialType { get; set; } = string.Empty;
        }
    }
}
