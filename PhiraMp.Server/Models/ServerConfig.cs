using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace PhiraMp.Server.Models;

public class ServerConfig
{
    public string BindIp { get; set; } = "::";
    public int Port { get; set; } = 12346;
    public int RoomMaxPlayers { get; set; } = 8;
    public List<int> Monitors { get; set; } = new() { 2 };

    public static ServerConfig Load(string path = "server_config.yml")
    {
        try
        {
            if (!File.Exists(path))
            {
                // Create default config file
                var defaultConfig = new ServerConfig();
                var serializer = new SerializerBuilder()
                    .WithNamingConvention(UnderscoredNamingConvention.Instance)
                    .Build();
                var yamlNew = serializer.Serialize(defaultConfig);
                File.WriteAllText(path, yamlNew);
                return new ServerConfig();
            }
                

            var deserializer = new DeserializerBuilder()
                .WithNamingConvention(UnderscoredNamingConvention.Instance)
                .Build();

            var yaml = File.ReadAllText(path);
            return deserializer.Deserialize<ServerConfig>(yaml) ?? new ServerConfig();
        }
        catch
        {
            return new ServerConfig();
        }
    }
}