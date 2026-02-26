using System.Net.Http.Json;
using Hubbly.Mobile.Models;
using Hubbly.Mobile.Services;
using Microsoft.Extensions.Logging;

namespace Hubbly.Mobile.Services;

public class RoomService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<RoomService> _logger;
    private readonly TokenManager _tokenManager;

    public RoomService(HttpClient httpClient, ILogger<RoomService> logger, TokenManager tokenManager)
    {
        _httpClient = httpClient;
        _logger = logger;
        _tokenManager = tokenManager;
    }

    /// <summary>
    /// Получить список активных комнат
    /// </summary>
    public async Task<IEnumerable<RoomInfoDto>> GetRoomsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetAsync("/api/rooms", cancellationToken);
            response.EnsureSuccessStatusCode();

            var rooms = await response.Content.ReadFromJsonAsync<List<ServerRoomInfoDto>>(cancellationToken: cancellationToken);
            return rooms?.Select(MapToClientDto) ?? Enumerable.Empty<RoomInfoDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get rooms");
            return Enumerable.Empty<RoomInfoDto>();
        }
    }

    /// <summary>
    /// Получить детали комнаты по ID
    /// </summary>
    public async Task<RoomInfoDto?> GetRoomAsync(Guid roomId, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetAsync($"/api/rooms/{roomId}", cancellationToken);
            response.EnsureSuccessStatusCode();

            var serverDto = await response.Content.ReadFromJsonAsync<ServerRoomInfoDto>(cancellationToken: cancellationToken);
            return serverDto != null ? MapToClientDto(serverDto) : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get room {RoomId}", roomId);
            return null;
        }
    }

    /// <summary>
    /// Создать новую пользовательскую комнату
    /// </summary>
    public async Task<RoomAssignmentData?> CreateRoomAsync(string name, string? description = null, int? maxUsers = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var request = new
            {
                name = name,
                description = description,
                maxUsers = maxUsers ?? 10
            };

            var response = await _httpClient.PostAsJsonAsync("/api/rooms", request, cancellationToken);
            response.EnsureSuccessStatusCode();

            return await response.Content.ReadFromJsonAsync<RoomAssignmentData>(cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create room");
            return null;
        }
    }

    /// <summary>
    /// Присоединиться к комнате
    /// </summary>
    public async Task<RoomAssignmentData?> JoinRoomAsync(Guid roomId, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.PostAsync($"/api/rooms/{roomId}/join", null, cancellationToken);
            response.EnsureSuccessStatusCode();

            return await response.Content.ReadFromJsonAsync<RoomAssignmentData>(cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to join room {RoomId}", roomId);
            return null;
        }
    }

    /// <summary>
    /// Покинуть комнату (только для авторизованных пользователей)
    /// </summary>
    public async Task<bool> LeaveRoomAsync(Guid roomId, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.PostAsync($"/api/rooms/{roomId}/leave", null, cancellationToken);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to leave room {RoomId}", roomId);
            return false;
        }
    }

    /// <summary>
    /// Маппинг серверного DTO в клиентский
    /// </summary>
    private static RoomInfoDto MapToClientDto(ServerRoomInfoDto serverDto)
    {
        return new RoomInfoDto
        {
            RoomId = serverDto.RoomId,
            RoomName = serverDto.RoomName,
            Description = serverDto.Description,
            Type = serverDto.Type,
            CurrentUsers = serverDto.CurrentUsers,
            MaxUsers = serverDto.MaxUsers,
            IsPrivate = serverDto.IsPrivate,
            CreatedAt = serverDto.CreatedAt,
            LastActiveAt = serverDto.LastActiveAt
        };
    }
}

/// <summary>
/// Server DTO - соответствует серверному RoomInfoDto
/// </summary>
internal class ServerRoomInfoDto
{
    public Guid RoomId { get; set; }
    public string RoomName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int Type { get; set; }
    public int CurrentUsers { get; set; }
    public int MaxUsers { get; set; }
    public bool IsPrivate { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset LastActiveAt { get; set; }
}