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

let bindLimitOffset (maxLimit: int) (limit: int) (offset: int) =
    limit |> min maxLimit |> max 0, offset |> max 0

let prependPagesOf<'T> (itemsToPrepend: list<'T>) (page: PagesOf<'T>) =
    { page with
        items = itemsToPrepend @ page.items }

let searchAlbum (limit: int) (offset: int) (strToSearch: string) =
    let bLimit, bOffset = bindLimitOffset 50 limit offset

    sprintf "%s/search?q=%s&type=track&limit=%d&offset=%d" BASE_URL strToSearch bLimit bOffset
    |> GET
    |> parseResponse<AlbumSearch>

let searchArtist (limit: int) (offset: int) (strToSearch: string) =
    let bLimit, bOffset = bindLimitOffset 50 limit offset

    sprintf "%s/search?q=%s&type=track&limit=%d&offset=%d" BASE_URL strToSearch bLimit bOffset
    |> GET
    |> parseResponse<ArtistSearch>

let searchPlaylist (limit: int) (offset: int) (strToSearch: string) =
    let bLimit, bOffset = bindLimitOffset 50 limit offset

    sprintf "%s/search?q=%s&type=track&limit=%d&offset=%d" BASE_URL strToSearch bLimit bOffset
    |> GET
    |> parseResponse<PlaylistSearch>

let searchTrack (limit: int) (offset: int) (strToSearch: string) =
    let bLimit, bOffset = bindLimitOffset 50 limit offset

    sprintf "%s/search?q=%s&type=track&limit=%d&offset=%d" BASE_URL strToSearch bLimit bOffset
    |> GET
    |> parseResponse<TrackSearch>

let getArtistAlbum (limit: int) (offset: int) (artistId: string) =
    let bLimit, bOffset = bindLimitOffset 50 limit offset

    sprintf "%s/artists/%s/albums?include_groups=album&limit=%d&offset=%d" BASE_URL artistId bLimit bOffset
    |> GET
    |> parseResponse<PagesOf<Album>>

let rec getAllArtistAlbums (artistId: string) (page: PagesOf<Album> option) =
    let previousPage =
        match page with
        | Some page -> page
        | None -> getArtistAlbum 50 0 artistId

    match previousPage.next with
    | null -> previousPage
    | url ->
        GET url
        |> parseResponse<PagesOf<Album>>
        |> prependPagesOf<Album> previousPage.items
        |> Some
        |> getAllArtistAlbums artistId

let rec getAllAlbumTracks (albumId: string) (page: PagesOf<AlbumTrack> option) =
    let previousPage =
        match page with
        | Some page -> page
        | None ->
            sprintf "%s/albums/%s/tracks?limit=50" BASE_URL albumId
            |> GET
            |> parseResponse<PagesOf<AlbumTrack>>

    match previousPage.next with
    | null -> previousPage
    | url ->
        GET url
        |> parseResponse<PagesOf<AlbumTrack>>
        |> prependPagesOf<AlbumTrack> previousPage.items
        |> Some
        |> getAllAlbumTracks albumId


let getSavedTracks (limit: int) (offset: int) =
    let bLimit, bOffset = bindLimitOffset 50 limit offset

    sprintf "%s/me/tracks?limit=%d&offset=%d" BASE_URL bLimit bOffset
    |> GET
    |> parseResponse<PagesOf<SavedTrack>>

let rec getAllSavedTracks (page: PagesOf<SavedTrack> option) =
    let previousPage =
        match page with
        | Some page -> page
        | None ->
            sprintf "%s/me/tracks?limit=50" BASE_URL
            |> GET
            |> parseResponse<PagesOf<SavedTrack>>

    match previousPage.next with
    | null -> previousPage
    | url ->
        GET url
        |> parseResponse<PagesOf<SavedTrack>>
        |> prependPagesOf<SavedTrack> previousPage.items
        |> Some
        |> getAllSavedTracks
