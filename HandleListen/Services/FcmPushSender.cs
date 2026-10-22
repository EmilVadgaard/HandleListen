using FirebaseAdmin;
using FirebaseAdmin.Messaging;

public class FcmPushSender
{
    private readonly ILogger<FcmPushSender> _logger;

    public FcmPushSender(ILogger<FcmPushSender> logger)
    {
        _logger = logger;
    }

    /// <param name="isFirstAlert">
    /// True for the first notification sent for a given (event, user) pair - uses the normal
    /// channel (sound + vibration). Later re-sends of the same reminder use the quiet channel
    /// instead, so the notification's text/timestamp still updates but the phone doesn't buzz
    /// again every hour for something the user hasn't dismissed yet.
    /// </param>
    /// <returns>false if the token is no longer valid and its subscription should be deleted; true otherwise.</returns>
    public async Task<bool> SendAsync(string token, string title, string body, string tag, bool isFirstAlert)
    {
        if (FirebaseApp.DefaultInstance is null)
        {
            _logger.LogWarning("Firebase is not configured. Push to {Token} was not sent. {Title}: {Body}", token, title, body);
            return true;
        }

        var message = new Message
        {
            Token = token,
            Notification = new FirebaseAdmin.Messaging.Notification { Title = title, Body = body },
            Android = new AndroidConfig
            {
                Notification = new AndroidNotification
                {
                    // A consistent tag per event means a re-sent reminder for the same event
                    // replaces the previous notification instead of stacking a duplicate.
                    Tag = tag,
                    // Without this, Android showed a nonsensical age on the notification
                    // (e.g. "2032y") instead of "now" - set explicitly rather than relying on
                    // whatever default the OS/SDK falls back to.
                    EventTimestamp = DateTime.UtcNow,
                    ChannelId = isFirstAlert ? "reminders" : "reminders-quiet"
                }
            }
        };

        try
        {
            await FirebaseMessaging.DefaultInstance.SendAsync(message);
            return true;
        }
        catch (FirebaseMessagingException ex) when (ex.MessagingErrorCode == MessagingErrorCode.Unregistered)
        {
            _logger.LogInformation("Push token no longer registered, removing subscription: {Token}", token);
            return false;
        }
        catch (FirebaseMessagingException ex)
        {
            _logger.LogError(ex, "Failed to send push to {Token}", token);
            return true;
        }
    }
}
