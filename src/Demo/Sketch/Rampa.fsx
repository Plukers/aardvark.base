﻿#I @"..\..\..\bin\Release"
#I @"..\..\..\Packages\Rx-Core\lib\net45"
#I @"..\..\..\Packages\Rx-Interfaces\lib\net45"
#I @"..\..\..\Packages\Rx-Linq\lib\net45"
#r "Aardvark.Base.dll"
#r "Aardvark.Base.Essentials.dll"
#r "Aardvark.Base.TypeProviders.dll"
#r "Aardvark.Base.FSharp.dll"
#r "Aardvark.Base.Incremental.dll"
#r "System.Reactive.Core.dll"
#r "System.Reactive.Interfaces.dll"
#r "System.Reactive.Linq.dll"

open System
open System.Threading
open System.Linq
open System.Collections.Generic
open Aardvark.Base
open Aardvark.Base.Incremental
open System.Reactive
open System.Reactive.Linq



type IBuffer =
    inherit IAdaptiveObject
    inherit IDisposable
    abstract member IsEmpty : bool

type IBuffer<'a> =
    inherit IBuffer
    abstract member Dequeue : IAdaptiveObject -> Option<'a>


type Token =
    | Time
    | Reactive of (unit -> IBuffer)

type Context(caller : IAdaptiveObject) =
    let buffers = Dict<Token, IBuffer>()
    let mutable containsTime = false

    member x.IsEmpty =
        not containsTime && lock buffers (fun () -> buffers.Values |> Seq.forall (fun b -> b.IsEmpty))

    member x.Listen(t : Token) =
        match t with
            | Time -> 
                containsTime <- true
            | Reactive create ->
                lock buffers (fun () -> buffers.[t] <- create())


    member x.Unlisten(t : Token) =
        match t with
            | Time ->
                containsTime <- false
            | Reactive _ ->
                match lock buffers (fun () -> buffers.TryRemove t) with
                    | (true, b) -> b.Dispose()
                    | _ -> ()

    member x.DequeValue<'a>(t : Token) : Option<'a> =
        match t with
            | Time -> 
                DateTime.Now |> unbox |> Some
            | _ ->
                match lock buffers (fun () -> buffers.TryGetValue t) with
                    | (true, (:? IBuffer<'a> as b)) ->
                        b.Dequeue caller
                    | _ ->
                        None



type Event<'a> =
    | Event of 'a
    | NoEvent

type sf<'a, 'b> =
    abstract member run : Context -> 'a -> 'b * sf<'a, 'b>
    abstract member dependencies : list<Token>

module SF =

    type private ModBuffer<'a>(m : IMod<'a>) as this =
        inherit AdaptiveObject()

        let mutable live = 1
        let q = Queue<'a> [m.GetValue this]
        

        override x.Mark() =
            if live = 1 then
                let v = m.GetValue x
                q.Enqueue v

            true

        member x.Dispose() =
            let wasLive = Interlocked.Exchange(&live,0)
            if wasLive = 1 then
                lock x (fun () ->
                    m.RemoveOutput x
                    q.Clear()
                )


        member x.IsEmpty = lock x (fun () -> q.Count = 0)

        member x.Dequeue(caller : IAdaptiveObject) =
            x.EvaluateAlways caller (fun () ->
                if live = 1 then 
                    if q.Count = 0 then None
                    else q.Dequeue() |> Some
                else 
                    None
            )

        interface IDisposable with
            member x.Dispose() = x.Dispose()

        interface IBuffer with
            member x.IsEmpty = x.IsEmpty

        interface IBuffer<'a> with
            member x.Dequeue caller = x.Dequeue caller

    type private ObservableBuffer<'a>(o : IObservable<'a>) as this =
        inherit AdaptiveObject()

        let mutable live = 1
        let mutable markingPending = 0
        let q = Queue<'a>()



        let push (v : 'a) =
            lock this (fun () ->
                q.Enqueue v
            )

            let alreadyMarking = Interlocked.Exchange(&markingPending, 1)
            if alreadyMarking = 0 then
                transact (fun () -> this.MarkOutdated())

        let sub = o.Subscribe push

        member x.Dispose() =
            let wasLive = Interlocked.Exchange(&live,0)
            if wasLive = 1 then
                sub.Dispose()
                lock x (fun () -> q.Clear())


        member x.IsEmpty = lock q (fun () -> q.Count = 0)

        member x.Dequeue(caller : IAdaptiveObject) =
            x.EvaluateAlways caller (fun () ->
                if live = 1 then 
                    if q.Count = 0 then None
                    else q.Dequeue() |> Some
                else 
                    None
            )

        interface IDisposable with
            member x.Dispose() = x.Dispose()

        interface IBuffer with
            member x.IsEmpty = x.IsEmpty

        interface IBuffer<'a> with
            member x.Dequeue caller = x.Dequeue caller



    type private ModSignal<'a, 'b>(e : IMod<'b>) =
        let token = Reactive (fun () -> new ModBuffer<'b>(e) :> IBuffer)

        member x.run (ctx : Context) (i : 'a) =
            match ctx.DequeValue<'b> token with
                | Some b -> Event b, x :> sf<_,_>
                | None -> NoEvent, x :> sf<_,_>

        interface sf<'a, Event<'b>> with
            member x.run ctx i = x.run ctx i
            member x.dependencies = [token]
               
    type private EventSignal<'a, 'b>(e : IObservable<'b>) =
        let token = Reactive (fun () -> new ObservableBuffer<'b>(e) :> IBuffer)     

        member x.run (ctx : Context) (i : 'a) =
            match ctx.DequeValue<'b> token with
                | Some b -> Event b, x :> sf<_,_>
                | None -> NoEvent, x :> sf<_,_>

        interface sf<'a, Event<'b>> with
            member x.run ctx i = x.run ctx i
            member x.dependencies = [token]
         
    type private SimpleSignal<'a, 'b>(f : 'a -> 'b) =
        interface sf<'a, 'b> with
            member x.run _ i = f i, x :> sf<_,_>
            member x.dependencies = []
      
    type private ConstantSignal<'a, 'b>(value : 'b) =
        interface sf<'a, 'b> with
            member x.run _ _ = value, x :> sf<_,_>
            member x.dependencies = []

    type private CustomSignal<'a, 'b>(f : sf<'a,'b> -> Context -> 'a -> 'b * sf<'a, 'b>, deps : list<Token>) as this =
        let f = f this
        interface sf<'a, 'b> with
            member x.run ctx v = f ctx v
            member x.dependencies = deps
     
            

    let ofMod (m : IMod<'a>) : sf<'x, Event<'a>> =
        ModSignal(m) :> _

    let ofObservable (m : IObservable<'a>) : sf<'x, Event<'a>> =
        EventSignal(m) :> _

    let arr (f : 'a -> 'b) : sf<'a, 'b> =
        SimpleSignal(f) :> _

    let constant (v : 'b) : sf<'a, 'b> =
        ConstantSignal(v) :> _

    let custom (dependencies : list<Token>) (f : sf<'a, 'b> -> Context -> 'a -> 'b * sf<'a, 'b>) : sf<'a, 'b> =
        CustomSignal(f, dependencies) :> _

    let rec mapOut (f : 'b -> 'c) (sf : sf<'a, 'b>) =
        custom sf.dependencies (fun self ctx a ->
            let (b, cont) = sf.run ctx a
            (f b), mapOut f cont
        )

    let rec mapIn (f : 'a -> 'b) (sf : sf<'b, 'c>) =
        custom sf.dependencies (fun self ctx a ->
            let (b, cont) = sf.run ctx (f a)
            b, mapIn f cont
        )

    let rec switch (guard : sf<'a, Event<'b>>) (onEvent : sf<'b, 'c>) (noEvent : sf<'a, 'c>) : sf<'a, 'c> =
        let deps = List.concat [guard.dependencies; onEvent.dependencies; noEvent.dependencies]
        custom deps (fun _ ctx a ->
            let (gv, gc) = guard.run ctx a
            match gv with
                | Event b ->
                    let (ev, ec) = onEvent.run ctx b
                    ev, switch gc ec noEvent

                | NoEvent ->
                    let (nv, nc) = noEvent.run ctx a
                    nv, switch gc onEvent nc
        )

    let rec integrate (seed : 's) (acc : 's -> 'a -> 's) (m : sf<'x, 'a>) =
        custom m.dependencies (fun _ ctx x ->
            let (mv,mc) = m.run ctx x
            let s = acc seed mv
            s, integrate s acc mc
        )

    type private Reactimator<'a, 'b>(sf : sf<'a, 'b>, input : 'a, output : 'b -> unit) as this =
        inherit AdaptiveObject()

        let ctx = Context(this)
        let evt = new ManualResetEventSlim(true)

        let mutable current = sf
        let mutable currentDeps =
            let res = sf.dependencies |> HashSet
            for a in res do ctx.Listen a
            res

        member x.StepEvent = evt

        member x.Step() =
            x.EvaluateAlways null (fun () ->
                let (v, newSf) = current.run ctx input
                output v

                let newDeps = newSf.dependencies |> HashSet

                let added = newDeps |> Seq.filter (currentDeps.Contains >> not)
                let removed = currentDeps |> Seq.filter (newDeps.Contains >> not)

                for r in removed do ctx.Unlisten r
                for a in added do ctx.Listen a

                if ctx.IsEmpty then
                    evt.Reset()


                current <- newSf
                currentDeps <- newDeps
            )

        override x.Mark() =
            evt.Set()
            true

    let reactimate (input : 'a) (output : 'b -> unit) (sf : sf<'a, 'b>) =
        
        let r = Reactimator(sf, input, output)
        r.Step()

        let rec run() =
            async {
                let! worked = Async.AwaitWaitHandle r.StepEvent.WaitHandle

                if not worked then
                    printfn "reactimate failed"
                else
                    r.Step()
                    return! run()

            }

        run()


[<AutoOpen>]
module Operators =
    let rec (>>>) (l : sf<'a, 'b>) (r : sf<'b, 'c>) : sf<'a, 'c> =
        SF.custom (l.dependencies @ r.dependencies) (fun self ctx a ->
            let (lv, lc) = l.run ctx a
            let (rv, rc) = r.run ctx lv
            rv, lc >>> rc
        )

    let (<<<) (r : sf<'b, 'c>) (l : sf<'a, 'b>) : sf<'a, 'c> =
        l >>> r

    let (||>) (l : sf<'a, 'b>) (r : 'b -> 'c) : sf<'a, 'c> =
        l |> SF.mapOut r

    let (<||) (r : 'b -> 'c) (l : sf<'a, 'b>) : sf<'a, 'c> =
        l |> SF.mapOut r







let test () = 
    let step = Mod.init 0

    // whenever we have a step use it otherwise step by 0
    let delta = 
        SF.switch (SF.ofMod step) 
            (SF.arr (fun v -> [v]))
            (SF.constant [])

    // integrate all steps
    let res = 
        delta |> SF.integrate [] (List.append)

    // start the sf as task
    res |> SF.reactimate () (printfn "out: %A") |> Async.Start

    


    transact(fun () -> Mod.change step 10)

    let mutable finish = false
    while not finish do
        let l = Console.ReadLine()

        match Int32.TryParse l with
            | (true, i) -> 
                transact(fun () -> Mod.change step i)
            | _ -> 
                if l.StartsWith "q" then finish <- true
                else ()

    ()


