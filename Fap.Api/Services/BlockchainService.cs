using Fap.Api.Interfaces;
using Fap.Domain.Settings;
using Microsoft.Extensions.Options;
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

        public async Task<bool> VerifyCertificateOnChainAsync(string transactionHash, string certificateHash)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(transactionHash))
                    return false;

                var receipt = await GetTransactionReceiptAsync(transactionHash);
                if (receipt == null || receipt.Status?.Value != 1)
                {
                    _logger.LogWarning("Transaction {TxHash} not found or failed", transactionHash);
                    return false;
                }

                // TODO: Decode logs to verify certificateHash if needed
                // For now, just verify transaction exists and succeeded
                _logger.LogInformation("Certificate verified on chain: {TxHash}", transactionHash);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verifying certificate on blockchain");
                return false;
            }
        }

        #region Credential Management Contract Methods

        // Smart Contract ABI for CredentialManagement.sol
        private const string CredentialManagementAbi = @"[
            {
                'inputs': [
                    {'internalType': 'uint256', 'name': '_credentialId', 'type': 'uint256'},
                    {'internalType': 'address', 'name': '_studentAddress', 'type': 'address'},
                    {'internalType': 'string', 'name': '_credentialData', 'type': 'string'}
                ],
                'name': 'issueCredential',
                'outputs': [],
                'stateMutability': 'nonpayable',
                'type': 'function'
            },
            {
                'inputs': [
                    {'internalType': 'uint256', 'name': '_credentialId', 'type': 'uint256'},
                    {'internalType': 'string', 'name': '_reason', 'type': 'string'}
                ],
                'name': 'revokeCredential',
                'outputs': [],
                'stateMutability': 'nonpayable',
                'type': 'function'
            },
            {
                'inputs': [
                    {'internalType': 'uint256', 'name': '_credentialId', 'type': 'uint256'}
                ],
                'name': 'verifyCredential',
                'outputs': [
                    {'internalType': 'bool', 'name': 'isValid', 'type': 'bool'},
                    {'internalType': 'address', 'name': 'studentAddress', 'type': 'address'},
                    {'internalType': 'string', 'name': 'credentialData', 'type': 'string'},
                    {'internalType': 'uint8', 'name': 'status', 'type': 'uint8'}
                ],
                'stateMutability': 'view',
                'type': 'function'
            },
            {
                'inputs': [
                    {'internalType': 'uint256', 'name': '_credentialId', 'type': 'uint256'}
                ],
                'name': 'getCredential',
                'outputs': [
                    {
                        'components': [
                            {'internalType': 'uint256', 'name': 'credentialId', 'type': 'uint256'},
                            {'internalType': 'address', 'name': 'studentAddress', 'type': 'address'},
                            {'internalType': 'string', 'name': 'credentialData', 'type': 'string'},
                            {'internalType': 'enum CredentialManagement.CredentialStatus', 'name': 'status', 'type': 'uint8'}
                        ],
                        'internalType': 'struct CredentialManagement.Credential',
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
                    {'internalType': 'uint256', 'name': '', 'type': 'uint256'}
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
            string credentialData)
        {
            try
            {
                // Get next credential ID from blockchain
                var credentialCount = await GetCredentialCountAsync();
                var blockchainCredentialId = credentialCount + 1;

                _logger.LogInformation(
                    "Issuing credential on blockchain. BlockchainId: {BlockchainId}, Student: {Student}",
                    blockchainCredentialId,
                    studentWalletAddress);

                // Call issueCredential on smart contract
                var txHash = await SendTransactionAsync(
                    _settings.CredentialContractAddress,
                    CredentialManagementAbi,
                    "issueCredential",
                    blockchainCredentialId,
                    studentWalletAddress,
                    credentialData);

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
        public async Task<string> RevokeCredentialOnChainAsync(long blockchainCredentialId, string reason)
        {
            try
            {
                _logger.LogInformation(
                    "Revoking credential on blockchain. BlockchainId: {BlockchainId}, Reason: {Reason}",
                    blockchainCredentialId,
                    reason);

                var txHash = await SendTransactionAsync(
                    _settings.CredentialContractAddress,
                    CredentialManagementAbi,
                    "revokeCredential",
                    blockchainCredentialId,
                    reason);

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
        /// Verify credential on blockchain
        /// </summary>
        public async Task<CredentialVerificationResult> VerifyCredentialOnChainAsync(long blockchainCredentialId)
        {
            try
            {
                _logger.LogInformation(
                    "Verifying credential on blockchain. BlockchainId: {BlockchainId}",
                    blockchainCredentialId);

                var contract = _web3.Eth.GetContract(CredentialManagementAbi, _settings.CredentialContractAddress);
                var verifyFunction = contract.GetFunction("verifyCredential");

                // Call verifyCredential - returns (bool isValid, address studentAddress, string credentialData, uint8 status)
                var result = await verifyFunction.CallDeserializingToObjectAsync<CredentialVerificationResult>(blockchainCredentialId);

                _logger.LogInformation(
                    "Credential verification completed. BlockchainId: {BlockchainId}, IsValid: {IsValid}, Status: {Status}",
                    blockchainCredentialId,
                    result.IsValid,
                    result.Status);

                return result;
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
        public async Task<CredentialOnChain> GetCredentialFromChainAsync(long blockchainCredentialId)
        {
            try
            {
                var contract = _web3.Eth.GetContract(CredentialManagementAbi, _settings.CredentialContractAddress);
                var getFunction = contract.GetFunction("getCredential");

                var result = await getFunction.CallDeserializingToObjectAsync<CredentialOnChain>(blockchainCredentialId);

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
                var count = await CallFunctionAsync<System.Numerics.BigInteger>(
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

        #region DTOs for Blockchain Credential Operations

        public class CredentialVerificationResult
        {
            public bool IsValid { get; set; }
            public string StudentAddress { get; set; } = string.Empty;
            public string CredentialData { get; set; } = string.Empty;
            public int Status { get; set; } // 0 = Active, 1 = Revoked
        }

        public class CredentialOnChain
        {
            public System.Numerics.BigInteger CredentialId { get; set; }
            public string StudentAddress { get; set; } = string.Empty;
            public string CredentialData { get; set; } = string.Empty;
            public int Status { get; set; } // 0 = Active, 1 = Revoked
        }

        #endregion
    }
}
