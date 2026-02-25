using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace PhiraMp.Server.Models;

public class ServerConfig
{
    public string BindIp { get; set; } = "::";
    public int Port { get; set; } = 12346;
    public int RoomMaxPlayers { get; set; } = 8;
    public List<int> Monitors { get; set; } = [2];

    /// <summary>
    /// 加载配置文件
    /// </summary>
    /// <param name="path">配置文件路径</param>
    /// <returns>配置文件</returns>
    public static ServerConfig Load(string path = "server_config.yml")
    {
        try
        {
            // 将path获取成绝对位置，防止玄学问题
            path = Path.GetFullPath(path);
            if (!File.Exists(path))
            {
                // Create default config file
                var defaultConfig = new ServerConfig();
                var serializer = new SerializerBuilder()
                    .WithNamingConvention(UnderscoredNamingConvention.Instance)
                    .Build();
                var yamlNew = serializer.Serialize(defaultConfig);
                File.WriteAllText(path, yamlNew);
                return defaultConfig;
            }
                

            var deserializer = new DeserializerBuilder()
                .WithNamingConvention(UnderscoredNamingConvention.Instance)
                .Build();

            var yaml = File.ReadAllText(path);
            return deserializer.Deserialize<ServerConfig>(yaml);
        }
        catch
        {
            return new ServerConfig();
        }
    }
}