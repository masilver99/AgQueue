// <copyright file="MessageDetailViewModel.cs" company="Michael Silver">
// Copyright (c) Michael Silver. All rights reserved.
// </copyright>

using Procession.Common.Models;
using Procession.Server.Common.Models;

namespace Procession.AdminWeb.Models;

/// <summary>
/// View model for the message detail page showing full message information.
/// </summary>
public class MessageDetailViewModel
{
    /// <summary>
    /// Gets or sets the message details.
    /// </summary>
    public Message? Message { get; set; }

    /// <summary>
    /// Gets or sets the queue information.
    /// </summary>
    public QueueInfo? Queue { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to show the payload content.
    /// </summary>
    public bool ShowPayload { get; set; }

    /// <summary>
    /// Gets the payload as a UTF-8 string if possible, otherwise returns hex representation.
    /// </summary>
    public string PayloadString
    {
        get
        {
            if (this.Message?.Payload == null)
            {
                return string.Empty;
            }

            try
            {
                return System.Text.Encoding.UTF8.GetString(this.Message.Payload);
            }
            catch
            {
                return BitConverter.ToString(this.Message.Payload).Replace("-", " ");
            }
        }
    }

    /// <summary>
    /// Gets the formatted add date time.
    /// </summary>
    public DateTime AddDateTime => DateTimeOffset.FromUnixTimeSeconds(this.Message?.AddDateTime ?? 0).DateTime;

    /// <summary>
    /// Gets the formatted close date time, if available.
    /// </summary>
    public DateTime? CloseDateTime => this.Message?.CloseDateTime.HasValue == true
        ? DateTimeOffset.FromUnixTimeSeconds(this.Message.CloseDateTime.Value).DateTime
        : null;

    /// <summary>
    /// Gets the formatted expiry date time, if available.
    /// </summary>
    public DateTime? ExpiryDateTime => this.Message?.ExpiryDateTime.HasValue == true
        ? DateTimeOffset.FromUnixTimeSeconds(this.Message.ExpiryDateTime.Value).DateTime
        : null;
}