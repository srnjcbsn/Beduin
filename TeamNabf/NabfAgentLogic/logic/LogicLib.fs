namespace NabfAgentLogic
module LogicLib =
    
    open FsPlanning.Agent.Planning
    open AgentTypes
    open Graphing.Graph
    open Constants
    open FsPlanning.Search
    open FsPlanning.Search.Problem
    open ActionSpecifications

    let flip f x y = f y x

    let normalIntention (label,intentionType,objectives) =
        {
            Label = label;
            Type = intentionType;
            Objectives = objectives;
            ChangeStateAfter = None
            ChangeStateBefore = None
        }

    let nodeListContains n (nl:string list) =
        (List.tryFind (fun s -> s = n) nl).IsSome

    let listContains element elementList =
        (List.tryFind (fun s -> s = element) elementList).IsSome

    let probedVertices (world : Graph) =
        List.choose (fun (name,vertex:Vertex)-> if vertex.Value.IsSome then Some name else None ) <| Map.toList world

    //WARNING: DEPRECATED
    let agentHasFulfilledRequirementEnemies aName state func =
        (List.tryFind (fun ag -> (func ag) && ag.Name = aName) state.EnemyData).IsSome
    
    //WARNING: DEPRECATED
    let agentHasFulfilledRequirementFriendlies aName state func =
        (List.tryFind (fun ag -> (func ag) && ag.Name = aName) state.FriendlyData).IsSome
        
    let neighbourNodes state (self:Agent) = 
        List.append (getNeighbourIds self.Node state.World) [self.Node]

    let nearbyEnemies state source = 
        List.filter (fun a -> nodeListContains a.Node (neighbourNodes state source)) state.EnemyData 
        
    let checkIfEnemyOnNode state node =
        let agentlist = List.filter (fun a -> a.Node = node) state.EnemyData
        agentlist.Length >= 1
        
    let nearbyAllies state = 
        List.filter (fun a -> nodeListContains a.Node (neighbourNodes state state.Self)) state.FriendlyData 

    let isUnexplored state vertex = 
        (not (List.exists (fun (value, _) -> Option.isSome value) <| Set.toList state.World.[vertex].Edges)) && vertex <> state.Self.Node

    let getJobsByType (jobtype:JobType) (list : Job list) : Job list = List.filter 
                                                                        (
                                                                            fun j -> 
                                                                                match j with
                                                                                | ((_, _, jt, _), _) when jt = jobtype -> true
                                                                                | _ -> false
                                                                        ) list

    let getJobId (job:Job) =
        let ((id, _, _, _),_) = job
        id

    let getJobFromJobID (s:State) (jid:JobID) : Job =
        (List.filter (fun j -> (getJobId j).Value = jid) s.Jobs).Head

    let excludeLesserJobs (s:State) calculateDesire (jobs:Job list) =
        if (s.MyJobs.IsEmpty) then
            jobs
        else
            let (id,_) = s.MyJobs.Head
            List.filter (fun j -> (calculateDesire j s) > (calculateDesire (getJobFromJobID s id) s)) jobs


    let createApplication id desire = 
        Communicate(ApplyJob(id,desire))     
        
    let createApplicationList state jobtype calculateDesire = 
        List.map (
                    fun (job:Job) -> 
                        let id = (getJobId job).Value
                        let desire = (calculateDesire job state)
                        (createApplication id desire)
                 ) 
                 (excludeLesserJobs state calculateDesire (getJobsByType jobtype state.Jobs))

    let getJobValueFromJoblist (list:(JobID*_) list) (s:State) : int =
        let (id,_) = list.Head
        let ((_,value,_,_),_) = (getJobFromJobID s id)
        value

    //pathfind through the graph. When the path is found, count it's length and return it
    //returns: (dist to job * number of enemy node)
    let getDistanceToJobAndNumberOfEnemyNodes (targetNode:VertexName) (s:State) =
        let distance_to_job = 1.0        

        distance_to_job     


    //let isPartOfOccupyJob n (s:State) = List.exists (fun (j:Job) -> j ) s.Jobs


    let distanceBetweenNodes node1 node2 (state:State) : int =
        let (heuMap,_) = state.GraphHeuristic
        let [nodeA;nodeB] = List.sort [node1; node2]
        match Map.tryFind (nodeA,nodeB) heuMap with
        | Some (cost,dist) ->
            //let rechargesRequiredCost = (cost / (state.Self.MaxEnergy.Value/2)) * turnCost state
            let minimumTraversalCost = dist * turnCost state
            minimumTraversalCost + cost
        | None -> INFINITE_HEURISTIC

    let distanceBetweenAgentAndNode node state : int = distanceBetweenNodes state.Self.Node node state
    
    let findTargetNode startNode condition (state:State) = 
        let nodesWithCond = List.filter (condition state) <| (List.map fst <| Map.toList state.World)
        let distNodes = List.map (fun v -> ((distanceBetweenNodes startNode v state),v)) nodesWithCond        
        match distNodes with
        | [] -> None
        | [single] -> Some <| snd single
        | nodes -> Some ( snd <| List.min nodes )

    let findNextBestNode startNode condition (state:State) = 
        let nodesWithCond = List.filter (condition state) <| (List.map fst <| Map.toList state.World)
        let distNodes = List.map (fun v -> ((distanceBetweenNodes startNode v state),v)) nodesWithCond        
        match distNodes with
        | [] -> None
        | [single] -> Some <| snd single
        | nodes -> 
                    let newNodes = nodes
                    let filteredNodes = List.filter (fun n -> not((List.min nodes) = n) ) newNodes
                    Some ( snd <| List.min filteredNodes )

        
    let findNextBestUnexplored state =
        let isVertexUnExplored vertex = 
            List.forall (fun (cost, _) -> Option.isSome cost) (Set.toList state.World.[vertex].Edges)

        let definiteCost cost = 
            match cost with 
            | Some c -> c
            | None -> Constants.MINIMUM_EDGE_COST

        let goalTest statePair = 
            match statePair with
            | (Some oldVertex, newVertex) when oldVertex <> newVertex -> 
                isVertexUnExplored newVertex
            | _ -> false

        let result (oldVertex, _) (_, resultVertex) =
            match oldVertex with
            | Some vertex -> (Some vertex, resultVertex)
            | None ->
                if isVertexUnExplored resultVertex then
                    (Some resultVertex, resultVertex)
                else
                    (None, resultVertex)

        let pathProblem = 
            { InitialState = (None, state.World.[state.Self.Node].Identifier)
            ; GoalTest = goalTest
            ; Actions = fun (_, vertex) -> Set.toList state.World.[vertex].Edges
            ; Result = result
            ; StepCost = fun _ (cost, _) -> definiteCost cost
            ; Heuristic = fun _ cost -> cost
            }
        
        let solution = Astar.solve Astar.aStar pathProblem (fun () -> false)
        match solution with
        | Some solution -> 
            Some <| snd (List.head <| List.rev solution.Path)
        | None -> None

    let findNextBestUnprobed state =
        let isVertexUnprobed vertex = state.World.ContainsKey(vertex) && state.World.[vertex].Value.IsNone

        let definiteCost cost = 
            match cost with 
            | Some c -> c
            | None -> Constants.MINIMUM_EDGE_COST

        let goalTest statePair = 
            match statePair with
            | (Some oldVertex, newVertex) when oldVertex <> newVertex -> 
                isVertexUnprobed newVertex
            | _ -> false

        let result (oldVertex, _) (_, resultVertex) =
            match oldVertex with
            | Some vertex -> (Some vertex, resultVertex)
            | None ->
                if isVertexUnprobed resultVertex then
                    (Some resultVertex, resultVertex)
                else
                    (None, resultVertex)

        let pathProblem = 
            { InitialState = (None, state.World.[state.Self.Node].Identifier)
            ; GoalTest = goalTest
            ; Actions = fun (_, vertex) -> Set.toList state.World.[vertex].Edges
            ; Result = result
            ; StepCost = fun _ (cost, _) -> definiteCost cost
            ; Heuristic = fun _ cost -> cost
            }
        
        let solution = Astar.solve Astar.aStar pathProblem (fun () -> false)
        match solution with
        | Some solution -> 
            Some <| snd (List.head <| List.rev solution.Path)
        | None -> None

    let myRankIsGreatest myName (other:Agent List) =
        let qq = List.filter (fun a -> a.Name < myName) other
        qq.IsEmpty


    let nearestVertexSatisfying (state : State) (condition : (State -> VertexName -> bool)) =
        let satisfying = List.map fst (Map.toList state.World)
                         |> List.filter (condition state)
        if List.length satisfying > 0 then
            Some (List.minBy (flip distanceBetweenAgentAndNode <| state) satisfying )
        else
            None

        
    let nodeHasNoOtherFriendlyAgentsOnIt (inputState:State) node : bool =
        let friendliesOnNode = List.filter (fun a -> a.Node = node) inputState.FriendlyData
        if (friendliesOnNode.Length = 1) then //is it me standing on the node?
            friendliesOnNode.Head.Name = inputState.Self.Name
        elif (friendliesOnNode.Length = 0) then //no one is standing on the node
            true
        else //more than 1 is standing on the node, including myself, so don't want
            false