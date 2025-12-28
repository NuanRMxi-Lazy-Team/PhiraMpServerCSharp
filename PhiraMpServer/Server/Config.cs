using System;
using System.Collections.Generic;
using System.IO;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace PhiraMpServer.Server;

public class ServerConfig
{
    public string BindIp { get; set; } = "::";
    public string ExternalAddress = "127.0.0.1:12346";
    public int Port { get; set; } = 12346;
    public bool EnableExternalInterface { get; set; } = true;
    public string ExternalInterfaceToken { get; set; } = Guid.NewGuid().ToString("N")[..16];
    public string ExternalInterfaceIp { get; set; } = "127.0.0.1";
    public int ExternalInterfacePort { get; set; } = 17181;
    public int ServerMaxPlayers { get; set; } = 256;
    public int RoomMaxPlayers { get; set; } = 8;
    public List<int> Monitors { get; set; } = [2];
    public bool CycleVotingMode { get; set; } = false;

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
                return defaultConfig;
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