﻿namespace NabfAgentLogic.Perception
module AnalyzePercepts =
    open FsPlanning.Agent
    open FsPlanning.Agent.Planning
    open NabfAgentLogic.AgentTypes
    open Graphing.Graph
    open NabfAgentLogic.Logging
    open NabfAgentLogic.LogicLib
    open NabfAgentLogic.Search.HeuristicDijkstra
    open NabfAgentLogic.Constants
    open NabfAgentLogic.GeneralLib
    open NabfAgentLogic
    open DetermineSharedPercepts
    open PerceptionLib
    open AnalyzeJobs
    open AnalyzeMails

    (* handlePercept State -> Percept -> State *)
    let handlePercept state percept =
        match percept with
        | VisibleEntity (name, team, node, status) when name <> state.Self.Name ->
            let entData = (name, team, node, status)
            let updater = updateAgentWithEntdata entData true
            updateAgentListsOnState name team updater state 

        | VisibleEntity (name,_,_,_) -> 
            //Ignore percept of itself
            //logImportant Perception ("IT GETS A VISBLE ENTITY OF ITSELF" + name)
            state
        
        | InspectedEntity agent ->
            let updater = updateAgentWithAgent agent
            updateAgentListsOnState agent.Name agent.Team updater state
        | VertexSeen seenVertex ->
            match seenVertex with
            | (nodeName,Some team) when team <> OUR_TEAM -> 
                { state with NodesControlledByEnemy = Set.add nodeName state.NodesControlledByEnemy }
            | _ -> state

        | VertexProbed (name, value) ->
            logStateImportant state Perception <| sprintf "vertex probed percept %A %A" name value
            { state with 
                    World = addVertexValue name value state.World
            }
                    
        | EdgeSeen edge ->
            { state with
                    World = updateEdgeOnWorld edge state.World 
            }

            
        | SimulationStep step  -> { state with SimulationStep = step }
        | MaxEnergyDisabled energy -> state //prob not needed, part of Self percept
        | LastAction action    ->  { state with LastAction = action }

        | LastActionResult res -> { state with LastActionResult = res }
        | ZoneScore score      -> { state with ThisZoneScore = score }
        | Team team ->
            { state with 
                TeamZoneScore = team.ZoneScore
                LastStepScore = team.LastStepScore
                Score = team.Score
                Money = team.Money
            }
        | Self self ->
                
            let newSelf = { self with 
                                Name = state.Self.Name
                                Team = state.Self.Team
                                Role = state.Self.Role
            }
            let newSelfDisabled = { self with 
                                        Name = state.Self.Name
                                        Team = state.Self.Team
                                        Role = state.Self.Role
                                        MaxEnergy = self.MaxEnergyDisabled
            }
            match self.Status with
            | EntityStatus.Disabled -> 
                logImportant Perception <| sprintf "Max Energy (when disabled): %A (%A)" self.MaxEnergy self.MaxEnergyDisabled
                { state with Self = newSelfDisabled }
            | _ -> { state with Self = newSelf }
                
                
        | NewRoundPercept -> state //is here for simplicity, should not do anything

        | AgentRolePercept (name,team,role,certainty) -> 
            
            let updater = updateAgentWithRole name role certainty false
            if team = state.Self.Team then
                { state with FriendlyData = updateAgentList name updater state.FriendlyData }  
            else
                { state with EnemyData = updateAgentList name updater state.EnemyData }  
       
        | JobPercept job -> 
            updateStateWithJob job state
        | HeuristicUpdate (n1,n2,dist) -> 
            let (heuMap,countMap) = state.GraphHeuristic 
            {state with GraphHeuristic = (Map.add (n1,n2) dist heuMap,countMap)}

        | MailPercept mail ->
            updateStateWithMail mail state
        | CommucationSent cs -> 
            match cs with
            | ShareKnowledge pl -> 
                let updatedNK = List.filter (fun p -> not <| List.exists ((=) p) pl) state.NewKnowledge
                //logImportant <| sprintf "Clearing knowledge sent. We sent %A knowledge" pl.Length
                { state with NewKnowledge = updatedNK } 
