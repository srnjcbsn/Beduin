namespace NabfAgentLogic
module GoalSpecifications =

    open AgentTypes
    open Graphing.Graph
    open LogicLib
    open Constants
    open Logging
    open GeneralLib

    let agentAt agentName state =
        let agent = List.find (fun ag -> ag.Name = agentName) (state.EnemyData @ state.FriendlyData)
        agent.Node

    let agentAttacked agent state = 
        let enemy = List.find (fun (enemy : Agent) -> enemy.Name = agent) state.EnemyData
        enemy.Status = Disabled

    let vertexProbed vertex state = 
        Option.isSome state.World.[vertex].Value

    let vertexInspected vertex state =
        List.forall (fun enemy -> Option.isSome enemy.Role) (enemiesHere state vertex)

    let parried state = 
        state.LastAction = Parry
    
    let charged charge (state : State) =
        match charge with
        | Some charge -> charge >= state.Self.Energy.Value
        | None -> state.LastAction = Recharge

    let agentRepaired agent state =
        match List.tryFind (fun a -> a.Name = agent) state.FriendlyData with
        | Some (agent) -> agent.Status = Normal
        | None -> false
        //state.LastAction = Repair agent
    
    let getCloseTo (agentName:AgentName) (state:State) =
        match tryFindAgentsByName agentName state.FriendlyData with
        | Some ({Node = vn},_) when vn <> ""-> 
            let nodes = vn::(getNeighbourIds vn state.World)
            List.exists ((=) state.Self.Node) nodes
        | _ -> true

    let atMinValueNode value state = 
        let n = state.World.[state.Self.Node] 
        if (n.Value.IsSome && nodeHasNoOtherFriendlyAgentsOnIt state n.Identifier) then
            n.Value.Value >= value
        else
            false

    let generateGoalCondition goal =
        match goal with
        | At vertex 
        | Explored vertex -> fun state -> state.Self.Node = vertex
        | Attacked agent -> agentAttacked agent
        | Probed vertex -> vertexProbed vertex
        | Inspected vertex -> vertexInspected vertex
        | Parried -> parried
        | Charged charge -> charged charge
        | AtMinValueNode value -> atMinValueNode value
        | Repaired agent -> agentRepaired agent
        | GetCloseTo agent -> getCloseTo agent

    let distanceHeuristics vertex =
        distanceBetweenAgentAndNode vertex

    let goalHeuristics goal =
        match goal with
        | At vertex 
        | Explored vertex
        | Probed vertex
        | Inspected vertex -> 
            distanceHeuristics vertex
        
        | GetCloseTo agent
        | Attacked agent 
        | Repaired agent ->
            fun state -> distanceHeuristics (agentAt agent state) state

        | Charged _
        | AtMinValueNode _
        | Parried-> fun _ -> 0

    let goalVertex goal state =
        match goal with
        | At vertex 
        | Explored vertex
        | Inspected vertex
        | Probed vertex ->
            Some <| vertex
        
        | Attacked agent 
        | Repaired agent ->
            Some <| agentAt agent state

        | _ -> None
