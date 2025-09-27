// <copyright file="DashboardController.cs" company="Michael Silver">
// Copyright (c) Michael Silver. All rights reserved.
// </copyright>

using Microsoft.AspNetCore.Mvc;
using Procession.AdminWeb.Models;
using Procession.AdminWeb.Services;
using Procession.Common;

namespace Procession.AdminWeb.Controllers;

/// <summary>
/// Controller for the dashboard page showing system statistics.
/// </summary>
public class DashboardController : Controller
{
    private readonly AdminService adminService;

    /// <summary>
    /// Initializes a new instance of the <see cref="DashboardController"/> class.
    /// </summary>
    /// <param name="adminService">The admin service.</param>
    public DashboardController(AdminService adminService)
    {
        this.adminService = adminService;
    }

    /// <summary>
    /// Shows the dashboard with system statistics and configuration.
    /// </summary>
    /// <returns>Dashboard view.</returns>
    public async Task<IActionResult> Index()
    {
        var model = new DashboardViewModel
        {
            Queues = await adminService.GetAllQueuesAsync(),
            MessageStatistics = await adminService.GetMessageStatisticsAsync(),
            DatabaseType = adminService.GetDatabaseType(),
            RecentMessages = await adminService.GetMessagesAsync(1, 20), // Get 20 most recent messages
            TotalMessageCount = await adminService.GetMessageCountAsync(),
            ActiveMessageCount = await adminService.GetMessageCountAsync(processedOnly: false),
            ProcessedMessageCount = await adminService.GetMessageCountAsync(processedOnly: true)
        };

        return View(model);
    }

    /// <summary>
    /// Shows error page.
    /// </summary>
    /// <returns>Error view.</returns>
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = System.Diagnostics.Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}