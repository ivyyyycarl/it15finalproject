using System.Collections.Concurrent;
using INTEGRATE_SUPPORT_SALES_MANAGEMENT_SYSTEM.DTOs;
using INTEGRATE_SUPPORT_SALES_MANAGEMENT_SYSTEM.Models;
using INTEGRATE_SUPPORT_SALES_MANAGEMENT_SYSTEM.Services;
using Microsoft.AspNetCore.SignalR;

namespace INTEGRATE_SUPPORT_SALES_MANAGEMENT_SYSTEM.Hubs;

public class SupportCallHub : Hub
{
    private static readonly ConcurrentDictionary<string, ConnectedUser> ConnectedUsers = new();
    private static readonly ConcurrentDictionary<string, SupportCallSession> Sessions = new();
    private static readonly ConcurrentDictionary<string, string> ConnectionToUserKey = new();
    private static readonly ConcurrentDictionary<string, CancellationTokenSource> RingTimeouts = new();
    private static readonly TimeSpan RingTimeoutDuration = TimeSpan.FromSeconds(30);

    private readonly ICustomerService _customerService;
    private readonly ICallService _callService;

    public SupportCallHub(ICustomerService customerService, ICallService callService)
    {
        _customerService = customerService;
        _callService = callService;
    }

    public async Task RegisterUser(int userId, string role, string displayName)
    {
        var userKey = BuildUserKey(userId, role);
        var user = new ConnectedUser
        {
            UserId = userId,
            Role = role,
            DisplayName = string.IsNullOrWhiteSpace(displayName) ? $"{role} {userId}" : displayName,
            ConnectionId = Context.ConnectionId
        };

        ConnectedUsers[userKey] = user;
        ConnectionToUserKey[Context.ConnectionId] = userKey;

        await Groups.AddToGroupAsync(Context.ConnectionId, role.Equals("Agent", StringComparison.OrdinalIgnoreCase) ? "Agents" : "Customers");
        await BroadcastAvailabilityAsync();
        await BroadcastQueueAsync();
    }

    public Task<SupportAvailabilitySnapshot> GetSupportAvailability()
    {
        return Task.FromResult(GetAvailabilitySnapshot());
    }

    public Task<SupportQueueSnapshot> GetSupportQueue()
    {
        return Task.FromResult(GetQueueSnapshot());
    }

    public async Task<string> StartSupportCall(int customerUserId, string customerName, int? ticketId)
    {
        if (Sessions.Values.Any(s => s.CustomerUserId == customerUserId && (s.Status == "Pending" || s.Status == "Accepted" || s.Status == "Connected")))
        {
            throw new HubException("You already have an ongoing support call.");
        }

        var availableAgentConnectionIds = ConnectedUsers.Values
            .Where(u => u.Role.Equals("Agent", StringComparison.OrdinalIgnoreCase))
            .Where(u => !Sessions.Values.Any(s => s.AgentUserId == u.UserId && (s.Status == "Accepted" || s.Status == "Connected")))
            .Select(u => u.ConnectionId)
            .Distinct()
            .ToList();

        if (!availableAgentConnectionIds.Any())
        {
            throw new HubException("No available agents right now. Please try again in a moment.");
        }

        var session = new SupportCallSession
        {
            CallId = Guid.NewGuid().ToString("N"),
            CustomerUserId = customerUserId,
            CustomerName = customerName,
            CustomerConnectionId = Context.ConnectionId,
            TicketId = ticketId,
            Status = "Pending"
        };

        Sessions[session.CallId] = session;

        foreach (var agentConnectionId in availableAgentConnectionIds)
        {
            await Clients.Client(agentConnectionId).SendAsync("IncomingSupportCall",
                session.CallId,
                session.CustomerUserId,
                session.CustomerName,
                session.TicketId);
        }

        StartRingTimeout(session.CallId);
        await BroadcastQueueAsync();
        return session.CallId;
    }

