namespace Sonaris.Services.Playlists;

using Sonaris.Domain.DTOs.Playlist;

public interface IPlaylistService
{
    List<PlaylistDto> GetAll();
    PlaylistDto GetById(string id);
    PlaylistDto Create(string name);
    PlaylistDto Rename(string id, string name);
    void Delete(string id);
    PlaylistTrackDto AddTrack(string playlistId, string relativePath);
    void RemoveTrack(string playlistId, long trackId);
    void ReorderTrack(string playlistId, long trackId, int newPosition);
    void Duplicate(string id, string newName);
}
