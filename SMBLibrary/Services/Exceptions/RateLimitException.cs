using System;

namespace SMBLibrary
{
    public class RateLimitException : Exception
    {
        public RateLimitException(string message)
            : base(message)
        {
        }
    }
}
