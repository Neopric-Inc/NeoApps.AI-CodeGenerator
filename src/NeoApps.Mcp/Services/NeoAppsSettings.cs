namespace NeoApps.Mcp.Services;

/// <summary>
/// Configuration, read from environment variables (set them in claude_desktop_config.json "env").
/// Defaults match deploy/docker-compose.infra.yml, so a fresh machine only needs Docker and the .NET SDK.
/// </summary>
public sealed record NeoAppsSettings
{
    /// <summary>Repository root (contains CodeGenerator/). NEOAPPS_HOME, else auto-detected.</summary>
    public required string RepoRoot { get; init; }
    public string GeneratorDir => Path.Combine(RepoRoot, "CodeGenerator");
    /// <summary>Per-project state, schemas, job logs and compose files.</summary>
    public string WorkspaceDir => Path.Combine(RepoRoot, "workspace");
    public string InfraComposeFile => Path.Combine(RepoRoot, "deploy", "docker-compose.infra.yml");

    public string MySqlHost { get; init; } = "127.0.0.1";
    public int MySqlPort { get; init; } = 3307;
    public string MySqlUser { get; init; } = "root";
    public string MySqlPassword { get; init; } = "neoapps";

    /// <summary>Connection details as seen from inside the Docker network (used by deployed apps).</summary>
    public string DockerMySqlHost { get; init; } = "mysql";
    public int DockerMySqlPort { get; init; } = 3306;
    public string DockerRabbitMqUrl { get; init; } = "amqp://neoapps:neoapps@rabbitmq:5672/";
    public string DockerRedis { get; init; } = "redis:6379";
    public string DockerNetwork { get; init; } = "neoapps";

    /// <summary>Connection details as seen from the host (written into appsettings for `dotnet run`).</summary>
    public string HostRabbitMqUrl { get; init; } = "amqp://neoapps:neoapps@localhost:5672/";
    public string HostRedis { get; init; } = "localhost:6379";

    public int FirstApiPort { get; init; } = 5100;
    public int FirstWebPort { get; init; } = 3100;

    /// <summary>How long a tool call waits for a job before handing back a job id to poll.</summary>
    public TimeSpan InlineWait { get; init; } = TimeSpan.FromSeconds(40);

    public static NeoAppsSettings FromEnvironment()
    {
        static string? Env(string name) => Environment.GetEnvironmentVariable(name) is { Length: > 0 } v ? v : null;
        static int EnvInt(string name, int fallback) => int.TryParse(Env(name), out var v) ? v : fallback;

        var defaults = new NeoAppsSettings { RepoRoot = "" };
        return new NeoAppsSettings
        {
            RepoRoot = Path.GetFullPath(Env("NEOAPPS_HOME") ?? FindRepoRoot()),
            MySqlHost = Env("NEOAPPS_MYSQL_HOST") ?? defaults.MySqlHost,
            MySqlPort = EnvInt("NEOAPPS_MYSQL_PORT", defaults.MySqlPort),
            MySqlUser = Env("NEOAPPS_MYSQL_USER") ?? defaults.MySqlUser,
            MySqlPassword = Env("NEOAPPS_MYSQL_PASSWORD") ?? defaults.MySqlPassword,
            DockerMySqlHost = Env("NEOAPPS_DOCKER_MYSQL_HOST") ?? defaults.DockerMySqlHost,
            DockerMySqlPort = EnvInt("NEOAPPS_DOCKER_MYSQL_PORT", defaults.DockerMySqlPort),
            DockerRabbitMqUrl = Env("NEOAPPS_DOCKER_RABBITMQ_URL") ?? defaults.DockerRabbitMqUrl,
            DockerRedis = Env("NEOAPPS_DOCKER_REDIS") ?? defaults.DockerRedis,
            DockerNetwork = Env("NEOAPPS_DOCKER_NETWORK") ?? defaults.DockerNetwork,
            HostRabbitMqUrl = Env("NEOAPPS_RABBITMQ_URL") ?? defaults.HostRabbitMqUrl,
            HostRedis = Env("NEOAPPS_REDIS") ?? defaults.HostRedis,
            FirstApiPort = EnvInt("NEOAPPS_FIRST_API_PORT", defaults.FirstApiPort),
            FirstWebPort = EnvInt("NEOAPPS_FIRST_WEB_PORT", defaults.FirstWebPort),
        };
    }

    private static string FindRepoRoot()
    {
        foreach (var start in new[] { AppContext.BaseDirectory, Directory.GetCurrentDirectory() })
        {
            for (var dir = new DirectoryInfo(start); dir is not null; dir = dir.Parent)
            {
                if (Directory.Exists(Path.Combine(dir.FullName, "CodeGenerator", "DotNetMySQLTemplate")))
                    return dir.FullName;
            }
        }
        throw new InvalidOperationException(
            "Cannot find the NeoApps repository. Set NEOAPPS_HOME to the folder that contains CodeGenerator/.");
    }
}