    public async Task AcceptSupportCall(string callId, int agentUserId, string agentName)
    {
        if (!Sessions.TryGetValue(callId, out var session))
        {
            throw new HubException("Call session no longer exists.");
        }

        if (session.Status != "Pending")
        {
            throw new HubException("This call is already handled by another agent.");
        }

        if (Sessions.Values.Any(s => s.AgentUserId == agentUserId && (s.Status == "Accepted" || s.Status == "Connected")))
        {
            throw new HubException("You are already in another active support call.");
        }

        session.AgentUserId = agentUserId;
        session.AgentName = string.IsNullOrWhiteSpace(agentName) ? $"Agent {agentUserId}" : agentName;
        session.AgentConnectionId = Context.ConnectionId;
        session.Status = "Accepted";

        CancelRingTimeout(callId);

        // Persist support call entry in call logs.
        var customer = await _customerService.GetCustomerByUserIdAsync(session.CustomerUserId);
        if (customer != null)
        {
            var createdCall = await _callService.CreateCallAsync(new CreateCallDto
            {
                CustomerId = customer.Id,
                AgentId = agentUserId,
                Type = CallType.Inbound,
                Subject = "Customer Support Voice Follow-up",
                Notes = session.TicketId.HasValue ? $"Related ticket ID: {session.TicketId.Value}" : "Voice follow-up from customer support page."
            });
            session.PersistedCallId = createdCall.Id;
            await _callService.StartCallAsync(createdCall.Id);
        }

        await Clients.Client(session.CustomerConnectionId).SendAsync("SupportCallAccepted", callId, agentUserId, session.AgentName);
        await Clients.Group("Agents").SendAsync("SupportCallHandled", callId);
        await BroadcastAvailabilityAsync();
        await BroadcastQueueAsync();
    }

    public async Task DeclineSupportCall(string callId, string reason)
    {
        if (!Sessions.TryGetValue(callId, out var session))
        {
            return;
        }

        CancelRingTimeout(callId);
        await Clients.Client(session.CustomerConnectionId).SendAsync("SupportCallDeclined", callId, reason);
        Sessions.TryRemove(callId, out _);
        await Clients.Group("Agents").SendAsync("SupportCallHandled", callId);
        await BroadcastAvailabilityAsync();
        await BroadcastQueueAsync();
    }

    public async Task SendOffer(string callId, string offerSdp)
    {
        if (!Sessions.TryGetValue(callId, out var session) || string.IsNullOrWhiteSpace(session.AgentConnectionId))
        {
            return;
        }

        await Clients.Client(session.AgentConnectionId).SendAsync("ReceiveOffer", callId, offerSdp);
    }

    public async Task SendAnswer(string callId, string answerSdp)
    {
        if (!Sessions.TryGetValue(callId, out var session))
        {
            return;
        }

        session.Status = "Connected";
        await Clients.Client(session.CustomerConnectionId).SendAsync("ReceiveAnswer", callId, answerSdp);
    }

    public async Task SendIceCandidate(string callId, string candidateJson)
    {
        if (!Sessions.TryGetValue(callId, out var session))
        {
            return;
        }

        var targetConnectionId = Context.ConnectionId == session.CustomerConnectionId
            ? session.AgentConnectionId
            : session.CustomerConnectionId;

        if (!string.IsNullOrWhiteSpace(targetConnectionId))
        {
            await Clients.Client(targetConnectionId).SendAsync("ReceiveIceCandidate", callId, candidateJson);
        }
    }

    public async Task EndSupportCall(string callId, string reason)
    {
        if (!Sessions.TryRemove(callId, out var session))
        {
            return;
        }

        CancelRingTimeout(callId);

        if (session.PersistedCallId.HasValue)
        {
            await _callService.UpdateCallAsync(session.PersistedCallId.Value, new UpdateCallDto
            {
                Outcome = reason,
                Notes = $"Voice call ended. Reason: {reason}"
            });
            await _callService.EndCallAsync(session.PersistedCallId.Value);

            if (IsMissedLikeReason(reason))
            {
                await _callService.UpdateCallAsync(session.PersistedCallId.Value, new UpdateCallDto
                {
                    Status = CallStatus.Missed
                });
            }
        }

        if (!string.IsNullOrWhiteSpace(session.CustomerConnectionId))
        {
            await Clients.Client(session.CustomerConnectionId).SendAsync("SupportCallEnded", callId, reason);
        }

        if (!string.IsNullOrWhiteSpace(session.AgentConnectionId))
        {
            await Clients.Client(session.AgentConnectionId).SendAsync("SupportCallEnded", callId, reason);
        }

        await Clients.Group("Agents").SendAsync("SupportCallHandled", callId);
        await BroadcastAvailabilityAsync();
        await BroadcastQueueAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        if (ConnectionToUserKey.TryRemove(Context.ConnectionId, out var userKey))
        {
            ConnectedUsers.TryRemove(userKey, out _);
        }

        var affectedSessions = Sessions.Values
            .Where(s => s.CustomerConnectionId == Context.ConnectionId || s.AgentConnectionId == Context.ConnectionId)
            .Select(s => s.CallId)
            .ToList();

        foreach (var callId in affectedSessions)
        {
            await EndSupportCall(callId, "Call ended due to disconnected user.");
        }

        await BroadcastAvailabilityAsync();
        await BroadcastQueueAsync();

        await base.OnDisconnectedAsync(exception);
    }

