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
        private readonly ILogger<BlockchainController> _logger;

        // ABI for CredentialManagement contract
        private const string CREDENTIAL_ABI = @"[{""inputs"":[{""name"":""credentialId"",""type"":""string""},{""name"":""studentCode"",""type"":""string""},{""name"":""certificateHash"",""type"":""string""}],""name"":""storeCredential"",""outputs"":[],""stateMutability"":""nonpayable"",""type"":""function""},{""inputs"":[{""name"":""credentialId"",""type"":""string""}],""name"":""getCredential"",""outputs"":[{""name"":""studentCode"",""type"":""string""},{""name"":""certificateHash"",""type"":""string""},{""name"":""issuedAt"",""type"":""uint256""},{""name"":""issuer"",""type"":""address""},{""name"":""isRevoked"",""type"":""bool""}],""stateMutability"":""view"",""type"":""function""}]";

        public BlockchainController(IBlockchainService blockchain, IOptions<BlockchainSettings> settings, ILogger<BlockchainController> logger)
        {
            _blockchain = blockchain;
            _settings = settings.Value;
            _logger = logger;
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

        [HttpPost("credentials")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> StoreCredential([FromBody] StoreCredentialRequest request)
        {
            try
            {
                var contractAddress = _settings.Contracts.CredentialManagement;
                var isDeployed = await _blockchain.IsContractDeployedAsync(contractAddress);
                if (!isDeployed) return BadRequest(new { success = false, message = $"CredentialManagement contract not deployed at {contractAddress}" });
                _logger.LogInformation("?? Storing credential: {CredentialId} for student {StudentCode}", request.CredentialId, request.StudentCode);
                var txHash = await _blockchain.SendTransactionAsync(contractAddress, CREDENTIAL_ABI, "storeCredential", request.CredentialId.ToString(), request.StudentCode, request.CertificateHash);
                return Ok(new { success = true, transactionHash = txHash, message = "Credential stored on blockchain successfully", data = new { credentialId = request.CredentialId, studentCode = request.StudentCode, certificateHash = request.CertificateHash }, timestamp = DateTime.UtcNow });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "? Failed to store credential");
                return StatusCode(500, new { success = false, message = "Failed to store credential on blockchain", error = ex.Message, timestamp = DateTime.UtcNow });
            }
        }

        [HttpGet("credentials/{credentialId}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetCredential(Guid credentialId)
        {
            try
            {
                var contractAddress = _settings.Contracts.CredentialManagement;
                _logger.LogInformation("?? Getting credential: {CredentialId}", credentialId);
                var result = await _blockchain.CallFunctionAsync<dynamic>(contractAddress, CREDENTIAL_ABI, "getCredential", credentialId.ToString());
                return Ok(new { success = true, data = new CredentialBlockchainData { CredentialId = credentialId, StudentCode = result.studentCode?.ToString() ?? "", CertificateHash = result.certificateHash?.ToString() ?? "", IssuedAt = DateTimeOffset.FromUnixTimeSeconds((long)result.issuedAt).DateTime, IssuerAddress = result.issuer?.ToString() ?? "", IsRevoked = result.isRevoked }, timestamp = DateTime.UtcNow });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "? Failed to get credential");
                return NotFound(new { success = false, message = "Credential not found on blockchain", error = ex.Message, timestamp = DateTime.UtcNow });
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

    public class StoreCredentialRequest
    {
        [Required] public Guid CredentialId { get; set; }
        [Required] public string StudentCode { get; set; } = string.Empty;
        [Required] public string CertificateHash { get; set; } = string.Empty;
    }
}
