module Endpoints

open HttpMethods
open Parsers

let BASE_URL = "https://api.spotify.com/v1"

type Item =
    | Album
    | Artist
    | Playlist
    | Track

    member this.queryParam =
        match this with
        | Album -> "album"
        | Artist -> "artist"
        | Playlist -> "playlist"
        | Track -> "track"

let searchAlbum (strToSearch: string) (limit: int option) (offset: int option) =
    let boundLimit =
        match limit with
        | Some l -> l |> min 50 |> max 0
        | None -> 1

    let boundOffset =
        match offset with
        | Some o -> o |> max 0
        | None -> 0

    sprintf "%s/search?q=%s&type=track&limit=%d&offset=%d" BASE_URL strToSearch boundLimit boundOffset
    |> GET
    |> parseResponse<AlbumSearch>

let searchArtist (strToSearch: string) (limit: int option) (offset: int option) =
    let boundLimit =
        match limit with
        | Some l -> l |> min 50 |> max 0
        | None -> 1

    let boundOffset =
        match offset with
        | Some o -> o |> max 0
        | None -> 0

    sprintf "%s/search?q=%s&type=track&limit=%d&offset=%d" BASE_URL strToSearch boundLimit boundOffset
    |> GET
    |> parseResponse<ArtistSearch>

let searchPlaylist (strToSearch: string) (limit: int option) (offset: int option) =
    let boundLimit =
        match limit with
        | Some l -> l |> min 50 |> max 0
        | None -> 1

    let boundOffset =
        match offset with
        | Some o -> o |> max 0
        | None -> 0

    sprintf "%s/search?q=%s&type=track&limit=%d&offset=%d" BASE_URL strToSearch boundLimit boundOffset
    |> GET
    |> parseResponse<PlaylistSearch>

let searchTrack (strToSearch: string) (limit: int option) (offset: int option) =
    let boundLimit =
        match limit with
        | Some l -> l |> min 50 |> max 0
        | None -> 1

    let boundOffset =
        match offset with
        | Some o -> o |> max 0
        | None -> 0

    sprintf "%s/search?q=%s&type=track&limit=%d&offset=%d" BASE_URL strToSearch boundLimit boundOffset
    |> GET
    |> parseResponse<TrackSearch>
