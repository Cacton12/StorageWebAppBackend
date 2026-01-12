// Middleware/CustomExceptions.cs
using System;

namespace StorageWebAppBackend.Middleware
{
    /// <summary>
    /// Custom exception types for better error handling
    /// Renamed to avoid conflicts with System.ComponentModel.DataAnnotations
    /// </summary>

    public class AppValidationException : Exception
    {
        public AppValidationException(string message) : base(message) { }
    }

    public class AppNotFoundException : Exception
    {
        public AppNotFoundException(string message) : base(message) { }
    }

    public class AppUnauthorizedException : Exception
    {
        public AppUnauthorizedException(string message) : base(message) { }
    }

    public class AppConflictException : Exception
    {
        public AppConflictException(string message) : base(message) { }
    }
}