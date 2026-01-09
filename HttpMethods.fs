module HttpMethods

open System.Net
open System.Net.Http
open System.Text.Json
open System.IO
open Auth
open System

let client = new HttpClient()

let getToken () = File.ReadAllText "./token.json"

let setBearerToken (client: HttpClient) =
    client.DefaultRequestHeaders.Authorization <- Headers.AuthenticationHeaderValue("Bearer", getToken ())

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
            | HttpStatusCode.OK -> response.Content.ToString()
            | otherCode -> otherCode.ToString() |> sprintf "Failed with %s code"

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
            | HttpStatusCode.OK -> response.Content.ToString()
            | otherCode -> otherCode.ToString() |> sprintf "Failed with %s code"

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
            | HttpStatusCode.OK -> response.Content.ToString()
            | otherCode -> otherCode.ToString() |> sprintf "Failed with %s code"

    }
    |> Async.AwaitTask
    |> Async.RunSynchronously

let rec DELETE (url: string) =
    printfn "Sending DELETE request to %s" url

    task {
        client |> setBearerToken

        let! response = client.GetAsync url

        return
            match response.StatusCode with
            | HttpStatusCode.Unauthorized ->
                refreshToken ()
                DELETE url
            | HttpStatusCode.OK -> response.Content.ToString()
            | otherCode -> otherCode.ToString() |> sprintf "Failed with %s code"

    }
    |> Async.AwaitTask
    |> Async.RunSynchronously
