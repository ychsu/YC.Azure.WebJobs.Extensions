namespace YC.Azure.WebJobs.Extensions.NotificationHub
{
    public class NotificationMessage
    {
        public Platform Platform { get; set; }
        public string TagExpression { get; set; }
        public string Payload { get; set; }
    }
}