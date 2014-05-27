﻿namespace NabfAgentLogic.Search
module HeuristicDijkstra =
    open FsPlanning.Search
    open FsPlanning.Search.Problem
    open FsPlanning.Search.Astar
    open Graphing.Graph
    open Graphing

    type Action = 
        | Move of VertexName 
    
    let cost (world:Graph) node1 (Move node2) =
        let vertex = world.[node1]
        let ne = Set.filter (fun (_,name) -> name=node2) vertex.Edges
        match Set.toList ne with
        | [((Some c), name)] -> c
        | _ -> 1

    let allDistances world from = 
        let prob =  {
                        InitialState = from
                        GoalTest  = (fun _ -> true)
                        Actions   = (fun a -> List.map (fun b -> Move b ) (Graph.getNeighbourIds a world))
                        Result    = (fun a (Move b) -> b)
                        StepCost  = cost world
                        Heuristic = (fun _ c -> c)
                    } : Problem<_,_,_>
        let a = Astar.aStarAllPaths prob
        Astar.allStatesWithCost a

    let allDistancesMap world from =
        let distances = allDistances world from
        let maplist = List.collect (fun (cost,node) -> [((node,from),cost);((from,node),cost)]) distances
        Map.ofList maplist
    
    let folder (state:Map<VertexName*VertexName,int>) (node,nodesWithCost) = 
        let expand = List.map (fun (cost,onode) -> ((node,onode),cost)) nodesWithCost
        Map.ofList ((Map.toList state)@expand)

    let allPairsDistances world =
        let all = List.map (fun a -> (a,(allDistances world a))) <| List.map fst (Map.toList world)
        List.fold folder Map.empty all
        