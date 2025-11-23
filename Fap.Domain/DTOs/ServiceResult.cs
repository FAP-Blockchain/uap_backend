using System;

namespace Fap.Domain.DTOs
{
    public class ServiceResult<T>
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public T Data { get; set; }

        public static ServiceResult<T> Fail(string message)
        {
            return new ServiceResult<T> { Success = false, Message = message };
        }

        public static ServiceResult<T> SuccessResult(T data, string message = null)
        {
            return new ServiceResult<T> { Success = true, Data = data, Message = message };
        }
        
        // Alias for SuccessResult to match common patterns
        public static ServiceResult<T> SuccessResponse(T data, string message = null)
        {
            return SuccessResult(data, message);
        }
    }
}
