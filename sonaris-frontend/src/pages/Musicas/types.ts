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
