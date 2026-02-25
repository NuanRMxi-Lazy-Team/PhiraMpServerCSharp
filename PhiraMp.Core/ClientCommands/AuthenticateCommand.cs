namespace PhiraMp.Core;

public class AuthenticateCommand : ClientCommand
{
    public override byte TypeTag => 1;
    public Varchar Token { get; }

    public AuthenticateCommand(Varchar token)
    {
        Token = token;
    }
}
