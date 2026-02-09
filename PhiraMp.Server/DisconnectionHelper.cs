using System.Net.Sockets;

namespace PhiraMp.Server;

/// <summary>
/// Helper class for handling network disconnection exceptions
/// </summary>
public static class DisconnectionHelper
{
    /// <summary>
    /// Checks if an exception represents a client disconnection (not an error to log)
    /// </summary>
    public static bool IsClientDisconnection(Exception ex)
    {
        return ex switch
        {
            SocketException { ErrorCode: 10054 } => true, // Connection reset by peer
            SocketException { ErrorCode: 10053 } => true, // Connection aborted
            IOException { InnerException: SocketException { ErrorCode: 10054 or 10053 } } => true,
            EndOfStreamException => true, // Graceful disconnect
            _ => false
        };
    }
}