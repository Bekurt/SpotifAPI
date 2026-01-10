module HttpMethods

open System.Net
open System.Net.Http
open System.Text.Json
open System.IO
open Auth

let client = new HttpClient()

let getToken () =
    File.ReadAllText "./token.json" |> JsonSerializer.Deserialize<AccessToken>

let setBearerToken (client: HttpClient) =
    let token = getToken ()
    client.DefaultRequestHeaders.Authorization <- Headers.AuthenticationHeaderValue("Bearer", token.access_token)

let rec GET (url: string) =
    printfn "Sending GET request to %s" url

    task {
        client |> setBearerToken

        let! response = client.GetAsync url

        return
            match response.StatusCode with
            | HttpStatusCode.Unauthorized ->
                refreshToken ()
                GET url
            | code when int code < 300 && int code >= 200 ->
                response.Content.ReadAsStringAsync()
                |> Async.AwaitTask
                |> Async.RunSynchronously
            | other ->
                (other.ToString(),
                 response.Content.ReadAsStringAsync()
                 |> Async.AwaitTask
                 |> Async.RunSynchronously)
                ||> sprintf "Failed with %s code - %s"

    }
    |> Async.AwaitTask
    |> Async.RunSynchronously

let rec POST<'T> (url: string) (body: 'T) =
    printfn "Sending POST request to %s" url

    task {
        client |> setBearerToken

        let requestBody = new StringContent(JsonSerializer.Serialize<'T> body)

        let! response = client.PostAsync(url, requestBody)

        return
            match response.StatusCode with
            | HttpStatusCode.Unauthorized ->
                refreshToken ()
                POST url body
            | code when int code < 300 && int code >= 200 ->
                response.Content.ReadAsStringAsync()
                |> Async.AwaitTask
                |> Async.RunSynchronously
            | other ->
                (other.ToString(),
                 response.Content.ReadAsStringAsync()
                 |> Async.AwaitTask
                 |> Async.RunSynchronously)
                ||> sprintf "Failed with %s code - %s"

    }
    |> Async.AwaitTask
    |> Async.RunSynchronously

let rec PUT<'T> (url: string) (body: 'T) =
    printfn "Sending PUT request to %s" url

    task {
        client |> setBearerToken

        let requestBody = new StringContent(JsonSerializer.Serialize<'T> body)

        let! response = client.PutAsync(url, requestBody)

        return
            match response.StatusCode with
            | HttpStatusCode.Unauthorized ->
                refreshToken ()
                PUT url body
            | code when int code < 300 && int code >= 200 ->
                response.Content.ReadAsStringAsync()
                |> Async.AwaitTask
                |> Async.RunSynchronously
            | other ->
                (other.ToString(),
                 response.Content.ReadAsStringAsync()
                 |> Async.AwaitTask
                 |> Async.RunSynchronously)
                ||> sprintf "Failed with %s code - %s"

    }
    |> Async.AwaitTask
    |> Async.RunSynchronously

let rec DELETE<'T> (url: string) (body: 'T) =
    printfn "Sending DELETE request to %s" url

    task {
        client |> setBearerToken
        use request = new HttpRequestMessage(HttpMethod.Delete, url)
        request.Content <- new StringContent(JsonSerializer.Serialize<'T> body)

        use! response = client.SendAsync request

        return
            match response.StatusCode with
            | HttpStatusCode.Unauthorized ->
                refreshToken ()
                DELETE url body
            | code when int code < 300 && int code >= 200 ->
                response.Content.ReadAsStringAsync()
                |> Async.AwaitTask
                |> Async.RunSynchronously
            | other ->
                (other.ToString(),
                 response.Content.ReadAsStringAsync()
                 |> Async.AwaitTask
                 |> Async.RunSynchronously)
                ||> sprintf "Failed with %s code - %s"


    }
    |> Async.AwaitTask
    |> Async.RunSynchronously
