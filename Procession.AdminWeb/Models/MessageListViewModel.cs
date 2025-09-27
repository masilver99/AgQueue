// <copyright file="MessageListViewModel.cs" company="Michael Silver">
// Copyright (c) Michael Silver. All rights reserved.
// </copyright>

using Procession.Common.Models;
using Procession.Server.Common.Models;

namespace Procession.AdminWeb.Models;

/// <summary>
/// View model for the message list page with paging and filtering.
/// </summary>
public class MessageListViewModel
{
    /// <summary>
    /// Gets or sets the list of messages for the current page.
    /// </summary>
    public List<Message> Messages { get; set; } = new List<Message>();

    /// <summary>
    /// Gets or sets the current page number (1-based).
    /// </summary>
    public int CurrentPage { get; set; } = 1;

    /// <summary>
    /// Gets or sets the page size.
    /// </summary>
    public int PageSize { get; set; } = 50;

    /// <summary>
    /// Gets or sets the total count of messages matching the filter.
    /// </summary>
    public int TotalCount { get; set; }

    /// <summary>
    /// Gets or sets the selected queue ID for filtering (null for all queues).
    /// </summary>
    public long? SelectedQueueId { get; set; }

    /// <summary>
    /// Gets or sets the processed filter (null for all, true for processed only, false for unprocessed only).
    /// </summary>
    public bool? ProcessedOnly { get; set; }

    /// <summary>
    /// Gets or sets the list of available queues for the filter dropdown.
    /// </summary>
    public List<QueueInfo> AvailableQueues { get; set; } = new List<QueueInfo>();

    /// <summary>
    /// Gets the total number of pages.
    /// </summary>
    public int TotalPages => (int)Math.Ceiling((double)this.TotalCount / this.PageSize);

    /// <summary>
    /// Gets a value indicating whether there is a previous page.
    /// </summary>
    public bool HasPreviousPage => this.CurrentPage > 1;

    /// <summary>
    /// Gets a value indicating whether there is a next page.
    /// </summary>
    public bool HasNextPage => this.CurrentPage < this.TotalPages;
}