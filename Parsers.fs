module Parsers

open System.Text.Json

type PagesOf<'Item> =
    { limit: int
      next: string
      offset: int
      previous: string
      total: int
      items: list<'Item> }

type Artist = { id: string; name: string }

type Album =
    { album_type: string
      total_tracks: int
      id: string
      name: string
      release_date: string
      artists: list<Artist> }

type Track =
    { artists: list<Artist>
      album: Album
      disc_number: int
      duration_ms: int
      id: string
      name: string
      track_number: int }

type AlbumTrack =
    { artists: list<Artist>
      disc_number: int
      duration_ms: int
      id: string
      name: string
      track_number: int }


type SavedTrack = { added_at: string; track: Track }

type Playlist =
    { id: string
      name: string
      tracks: PagesOf<SavedTrack> }

type TrackSearch = { tracks: PagesOf<Track> }
type AlbumSearch = { albums: PagesOf<Album> }
type ArtistSearch = { artists: PagesOf<Artist> }
type PlaylistSearch = { playlists: PagesOf<Playlist> }


type ParsedItem =
    { idx: int
      id: string
      track: string
      album: string
      artist: string }

type ParsedResponse = list<ParsedItem>

let parseResponse<'T> (APIresponse: string) =
    JsonSerializer.Deserialize<'T> APIresponse
