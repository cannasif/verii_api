using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net;
using System.Net.Mail;
using V3RII.Application.Interfaces;
using V3RII.Infrastructure.Options;
using V3RII.Infrastructure.Persistence;

namespace V3RII.Infrastructure.Services;

public sealed class MailOutboxProcessor(V3RiiDbContext dbContext, ILogger<MailOutboxProcessor> logger, IOptions<MailOptions> mailOptions) : IMailOutboxProcessor
{
    private readonly MailOptions _mailOptions = mailOptions.Value;

    public async Task ProcessPendingAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;
        var items = await dbContext.MailOutboxItems
            .Where(x => x.SentAt == null && (x.NextAttemptAt == null || x.NextAttemptAt <= now))
            .OrderBy(x => x.CreatedAt)
            .Take(20)
            .ToListAsync(cancellationToken);

        foreach (var item in items)
        {
            try
            {
                if (_mailOptions.Enabled)
                {
                    using var message = new MailMessage(_mailOptions.From, item.To, item.Subject, item.BodyHtml)
                    {
                        IsBodyHtml = true
                    };
                    using var client = new SmtpClient(_mailOptions.Host, _mailOptions.Port)
                    {
                        EnableSsl = _mailOptions.EnableSsl,
                        Credentials = string.IsNullOrWhiteSpace(_mailOptions.UserName)
                            ? CredentialCache.DefaultNetworkCredentials
                            : new NetworkCredential(_mailOptions.UserName, _mailOptions.Password)
                    };
                    await client.SendMailAsync(message, cancellationToken);
                }
                else
                {
                    logger.LogInformation("Mail dry-run for outbox item {OutboxId} to {Recipient}. Subject: {Subject}", item.Id, item.To, item.Subject);
                }
                item.SentAt = now;
                item.LastError = null;
            }
            catch (Exception ex)
            {
                item.AttemptCount++;
                item.LastError = ex.Message;
                item.NextAttemptAt = now.AddMinutes(Math.Min(60, Math.Pow(2, item.AttemptCount)));
            }
        }

        if (items.Count > 0)
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}
