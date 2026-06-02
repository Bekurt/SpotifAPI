#load "Auth.fs"
#load "HttpMethods.fs"
#load "Parsers.fs"
#load "Endpoints.fs"

open Parsers
open Endpoints
open System.IO
open System.Text.Json

let printResult<'T> (response: 'T) =
    let options = JsonSerializerOptions(WriteIndented = true)
    let text = JsonSerializer.Serialize(response, options)
    printfn "%s" text

let writeJson<'T> (file: string option) (response: 'T) =
    let options = JsonSerializerOptions(WriteIndented = true)
    let text = JsonSerializer.Serialize(response, options)

    match file with
    | Some s -> File.WriteAllText(s, text)
    | None -> File.WriteAllText("response.json", text)

let readJson<'T> (file: string) =
    File.ReadAllText file |> JsonSerializer.Deserialize<'T>

let getAllCurrentTracks () =
    let currentPlaylists = getAllCurrentUserPlaylists None

    let getTrackListFromPlaylists (state: PlaylistTrack list) (item: SimplePlaylist) =
        state @ (getAllPlaylistTracks item.id None).items

    let tracks =
        currentPlaylists.items
        |> List.fold getTrackListFromPlaylists []
        |> writeJson (Some "saved_backup.json")

    tracks

let createNewPlaylistsFromBackupTracks () =
    let tracks = readJson<string list> "saved_backup.json"

    let me = getCurrentUser ()

    tracks
    |> List.randomShuffle
    |> List.randomShuffle
    |> List.randomShuffle
    |> List.randomShuffle
    |> List.randomShuffle
    |> List.randomShuffle
    |> List.randomShuffle
    |> List.chunkBySize 500
    |> List.iteri (fun idx chunk ->
        let p = createPlaylist me (sprintf "Playlist %02d" (idx + 1))
        addTracksToPlaylist p.id chunk)

let createSingleTotalPlaylist (tracksID: string list) =
    let me = getCurrentUser ()
    let newP = createPlaylist me "ALL SONGS"

    tracksID |> List.randomShuffle |> addTracksToPlaylist newP.id

// NOTE TO SELF: NEXT TIME BE FUCKING CONSISTENT IN SAVING OUTPUTS!

(*
let SokenArtist = searchArtist 1 0 "Soken"
let SokenAlbums = getAllArtistAlbums SokenArtist.artists.items[0].id None

let SokenTracks =
    getAllAlbumTracks SokenAlbums.items[1].id None
    |> writeJson (Some "z_SokenTracks.json")
*)

(*
let jusArtist = searchArtist 2 0 "Justefunk Funk"
let jusAlbums = getAllArtistAlbums jusArtist.artists.items[0].id None

let jusTracks =
    jusAlbums.items
    |> List.fold (fun state album -> state @ (getAllAlbumTracks album.id None).items) []
    |> writeJson (Some "z_jusTracks.json")
*)


(readJson<PlaylistTrack list> "saved_backup.json"
 |> List.map (fun pt -> pt.item.id))
@ (readJson<AlbumTrack list> "z_jusTracks.json" |> List.map (fun track -> track.id))
@ ((readJson<PageOf<AlbumTrack>> "z_SokenTracks.json").items
   |> List.map (fun track -> track.id))
|> createSingleTotalPlaylist
