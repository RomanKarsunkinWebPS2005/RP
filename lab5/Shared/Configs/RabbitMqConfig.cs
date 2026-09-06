namespace Shared.Configs;

public static class RabbitMqConfig
{
    public static readonly string Host = "RABBITMQ_HOST";
    public static readonly string Port = "RABBITMQ_PORT";
    public static readonly string User = "RABBITMQ_USER";
    public static readonly string Password = "RABBITMQ_PASSWORD";
    
    public static readonly string Queue = "RABBITMQ_QUEUE";
    
    public static readonly string ValuatorExchange = "RABBITMQ_EXCHANGE";
    public static readonly string EventsExchange = "RABBITMQ_EVENTS_EXCHANGE";
    
    public static readonly string ValuatorRoutingKey = "RABBITMQ_ROUTING_KEY";
    public static readonly string RankCalculatedEventRoutingKey = "RABBITMQ_RANK_ROUTING_KEY";
    public static readonly string SimilarityRoutingKey = "RABBITMQ_SIMILARITY_ROUTING_KEY";
}