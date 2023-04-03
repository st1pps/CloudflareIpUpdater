namespace Stipps.CloudflareIpUpdater.Exceptions;

public class ProcureIpException : Exception
{
    public ProcureIpException(string message, Exception inner) : base(message, inner)
    {
    }
    
    public ProcureIpException(string message) : base(message)
    {
    }
}