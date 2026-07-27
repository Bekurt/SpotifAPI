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

let SPOTIFY_BATCH_LIMIT = 10

let bindLimitOffset (limit: int) (offset: int) =
    limit |> min SPOTIFY_BATCH_LIMIT |> max 0, offset |> max 0

let prependPagesOf<'T> (itemsToPrepend: list<'T>) (page: PageOf<'T>) =
    { page with
        items = itemsToPrepend @ page.items }

let getCurrentUser () =
    BASE_URL |> sprintf "%s/me" |> GET |> parseResponse<User>

let searchAlbum (limit: int) (offset: int) (strToSearch: string) =
    let bLimit, bOffset = bindLimitOffset limit offset

    sprintf "%s/search?q=%s&type=album&limit=%d&offset=%d" BASE_URL strToSearch bLimit bOffset
    |> GET
    |> parseResponse<AlbumSearch>

let searchArtist (limit: int) (offset: int) (strToSearch: string) =
    let bLimit, bOffset = bindLimitOffset limit offset

    sprintf "%s/search?q=%s&type=artist&limit=%d&offset=%d" BASE_URL strToSearch bLimit bOffset
    |> GET
    |> parseResponse<ArtistSearch>

let searchPlaylist (limit: int) (offset: int) (strToSearch: string) =
    let bLimit, bOffset = bindLimitOffset limit offset

    sprintf "%s/search?q=%s&type=playlist&limit=%d&offset=%d" BASE_URL strToSearch bLimit bOffset
    |> GET
    |> parseResponse<PlaylistSearch>

let searchTrack (limit: int) (offset: int) (strToSearch: string) =
    let bLimit, bOffset = bindLimitOffset limit offset

    sprintf "%s/search?q=%s&type=track&limit=%d&offset=%d" BASE_URL strToSearch bLimit bOffset
    |> GET
    |> parseResponse<TrackSearch>

let getArtistAlbum (limit: int) (offset: int) (artistId: string) =
    let bLimit, bOffset = bindLimitOffset limit offset

    sprintf "%s/artists/%s/albums?include_groups=album&limit=%d&offset=%d" BASE_URL artistId bLimit bOffset
    |> GET
    |> parseResponse<PageOf<Album>>

let rec getAllArtistAlbums (artistId: string) (page: PageOf<Album> option) =
    let previousPage =
        match page with
        | Some page -> page
        | None -> getArtistAlbum SPOTIFY_BATCH_LIMIT 0 artistId

    match previousPage.next with
    | null -> previousPage
    | url ->
        GET url
        |> parseResponse<PageOf<Album>>
        |> prependPagesOf<Album> previousPage.items
        |> Some
        |> getAllArtistAlbums artistId

let rec getAllAlbumTracks (albumId: string) (page: PageOf<AlbumTrack> option) =
    let previousPage =
        match page with
        | Some page -> page
        | None ->
            sprintf "%s/albums/%s/tracks?limit=%d" BASE_URL albumId SPOTIFY_BATCH_LIMIT
            |> GET
            |> parseResponse<PageOf<AlbumTrack>>

    match previousPage.next with
    | null -> previousPage
    | url ->
        GET url
        |> parseResponse<PageOf<AlbumTrack>>
        |> prependPagesOf<AlbumTrack> previousPage.items
        |> Some
        |> getAllAlbumTracks albumId


let getSavedTracks (limit: int) (offset: int) =
    let bLimit, bOffset = bindLimitOffset limit offset

    sprintf "%s/me/tracks?limit=%d&offset=%d" BASE_URL bLimit bOffset
    |> GET
    |> parseResponse<PageOf<SavedTrack>>

let rec getAllSavedTracks (page: PageOf<SavedTrack> option) =
    let previousPage =
        match page with
        | Some page -> page
        | None ->
            sprintf "%s/me/tracks?limit=%d" BASE_URL SPOTIFY_BATCH_LIMIT
            |> GET
            |> parseResponse<PageOf<SavedTrack>>

    match previousPage.next with
    | null -> previousPage
    | url ->
        GET url
        |> parseResponse<PageOf<SavedTrack>>
        |> prependPagesOf<SavedTrack> previousPage.items
        |> Some
        |> getAllSavedTracks

let deleteSavedTracks (trackIdList: list<string>) =
    trackIdList
    |> List.chunkBySize SPOTIFY_BATCH_LIMIT
    |> List.iter (fun chunk -> {| ids = chunk |} |> DELETE(sprintf "%s/me/tracks" BASE_URL) |> printfn "%s")

let createPlaylist (user: User) (name: string) =
    {| name = name |}
    |> POST(sprintf "%s/users/%s/playlists" BASE_URL user.id)
    |> parseResponse<Playlist>

let addTracksToPlaylist (playlistId: string) (trackIdList: list<string>) =
    trackIdList
    |> List.chunkBySize 100
    |> List.iter (fun chunk ->
        {| uris = chunk |> List.map (fun track -> sprintf "spotify:track:%s" track) |}
        |> POST(sprintf "%s/playlists/%s/tracks" BASE_URL playlistId)
        |> ignore

        printfn "POST chunk success")

let rec getAllCurrentUserPlaylists (page: PageOf<SimplePlaylist> option) =
    let previousPage =
        match page with
        | Some page -> page
        | None ->
            sprintf "%s/me/playlists?limit=%d" BASE_URL SPOTIFY_BATCH_LIMIT
            |> GET
            |> parseResponse<PageOf<SimplePlaylist>>

    match previousPage.next with
    | null -> previousPage
    | url ->
        GET url
        |> parseResponse<PageOf<SimplePlaylist>>
        |> prependPagesOf<SimplePlaylist> previousPage.items
        |> Some
        |> getAllCurrentUserPlaylists

let rec getAllPlaylistTracks (playlistId: string) (page: PageOf<PlaylistTrack> option) =
    let previousPage =
        match page with
        | Some page -> page
        | None ->
            sprintf "%s/playlists/%s/items?limit=100" BASE_URL playlistId
            |> GET
            |> parseResponse<PageOf<PlaylistTrack>>

    match previousPage.next with
    | null -> previousPage
    | url ->
        GET url
        |> parseResponse<PageOf<PlaylistTrack>>
        |> prependPagesOf<PlaylistTrack> previousPage.items
        |> Some
        |> getAllPlaylistTracks playlistId

type DeletePlaylistTrackBody =
    { tracks: list<{| uri: string |}>
      snapshot_id: string }

let rec deletePlaylistTracks (playlistId: string) (snap_id: string) (trackIdList: list<string>) =
    let assembleBody (processedChunk: list<{| uri: string |}>) : DeletePlaylistTrackBody =
        { tracks = processedChunk
          snapshot_id = snap_id }

    let chunkedList = trackIdList |> List.chunkBySize 100

    match chunkedList with
    | first :: rest ->
        let newState =
            first
            |> List.map (fun track -> {| uri = sprintf "spotify:track:%s" track |})
            |> assembleBody
            |> DELETE<DeletePlaylistTrackBody>(sprintf "%s/playlists/%s/items" BASE_URL playlistId)
            |> parseResponse<SimplePlaylist>

        rest
        |> List.fold (fun state item -> state @ item) []
        |> deletePlaylistTracks playlistId newState.snapshot_id

        printfn "Chunk DELETE successful"
    | [] -> printfn "DELETE completed"
