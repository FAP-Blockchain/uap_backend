using System.Linq;
using System.Numerics;
using System.Text;
using System.Text.Json;
using System.Globalization;
using System.Collections.Generic;
using Fap.Api.Interfaces;
using Fap.Domain.Enums;
using Fap.Domain.Settings;
using Microsoft.Extensions.Options;
using Nethereum.ABI.FunctionEncoding.Attributes;
using Nethereum.Contracts;
using Nethereum.Hex.HexConvertors.Extensions;
using Nethereum.Hex.HexTypes;
using Nethereum.RPC.Eth.DTOs;
using Nethereum.Util;
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

        // ==================== RECEIPT EVENT DECODING (AUDITABILITY) ====================

        private record KnownEventDefinition(
            string EventName,
            string Signature,
            (string Name, string Type)[] IndexedArgs
        );

        private static readonly IReadOnlyDictionary<string, KnownEventDefinition> KnownEventTopic0Map =
            BuildKnownEventTopic0Map();

        private static IReadOnlyDictionary<string, KnownEventDefinition> BuildKnownEventTopic0Map()
        {
            var sha3 = new Sha3Keccack();

            // NOTE: These are the events your UAP contracts emit (UniversityManagement + Roles library)
            // plus key events in CredentialManagement/AttendanceManagement already present in this service.
            var events = new List<KnownEventDefinition>
            {
                // Roles.sol (library events) - appear in receipt logs
                new(
                    EventName: "RoleGranted",
                    Signature: "RoleGranted(address,uint8,address)",
                    IndexedArgs: new[] { ("account","address"), ("grantedBy","address") }
                ),
                new(
                    EventName: "RoleRevoked",
                    Signature: "RoleRevoked(address,uint8,address)",
                    IndexedArgs: new[] { ("account","address"), ("revokedBy","address") }
                ),

                // UniversityManagement.sol
                new(
                    EventName: "UserRegistered",
                    Signature: "UserRegistered(address,string,uint8)",
                    IndexedArgs: new[] { ("userAddress","address") }
                ),
                new(
                    EventName: "UserUpdated",
                    Signature: "UserUpdated(address,string)",
                    IndexedArgs: new[] { ("userAddress","address") }
                ),
                new(
                    EventName: "UserDeactivated",
                    Signature: "UserDeactivated(address)",
                    IndexedArgs: new[] { ("userAddress","address") }
                ),
                new(
                    EventName: "ContractUpdated",
                    Signature: "ContractUpdated(string,address)",
                    IndexedArgs: Array.Empty<(string, string)>()
                ),

                // CredentialManagement.sol (event is also present in CredentialManagementAbi)
                new(
                    EventName: "CredentialIssued",
                    Signature: "CredentialIssued(uint256,address,string,address)",
                    IndexedArgs: new[] { ("credentialId","uint256"), ("studentAddress","address"), ("issuedBy","address") }
                ),

                // AttendanceManagement.sol
                new(
                    EventName: "AttendanceMarked",
                    Signature: "AttendanceMarked(uint256,uint256,address,uint8,address)",
                    IndexedArgs: new[] { ("recordId","uint256"), ("classId","uint256"), ("studentAddress","address") }
                ),
                new(
                    EventName: "AttendanceUpdated",
                    Signature: "AttendanceUpdated(uint256,uint8,uint8,address)",
                    IndexedArgs: new[] { ("recordId","uint256") }
                ),
            };

            return events.ToDictionary(
                e => ("0x" + sha3.CalculateHash(e.Signature)).ToLowerInvariant(),
                e => e
            );
        }

        private static string? DecodeIndexedAddressTopic(string topic)
        {
            if (string.IsNullOrWhiteSpace(topic)) return null;
            var hex = topic.StartsWith("0x", StringComparison.OrdinalIgnoreCase) ? topic[2..] : topic;
            if (hex.Length < 40) return null;
            var addr = hex[^40..];
            return "0x" + addr.ToLowerInvariant();
        }

        private static string DecodeIndexedUint256Topic(string topic)
        {
            var hex = topic.StartsWith("0x", StringComparison.OrdinalIgnoreCase) ? topic[2..] : topic;
            // BigInteger.Parse requires culture-invariant hex parsing
            var value = BigInteger.Parse("0" + hex, NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture);
            return value.ToString(CultureInfo.InvariantCulture);
        }

        private static List<FilterLog> ExtractFilterLogs(TransactionReceipt receipt)
        {
            // In current Nethereum versions used by this repo, TransactionReceipt.Logs is FilterLog[].
            return receipt.Logs?.ToList() ?? new List<FilterLog>();
        }

        public async Task<IReadOnlyList<(string EventName, string ContractAddress, string DetailJson)>> DecodeReceiptEventsAsync(string txHash)
        {
            var receipt = await GetTransactionReceiptAsync(txHash);
            if (receipt == null)
            {
                return Array.Empty<(string, string, string)>();
            }

            var decoded = new List<(string EventName, string ContractAddress, string DetailJson)>();
            var logs = ExtractFilterLogs(receipt);
            // Serialize as string to avoid System.Text.Json emitting BigInteger internal fields.
            var blockNumber = receipt.BlockNumber?.Value.ToString(CultureInfo.InvariantCulture);

            foreach (var log in logs)
            {
                if (log.Topics == null || log.Topics.Length == 0) continue;
                var topic0 = (log.Topics[0]?.ToString() ?? string.Empty).ToLowerInvariant();
                if (!KnownEventTopic0Map.TryGetValue(topic0, out var def))
                {
                    continue; // only decode known events
                }

                var indexed = new Dictionary<string, object?>();
                // Topics[0] = signature; Topics[1..] = indexed params (if any)
                for (var i = 0; i < def.IndexedArgs.Length; i++)
                {
                    var topicIndex = i + 1;
                    if (log.Topics.Length <= topicIndex) break;
                    var (name, type) = def.IndexedArgs[i];
                    var topic = log.Topics[topicIndex]?.ToString() ?? string.Empty;
                    object? value = type switch
                    {
                        "address" => DecodeIndexedAddressTopic(topic),
                        "uint256" => DecodeIndexedUint256Topic(topic),
                        _ => topic
                    };
                    indexed[name] = value;
                }

                var contractAddress = log.Address?.ToString() ?? string.Empty;

                // Keep payload compact to fit ActionLog.Detail (nvarchar(500))
                var detailJson = System.Text.Json.JsonSerializer.Serialize(new
                {
                    transactionHash = txHash,
                    blockNumber,
                    contractAddress,
                    eventName = def.EventName,
                    signature = def.Signature,
                    indexedArgs = indexed
                });

                decoded.Add((def.EventName, contractAddress, detailJson));
            }

            return decoded;
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
                            { 'internalType': 'address', 'name': 'studentAddress',   'type': 'address' },
                            { 'internalType': 'string',  'name': 'credentialType',   'type': 'string' },
                            { 'internalType': 'string',  'name': 'credentialData',   'type': 'string' },
                            { 'internalType': 'bytes32', 'name': 'verificationHash', 'type': 'bytes32' },
                            { 'internalType': 'uint256', 'name': 'expiresAt',        'type': 'uint256' }
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
                                    { 'internalType': 'uint256', 'name': 'credentialId',      'type': 'uint256' },
                                    { 'internalType': 'address', 'name': 'studentAddress',    'type': 'address' },
                                    { 'internalType': 'string',  'name': 'credentialType',    'type': 'string' },
                                    { 'internalType': 'string',  'name': 'credentialData',    'type': 'string' },
                                    { 'internalType': 'bytes32', 'name': 'verificationHash',  'type': 'bytes32' },
                                    { 'internalType': 'uint8',   'name': 'status',            'type': 'uint8' },
                                    { 'internalType': 'address', 'name': 'issuedBy',          'type': 'address' },
                                    { 'internalType': 'uint256', 'name': 'issuedAt',          'type': 'uint256' },
                                    { 'internalType': 'uint256', 'name': 'expiresAt',         'type': 'uint256' }
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
            string verificationHashBase64,
            ulong expiresAtUnixSeconds)
        {
            try
            {
                _logger.LogInformation(
                    "Issuing credential on blockchain. Student: {Student}, Type: {Type}",
                    studentWalletAddress,
                    credentialType);

                // Decode Base64 verification hash to bytes32
                var hashBytes = Convert.FromBase64String(verificationHashBase64);
                if (hashBytes.Length != 32)
                {
                    throw new InvalidOperationException(
                        $"verificationHash must be 32 bytes (got {hashBytes.Length})");
                }

                // Call issueCredential: (address studentAddress, string credentialType, string credentialData, bytes32 verificationHash, uint256 expiresAt)
                var txHash = await SendTransactionAsync(
                    _settings.CredentialContractAddress,
                    CredentialManagementAbi,
                    "issueCredential",
                    studentWalletAddress,
                    credentialType,
                    credentialDataJson,
                    hashBytes,
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
        /// Get credential from blockchain using typed DTO decoding
        /// </summary>
        public async Task<CredentialOnChainStructDto> GetCredentialFromChainAsync(long blockchainCredentialId)
        {
            try
            {
                _logger.LogInformation(
                    "GetCredentialFromChainAsync START. BlockchainId: {BlockchainId}, DTO: {DtoType}",
                    blockchainCredentialId,
                    typeof(CredentialOnChainStructDto).AssemblyQualifiedName);

                var handler = _web3.Eth.GetContractQueryHandler<GetCredentialFunction>();
                var function = new GetCredentialFunction
                {
                    CredentialId = new BigInteger(blockchainCredentialId)
                };

                var response = await handler
                    .QueryDeserializingToObjectAsync<GetCredentialFunctionOutput>(
                        function,
                        _settings.CredentialContractAddress);

                var result = response.Credential ?? new CredentialOnChainStructDto();

                try
                {
                    result.StatusEnum = MapStatus(result.Status);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(
                        ex,
                        "Failed to map credential status value {Status} to BlockchainCredentialStatus for credential {CredentialId}",
                        result.Status,
                        result.CredentialId);
                }

                _logger.LogInformation(
                    "GetCredentialFromChainAsync DONE. BlockchainId: {BlockchainId}, CredentialId: {CredentialId}, StatusRaw: {StatusRaw}, StatusEnum: {StatusEnum}",
                    blockchainCredentialId,
                    result.CredentialId,
                    result.Status,
                    result.StatusEnum);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to get credential from blockchain. BlockchainId: {BlockchainId}",
                    blockchainCredentialId);
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
                var decodedDto = await GetCredentialFromChainAsync(blockchainCredentialId);
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
                        verificationHashBase64 = decodedDto.VerificationHashBase64,
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

                #region Attendance Management Contract Methods

                private const string AttendanceManagementAbi = @"[
                    {
                        'anonymous': false,
                        'inputs': [
                            { 'indexed': true,  'internalType': 'uint256', 'name': 'recordId',       'type': 'uint256' },
                            { 'indexed': true,  'internalType': 'uint256', 'name': 'classId',        'type': 'uint256' },
                            { 'indexed': true,  'internalType': 'address', 'name': 'studentAddress', 'type': 'address' },
                            { 'indexed': false, 'internalType': 'uint8',   'name': 'status',         'type': 'uint8' },
                            { 'indexed': false, 'internalType': 'address', 'name': 'markedBy',       'type': 'address' }
                        ],
                        'name': 'AttendanceMarked',
                        'type': 'event'
                    },
                    {
                        'anonymous': false,
                        'inputs': [
                            { 'indexed': true,  'internalType': 'uint256', 'name': 'recordId', 'type': 'uint256' },
                            { 'indexed': false, 'internalType': 'uint8',   'name': 'oldStatus','type': 'uint8' },
                            { 'indexed': false, 'internalType': 'uint8',   'name': 'newStatus','type': 'uint8' },
                            { 'indexed': false, 'internalType': 'address', 'name': 'updatedBy','type': 'address' }
                        ],
                        'name': 'AttendanceUpdated',
                        'type': 'event'
                    },
                    {
                        'inputs': [
                            { 'internalType': 'uint256', 'name': 'classId',       'type': 'uint256' },
                            { 'internalType': 'address', 'name': 'studentAddress', 'type': 'address' },
                            { 'internalType': 'uint256', 'name': 'sessionDate',   'type': 'uint256' },
                            { 'internalType': 'uint8',   'name': 'status',        'type': 'uint8' },
                            { 'internalType': 'string',  'name': 'notes',         'type': 'string' }
                        ],
                        'name': 'markAttendance',
                        'outputs': [
                            { 'internalType': 'uint256', 'name': '', 'type': 'uint256' }
                        ],
                        'stateMutability': 'nonpayable',
                        'type': 'function'
                    },
                    {
                        'inputs': [
                            { 'internalType': 'uint256', 'name': 'recordId',  'type': 'uint256' },
                            { 'internalType': 'uint8',   'name': 'newStatus', 'type': 'uint8' },
                            { 'internalType': 'string',  'name': 'notes',     'type': 'string' }
                        ],
                        'name': 'updateAttendance',
                        'outputs': [],
                        'stateMutability': 'nonpayable',
                        'type': 'function'
                    },
                    {
                        'inputs': [
                            { 'internalType': 'uint256', 'name': 'recordId', 'type': 'uint256' }
                        ],
                        'name': 'getAttendanceRecord',
                        'outputs': [
                            {
                                'components': [
                                    { 'internalType': 'uint256', 'name': 'recordId',      'type': 'uint256' },
                                    { 'internalType': 'uint256', 'name': 'classId',       'type': 'uint256' },
                                    { 'internalType': 'address', 'name': 'studentAddress','type': 'address' },
                                    { 'internalType': 'uint256', 'name': 'sessionDate',   'type': 'uint256' },
                                    { 'internalType': 'uint8',   'name': 'status',        'type': 'uint8' },
                                    { 'internalType': 'string',  'name': 'notes',         'type': 'string' },
                                    { 'internalType': 'address', 'name': 'markedBy',      'type': 'address' },
                                    { 'internalType': 'uint256', 'name': 'markedAt',      'type': 'uint256' }
                                ],
                                'internalType': 'struct DataTypes.AttendanceRecord',
                                'name': '',
                                'type': 'tuple'
                            }
                        ],
                        'stateMutability': 'view',
                        'type': 'function'
                    }
                ]";

                [Event("AttendanceMarked")]
                private class AttendanceMarkedEventDto : IEventDTO
                {
                        [Parameter("uint256", "recordId", 1, true)]
                        public BigInteger RecordId { get; set; }

                        [Parameter("uint256", "classId", 2, true)]
                        public BigInteger ClassId { get; set; }

                        [Parameter("address", "studentAddress", 3, true)]
                        public string StudentAddress { get; set; } = string.Empty;

                        [Parameter("uint8", "status", 4, false)]
                        public byte Status { get; set; }

                        [Parameter("address", "markedBy", 5, false)]
                        public string MarkedBy { get; set; } = string.Empty;
                }

                [FunctionOutput]
                public class AttendanceOnChainStructDto : IFunctionOutputDTO
                {
                    [Parameter("uint256", "recordId", 1)]
                    public BigInteger RecordId { get; set; }

                    [Parameter("uint256", "classId", 2)]
                    public BigInteger ClassId { get; set; }

                    [Parameter("address", "studentAddress", 3)]
                    public string StudentAddress { get; set; } = string.Empty;

                    [Parameter("uint256", "sessionDate", 4)]
                    public BigInteger SessionDate { get; set; }

                    [Parameter("uint8", "status", 5)]
                    public byte Status { get; set; }

                    // Convenience property for mapping on-chain status to domain enum
                    public AttendanceStatusEnum StatusEnum { get; set; }

                    [Parameter("string", "notes", 6)]
                    public string Notes { get; set; } = string.Empty;

                    [Parameter("address", "markedBy", 7)]
                    public string MarkedBy { get; set; } = string.Empty;

                    [Parameter("uint256", "markedAt", 8)]
                    public BigInteger MarkedAt { get; set; }
                }

                [Function("getAttendanceRecord", typeof(AttendanceOnChainStructDto))]
                public class GetAttendanceRecordFunction : FunctionMessage
                {
                        [Parameter("uint256", "recordId", 1)]
                        public BigInteger RecordId { get; set; }
                }

                public async Task<(long BlockchainRecordId, string TransactionHash)> MarkAttendanceOnChainAsync(
                        ulong classId,
                        string studentWalletAddress,
                        ulong sessionDateUnixSeconds,
                        byte status,
                        string notes)
                {
                        try
                        {
                                _logger.LogInformation(
                                        "Marking attendance on blockchain. Class: {ClassId}, Student: {Student}, Status: {Status}",
                                        classId,
                                        studentWalletAddress,
                                        status);

                                var txHash = await SendTransactionAsync(
                                        _settings.Contracts.AttendanceManagement,
                                        AttendanceManagementAbi,
                                        "markAttendance",
                                        (BigInteger)classId,
                                        studentWalletAddress,
                                        (BigInteger)sessionDateUnixSeconds,
                                        status,
                                        notes
                                );

                                var receipt = await WaitForTransactionReceiptAsync(txHash, _settings.TransactionTimeout);

                                var evt = receipt
                                        .DecodeAllEvents<AttendanceMarkedEventDto>()
                                        .Select(e => e.Event)
                                        .FirstOrDefault(e =>
                                                string.Equals(e.StudentAddress, studentWalletAddress, StringComparison.OrdinalIgnoreCase) &&
                                                (ulong)e.ClassId == classId);

                                long recordId = 0;
                                if (evt != null)
                                {
                                        recordId = (long)evt.RecordId;
                                        _logger.LogDebug(
                                                "AttendanceMarked event decoded. RecordId: {RecordId}, ClassId: {ClassId}, Student: {Student}",
                                                recordId,
                                                evt.ClassId,
                                                evt.StudentAddress);
                                }
                                else
                                {
                                        _logger.LogWarning(
                                                "AttendanceMarked event not found in receipt {TxHash}. RecordId will be 0.",
                                                txHash);
                                }

                                _logger.LogInformation(
                                        "Attendance marked successfully. TxHash: {TxHash}, RecordId: {RecordId}",
                                        txHash,
                                        recordId);

                                return (recordId, txHash);
                        }
                        catch (Exception ex)
                        {
                                _logger.LogError(ex, "Failed to mark attendance on blockchain");
                                throw;
                        }
                }

                public async Task<AttendanceOnChainStructDto> GetAttendanceFromChainAsync(long blockchainRecordId)
                {
                        try
                        {
                                _logger.LogInformation(
                                        "Getting attendance from blockchain. RecordId: {RecordId}",
                                        blockchainRecordId);

                                var handler = _web3.Eth.GetContractQueryHandler<GetAttendanceRecordFunction>();
                                var function = new GetAttendanceRecordFunction
                                {
                                        RecordId = new BigInteger(blockchainRecordId)
                                };

                                    var result = await handler
                                        .QueryDeserializingToObjectAsync<AttendanceOnChainStructDto>(
                                            function,
                                            _settings.Contracts.AttendanceManagement);

                                    try
                                    {
                                        result.StatusEnum = (AttendanceStatusEnum)result.Status;
                                    }
                                    catch (Exception ex)
                                    {
                                        _logger.LogWarning(
                                            ex,
                                            "Failed to map attendance status value {Status} to AttendanceStatusEnum for record {RecordId}",
                                            result.Status,
                                            blockchainRecordId);
                                    }

                                    _logger.LogInformation(
                                        "Got attendance from chain. RecordId: {RecordId}, Status: {Status}, ClassId: {ClassId}",
                                        result.RecordId,
                                        result.Status,
                                        result.ClassId);

                                    return result;
                        }
                        catch (Exception ex)
                        {
                                _logger.LogError(ex, "Failed to get attendance from blockchain. RecordId: {RecordId}", blockchainRecordId);
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

            // New field in smart contract: bytes32 verificationHash
            // Placed between credentialData and status in the struct
            [Parameter("bytes32", "verificationHash", 5)]
            public byte[] VerificationHash { get; set; } = Array.Empty<byte>();

            // Convenience property to expose verification hash as Base64 for logging/diagnostics
            public string VerificationHashBase64 =>
                VerificationHash == null || VerificationHash.Length == 0
                    ? string.Empty
                    : Convert.ToBase64String(VerificationHash);

            [Parameter("uint8", "status", 6)]
            public byte Status { get; set; }

            public BlockchainCredentialStatus StatusEnum { get; set; }

            [Parameter("address", "issuedBy", 7)]
            public string IssuedBy { get; set; } = string.Empty;

            [Parameter("uint256", "issuedAt", 8)]
            public BigInteger IssuedAt { get; set; }

            [Parameter("uint256", "expiresAt", 9)]
            public BigInteger ExpiresAt { get; set; }
        }

        [FunctionOutput]
        public class GetCredentialFunctionOutput : IFunctionOutputDTO
        {
            [Parameter("tuple", "", 1)]
            public CredentialOnChainStructDto Credential { get; set; } = new();
        }

        [Function("getCredential", typeof(GetCredentialFunctionOutput))]
        public class GetCredentialFunction : FunctionMessage
        {
            [Parameter("uint256", "credentialId", 1)]
            public BigInteger CredentialId { get; set; }
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
