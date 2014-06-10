﻿namespace NabfAgentLogic
module GoalSpecifications =

    open AgentTypes
    open Graphing.Graph
    open LogicLib
    open Constants
    open Logging

    let agentAt agentName state =
        let agent = List.find (fun ag -> ag.Name = agentName) (state.EnemyData @ state.FriendlyData)
        agent.Node

    let agentAttacked agent state = 
        let enemy = List.find (fun (enemy : Agent) -> enemy.Name = agent) state.EnemyData
        enemy.Status = Disabled

    let vertexProbed vertex state = 
        Option.isSome state.World.[vertex].Value

    let agentInspected agent state =
        let enemy = List.find (fun enemy -> enemy.Name = agent) state.EnemyData
        enemy.Role.IsSome

    let parried state = 
        state.LastAction = Parry
    
    let charged charge (state : State) =
        match charge with
        | Some charge -> charge >= state.Self.Energy.Value
        | None -> state.LastAction = Recharge

    let agentRepaired agent state =
        state.LastAction = Repair agent


    let generateSomeValue state = 
        let n = state.World.[state.Self.Node] 
        if (n.Value.IsSome ) then//&& nodeHasNoOtherFriendlyAgentsOnIt state n.Identifier
            n.Value.Value >= SOME_VALUE_VALUE
        else
            false

    let generateLittleValue state = 
        let n = state.World.[state.Self.Node] 
        if (n.Value.IsSome ) then//&& nodeHasNoOtherFriendlyAgentsOnIt state n.Identifier
            n.Value.Value >= LITTLE_VALUE_VALUE
        else
            false

    let generateLeastValue state = 
        let n = state.World.[state.Self.Node] 
        if (n.Value.IsSome ) then//&& nodeHasNoOtherFriendlyAgentsOnIt state n.Identifier
            n.Value.Value >= LEAST_VALUE_VALUE
        else
            false

    let generateMinValue state = 
        let n = state.World.[state.Self.Node] 
        if (n.Value.IsSome ) then//&& nodeHasNoOtherFriendlyAgentsOnIt state n.Identifier
            n.Value.Value >= MINIMUM_VALUE_VALUE
        else
            false

    let generateGoalCondition goal =
        match goal with
        | At vertex 
        | Explored vertex -> fun state -> state.Self.Node = vertex
        | Attacked agent -> agentAttacked agent
        | Probed vertex -> vertexProbed vertex
        | Inspected agent -> agentInspected agent
        | Parried -> parried
        | Charged charge -> charged charge
        | GenerateSomeValue -> generateSomeValue
        | GenerateLittleValue -> generateLittleValue
        | GenerateLeastValue -> generateLeastValue
        | GenerateMinValue -> generateMinValue
        | Repaired agent -> agentRepaired agent

    let distanceHeuristics vertex =
        distanceBetweenAgentAndNode vertex

    let goalHeuristics goal =
        match goal with
        | At vertex 
        | Explored vertex
        | Probed vertex -> 
            distanceHeuristics vertex

        | Attacked agent 
        | Repaired agent
        | Inspected agent -> 
            fun state -> distanceHeuristics (agentAt agent state) state

        | Charged _
        | GenerateSomeValue
        | GenerateLittleValue
        | GenerateLeastValue
        | GenerateMinValue
        | Parried-> fun _ -> 0