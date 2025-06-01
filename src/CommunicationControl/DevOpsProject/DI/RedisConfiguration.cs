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
            var redisEnvironmentConnectionString = Environment.GetEnvironmentVariable("REDIS_CONNECTION_STRING") ?? redisConfiguration.ConnectionString;

            var options = new ConfigurationOptions
            {
                EndPoints = { redisEnvironmentConnectionString ?? redisConfiguration.ConnectionString },
                Ssl = true
            };
            var redis = ConnectionMultiplexer.Connect(options);
            
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
