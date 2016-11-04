﻿open System
open FsXaml
open Gjallarhorn
open Gjallarhorn.Bindable

// ----------------------------------     Model     ---------------------------------- 
// Model is a simple integer for counter
type Model = int
let initModel i : Model = i

// ----------------------------------    Update     ---------------------------------- 
// We define a union type for each possible message
type Msg = 
    | Increment 
    | Decrement

// Create a function that updates the model given a message
let update msg (model : Model) =
    match msg with
    | Increment -> model + 1
    | Decrement -> model - 1

// ----------------------------------    Binding    ---------------------------------- 
// Create a function that binds a model to a source, and outputs messages
// This essentially acts as our "view" in Elm terminology, though it doesn't actually display 
// the view as much as map from our type to bindings
let bindToSource source (model : ISignal<Model>) =    
    // Create a property to display our current value    
    Binding.toView source "Current" model

    // Create commands for our buttons
    [
        Binding.createMessage "Increment" Increment source
        Binding.createMessage "Decrement" Decrement source
    ]

// ----------------------------------   Framework  ----------------------------------- 
// The core framework information is platform agnostic
let applicationCore = Framework.info (initModel 5) update bindToSource 


// ***********************************************************************************
// Note that here down is Platform specific 
// ***********************************************************************************

// ----------------------------------     View      ---------------------------------- 
// Our platform specific view type
type MainWin = XAML<"MainWindow.xaml">

// ----------------------------------  Application  ---------------------------------- 
[<STAThread>]
[<EntryPoint>]
let main _ =         
    // Run using the WPF wrappers around the basic application framework
    MainWin()
    |> Wpf.Framework.fromInfoAndWindow applicationCore
    |> Wpf.Framework.runApplication