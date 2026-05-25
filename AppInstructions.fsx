#load "Auth.fs"
#load "HttpMethods.fs"
#load "Parsers.fs"
#load "Endpoints.fs"

open Parsers
open Endpoints
open System.IO
open System.Text.Json

let writeJson<'T> (response: 'T) =
    let options = JsonSerializerOptions(WriteIndented = true)
    let text = JsonSerializer.Serialize(response, options)
    File.WriteAllText("response.json", text)

let readJson<'T> (file: string) =
    File.ReadAllText file |> JsonSerializer.Deserialize<'T>

let currentPlaylists = getAllCurrentUserPlaylists None

let getTrackListFromPlaylists (state: PlaylistTrack list) (item: SimplePlaylist) =
    state @ (getAllPlaylistTracks item.id None).items

let tracks =
    currentPlaylists.items |> List.fold getTrackListFromPlaylists [] |> writeJson
//let tracks = readJson<string list> "saved_backup.json"


(* let me = getCurrentUser ()

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
 *)
