﻿namespace Gjallarhorn.Linq

open Gjallarhorn
open Gjallarhorn.Helpers
open Gjallarhorn.Internal
open Gjallarhorn.Validation

open System
open System.ComponentModel
open System.Runtime.CompilerServices
open System.Threading.Tasks

/// Functions for working with Mutables from C#
[<AbstractClass;Sealed>]
type Mutable() =
    /// Create a mutable given an initial value
    static member Create<'a> (value : 'a) =
        Mutable<'a>(value) :> IMutatable<'a>
    
    /// Update a mutable given a stepping function
    static member Update<'a> (original : IMutatable<'a>, steppingFunction : Func<'a, 'a>) =
        Mutable.step steppingFunction.Invoke original

/// Functions for working with Signals from C#
[<AbstractClass;Sealed>]
type Signal() =
    /// Create a signal (which never notifies) given a constant value
    static member FromConstant<'a> (value : 'a) =
        Signal.constant value

/// Extension methods for working with Signals from C# using a LINQ inspired API    
[<AbstractClass;Sealed;Extension>]
type SignalExtensions() =
    /// Create a cached signal over a provider
    /// <remarks>
    /// This will not hold a reference to the provider, and will allow it to be garbage collected.
    /// As such, it caches the "last valid" state of the signal locally.
    /// </remarks>
    [<Extension>]
    static member Cached<'a>(this : ISignal<'a>) =
        Signal.cache this

    /// Create a subscription to the changes of a signal which calls the provided function upon each change
    [<Extension>]
    static member Subscribe<'a>(this : ISignal<'a>, func : Action<'a>) =
        Signal.Subscription.create func.Invoke this

    /// Create a subscription to the changes of a signal which copies its value upon change into a mutable
    [<Extension>]
    static member CopyTo<'a>(this : ISignal<'a>, target : IMutatable<'a>) =
        Signal.Subscription.copyTo target this

    [<Extension>]
    /// Create a subscription to the changes of a signal which copies its value upon change into a mutable via a stepping function
    static member SubscribeAndUpdate<'a,'b>(this : ISignal<'a>, target : IMutatable<'b>, stepFunction : Func<'b,'a,'b>) =
        let f b a = stepFunction.Invoke(b,a)
        Signal.Subscription.copyStep target f this

    // TODO: port fromObservable

    [<Extension>]
    /// Perform a mapping from one signal to another
    static member Select<'a,'b> (this:ISignal<'a>, mapper:Func<'a,'b>) =
        this
        |> Signal.map mapper.Invoke 

    [<Extension>]
    /// Perform an asynchronous mapping from one signal to another
    static member SelectAsync<'a,'b> (this:ISignal<'a>, initialValue:'b, mapper:Func<'a,Task<'b>>) =
        let mapping a = 
            async {
                let! result = Async.AwaitTask (mapper.Invoke a)
                return result
            }
        this
        |> Signal.mapAsync mapping initialValue

    [<Extension>]
    /// Perform an asynchronous mapping from one signal to another, tracking execution via an IdleTracker
    static member SelectAsync<'a,'b> (this:ISignal<'a>, initialValue:'b, tracker, mapper:Func<'a,Task<'b>>) =
        let mapping a = 
            async {
                let! result = Async.AwaitTask (mapper.Invoke a)
                return result
            }
        this
        |> Signal.mapAsyncTracked mapping initialValue tracker
                
    [<Extension;EditorBrowsable(EditorBrowsableState.Never)>]
    /// Perform a projection from a signal, typically only used for query syntax
    static member SelectMany<'a,'b>(this:ISignal<'a>, mapper:Func<'a,ISignal<'b>>) =
        mapper.Invoke(this.Value)

    [<Extension;EditorBrowsable(EditorBrowsableState.Never)>]
    /// Perform a projection from a signal, typically only used for query syntax
    static member SelectMany<'a,'b,'c>(this:ISignal<'a>, mapper:Func<'a,ISignal<'b>>, selector:Func<'a,'b,'c>) : ISignal<'c> =
        let b' = mapper.Invoke(this.Value)
        Signal.map2 (fun a b -> selector.Invoke(a,b)) this b'

/// Extension methods for working with Observables from C# using a LINQ inspired API    
[<AbstractClass;Sealed;Extension>]
type ObservableExtensions() =
    [<Extension>]
    /// Convert from an observable and an initial value to a signal
    static member ToSignal<'a>(this:IObservable<'a>, initialValue:'a) =
        Signal.fromObservable initialValue this