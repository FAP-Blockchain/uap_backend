using Fap.Api.Interfaces;
using Fap.Api.DTOs.Blockchain;
using Fap.Api.Extensions;
using Fap.Domain.Constants;
using Fap.Domain.Enums;
using Fap.Domain.Settings;
using Fap.Domain.Entities;
using Fap.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace Fap.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class BlockchainController : ControllerBase
    {
        private readonly IBlockchainService _blockchain;
        private readonly BlockchainSettings _settings;
        private readonly IConfiguration _config;
        private readonly ILogger<BlockchainController> _logger;
        private readonly FapDbContext _db;

        public BlockchainController(IBlockchainService blockchain, IOptions<BlockchainSettings> settings, IConfiguration config, ILogger<BlockchainController> logger, FapDbContext db)
        {
            _blockchain = blockchain;
            _settings = settings.Value;
            _config = config;
            _logger = logger;
            _db = db;
        }

        /// <summary>
        /// Audit helper: fetch tx receipt, decode known events, and persist them into ActionLogs.
        /// Frontend signs the tx (MetaMask), backend reads receipt via RPC and stores audit trail.
        /// </summary>
        [HttpPost("tx-receipt")]
        [Authorize]
        public async Task<IActionResult> SaveTxReceiptAudit([FromBody] TxReceiptAuditRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new { success = false, message = "Invalid request", errors = ModelState });
            }

            var userId = User.GetRequiredUserId();
            var txHash = request.TxHash.Trim();

            if (!txHash.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
            {
                txHash = "0x" + txHash;
            }

            if (txHash.Length != 66)
            {
                return BadRequest(new { success = false, message = "TxHash must be 66 characters (0x + 64 hex)." });
            }

            var receipt = await _blockchain.GetTransactionReceiptAsync(txHash);
            if (receipt == null)
            {
                return NotFound(new { success = false, message = "Transaction not found or still pending", transactionHash = txHash });
            }

            // Fetch tx for TxFrom/TxTo (auditability)
            string? txFrom = null;
            string? txTo = null;
            try
            {
                var web3 = _blockchain.GetWeb3();
                var tx = await web3.Eth.Transactions.GetTransactionByHash.SendRequestAsync(txHash);
                txFrom = tx?.From;
                txTo = tx?.To;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not resolve tx from/to for TxHash={TxHash}", txHash);
            }

            var decodedEvents = await _blockchain.DecodeReceiptEventsAsync(txHash);
            var blockNumber = (long?)receipt.BlockNumber?.Value;

            static string Truncate(string value, int maxLen)
            {
                if (string.IsNullOrEmpty(value)) return value;
                return value.Length <= maxLen ? value : value[..maxLen];
            }

            // If no known events were decoded, still store a single audit record
            if (decodedEvents.Count == 0)
            {
                var detail = JsonSerializer.Serialize(new
                {
                    request.Detail,
                    transactionHash = txHash,
                    blockNumber,
                    from = receipt.From,
                    to = receipt.To,
                    status = receipt.Status?.Value,
                    logsCount = receipt.Logs?.Length ?? 0
                });

                detail = Truncate(detail, 500);

                _db.ActionLogs.Add(new ActionLog
                {
                    Id = Guid.NewGuid(),
                    CreatedAt = DateTime.UtcNow,
                    Action = request.Action ?? ActionLogActions.ChainTx,
                    EventName = "UNKNOWN_EVENT",
                    Detail = detail,
                    UserId = userId,
                    CredentialId = request.CredentialId,
                    TransactionHash = txHash,
                    BlockNumber = blockNumber,
                    TxFrom = txFrom,
                    TxTo = txTo,
                    ContractAddress = receipt.To
                });

                await _db.SaveChangesAsync();
                return Ok(new { success = true, transactionHash = txHash, blockNumber, saved = 1, events = Array.Empty<string>() });
            }

            var now = DateTime.UtcNow;
            foreach (var ev in decodedEvents)
            {
                // Keep detail short to avoid nvarchar(500) truncation errors
                string wrappedDetail;
                if (string.IsNullOrWhiteSpace(request.Detail))
                {
                    wrappedDetail = ev.DetailJson;
                }
                else
                {
                    try
                    {
                        using var doc = JsonDocument.Parse(ev.DetailJson);
                        wrappedDetail = JsonSerializer.Serialize(new
                        {
                            note = request.Detail,
                            decoded = doc.RootElement.Clone()
                        });
                    }
                    catch
                    {
                        wrappedDetail = $"{request.Detail} | {ev.DetailJson}";
                    }
                }

                wrappedDetail = Truncate(wrappedDetail, 500);

                _db.ActionLogs.Add(new ActionLog
                {
                    Id = Guid.NewGuid(),
                    CreatedAt = now,
                    Action = request.Action ?? ActionLogActions.ChainEvent,
                    EventName = ev.EventName,
                    Detail = wrappedDetail,
                    UserId = userId,
                    CredentialId = request.CredentialId,
                    TransactionHash = txHash,
                    BlockNumber = blockNumber,
                    TxFrom = txFrom,
                    TxTo = txTo,
                    ContractAddress = ev.ContractAddress
                });
            }

            var savedCount = await _db.SaveChangesAsync();
            return Ok(new
            {
                success = true,
                transactionHash = txHash,
                blockNumber,
                saved = savedCount,
                events = decodedEvents.Select(e => e.EventName).ToArray()
            });
        }

        /// <summary>
        /// ?? DIAGNOSTIC ENDPOINT - Check blockchain configuration values
        /// </summary>
        [HttpGet("config/check")]
        [AllowAnonymous]
        public IActionResult CheckConfig()
        {
            try
            {
                // ? FIX: Use correct config path
                var enableRegistrationFromConfig = _config.GetValue<bool>("BlockchainSettings:EnableRegistration", false);
                var enableRegistrationFromSettings = _settings.EnableRegistration;

                return Ok(new
                {
                    success = true,
                    message = "Configuration values retrieved successfully",
                    configuration = new
                    {
                        // From IConfiguration (raw config)
                        fromIConfiguration = new
                        {
                            enableRegistration = enableRegistrationFromConfig,
                            configPath = "BlockchainSettings:EnableRegistration"
                        },
                        // From BlockchainSettings (Options pattern)
                        fromBlockchainSettings = new
                        {
                            enableRegistration = enableRegistrationFromSettings,
                            networkUrl = _settings.NetworkUrl,
                            chainId = _settings.ChainId,
                            contracts = new
                            {
                                universityManagement = _settings.Contracts?.UniversityManagement ?? "NULL",
                                credentialManagement = _settings.Contracts?.CredentialManagement ?? "NULL"
                            },
                            gasLimit = _settings.GasLimit,
                            gasPrice = _settings.GasPrice
                        },
                        // Environment
                        environment = new
                        {
                            aspnetcoreEnvironment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "NULL",
                            // Check if env variable overrides config
                            blockchainEnableRegistrationEnvVar = Environment.GetEnvironmentVariable("BlockchainSettings__EnableRegistration") ?? "NOT SET"
                        }
                    },
                    verdict = new
                    {
                        isEnabled = enableRegistrationFromConfig || enableRegistrationFromSettings,
                        reason = enableRegistrationFromConfig
                            ? "Enabled via IConfiguration"
                            : (enableRegistrationFromSettings ? "Enabled via BlockchainSettings" : "DISABLED in both sources"),
                        recommendation = (!enableRegistrationFromConfig && !enableRegistrationFromSettings)
                            ? "?? Set 'BlockchainSettings.EnableRegistration: true' in appsettings.json or appsettings.Development.json"
                            : "? Configuration is correct"
                    },
                    timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "? Failed to check configuration");
                return StatusCode(500, new
                {
                    success = false,
                    message = "Failed to retrieve configuration",
                    error = ex.Message
                });
            }
        }

        [HttpGet("health")]
        [AllowAnonymous]
        public async Task<IActionResult> GetHealth()
        {
            try
            {
                var blockNumber = await _blockchain.GetBlockNumberAsync();
                var accountAddress = _blockchain.GetAccountAddress();
                var credentialContractDeployed = await _blockchain.IsContractDeployedAsync(_settings.Contracts.CredentialManagement);
                return Ok(new { success = true, connected = true, network = new { url = _settings.NetworkUrl, chainId = _settings.ChainId, currentBlock = blockNumber }, account = new { address = accountAddress }, contracts = new { credentialManagement = new { address = _settings.Contracts.CredentialManagement, deployed = credentialContractDeployed }, attendanceManagement = new { address = _settings.Contracts.AttendanceManagement, deployed = await _blockchain.IsContractDeployedAsync(_settings.Contracts.AttendanceManagement) }, gradeManagement = new { address = _settings.Contracts.GradeManagement, deployed = await _blockchain.IsContractDeployedAsync(_settings.Contracts.GradeManagement) }, classManagement = new { address = _settings.Contracts.ClassManagement, deployed = await _blockchain.IsContractDeployedAsync(_settings.Contracts.ClassManagement) }, universityManagement = new { address = _settings.Contracts.UniversityManagement, deployed = await _blockchain.IsContractDeployedAsync(_settings.Contracts.UniversityManagement) } }, timestamp = DateTime.UtcNow });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "? Health check failed");
                return StatusCode(500, new { success = false, connected = false, message = "Failed to connect to blockchain", error = ex.Message, timestamp = DateTime.UtcNow });
            }
        }

        /// <summary>
        /// Get credential from blockchain using on-chain credential ID (BlockchainCredentialId)
        /// </summary>
        [HttpGet("credentials/on-chain/{blockchainCredentialId:long}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetOnChainCredential(long blockchainCredentialId)
        {
            try
            {
            _logger.LogInformation("?? Getting on-chain credential: {BlockchainCredentialId}", blockchainCredentialId);
            var data = await _blockchain.GetCredentialFromChainAsync(blockchainCredentialId);

                var statusEnum = data.StatusEnum;
                var statusText = statusEnum switch
                {
                    BlockchainCredentialStatus.Pending => "Pending",
                    BlockchainCredentialStatus.Active => "Issued",
                    BlockchainCredentialStatus.Revoked => "Revoked",
                    BlockchainCredentialStatus.Expired => "Expired",
                    _ => "Unknown"
                };

                return Ok(new
                {
                    success = true,
                    data = new
                    {
                        credentialId = data.CredentialId.ToString(),
                        studentAddress = data.StudentAddress,
                        credentialType = data.CredentialType,
                        credentialData = data.CredentialData,
                        status = (byte)statusEnum,
                        statusName = statusText,
                        statusText,
                        issuedBy = data.IssuedBy,
                        issuedAt = data.IssuedAt.ToString(),
                        expiresAt = data.ExpiresAt.ToString()
                    },
                    timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "? Failed to get on-chain credential {BlockchainCredentialId}", blockchainCredentialId);
                return NotFound(new
                {
                    success = false,
                    message = "On-chain credential not found",
                    error = ex.Message,
                    timestamp = DateTime.UtcNow
                });
            }
        }

        /// <summary>
        /// Debug endpoint to inspect raw getCredential output
        /// </summary>
        [HttpGet("credentials/on-chain/{blockchainCredentialId:long}/raw")]
        [AllowAnonymous]
        public async Task<IActionResult> GetOnChainCredentialRaw(long blockchainCredentialId)
        {
            var raw = await _blockchain.DebugGetCredentialRawAsync(blockchainCredentialId);
            return Ok(new
            {
                success = true,
                blockchainCredentialId,
                raw,
                timestamp = DateTime.UtcNow
            });
        }

        /// <summary>
        /// Debug endpoint to decode tuple manually for troubleshooting DTO mapping
        /// </summary>
        [HttpGet("credentials/on-chain/{blockchainCredentialId:long}/debug-decode")]
        [AllowAnonymous]
        public async Task<IActionResult> DebugDecodeOnChainCredential(long blockchainCredentialId)
        {
            var decoded = await _blockchain.DebugDecodeCredentialAsync(blockchainCredentialId);
            return Ok(new
            {
                success = true,
                blockchainCredentialId,
                decoded,
                timestamp = DateTime.UtcNow
            });
        }

        /// <summary>
        /// Get total number of credentials stored on-chain
        /// </summary>
        [HttpGet("credentials/count")]
        [AllowAnonymous]
        public async Task<IActionResult> GetOnChainCredentialCount()
        {
            try
            {
                var count = await _blockchain.GetCredentialCountAsync();
                return Ok(new
                {
                    success = true,
                    count,
                    timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get credential count from blockchain");
                return StatusCode(500, new
                {
                    success = false,
                    message = "Failed to get credential count",
                    error = ex.Message,
                    timestamp = DateTime.UtcNow
                });
            }
        }

        [HttpGet("transactions/{txHash}/verify")]
        [AllowAnonymous]
        public async Task<IActionResult> VerifyTransaction(string txHash)
        {
            try
            {
                _logger.LogInformation("?? Verifying transaction: {TxHash}", txHash);
                var receipt = await _blockchain.GetTransactionReceiptAsync(txHash);
                if (receipt == null) return NotFound(new { success = false, message = "Transaction not found or still pending", transactionHash = txHash, timestamp = DateTime.UtcNow });
                var isValid = receipt.Status?.Value == 1;
                return Ok(new { success = true, transactionHash = txHash, isValid, status = isValid ? "confirmed" : "failed", blockNumber = receipt.BlockNumber?.Value.ToString(), gasUsed = receipt.GasUsed?.Value.ToString(), from = receipt.From, to = receipt.To, timestamp = DateTime.UtcNow });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "? Failed to verify transaction");
                return StatusCode(500, new { success = false, message = "Failed to verify transaction", error = ex.Message, timestamp = DateTime.UtcNow });
            }
        }

        [HttpGet("transactions/{txHash}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetTransactionReceipt(string txHash)
        {
            try
            {
                var receipt = await _blockchain.GetTransactionReceiptAsync(txHash);
                if (receipt == null) return NotFound(new { success = false, message = "Transaction not found", transactionHash = txHash });
                return Ok(new { success = true, data = new { transactionHash = receipt.TransactionHash, blockNumber = receipt.BlockNumber?.Value.ToString(), blockHash = receipt.BlockHash, from = receipt.From, to = receipt.To, gasUsed = receipt.GasUsed?.Value.ToString(), status = receipt.Status?.Value == 1 ? "success" : "failed", contractAddress = receipt.ContractAddress }, timestamp = DateTime.UtcNow });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "? Failed to get transaction receipt");
                return StatusCode(500, new { success = false, message = "Failed to get transaction receipt", error = ex.Message });
            }
        }
    }

}