//            | UnapplyJob id -> 
//                let updatedMyJobs = List.filter (fst >> ((<>) id)) state.MyJobs
//                { state with MyJobs =  updatedMyJobs } agents are now fired when they unapply from a job, so no need for "loop-back percept"
            | _ -> state

        | EdgeKnowledge edge ->
            { state with
                    World = updateEdgeOnWorld edge state.World 
            }
        | NodeKnowledge (name,Some value) ->
            { state with World = addVertexValue name value state.World; ExploredNodes = Set.add name state.ExploredNodes  } 
         | NodeKnowledge (name,None) ->
            { state with ExploredNodes = Set.add name state.ExploredNodes  }    
    let removedNodesControlledByEnemy state =
        { state with
            NodesControlledByEnemy = Set.empty
        }

    let generateFakeRolePercepts (state:State) =
        match state.LastAction, state.LastActionResult with
        | Attack name, FailedParried ->
            match tryFindAgentByName name state.EnemyData with
            | Some (agent) & Some ({RoleCertainty = certainty}) when certainty < 50 ->
                [AgentRolePercept (name,agent.Team, Sentinel, 50)]
            | _ -> []
        | _ -> []
            
            
    let generateFakeEdgePercept (oldState : State) (newState : State) =
        match (oldState.Self.Node, newState.LastAction, newState.LastActionResult) with
        | (fromVertex, Goto toVertex, Successful) -> 
            [EdgeSeen(Some (oldState.Self.Energy.Value - newState.Self.Energy.Value), fromVertex, toVertex)]
        | _ -> []
    
    let generateFakeNodeExploredPercepts (newState : State) =
        
        let isExplored node =     
            let heuMap =  fst newState.GraphHeuristic
            let [first;second] = List.sort [newState.Self.Node;node]  
            (heuMap.ContainsKey (first,second) && edgeDistance newState.Self.Node node newState < newState.Self.VisionRange.Value) 
                && (not <| Set.contains node newState.ExploredNodes) 
        let toPercept node = NodeKnowledge(node,None)
        let explorednodes = List.filter isExplored (Set.toList newState.NodesInVisionRange)
        logStateImportant newState Perception <| sprintf "Explored Nodes: %A" explorednodes.Length
        List.map toPercept explorednodes

    let generateFakePercepts (oldState : State) (newState : State) =    
        generateFakeEdgePercept oldState newState
        @generateFakeRolePercepts newState
        @generateFakeNodeExploredPercepts newState
       

    let updateLastPos (lastState:State) (state:State) =
        { state with LastPosition = lastState.Self.Node }

    let updateNodesInVision percepts (state:State) =
        let updateNode s percept =
            match percept with
            | VertexSeen (vn,_) ->
                { s with NodesInVisionRange = Set.add vn s.NodesInVisionRange }
            | _ -> s
        List.fold updateNode { state with NodesInVisionRange = Set.empty } percepts

    let removeAgentPositionsForVisibleNodes (state:State) =
        let removePos (agent:Agent) = 
            if Set.contains agent.Node state.NodesInVisionRange then { agent with Node = ""} else agent    
        { state with
            FriendlyData = List.map removePos state.FriendlyData
            EnemyData = List.map removePos state.EnemyData
        }

    let removeVisibilityFromAgents (state:State) = 
        let removeVision (agent:Agent) = { agent with IsInVisionRange = false }
        { state with 
            FriendlyData = List.map removeVision state.FriendlyData
            EnemyData = List.map removeVision state.EnemyData
        }


    let handlePercepts percepts state = List.fold handlePercept state percepts

    (* let updateState : State -> Percept list -> State *)
    let updateState state percepts = 
        
        let finalState = 
            match percepts with
            | NewRoundPercept::_ -> 
                let newStateWithOutHeuristics =
                    updateNodesInVision percepts state
                    |> removeAgentPositionsForVisibleNodes
                    |> removeVisibilityFromAgents
                    |> removedNodesControlledByEnemy
                    |> handlePercepts percepts
                    |> updateLastPos state
                let newState = updateHeuristic newStateWithOutHeuristics.Self.Node newStateWithOutHeuristics

                let fakepercepts = generateFakePercepts state newState
            
                let mergedState = handlePercepts fakepercepts newState
                                  |> selectSharedPercepts (fakepercepts@percepts) state
                                
                logImportant Perception ("Finished analyzing round percepts now at step " + mergedState.SimulationStep.ToString())
                mergedState       
                //{ mergedState with LastRoundState = Some state}    
            | _ -> 
                handlePercepts percepts state
        
        let allies = List.map (fun a -> (a.Name,a.Node,a.IsInVisionRange)) finalState.FriendlyData
        logStateImportant finalState Perception <| sprintf "Allies: %A" allies
        finalState

        
