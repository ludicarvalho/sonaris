namespace Sonaris.Services.Playlists;

using Sonaris.Domain.DTOs.Playlist;

public interface IPlaylistService
{
    List<PlaylistDto> GetAll(string userId);
    PlaylistDto GetById(string userId, string id);
    PlaylistDto Create(string userId, string name);
    PlaylistDto Rename(string userId, string id, string name);
    void Delete(string userId, string id);
    PlaylistTrackDto AddTrack(string userId, string playlistId, string relativePath);
    void RemoveTrack(string userId, string playlistId, long trackId);
    void ReorderTrack(string userId, string playlistId, long trackId, int newPosition);
    void Duplicate(string userId, string id, string newName);
}
