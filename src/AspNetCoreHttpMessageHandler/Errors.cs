namespace AspNetCoreHttpMessageHandler
{
    public enum ArgumentValidationError
    {
        ArgumentIsNull
    }

    public enum InvalidOperation
    {
        NotPermittedToChangeCookieUsageAfterInitialOperation,
        NotPermittedToChangeCookieUsageAfterDisposing
    }
}