    private void StartRingTimeout(string callId)
    {
        var cts = new CancellationTokenSource();
        RingTimeouts[callId] = cts;

        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(RingTimeoutDuration, cts.Token);
                if (cts.IsCancellationRequested)
                {
                    return;
                }

                if (Sessions.TryGetValue(callId, out var session) && session.Status == "Pending")
                {
                    Sessions.TryRemove(callId, out _);
                    await Clients.Client(session.CustomerConnectionId).SendAsync("SupportCallDeclined", callId, "No agent answered. Please try again.");
                    await Clients.Group("Agents").SendAsync("SupportCallHandled", callId);
                    await BroadcastAvailabilityAsync();
                    await BroadcastQueueAsync();
                }
            }
            catch (TaskCanceledException)
            {
                // expected on cancel
            }
            finally
            {
                CancelRingTimeout(callId);
            }
        });
    }

    private static void CancelRingTimeout(string callId)
    {
        if (RingTimeouts.TryRemove(callId, out var cts))
        {
            cts.Cancel();
            cts.Dispose();
        }
    }

    private static bool IsMissedLikeReason(string reason)
    {
        var value = reason?.ToLowerInvariant() ?? string.Empty;
        return value.Contains("declined") || value.Contains("no agent") || value.Contains("timeout") || value.Contains("disconnected");
    }

    private static string BuildUserKey(int userId, string role) => $"{role}:{userId}";

    private Task BroadcastAvailabilityAsync()
    {
        var snapshot = GetAvailabilitySnapshot();
        return Clients.Group("Customers").SendAsync("SupportAvailabilityChanged", snapshot.OnlineAgents, snapshot.AvailableAgents);
    }

    private Task BroadcastQueueAsync()
    {
        var queue = GetQueueSnapshot();
        return Clients.Group("Agents").SendAsync("SupportQueueChanged", queue.WaitingCustomers, queue.OnlineCustomers, queue.ActiveCalls);
    }

    private static SupportAvailabilitySnapshot GetAvailabilitySnapshot()
    {
        var onlineAgentIds = ConnectedUsers.Values
            .Where(u => u.Role.Equals("Agent", StringComparison.OrdinalIgnoreCase))
            .Select(u => u.UserId)
            .Distinct()
            .ToList();

        var busyAgentIds = Sessions.Values
            .Where(s => s.AgentUserId.HasValue && (s.Status == "Accepted" || s.Status == "Connected"))
            .Select(s => s.AgentUserId!.Value)
            .Distinct()
            .ToHashSet();

        var availableAgents = onlineAgentIds.Count(id => !busyAgentIds.Contains(id));
        return new SupportAvailabilitySnapshot
        {
            OnlineAgents = onlineAgentIds.Count,
            AvailableAgents = availableAgents
        };
    }

    private static SupportQueueSnapshot GetQueueSnapshot()
    {
        var waitingCustomers = Sessions.Values.Count(s => s.Status == "Pending");
        var activeCalls = Sessions.Values.Count(s => s.Status == "Accepted" || s.Status == "Connected");
        var onlineCustomers = ConnectedUsers.Values
            .Where(u => u.Role.Equals("Customer", StringComparison.OrdinalIgnoreCase))
            .Select(u => u.UserId)
            .Distinct()
            .Count();

        return new SupportQueueSnapshot
        {
            WaitingCustomers = waitingCustomers,
            OnlineCustomers = onlineCustomers,
            ActiveCalls = activeCalls
        };
    }

    private sealed class ConnectedUser
    {
        public int UserId { get; set; }
        public string Role { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string ConnectionId { get; set; } = string.Empty;
    }

    private sealed class SupportCallSession
    {
        public string CallId { get; set; } = string.Empty;
        public int CustomerUserId { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public string CustomerConnectionId { get; set; } = string.Empty;
        public int? TicketId { get; set; }
        public int? AgentUserId { get; set; }
        public string? AgentName { get; set; }
        public string? AgentConnectionId { get; set; }
        public string Status { get; set; } = "Pending";
        public int? PersistedCallId { get; set; }
    }

    public sealed class SupportAvailabilitySnapshot
    {
        public int OnlineAgents { get; set; }
        public int AvailableAgents { get; set; }
    }

    public sealed class SupportQueueSnapshot
    {
        public int WaitingCustomers { get; set; }
        public int OnlineCustomers { get; set; }
        public int ActiveCalls { get; set; }
    }
}
