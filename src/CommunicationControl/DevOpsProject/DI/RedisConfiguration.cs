using DevOpsProject.CommunicationControl.Logic.Services.Interfaces;
using DevOpsProject.CommunicationControl.Logic.Services;
using DevOpsProject.Shared.Configuration;
using StackExchange.Redis;

namespace DevOpsProject.CommunicationControl.API.DI
{
    public static class RedisConfiguration
    {
        public static IServiceCollection AddRedis(this IServiceCollection serviceCollection, IConfiguration configuration)
        {
            var redisConfiguration = configuration.GetSection("Redis").Get<RedisOptions>();
            var redisEnvironmentConnectionString = Environment.GetEnvironmentVariable("REDIS_CONNECTION") ?? redisConfiguration.ConnectionString;
            // var redisEnvironmentConnectionString = Environment.GetEnvironmentVariable("DATABASE_URL") ?? redisConfiguration.ConnectionString;

            System.Console.WriteLine($"Using Redis connection string from digital ocean: {redisEnvironmentConnectionString}");

            // var options = new ConfigurationOptions
            // {
            //     EndPoints = { redisEnvironmentConnectionString ?? redisConfiguration.ConnectionString },
            //     Ssl = true,
            //     AbortOnConnectFail = false
            // };
            // var redis = ConnectionMultiplexer.Connect(options);
            var redis = ConnectionMultiplexer.Connect(redisEnvironmentConnectionString);
            
            serviceCollection.AddSingleton<IConnectionMultiplexer>(redis);

            serviceCollection.Configure<RedisOptions>(
                configuration.GetSection("Redis"));

            serviceCollection.Configure<RedisKeys>(
                configuration.GetSection("RedisKeys"));

            serviceCollection.AddTransient<IRedisKeyValueService, RedisKeyValueService>();

            serviceCollection.AddTransient<IPublishService, RedisPublishService>();

            return serviceCollection;
        }
    }
}
