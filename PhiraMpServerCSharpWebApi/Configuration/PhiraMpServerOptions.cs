using System.Security.Cryptography;
using System.Text;

namespace PhiraMpServerCSharpWebApi.Configuration;

public class PhiraMpServerOption
{
    public string Name { get; set; } = string.Empty;
    public string Host { get; set; } = "127.0.0.1";
    public int Port { get; set; } = 11452;
    public bool Enabled { get; set; } = true;
    public string? Token { get; set; }
    
    /// <summary>
    /// 获取 Token 的 SHA256 哈希值
    /// </summary>
    public string? GetTokenSha256()
    {
        if (string.IsNullOrEmpty(Token))
            return null;
        
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(Token));
        return Convert.ToHexString(hash).ToLower();
    }
}

public class PhiraMpServersOptions
{
    public List<PhiraMpServerOption> Servers { get; set; } = new();
}

