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
                _logger.LogInformation("Sending transaction: {Function} to {Contract}", functionName, contractAddress);
                var contract = _web3.Eth.GetContract(abi, contractAddress);
                var function = contract.GetFunction(functionName);
                var estimatedGas = await function.EstimateGasAsync(_account.Address, null, null, parameters);
                _logger.LogDebug("Estimated gas: {Gas}", estimatedGas.Value);
                var gas = new HexBigInteger(estimatedGas.Value * 2);
                var gasPrice = new HexBigInteger(_settings.GasPrice);
                var txHash = await function.SendTransactionAsync(_account.Address, gas, gasPrice, new HexBigInteger(0), parameters);
                _logger.LogInformation("Transaction sent: {TxHash}", txHash);
                var receipt = await WaitForTransactionReceiptAsync(txHash, _settings.TransactionTimeout);
                if (receipt.Status?.Value != 1)
                {
                    throw new Exception($"Transaction failed: {txHash}");
                }
                _logger.LogInformation("Transaction confirmed in block {Block}. Gas used: {GasUsed}", receipt.BlockNumber.Value, receipt.GasUsed.Value);
                return txHash;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Transaction failed");
                throw;
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
    }
}
