﻿namespace Aardvark.Base

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module Dictionary =
    open System.Collections.Generic

    let empty<'k, 'v when 'k : equality> = Dictionary<'k, 'v>()
    let emptyNoEquality<'k,'v> = 
        Dictionary<'k,'v>(
            { new IEqualityComparer<'k> with 
                    member x.Equals(a,b)    = Unchecked.equals a b
                    member x.GetHashCode(t) = Unchecked.hash t 
            })

    let inline add (key : 'k) (value : 'v) (d : Dictionary<'k, 'v>) =
        d.Add(key,value)

    let inline set (key : 'k) (value : 'v) (d : Dictionary<'k, 'v>) =
        d.[key] <- value

    let inline remove (key : 'k) (d : Dictionary<'k, 'v>) =
        d.Remove key

    let inline clear (d : Dictionary<'k, 'v>) =
        d.Clear()


    let inline map (f : 'k -> 'a -> 'b) (d : Dictionary<'k, 'a>) =
        let result = Dictionary()
        for (KeyValue(k,v)) in d do
            result.[k] <- f k v
        result

    let inline mapKeys (f : 'k -> 'a -> 'b) (d : Dictionary<'k, 'a>) =
        let result = Dictionary()
        for (KeyValue(k,v)) in d do
            result.[f k v] <- v
        result

    let inline union (dicts : #seq<Dictionary<'k, 'v>>) =
        let result = Dictionary()
        for d in dicts do
            for v in d do
                result.[v.Key] <- v.Value
        result

    let inline contains (key : 'k) (d : Dictionary<'k, 'v>) =
        d.ContainsKey key

    let inline tryFind (key : 'k) (d : Dictionary<'k, 'v>) =
        match d.TryGetValue key with
            | (true, v) -> Some v
            | _ -> None

    let inline ofSeq (elements : seq<'k * 'v>) =
        let result = Dictionary()
        for (k,v) in elements do
            result.[k] <- v
        result

    let inline ofList (elements : list<'k * 'v>) =
        ofSeq elements

    let inline ofArray (elements : ('k * 'v)[]) =
        ofSeq elements

    let inline ofMap (elements : Map<'k, 'v>) =
        elements |> Map.toSeq |> ofSeq

    let inline toSeq (d : Dictionary<'k, 'v>) =
        d |> Seq.map (fun (KeyValue(k,v)) -> k,v)

    let inline toList (d : Dictionary<'k, 'v>) =
        d |> toSeq |> Seq.toList

    let inline toArray (d : Dictionary<'k, 'v>) =
        d |> toSeq |> Seq.toArray

    let inline toMap (d : Dictionary<'k, 'v>) =
        d |> toSeq |> Map.ofSeq

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module Dict =

    #if __SLIM__
    let empty<'k, 'v> = Dictionary.emptyNoEquality<'k,'v>
    #else
    let empty<'k, 'v> = Dict<'k,'v>()
    #endif

    let inline add (key : 'k) (value : 'v) (d : Dict<'k, 'v>) =
        d.Add(key,value)

    let inline set (key : 'k) (value : 'v) (d : Dict<'k, 'v>) =
        d.[key] <- value

    let inline remove (key : 'k) (d : Dict<'k, 'v>) =
        d.Remove key

    let inline clear (d : Dict<'k, 'v>) =
        d.Clear()

    let inline keys (k : Dict<'k,'v>) =
        #if __SLIM__
        k.Keys |> Seq.map id
        #else
        k.Keys
        #endif
    let inline values (k : Dict<'k,'v>) =
        #if __SLIM__
        k.Values |> Seq.map id
        #else
        k.Values
        #endif
    let inline keyValues (k : Dict<'k,'v>) =
        #if __SLIM__
        k |> Seq.map id
        #else
        k.KeyValuePairs
        #endif

    let inline map (f : 'k -> 'a -> 'b) (d : Dict<'k, 'a>) =
        let result = Dict()
        for (KeyValue(k,v)) in d do
            result.[k] <- f k v
        result

    let inline mapKeys (f : 'k -> 'a -> 'b) (d : Dict<'k, 'a>) =
        let result = Dict()
        for (KeyValue(k,v)) in d do
            result.[f k v] <- v
        result

    let inline union (dicts : #seq<Dict<'k, 'v>>) =
        let result = Dict()
        for d in dicts do
            for (KeyValue(k,v)) in d do
                result.[k] <- v
        result

    let inline contains (key : 'k) (d : Dict<'k, 'v>) =
        d.ContainsKey key

    let inline tryFind (key : 'k) (d : Dict<'k, 'v>) =
        match d.TryGetValue key with
            | (true, v) -> Some v
            | _ -> None

    let inline ofSeq (elements : seq<'k * 'v>) =
        let result = Dict()
        for (k,v) in elements do
            result.[k] <- v
        result

    let inline ofList (elements : list<'k * 'v>) =
        ofSeq elements

    let inline ofArray (elements : ('k * 'v)[]) =
        ofSeq elements

    let inline ofMap (elements : Map<'k, 'v>) =
        elements |> Map.toSeq |> ofSeq

    let inline toSeq (d : Dict<'k, 'v>) =
        d |> Seq.map (fun (KeyValue(k,v)) -> k,v)

    let inline toList (d : Dict<'k, 'v>) =
        d |> toSeq |> Seq.toList

    let inline toArray (d : Dict<'k, 'v>) =
        d |> toSeq |> Seq.toArray  

    let inline toMap (d : Dict<'k, 'v>) =
        d |> toSeq |> Map.ofSeq

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module SymDict =

    let empty<'v> = SymbolDict<'v>()

    let inline add (key : Symbol) (value : 'v) (d : SymbolDict<'v>) =
        d.Add(key,value)

    let inline set (key : Symbol) (value : 'v) (d : SymbolDict<'v>) =
        d.[key] <- value

    let inline remove (key : Symbol) (d : SymbolDict<'v>) =
        d.Remove key

    let inline clear (d : SymbolDict<'v>) =
        d.Clear()


    let inline map (f : Symbol -> 'a -> 'b) (d : SymbolDict<'a>) =
        let result = SymbolDict()
        for (KeyValue(k,v)) in d do
            result.[k] <-  f k v
        result

    let inline mapKeys (f : Symbol -> 'a -> Symbol) (d : SymbolDict<'a>) =
        let result = SymbolDict()
        for (KeyValue(k,v)) in d do
            result.[f k v] <- v
        result

    let inline union (dicts : #seq<SymbolDict<'v>>) =
        let result = SymbolDict()
        for d in dicts do
            for v in d do
                result.[v.Key] <- v.Value
        result

    let inline contains (key : Symbol) (d : SymbolDict<'v>) =
        d.ContainsKey key

    let inline tryFind (key : Symbol) (d : SymbolDict<'v>) =
        match d.TryGetValue key with
            | (true, v) -> Some v
            | _ -> None

    let inline ofSeq (elements : seq<Symbol * 'v>) =
        let result = SymbolDict()
        for (k,v) in elements do
            result.Add(k,v)
        result

    let inline ofList (elements : list<Symbol * 'v>) =
        ofSeq elements

    let inline ofArray (elements : (Symbol * 'v)[]) =
        ofSeq elements

    let inline ofMap (elements : Map<Symbol, 'v>) =
        elements |> Map.toSeq |> ofSeq

    let inline toSeq (d : SymbolDict<'v>) =
        d |> Seq.map (fun (KeyValue(k,v)) -> k,v)

    let inline toList (d : SymbolDict<'v>) =
        d |> toSeq |> Seq.toList

    let inline toArray (d : SymbolDict<'v>) =
        d |> toSeq |> Seq.toArray  

    let inline toMap (elements : SymbolDict<'v>) =
        elements |> toSeq |> Map.ofSeq
