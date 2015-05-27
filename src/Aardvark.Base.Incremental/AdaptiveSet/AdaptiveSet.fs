﻿namespace Aardvark.Base.Incremental

open System
open System.Runtime.CompilerServices
open System.Collections.Concurrent
open Aardvark.Base
open Aardvark.Base.Incremental.ASetReaders


module ASet =
    type AdaptiveSet<'a>(newReader : unit -> IReader<'a>) =
        let state = ReferenceCountingSet<'a>()
        let readers = WeakSet<BufferedReader<'a>>()

        let mutable inputReader = None
        let getReader() =
            match inputReader with
                | Some r -> r
                | None ->
                    let r = newReader()
                    inputReader <- Some r
                    r

        let bringUpToDate () =
            let r = getReader()
            let delta = r.GetDelta ()
            if not <| List.isEmpty delta then
                delta |> apply state |> ignore
                readers  |> Seq.iter (fun ri ->
                    ri.Emit(state, Some delta)
                )

        interface aset<'a> with
            member x.GetReader () =
                bringUpToDate()
                let r = getReader()

                let remove ri =
                    r.RemoveOutput ri
                    readers.Remove ri |> ignore

                    if readers.IsEmpty then
                        r.Dispose()
                        inputReader <- None

                let reader = new BufferedReader<'a>(bringUpToDate, remove)
                reader.Emit (state, None)
                r.AddOutput reader
                readers.Add reader |> ignore

                reader :> _

    type ConstantSet<'a>(content : seq<'a>) =
        let content = ReferenceCountingSet content

        interface aset<'a> with
            member x.GetReader () =
                let r = new BufferedReader<'a>()
                r.Emit(content, None)
                r :> IReader<_>

    type private EmptySetImpl<'a> private() =
        static let emptySet = ConstantSet [] :> aset<'a>
        static member Instance = emptySet

    let private scoped (f : 'a -> 'b) =
        let scope = Ag.getContext()
        fun v -> Ag.useScope scope (fun () -> f v)

    let empty<'a> : aset<'a> =
        EmptySetImpl<'a>.Instance

    let single (v : 'a) =
        ConstantSet [v] :> aset<_>

    let ofSeq (s : seq<'a>) =
        ConstantSet(s) :> aset<_>

    let ofList (l : list<'a>) =
        ConstantSet(l) :> aset<_>

    let ofArray (a : 'a[]) =
        ConstantSet(a) :> aset<_>

    let toList (set : aset<'a>) =
        use r = set.GetReader()
        r.GetDelta() |> ignore
        r.Content |> Seq.toList

    let toSeq (set : aset<'a>) =
        let l = toList set
        l :> seq<_>

    let toArray (set : aset<'a>) =
        use r = set.GetReader()
        r.GetDelta() |> ignore
        r.Content |> Seq.toArray

    let ofMod (m : IMod<'a>) =
        AdaptiveSet(fun () -> ofMod m) :> aset<_>

    let toMod (s : aset<'a>) =
        let r = s.GetReader()
        let c = r.Content :> IVersionedSet<_>

        let m = Mod.custom (fun () ->
            r.GetDelta() |> ignore
            c
        )
        r.AddOutput m
        m

    let contains (elem : 'a) (set : aset<'a>) =
        set |> toMod |> Mod.map (fun s -> s.Contains elem)

    let containsAll (elems : #seq<'a>) (set : aset<'a>) =
        set |> toMod |> Mod.map (fun s -> elems |> Seq.forall (fun e -> s.Contains e))

    let containsAny (elems : #seq<'a>) (set : aset<'a>) =
        set |> toMod |> Mod.map (fun s -> elems |> Seq.exists (fun e -> s.Contains e))


    let map (f : 'a -> 'b) (set : aset<'a>) = 
        let scope = Ag.getContext()
        AdaptiveSet(fun () -> set.GetReader() |> map scope f) :> aset<'b>

    let bind (f : 'a -> aset<'b>) (m : IMod<'a>) =
        let scope = Ag.getContext()
        AdaptiveSet(fun () -> m |> bind scope (fun v -> (f v).GetReader())) :> aset<'b>

    let bind2 (f : 'a -> 'b -> aset<'c>) (ma : IMod<'a>) (mb : IMod<'b>) =
        let scope = Ag.getContext()
        AdaptiveSet(fun () -> bind2 scope (fun a b -> (f a b).GetReader()) ma mb) :> aset<'c>


    let collect (f : 'a -> aset<'b>) (set : aset<'a>) = 
        let scope = Ag.getContext()
        AdaptiveSet(fun () -> set.GetReader() |> collect scope (fun v -> (f v).GetReader())) :> aset<'b>

    let choose (f : 'a -> Option<'b>) (set : aset<'a>) =
        let scope = Ag.getContext()
        AdaptiveSet(fun () -> set.GetReader() |> choose scope f) :> aset<'b>

    let filter (f : 'a -> bool) (set : aset<'a>) =
        choose (fun v -> if f v then Some v else None) set

    let concat (set : aset<aset<'a>>) =
        collect id set

    let concat' (set : seq<aset<'a>>) =
        concat (ConstantSet set)

    let collect' (f : 'a -> aset<'b>) (set : seq<'a>) =
        set |> Seq.map f |> concat'
    
    let filterM (f : 'a -> IMod<bool>) (s : aset<'a>) =
        s |> collect (fun v ->
            v |> f |> bind (fun b -> if b then single v else empty)
        )


    let union (sets : aset<aset<'a>>) =
        collect id sets


    let reduce (f : seq<'a> -> 'b) (s : aset<'a>) : IMod<'b> =
        s |> toMod |> Mod.map f

    let foldSemiGroup (add : 'a -> 'a -> 'a) (zero : 'a) (s : aset<'a>) : IMod<'a> =
        let r = s.GetReader()
        let sum = ref zero

        let res =
            Mod.custom (fun () ->
                let mutable rem = false
                let delta = r.GetDelta()
                for d in delta do
                    match d with
                        | Add v -> sum := add !sum v
                        | Rem v -> rem <- true

                if rem then
                    sum := r.Content |> Seq.fold add zero

                !sum
            )

        r.AddOutput res
        res

    let foldGroup (add : 'a -> 'a -> 'a) (sub : 'a -> 'a -> 'a) (zero : 'a) (s : aset<'a>) : IMod<'a> =
        let r = s.GetReader()
        let sum = ref zero

        let res =
            Mod.custom (fun () ->
                let delta = r.GetDelta()
                for d in delta do
                    match d with
                        | Add v -> sum := add !sum v
                        | Rem v -> sum := sub !sum v
                !sum
            )

        r.AddOutput res
        res

    let reduceM (f : seq<'a> -> 'b) (s : aset<IMod<'a>>) : IMod<'b> =
        s |> toMod |> Mod.bind (Mod.mapN f)

    let foldSemiGroupM (add : 'a -> 'a -> 'a) (zero : 'a) (s : aset<IMod<'a>>) : IMod<'a> =
        let r = s.GetReader()
        let sum = ref zero

        let res = ref Unchecked.defaultof<_>
        res :=
            Mod.custom (fun () ->
                let mutable rem = false
                let delta = r.GetDelta()
                for d in delta do
                    match d with
                        | Add v -> 
                            v.AddOutput !res
                            sum := add !sum (v.GetValue())
                        | Rem v -> 
                            v.RemoveOutput !res
                            rem <- true

                if rem then
                    sum := r.Content |> Seq.fold (fun s v -> add s (v.GetValue())) zero

                !sum
            )

        r.AddOutput !res
        !res

    let foldGroupM (add : 'a -> 'a -> 'a) (sub : 'a -> 'a -> 'a) (zero : 'a) (s : aset<IMod<'a>>) : IMod<'a> =
        let r = s.GetReader()
        let sum = ref zero

        let res = ref Unchecked.defaultof<_>
        res :=
            Mod.custom (fun () ->
                let delta = r.GetDelta()
                for d in delta do
                    match d with
                        | Add v -> 
                            v.AddOutput !res
                            sum := add !sum (v.GetValue())
                        | Rem v -> 
                            v.RemoveOutput !res
                            sum := sub !sum (v.GetValue())
                !sum
            )

        r.AddOutput !res
        !res


    let inline sum (s : aset<'a>) = foldGroup (+) (-) LanguagePrimitives.GenericZero s
    let inline product (s : aset<'a>) = foldGroup (*) (/) LanguagePrimitives.GenericOne s
    let inline sumM (s : aset<IMod<'a>>) = foldGroupM (+) (-) LanguagePrimitives.GenericZero s
    let inline productM (s : aset<IMod<'a>>) = foldGroupM (*) (/) LanguagePrimitives.GenericOne s

    let private callbackTable = ConditionalWeakTable<IAdaptiveObject, ConcurrentHashSet<IDisposable>>()
    type private CallbackSubscription(m : IAdaptiveObject, cb : unit -> unit, live : ref<bool>, reader : IDisposable, set : ConcurrentHashSet<IDisposable>) =
        
        member x.Dispose() = 
            if !live then
                live := false
                reader.Dispose()
                m.MarkingCallbacks.Remove cb |> ignore
                set.Remove x |> ignore

        interface IDisposable with
            member x.Dispose() = x.Dispose()

        override x.Finalize() =
            try x.Dispose()
            with _ -> ()


    /// <summary>
    /// registers a callback for execution whenever the
    /// set's value might have changed and returns a disposable
    /// subscription in order to unregister the callback.
    /// Note that the callback will be executed immediately
    /// once here.
    /// </summary>
    let registerCallback (f : list<Delta<'a>> -> unit) (set : aset<'a>) =
        let m = set.GetReader()
        let f = scoped f
        let self = ref id
        let live = ref true
        self := fun () ->
            if !live then
                try
                    m.GetDelta() |> f
                finally 
                    m.MarkingCallbacks.Add !self |> ignore
        
        lock m (fun () ->
            !self ()
        )

        let set = callbackTable.GetOrCreateValue(m)
        let s = new CallbackSubscription(m, !self, live, m, set)
        set.Add s |> ignore
        s :> IDisposable

