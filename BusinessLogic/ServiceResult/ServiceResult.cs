using System;
using System.Collections.Generic;
using System.Text;

namespace BusinessLogic.Common
{
    public class ServiceResult
    {
        public bool IsSuccess { get; init; }

        public int StatusCode { get; init; }

        public string? ErrorMessage { get; init; }

        private ServiceResult(
            bool isSuccess,
            int statusCode,
            string? errorMessage)
        {
            IsSuccess = isSuccess;
            StatusCode = statusCode;
            ErrorMessage = errorMessage;
        }

        public static ServiceResult Success()
        {
            return new ServiceResult(
                true,
                200,
                null);
        }

        public static ServiceResult NoContent()
        {
            return new ServiceResult(
                true,
                204,
                null);
        }

        public static ServiceResult BadRequest(string message)
        {
            return new ServiceResult(
                false,
                400,
                message);
        }

        public static ServiceResult Unauthorized(string message)
        {
            return new ServiceResult(
                false,
                401,
                message);
        }

        public static ServiceResult Forbidden(string message)
        {
            return new ServiceResult(
                false,
                403,
                message);
        }

        public static ServiceResult NotFound(string message)
        {
            return new ServiceResult(
                false,
                404,
                message);
        }

        public static ServiceResult Conflict(string message)
        {
            return new ServiceResult(
false,
                409,
                message);
        }
    }
}
