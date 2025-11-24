using Fap.Api.Interfaces;
using Fap.Domain.Settings;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.ComponentModel.DataAnnotations;

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

        public BlockchainController(IBlockchainService blockchain, IOptions<BlockchainSettings> settings, IConfiguration config, ILogger<BlockchainController> logger)
        {
            _blockchain = blockchain;
            _settings = settings.Value;
            _config = config;
            _logger = logger;
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
        /// Get credential from blockchain using on-chain credential ID
        /// </summary>
        [HttpGet("credentials/on-chain/{credentialId:long}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetOnChainCredential(long credentialId)
        {
            try
            {
                _logger.LogInformation("?? Getting on-chain credential: {CredentialId}", credentialId);
                var data = await _blockchain.GetCredentialFromChainAsync(credentialId);

                return Ok(new
                {
                    success = true,
                    data = new
                    {
                        credentialId = (long)data.CredentialId,
                        studentAddress = data.StudentAddress,
                        credentialType = data.CredentialType,
                        credentialData = data.CredentialData,
                        status = data.Status,
                        issuedBy = data.IssuedBy,
                        issuedAt = (long)data.IssuedAt,
                        expiresAt = (long)data.ExpiresAt
                    },
                    timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "? Failed to get on-chain credential {CredentialId}", credentialId);
                return NotFound(new
                {
                    success = false,
                    message = "On-chain credential not found",
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
