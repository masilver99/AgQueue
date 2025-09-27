// <copyright file="DashboardViewModel.cs" company="Michael Silver">
// Copyright (c) Michael Silver. All rights reserved.
// </copyright>

using Procession.Common;
using Procession.Common.Models;
using Procession.Server.Common.Models;

namespace Procession.AdminWeb.Models;

/// <summary>
/// View model for the dashboard page containing statistics and configuration info.
/// </summary>
public class DashboardViewModel
{
    /// <summary>
    /// Gets or sets the list of all queues.
    /// </summary>
    public List<QueueInfo> Queues { get; set; } = new();

    /// <summary>
    /// Gets or sets message statistics by state.
    /// </summary>
    public Dictionary<MessageState, int> MessageStatistics { get; set; } = new();

    /// <summary>
    /// Gets or sets the database type being used.
    /// </summary>
    public string DatabaseType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets recent messages for the summary list.
    /// </summary>
    public List<Message> RecentMessages { get; set; } = new();

    /// <summary>
    /// Gets or sets the total message count.
    /// </summary>
    public int TotalMessageCount { get; set; }

    /// <summary>
    /// Gets or sets the active message count (not processed).
    /// </summary>
    public int ActiveMessageCount { get; set; }

    /// <summary>
    /// Gets or sets the processed message count.
    /// </summary>
    public int ProcessedMessageCount { get; set; }
}