namespace Aranea.HttpMessageHandler
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