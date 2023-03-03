using System.Diagnostics.CodeAnalysis;

namespace YC.Azure.WebJobs.Extensions.NotificationHub
{
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public enum Platform : byte
    {
        Fcm = 1,
        Apple = 2
    }
}