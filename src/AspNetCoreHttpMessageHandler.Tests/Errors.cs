namespace Eru
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