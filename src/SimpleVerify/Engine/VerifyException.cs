using System.Text;

namespace SimpleVerify.Engine;

public class VerifyException(string message) : Exception(message)
{
    internal static VerifyException New(
        string directory,
        string receivedName,
        string verifiedName,
        string receivedContent
    )
    {
        StringBuilder builder = new();
        builder.Append($"Directory: {directory}\n");
        builder.Append("New:\n");
        builder.Append($"  - Received: {receivedName}\n");
        builder.Append($"    Verified: {verifiedName}\n");
        builder.Append('\n');
        builder.Append("FileContent:\n");
        builder.Append('\n');
        builder.Append($"Received: {receivedName}\n");
        builder.Append(receivedContent);
        builder.Append('\n');
        return new VerifyException(builder.ToString());
    }

    internal static VerifyException NotEqual(
        string directory,
        string receivedName,
        string verifiedName,
        string receivedContent,
        string verifiedContent
    )
    {
        StringBuilder builder = new();
        builder.Append($"Directory: {directory}\n");
        builder.Append("NotEqual:\n");
        builder.Append($"  - Received: {receivedName}\n");
        builder.Append($"    Verified: {verifiedName}\n");
        builder.Append('\n');
        builder.Append("FileContent:\n");
        builder.Append('\n');
        builder.Append($"Received: {receivedName}\n");
        builder.Append(receivedContent);
        builder.Append('\n');
        builder.Append('\n');
        builder.Append($"Verified: {verifiedName}\n");
        builder.Append(verifiedContent);
        builder.Append('\n');
        return new VerifyException(builder.ToString());
    }
}
