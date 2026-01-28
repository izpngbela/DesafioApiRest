namespace DesafioApiRest.Api.Services;

/// Serviço para hash e validação de senhas usando BCrypt
public interface IPasswordHasher
{

    /// Cria um hash seguro da senha
    string HashPassword(string password);
    
    /// Verifica se a senha corresponde ao hash
    bool VerifyPassword(string password, string hash);
}

public class PasswordHasher : IPasswordHasher
{
    public string HashPassword(string password)
    {
        return BCrypt.Net.BCrypt.HashPassword(password, BCrypt.Net.BCrypt.GenerateSalt(12));
    }

    public bool VerifyPassword(string password, string hash)
    {
        return BCrypt.Net.BCrypt.Verify(password, hash);
    }
}
