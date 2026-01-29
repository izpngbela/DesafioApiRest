namespace DesafioApiRest.Api.Configuration;


/// Configurações de usuários para autenticação
/// NOTA: Em produção, usar banco de dados com senhas em hash BCrypt
public class UserCredentialsOptions
{
    public const string SectionName = "Authentication:Users";
    
    /// Lista de usuários permitidos
    public List<UserCredential> AllowedUsers { get; set; } = new();
}

public class UserCredential
{

    /// Nome de usuário
    public string Username { get; set; } = string.Empty;
    
    /// Hash da senha (BCrypt)
    /// Para gerar: Use BCrypt.Net.BCrypt.HashPassword("senha")
    public string PasswordHash { get; set; } = string.Empty;
}
