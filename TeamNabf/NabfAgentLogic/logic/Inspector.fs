﻿namespace NabfAgentLogic
module Inspector =

    open FsPlanning.Agent.Planning
    open AgentTypes
    open LogicLib
    open Constants
    open Graphing.Graph

    let distanceToOccupyJobMod = 0.1

    ///////////////////////////////////Helper functions//////////////////////////////////////
    let calculateDesireOccupyJob (j:Job) (s:State) = 
        let ((_,newValue,_,_),(jobData)) = j      
        let oldJobValue = 
                            if (s.MyJobs.IsEmpty) then
                                0
                            else
                                (getJobValueFromJoblist s.MyJobs s)

        let jobTargetNode = 
            match jobData with
            | OccupyJob (_,zone) -> zone.Head
        

        let (distanceToJob,personalValueMod) = (getDistanceToJobAndNumberOfEnemyNodes jobTargetNode s)
        
        //final desire
        int <| (((float newValue) * personalValueMod) - (float oldJobValue)) + (-(distanceToJob * DISTANCE_TO_OCCUPY_JOB_MOD)) + INSPECTOR_OCCUPYJOB_MOD
   

    let nodeHasUninspectedAgent (state:State) node =
        List.exists (fun a -> a.Role.IsNone && a.Node = node) state.EnemyData

    ////////////////////////////////////////Logic////////////////////////////////////////////

    
    let spontanousInspectAgent (inputState:State) = 
        let uninspectedNearbyEnemies = List.filter (fun a -> a.Role.IsNone) (nearbyEnemies inputState inputState.Self)
        match uninspectedNearbyEnemies with
        | [] -> None
        | head::tail ->     
            Some(
                    "inspect agent " + head.Name
                    , Activity
                    , [Requirement (Inspected head.Name)]
                )

    let applyToOccupyJob (inputState:State) = 
        let applicationList = createApplicationList inputState JobType.OccupyJob calculateDesireOccupyJob
        Some(
                "apply to all occupy jobs"
                , Communication
                , [Plan(fun state -> Some applicationList)]
            )
    
    let applyToDisruptJob (inputState:State) = None //advanced feature
    
    let workOnDisruptJob (inputState:State) = None //advanced feature
    
    let findAgentToInspect (inputState:State) = 
        let neighbours = List.filter ((<>) inputState.LastPosition) <| getNeighbourIds inputState.Self.Node inputState.World
        let rand = System.Random()
        let index = rand.Next(0, List.length neighbours)
        let target = List.nth neighbours index
        Some
            (   "go to node " +  target
            ,   Activity
            ,   [Requirement (At target)]
            )