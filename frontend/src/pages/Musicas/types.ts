export interface FileSystemItem {
  Name: string;
  RelativePath: string;
  IsDirectory: boolean;
  Size: number | null;
  LastModified: string;
}

export interface MusicMetadata {
  Title: string | null;
  Artist: string | null;
  Album: string | null;
  Track: string | null;
  Year: string | null;
  Duration: string | null;
  Bitrate: number | null;
}

export interface MusicSearchResult {
  Id: number;
  Title: string;
  Artist: string;
  Album: string;
  Filename: string;
  RelativePath: string;
  Rank: number;
  Snippet: string;
  MatchSource: string;
}

export interface Playlist {
  Id: string;
  Name: string;
  CreatedAt: string;
  UpdatedAt: string;
  Tracks: PlaylistTrack[];
}

export interface PlaylistTrack {
  Id: number;
  PlaylistId: string;
  RelativePath: string;
  Title: string;
  Artist: string;
  Album: string;
  Position: number;
  AddedAt: string;
}

export function arquivoDePath(relativePath: string): FileSystemItem {
  return {
    Name: relativePath.split('/').pop() ?? '',
    RelativePath: relativePath,
    IsDirectory: false,
    Size: null,
    LastModified: '',
  };
}
