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

let prependPagesOf<'T> (itemsToPrepend: list<'T>) (page: PageOf<'T>) =
    { page with
        items = itemsToPrepend @ page.items }

let getCurrentUser () =
    BASE_URL |> sprintf "%s/me" |> GET |> parseResponse<User>

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

let getArtistAlbum (limit: int) (offset: int) (artist: Artist) =
    let bLimit, bOffset = bindLimitOffset 50 limit offset

    sprintf "%s/artists/%s/albums?include_groups=album&limit=%d&offset=%d" BASE_URL artist.id bLimit bOffset
    |> GET
    |> parseResponse<PageOf<Album>>

let rec getAllArtistAlbums (artist: Artist) (page: PageOf<Album> option) =
    let previousPage =
        match page with
        | Some page -> page
        | None -> getArtistAlbum 50 0 artist

    match previousPage.next with
    | null -> previousPage
    | url ->
        GET url
        |> parseResponse<PageOf<Album>>
        |> prependPagesOf<Album> previousPage.items
        |> Some
        |> getAllArtistAlbums artist

let rec getAllAlbumTracks (album: Album) (page: PageOf<AlbumTrack> option) =
    let previousPage =
        match page with
        | Some page -> page
        | None ->
            sprintf "%s/albums/%s/tracks?limit=50" BASE_URL album.id
            |> GET
            |> parseResponse<PageOf<AlbumTrack>>

    match previousPage.next with
    | null -> previousPage
    | url ->
        GET url
        |> parseResponse<PageOf<AlbumTrack>>
        |> prependPagesOf<AlbumTrack> previousPage.items
        |> Some
        |> getAllAlbumTracks album


let getSavedTracks (limit: int) (offset: int) =
    let bLimit, bOffset = bindLimitOffset 50 limit offset

    sprintf "%s/me/tracks?limit=%d&offset=%d" BASE_URL bLimit bOffset
    |> GET
    |> parseResponse<PageOf<SavedTrack>>

let rec getAllSavedTracks (page: PageOf<SavedTrack> option) =
    let previousPage =
        match page with
        | Some page -> page
        | None ->
            sprintf "%s/me/tracks?limit=50" BASE_URL
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

let deleteSavedTracks (trackList: list<SavedTrack>) =
    trackList
    |> List.map (fun savedTrack -> savedTrack.track.id)
    |> List.chunkBySize 50
    |> List.iter (fun chunk -> {| ids = chunk |} |> DELETE(sprintf "%s/me/tracks" BASE_URL) |> printfn "%s")

let createPlaylist (user: User) (name: string) =
    {| name = name |}
    |> POST(sprintf "%s/users/%s/playlists" BASE_URL user.id)
    |> parseResponse<Playlist>


let addTracksToPlaylist (playlist: Playlist) (trackList: list<Track>) =
    trackList
    |> List.chunkBySize 100
    |> List.iter (fun chunk ->
        {| uris = chunk |> List.map (fun track -> sprintf "spotify:track:%s" track.id) |}
        |> POST(sprintf "%s/playlists/%s/tracks" BASE_URL playlist.id)
        |> ignore

        printfn "POST chunk success")

let rec getAllCurrentUserPlaylists (page: PageOf<SimplePlaylist> option) =
    let previousPage =
        match page with
        | Some page -> page
        | None ->
            sprintf "%s/me/playlists?limit=50" BASE_URL
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

let rec getAllPlaylistTracks (playlist: Playlist) (page: PageOf<SavedTrack> option) =
    let previousPage =
        match page with
        | Some page -> page
        | None ->
            sprintf "%s/playlist/%s/tracks?limit=50" BASE_URL playlist.id
            |> GET
            |> parseResponse<PageOf<SavedTrack>>

    match previousPage.next with
    | null -> previousPage
    | url ->
        GET url
        |> parseResponse<PageOf<SavedTrack>>
        |> prependPagesOf<SavedTrack> previousPage.items
        |> Some
        |> getAllPlaylistTracks playlist

type DeletePlaylistTrackBody =
    { tracks: list<{| uri: string |}>
      snapshot_id: string }

let rec deletePlaylistTracks (playlist: Playlist) (snap_id: string) (trackList: list<Track>) =
    let assembleBody (processedChunk: list<{| uri: string |}>) : DeletePlaylistTrackBody =
        { tracks = processedChunk
          snapshot_id = snap_id }

    let chunkedList = trackList |> List.chunkBySize 100

    match chunkedList with
    | first :: rest ->
        let newState =
            first
            |> List.map (fun track -> {| uri = sprintf "spotify:track:%s" track.id |})
            |> assembleBody
            |> DELETE<DeletePlaylistTrackBody>(sprintf "%s/playlists/%s/tracks" BASE_URL playlist.id)
            |> parseResponse<Playlist>

        rest
        |> List.fold (fun state item -> state @ item) []
        |> deletePlaylistTracks playlist newState.snapshot_id

        printfn "Chunk DELETE successful"
    | [] -> printfn "DELETE completed"
