using System.Net;
using System.Net.Mail;
using System.Text.Json;

public class EmailNotificationService : INotificationService
{
    private readonly string _filePath;
    private readonly List<NotificationRequest> _notifications;
    private readonly string _smtpHostName;
    private readonly int _smtpPort;
    private readonly string _smtpUsername;
    private readonly string _smtpPassword;

    public EmailNotificationService(string smtpHostname, int smtpPort, string smtpUserName, string smtpPassword)
    {
        var folder = "Notifications";
        var fileName = "notifications.json";
        _filePath = Path.Combine(Directory.GetCurrentDirectory(), folder, fileName);

        _notifications = new List<NotificationRequest>();
        if (File.Exists(_filePath))
        {
            var json = File.ReadAllText(_filePath);
            var deserialized = JsonSerializer.Deserialize<List<NotificationRequest>>(json);

            if (deserialized != null)
                _notifications = deserialized;
        }

        this._smtpHostName = smtpHostname;
        this._smtpPort = smtpPort;
        this._smtpUsername = smtpUserName;
        this._smtpPassword = smtpPassword;
    }

    public async Task RatingAdded(Whisky whisky, Rating rating)
    {
        var subject = "[Whisky API] New Rating Added";
        var body = @$"Hey there!  We thought you'd like to know a new rating has been added for {whisky.Name}!  

                      It was given a {rating.Stars} star rating with the following message: {rating.Message}";

        foreach (var notification in _notifications.Where(p => p.NotificationType == NotificationType.NEW_RATING))
        {
            // notify!
            await SendEmail(notification.EmailAddress, subject, body);
        }
    }

    public async Task WhiskeyAdded(Whisky whisky)
    {
        var subject = "[Whisky API] New Whisky Added";
        var body = @$"Hey there!  We thought you'd like to know a new whisky has been added!  
                      It is named {whisky.Name} and is from the {whisky.RegionStyle} region.";
        foreach (var notification in _notifications.Where(p => p.NotificationType == NotificationType.NEW_WHISKY))
        {
            // notify!
            await SendEmail(notification.EmailAddress, subject, body);
        }

        subject = $"[Whisky API] New Whisky Added in {whisky.RegionStyle} Region";

        foreach (var notification in _notifications.Where(p =>
                                                    p.NotificationType == NotificationType.NEW_WHISKY_IN_REGION &&
                                                    p.Region.Equals(whisky.RegionStyle, StringComparison.OrdinalIgnoreCase)))
        {
            // notify!
            await SendEmail(notification.EmailAddress, subject, body);
        }
    }


    private async Task SendEmail(string emailAddress, string subject, string body)
    {
        MailMessage mailMessage = new MailMessage("notifications@whiskyapi.com", emailAddress, subject, body);
        SmtpClient client = new SmtpClient(_smtpHostName, _smtpPort);
        client.Credentials = new NetworkCredential(_smtpUsername, _smtpPassword);

        await client.SendMailAsync(mailMessage);
    }
}