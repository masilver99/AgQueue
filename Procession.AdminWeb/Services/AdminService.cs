// <copyright file="AdminService.cs" company="Michael Silver">
// Copyright (c) Michael Silver. All rights reserved.
// </copyright>

using Procession.Common;
using Procession.Common.Models;
using Procession.Server.Common;
using Procession.Server.Common.Models;

namespace Procession.AdminWeb.Services;

/// <summary>
/// Service providing data access for the admin interface.
/// </summary>
public class AdminService
{
    private readonly IStorage storage;

    /// <summary>
    /// Initializes a new instance of the <see cref="AdminService"/> class.
    /// </summary>
    /// <param name="storage">The storage provider.</param>
    public AdminService(IStorage storage)
    {
        this.storage = storage;
    }

    /// <summary>
    /// Gets all queues in the system.
    /// </summary>
    /// <returns>List of all queues.</returns>
    public async Task<List<QueueInfo>> GetAllQueuesAsync()
    {
        return await storage.GetAllQueues();
    }

    /// <summary>
    /// Gets message statistics by state.
    /// </summary>
    /// <returns>Dictionary with message states and counts.</returns>
    public async Task<Dictionary<MessageState, int>> GetMessageStatisticsAsync()
    {
        return await storage.GetMessageStatistics();
    }

    /// <summary>
    /// Gets paginated messages with optional filtering.
    /// </summary>
    /// <param name="page">Page number (1-based).</param>
    /// <param name="pageSize">Number of messages per page.</param>
    /// <param name="queueId">Optional queue ID filter.</param>
    /// <param name="processedOnly">Optional processed state filter.</param>
    /// <returns>List of messages for the specified page.</returns>
    public async Task<List<Message>> GetMessagesAsync(int page, int pageSize, long? queueId = null, bool? processedOnly = null)
    {
        var offset = (page - 1) * pageSize;
        return await storage.GetMessages(offset, pageSize, queueId, processedOnly);
    }

    /// <summary>
    /// Gets the total count of messages matching the filter criteria.
    /// </summary>
    /// <param name="queueId">Optional queue ID filter.</param>
    /// <param name="processedOnly">Optional processed state filter.</param>
    /// <returns>Total count of matching messages.</returns>
    public async Task<int> GetMessageCountAsync(long? queueId = null, bool? processedOnly = null)
    {
        return await storage.GetMessageCount(queueId, processedOnly);
    }

    /// <summary>
    /// Gets a specific message by ID.
    /// </summary>
    /// <param name="messageId">The message ID.</param>
    /// <returns>The message or null if not found.</returns>
    public async Task<Message?> GetMessageByIdAsync(long messageId)
    {
        return await storage.PeekMessageByMessageId(messageId);
    }

    /// <summary>
    /// Gets queue information by ID.
    /// </summary>
    /// <param name="queueId">The queue ID.</param>
    /// <returns>Queue information or null if not found.</returns>
    public async Task<QueueInfo?> GetQueueByIdAsync(long queueId)
    {
        return await storage.GetQueueInfoById(queueId);
    }

    /// <summary>
    /// Gets database type information (for display purposes).
    /// </summary>
    /// <returns>String describing the database type.</returns>
    public string GetDatabaseType()
    {
        var storageType = storage.GetType().Name;
        return storageType switch
        {
            "StorageSqlite" => "SQLite",
            "StoragePostgres" => "PostgreSQL",
            _ => "Unknown"
        };
    }
}