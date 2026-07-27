#load "Auth.fs"
#load "HttpMethods.fs"
#load "Parsers.fs"
#load "Endpoints.fs"

open Parsers
open Endpoints
open System.IO
open System.Text.Json

let ALL_SONGS = "5WX5E1boHIKZ3w1Vhbxpva"

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

let getAllCurrentTracks () = getAllPlaylistTracks ALL_SONGS None

let backupAllSongs () =
    getAllCurrentTracks () |> writeJson (Some "saved_backup.json")

let readBackup () =
    readJson<PageOf<PlaylistTrack>> "saved_backup.json"

// Create N playlists with 500 songs each from the library backup
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

// Create the playlist "ALL SONGS" from list of track ids
let createSingleTotalPlaylist (tracksID: string list) =
    let me = getCurrentUser ()
    let newP = createPlaylist me "ALL SONGS"

    tracksID |> List.randomShuffle |> addTracksToPlaylist newP.id

let tracksToDelete =
    readBackup().items
    |> List.filter (fun item ->
        let artist = item.item.artists.Head.name

        artist.Contains "Beatles"
        || artist.Contains "Who"
        || artist.Contains "Linkin Park"
        || artist.Contains "Florence"
        || artist.Contains "Queen"
        || artist.Contains "Sheeran")
    |> List.map (fun item -> item.item.id)


let artistList =
    readBackup().items
    |> List.map (fun item -> item.item.artists.Head.name)
    |> List.distinct

let currentSnapshot = (getUserPlaylist ALL_SONGS).snapshot_id

deletePlaylistTracks ALL_SONGS currentSnapshot tracksToDelete

backupAllSongs ()
