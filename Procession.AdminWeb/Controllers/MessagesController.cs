// <copyright file="MessagesController.cs" company="Michael Silver">
// Copyright (c) Michael Silver. All rights reserved.
// </copyright>

using Microsoft.AspNetCore.Mvc;
using Procession.AdminWeb.Models;
using Procession.AdminWeb.Services;

namespace Procession.AdminWeb.Controllers;

/// <summary>
/// Controller for message management and viewing.
/// </summary>
public class MessagesController : Controller
{
    private readonly AdminService adminService;

    /// <summary>
    /// Initializes a new instance of the <see cref="MessagesController"/> class.
    /// </summary>
    /// <param name="adminService">The admin service.</param>
    public MessagesController(AdminService adminService)
    {
        this.adminService = adminService;
    }

    /// <summary>
    /// Shows paginated list of messages with filtering options.
    /// </summary>
    /// <param name="page">Page number (1-based).</param>
    /// <param name="queueId">Optional queue ID filter.</param>
    /// <param name="processedOnly">Optional processed state filter.</param>
    /// <returns>Message list view.</returns>
    public async Task<IActionResult> Index(int page = 1, long? queueId = null, bool? processedOnly = null)
    {
        const int pageSize = 50;

        var model = new MessageListViewModel
        {
            CurrentPage = page,
            PageSize = pageSize,
            SelectedQueueId = queueId,
            ProcessedOnly = processedOnly,
            Messages = await adminService.GetMessagesAsync(page, pageSize, queueId, processedOnly),
            TotalCount = await adminService.GetMessageCountAsync(queueId, processedOnly),
            AvailableQueues = await adminService.GetAllQueuesAsync()
        };

        return View(model);
    }

    /// <summary>
    /// Shows detailed information for a specific message.
    /// </summary>
    /// <param name="id">The message ID.</param>
    /// <param name="showPayload">Whether to show the payload content.</param>
    /// <returns>Message detail view.</returns>
    public async Task<IActionResult> Details(long id, bool showPayload = false)
    {
        var message = await adminService.GetMessageByIdAsync(id);
        if (message == null)
        {
            return NotFound();
        }

        var queue = await adminService.GetQueueByIdAsync(message.QueueId);

        var model = new MessageDetailViewModel
        {
            Message = message,
            Queue = queue,
            ShowPayload = showPayload
        };

        return View(model);
    }
}