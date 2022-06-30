module App

open Feliz
open Elmish
open Shared

type State = {
    Counter: Deferred<Result<Counter, string>>
    Print: Deferred<Result<PrintResult, string>>
}

type Msg =
    | Increment
    | Decrement
    | LoadCounter of AsyncOperationStatus<Result<Counter, string>>
    | LoadPrint of AsyncOperationStatus<Result<PrintResult, string>>

let init() = { Counter = HasNotStartedYet; Print = HasNotStartedYet }, Cmd.ofMsg (LoadCounter Started)

let update (msg: Msg) (state: State) =
    match msg with
    | LoadCounter Started ->
        let loadCounter = async {
            try
                let! counter = Server.api.Counter()
                return LoadCounter (Finished (Ok counter))
            with error ->
                Log.developmentError error
                return LoadCounter (Finished (Error "Error while retrieving Counter from server"))
        }

        { state with Counter = InProgress }, Cmd.fromAsync loadCounter

    | LoadCounter (Finished counter) ->
        { state with Counter = Resolved counter }, Cmd.none

    | LoadPrint Started ->
        let loadPrint = async {
            try
                let req = { value = "Hi there!" }
                let! res = Server.api.Print req
                return LoadPrint (Finished (Ok res))
            with error ->
                Log.developmentError error
                return LoadPrint (Finished (Error "Error while calling print on server"))
        }

        { state with Print = InProgress }, Cmd.fromAsync loadPrint

    | LoadPrint (Finished print) ->
        { state with Print = Resolved print }, Cmd.none

    | Increment ->
        let updatedCounter =
            state.Counter
            |> Deferred.map (function
                | Ok counter -> Ok { counter with value = counter.value + 1 }
                | Error error -> Error error)

        { state with Counter = updatedCounter }, Cmd.none

    | Decrement ->
        let updatedCounter =
            state.Counter
            |> Deferred.map (function
                | Ok counter -> Ok { counter with value = counter.value - 1 }
                | Error error -> Error error)

        { state with Counter = updatedCounter }, Cmd.none

let renderCounter (counter: Deferred<Result<Counter, string>>)=
    match counter with
    | HasNotStartedYet -> Html.none
    | InProgress -> Html.h1 "Loading..."
    | Resolved (Ok counter) -> Html.h1 counter.value
    | Resolved (Error errorMsg) ->
        Html.h1 [
            prop.style [ style.color.crimson ]
            prop.text errorMsg
        ]

let renderPrint dispatch (res: Deferred<Result<PrintResult, string>>)=
    match res with
    | HasNotStartedYet ->
        Html.button [
            prop.style [ style.margin 5; style.padding 15 ]
            prop.onClick (fun _ -> dispatch (LoadPrint Started))
            prop.text "Try call print"
        ]
    | InProgress -> Html.h1 "Loading..."
    | Resolved (Ok res) -> Html.h1 "Printed :)"
    | Resolved (Error errorMsg) ->
        Html.h1 [
            prop.style [ style.color.crimson ]
            prop.text errorMsg
        ]

let fableLogo() = StaticFile.import "./imgs/fable_logo.png"

let render (state: State) (dispatch: Msg -> unit) =

    Html.div [
        prop.style [
            style.textAlign.center
            style.padding 40
        ]

        prop.children [

            Html.img [
                prop.src(fableLogo())
                prop.width 250
            ]

            Html.h1 "Full-Stack Counter"

            Html.button [
                prop.style [ style.margin 5; style.padding 15 ]
                prop.onClick (fun _ -> dispatch Increment)
                prop.text "Increment"
            ]

            Html.button [
                prop.style [ style.margin 5; style.padding 15 ]
                prop.onClick (fun _ -> dispatch Decrement)
                prop.text "Decrement"
            ]

            renderCounter state.Counter
            renderPrint dispatch state.Print
        ]
    ]