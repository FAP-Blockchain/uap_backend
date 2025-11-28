using System.Linq;
using System.Numerics;
using System.Text;
using Fap.Api.Interfaces;
using Fap.Domain.Enums;
using Fap.Domain.Settings;
using Microsoft.Extensions.Options;
using Nethereum.ABI.FunctionEncoding.Attributes;
using Nethereum.Contracts;
using Nethereum.Hex.HexConvertors.Extensions;
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
                            { 'indexed': false, 'internalType': 'string',  'name': 'credentialType', 'type': 'string' },
                            { 'indexed': true,  'internalType': 'address', 'name': 'issuedBy',       'type': 'address' }
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
        public async Task<CredentialOnChainStructDto> GetCredentialFromChainAsync(long blockchainCredentialId)
        {
            try
            {
                _logger.LogInformation(
                    "GetCredentialFromChainAsync START. BlockchainId: {BlockchainId}, DTO: {DtoType}",
                    blockchainCredentialId,
                    typeof(CredentialOnChainStructDto).AssemblyQualifiedName);

                var contract = _web3.Eth.GetContract(CredentialManagementAbi, _settings.CredentialContractAddress);
                var getFunction = contract.GetFunction("getCredential");
                var callInput = getFunction.CreateCallInput((BigInteger)blockchainCredentialId);
                var raw = await _web3.Eth.Transactions.Call.SendRequestAsync(callInput);
                _logger.LogInformation("Raw getCredential output for {BlockchainId}: {Raw}", blockchainCredentialId, raw);

                var result = DecodeCredentialOutput(raw);

                _logger.LogInformation(
                    "GetCredentialFromChainAsync DONE. BlockchainId: {BlockchainId}, CredentialId: {CredentialId}, StatusRaw: {StatusRaw}, StatusEnum: {StatusEnum}",
                    blockchainCredentialId,
                    result.CredentialId,
                    result.Status,
                    result.StatusEnum);

                return result;
            }
            catch (OverflowException overflowEx)
            {
                _logger.LogError(
                    overflowEx,
                    "Overflow while decoding credential {BlockchainId} from chain. DTO: {DtoType}",
                    blockchainCredentialId,
                    typeof(CredentialOnChainStructDto).AssemblyQualifiedName);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to get credential from blockchain. BlockchainId: {BlockchainId}, DTO: {DtoType}",
                    blockchainCredentialId,
                    typeof(CredentialOnChainStructDto).AssemblyQualifiedName);
                throw;
            }
        }

        public async Task<string> DebugGetCredentialRawAsync(long blockchainCredentialId)
        {
            var contract = _web3.Eth.GetContract(CredentialManagementAbi, _settings.CredentialContractAddress);
            var function = contract.GetFunction("getCredential");
            var callInput = function.CreateCallInput((BigInteger)blockchainCredentialId);
            var raw = await _web3.Eth.Transactions.Call.SendRequestAsync(callInput);
            _logger.LogInformation("Debug raw getCredential for {BlockchainId}: {Raw}", blockchainCredentialId, raw);
            return raw;
        }

        public async Task<object> DebugDecodeCredentialAsync(long blockchainCredentialId)
        {
            var raw = await DebugGetCredentialRawAsync(blockchainCredentialId);
            if (string.IsNullOrWhiteSpace(raw))
            {
                return new { message = "Empty response", raw };
            }

            try
            {
                var decodedDto = DecodeCredentialOutput(raw);
                var statusLabel = decodedDto.StatusEnum.ToString();

                var result = new
                {
                    raw,
                    decoded = new
                    {
                        credentialId = decodedDto.CredentialId.ToString(),
                        studentAddress = decodedDto.StudentAddress,
                        credentialType = decodedDto.CredentialType,
                        credentialData = decodedDto.CredentialData,
                        status = decodedDto.Status,
                        statusName = statusLabel,
                        statusText = statusLabel,
                        issuedBy = decodedDto.IssuedBy,
                        issuedAt = decodedDto.IssuedAt.ToString(),
                        expiresAt = decodedDto.ExpiresAt.ToString()
                    }
                };

                _logger.LogInformation("Debug decoded credential for {BlockchainId}: {@Decoded}", blockchainCredentialId, result);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to decode credential {BlockchainId}. Raw: {Raw}", blockchainCredentialId, raw);
                return new { message = "Failed to decode output", raw, error = ex.Message };
            }
        }

        private CredentialOnChainStructDto DecodeCredentialOutput(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw) || raw.Equals("0x", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Empty output received while decoding credential");
            }

            var bytes = raw.StartsWith("0x", StringComparison.OrdinalIgnoreCase)
                ? raw.Substring(2).HexToByteArray()
                : raw.HexToByteArray();

            const int wordSize = 32;

            static BigInteger ReadUInt256(byte[] source, int byteOffset)
            {
                if (byteOffset < 0 || byteOffset + wordSize > source.Length)
                {
                    throw new ArgumentOutOfRangeException(nameof(byteOffset), "Offset exceeds source length");
                }

                var buffer = new byte[wordSize + 1];
                Array.Copy(source, byteOffset, buffer, 1, wordSize);
                Array.Reverse(buffer);
                return new BigInteger(buffer);
            }

            static string ReadAddress(byte[] source, int byteOffset)
            {
                if (byteOffset < 0 || byteOffset + wordSize > source.Length)
                {
                    throw new ArgumentOutOfRangeException(nameof(byteOffset), "Offset exceeds source length");
                }

                var slice = new byte[wordSize];
                Array.Copy(source, byteOffset, slice, 0, wordSize);
                var addressBytes = slice.Skip(wordSize - 20).ToArray();
                return "0x" + BitConverter.ToString(addressBytes).Replace("-", string.Empty).ToLowerInvariant();
            }

            static int ToSafeInt32(BigInteger value, string fieldName, int max)
            {
                if (value < 0 || value > max)
                {
                    throw new InvalidOperationException($"Offset for {fieldName} out of range: {value}");
                }

                return (int)value;
            }

            static string ReadString(byte[] source, int absoluteOffset)
            {
                var lengthValue = ReadUInt256(source, absoluteOffset);
                var length = ToSafeInt32(lengthValue, "string length", source.Length - absoluteOffset - wordSize);
                var dataOffset = absoluteOffset + wordSize;

                if (dataOffset < 0 || dataOffset + length > source.Length)
                {
                    throw new InvalidOperationException("String data exceeds available payload");
                }

                return Encoding.UTF8.GetString(source, dataOffset, length);
            }

            var tupleOffsetValue = ReadUInt256(bytes, 0);
            var tupleOffset = ToSafeInt32(tupleOffsetValue, "tuple offset", bytes.Length - wordSize);

            BigInteger ReadTupleUInt256(int wordIndex) => ReadUInt256(bytes, tupleOffset + wordIndex * wordSize);
            string ReadTupleAddress(int wordIndex) => ReadAddress(bytes, tupleOffset + wordIndex * wordSize);

            var credentialIdValue = ReadTupleUInt256(0);
            var studentAddressValue = ReadTupleAddress(1);
            var credentialTypeRelativeOffset = ReadTupleUInt256(2);
            var credentialDataRelativeOffset = ReadTupleUInt256(3);
            var statusValueRaw = ReadTupleUInt256(4);
            var issuedByValue = ReadTupleAddress(5);
            var issuedAtValue = ReadTupleUInt256(6);
            var expiresAtValue = ReadTupleUInt256(7);

            var credentialTypeOffset = tupleOffset + ToSafeInt32(credentialTypeRelativeOffset, nameof(credentialTypeRelativeOffset), bytes.Length - tupleOffset);
            var credentialDataOffset = tupleOffset + ToSafeInt32(credentialDataRelativeOffset, nameof(credentialDataRelativeOffset), bytes.Length - tupleOffset);

            var credentialTypeValue = ReadString(bytes, credentialTypeOffset);
            var credentialDataValue = ReadString(bytes, credentialDataOffset);

            var statusValue = (byte)statusValueRaw;
            var statusEnum = MapStatus(statusValue);

            return new CredentialOnChainStructDto
            {
                CredentialId = credentialIdValue,
                StudentAddress = studentAddressValue,
                CredentialType = credentialTypeValue,
                CredentialData = credentialDataValue,
                Status = statusValue,
                StatusEnum = statusEnum,
                IssuedBy = issuedByValue,
                IssuedAt = issuedAtValue,
                ExpiresAt = expiresAtValue
            };
        }

        private static BlockchainCredentialStatus MapStatus(byte statusValue)
        {
            return statusValue switch
            {
                (byte)BlockchainCredentialStatus.Pending => BlockchainCredentialStatus.Pending,
                (byte)BlockchainCredentialStatus.Active => BlockchainCredentialStatus.Active,
                (byte)BlockchainCredentialStatus.Revoked => BlockchainCredentialStatus.Revoked,
                (byte)BlockchainCredentialStatus.Expired => BlockchainCredentialStatus.Expired,
                _ => throw new InvalidOperationException($"Unsupported credential status value '{statusValue}' returned from contract")
            };
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

        /// <summary>
        /// DTO for getCredential return struct
        /// Must match DataTypes.Credential tuple in the smart contract.
        /// </summary>
        [FunctionOutput]
        public class CredentialOnChainStructDto : IFunctionOutputDTO
        {
            [Parameter("uint256", "credentialId", 1)]
            public BigInteger CredentialId { get; set; }

            [Parameter("address", "studentAddress", 2)]
            public string StudentAddress { get; set; } = string.Empty;

            [Parameter("string", "credentialType", 3)]
            public string CredentialType { get; set; } = string.Empty;

            [Parameter("string", "credentialData", 4)]
            public string CredentialData { get; set; } = string.Empty;

            [Parameter("uint8", "status", 5)]
            public byte Status { get; set; }

            public BlockchainCredentialStatus StatusEnum { get; set; }

            [Parameter("address", "issuedBy", 6)]
            public string IssuedBy { get; set; } = string.Empty;

            [Parameter("uint256", "issuedAt", 7)]
            public BigInteger IssuedAt { get; set; }

            [Parameter("uint256", "expiresAt", 8)]
            public BigInteger ExpiresAt { get; set; }
        }

        [Event("CredentialIssued")]
        private class CredentialIssuedEventDto : IEventDTO
        {
            [Parameter("uint256", "credentialId", 1, true)]
            public BigInteger CredentialId { get; set; }

            [Parameter("address", "studentAddress", 2, true)]
            public string StudentAddress { get; set; } = string.Empty;

            [Parameter("string", "credentialType", 3, false)]
            public string CredentialType { get; set; } = string.Empty;

            [Parameter("address", "issuedBy", 4, true)]
            public string IssuedBy { get; set; } = string.Empty;
        }
    }
}
