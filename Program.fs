open Giraffe
open Microsoft.AspNetCore.Builder
open Giraffe.ViewEngine
open CodeHollow.FeedReader
open System.Threading.Tasks

let rssEndpoint = "/rss"

let renderFilterForm (filterFeed: string) =
    form [
        _action rssEndpoint
        _method "get"
    ] [
        label [_for "filterFeed"] [str "Filter by Feed: "]
        input [
            _type "text"
            _name "filterFeed"
            _id "filterFeed"
            _value filterFeed
        ]
        button [_type "submit"] [str "Apply Filter"]
    ]

let filterItemsByFeedTitle (feeds: Feed seq) (filterFeedTitle: string option) =
    match filterFeedTitle with
    | Some title ->
        feeds
        |> Seq.choose (fun feed -> if feed.Title = title then Some feed else None)
        |> Seq.collect (fun feed -> feed.Items)
    | None -> feeds |> Seq.collect (fun feed -> feed.Items)

let rssHandler (urls: string seq) : HttpHandler =
    fun next ctx ->
        task {
            let! tasks =
                urls
                |> Seq.map FeedReader.ReadAsync
                |> Task.WhenAll

            let items =
                tasks
                |> Seq.collect (fun feed -> feed.Items)

            let filterFeedTitle =
                ctx.Request.Query.["filterFeed"].ToString()

            let filteredItems =
                match filterFeedTitle with
                | null | "" -> items
                | _ ->
                    let filteredTitle = filterFeedTitle.Trim()
                    items |> Seq.filter (fun item -> item.Title = filteredTitle)

            let tableRows =
                filteredItems
                |> Seq.map (fun item -> tr [] [ td [] [ a [ _href item.Link ] [ str item.Title ] ] ])
                |> List.ofSeq

            let response =
                html [] [
                    head [] [ title [] [ str "RSS Feed" ] ]
                    body [] [
                        h1 [] [ str "RSS Feed" ]
                        div [] [renderFilterForm filterFeedTitle]
                        table [] [ tbody [] tableRows ]
                        p [] [ str "" ]
                    ]
                ]
            return! (htmlView response) next ctx
        }



let builder = WebApplication.CreateBuilder()
builder.Services.AddGiraffe() |> ignore
let app = builder.Build()

let rssFeedUrls = [
    "https://arminreiter.com/feed"
    "http://rss.cnn.com/rss/cnn_topstories.rss"
]

let webApp = choose [ route rssEndpoint >=> rssHandler rssFeedUrls ]

app.UseGiraffe(webApp)
app.Run()
