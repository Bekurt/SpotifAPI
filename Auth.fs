module Auth

open System
open System.Collections.Generic
open System.Net
open System.Net.Http
open System.Diagnostics
open System.Text.Json
open System.IO
open System.Text

type AuthBody =
    { grant_type: string
      code: string
      redirect_uri: string }

type AccessToken =
    { access_token: string
      token_type: string
      scope: string
      expires_in: int
      refresh_token: string }

let requestAuthorization () =
    let client_id = Environment.GetEnvironmentVariable "SPOTIFY_CLIENT_ID"
    let response_type = "code"
    let redirect_uri = "http://127.0.0.1:5000"
    let state = Random().Next(9999).ToString()

    let scope =
        "user-read-private"
        + " user-read-email"
        + " user-library-read"
        + " user-library-modify"
        + " user-top-read"
        + " playlist-read-private"
        + " playlist-modify-private"
        + " playlist-modify-public"

    let url =
        sprintf
            "client_id=%s&response_type=%s&redirect_uri=%s&state=%s&scope=%s"
            (Uri.EscapeDataString client_id)
            (Uri.EscapeDataString response_type)
            (Uri.EscapeDataString redirect_uri)
            (Uri.EscapeDataString state)
            (Uri.EscapeDataString scope)
        |> sprintf "https://accounts.spotify.com/authorize?%s"


    Process.Start(ProcessStartInfo(FileName = url, UseShellExecute = true))
    |> ignore

    let code =
        task {
            use listener = new HttpListener()
            listener.Prefixes.Add(redirect_uri + "/")
            listener.Start()

            try
                let! context = listener.GetContextAsync()
                let responseQuery = context.Request.QueryString

                let accessCode =
                    if responseQuery.AllKeys |> Array.contains "code" then
                        (responseQuery.GetValues "code")[0]
                    else
                        "404"

                return accessCode
            finally
                listener.Stop()
        }
        |> Async.AwaitTask
        |> Async.RunSynchronously

    if code = "404" then
        printfn "Auth Failed"
    else
        let client_secret = Environment.GetEnvironmentVariable "SPOTIFY_CLIENT_SECRET"

        task {
            use http = new HttpClient()

            use request =
                new HttpRequestMessage(HttpMethod.Post, "https://accounts.spotify.com/api/token")

            let encodedSecrets =
                Convert.ToBase64String(Encoding.UTF8.GetBytes(sprintf "%s:%s" client_id client_secret))

            let form =
                [ KeyValuePair<string, string>("grant_type", "authorization_code")
                  KeyValuePair<string, string>("code", code)
                  KeyValuePair<string, string>("redirect_uri", redirect_uri) ]

            request.Content <- new FormUrlEncodedContent(form)
            request.Headers.Authorization <- Headers.AuthenticationHeaderValue("Basic", encodedSecrets)

            use! resp = request |> http.SendAsync

            let! body = resp.Content.ReadAsStringAsync()

            File.WriteAllText("./token.json", body)

        }
        |> Async.AwaitTask
        |> Async.RunSynchronously

let refreshToken () =

    let oldToken =
        File.ReadAllText "./token.json" |> JsonSerializer.Deserialize<AccessToken>

    task {
        use http = new HttpClient()

        use request =
            new HttpRequestMessage(HttpMethod.Post, "https://accounts.spotify.com/api/token")

        let form =
            [ KeyValuePair<string, string>("grant_type", "refresh_token")
              KeyValuePair<string, string>("refresh_token", oldToken.refresh_token) ]

        request.Content <- new FormUrlEncodedContent(form)

        use! resp = request |> http.SendAsync

        let! body = resp.Content.ReadAsStringAsync()

        File.WriteAllText("./token.json", body)

    }
    |> Async.AwaitTask
    |> Async.RunSynchronously
