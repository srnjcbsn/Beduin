﻿namespace NabfAgentLogic
module Common =

    open FsPlanning.Agent.Planning
    open AgentTypes
    open LogicLib
    open Constants
    open Graphing.Graph
    


    ///////////////////////////////////Helper functions//////////////////////////////////////
    
    //Calculate the desire to an occupy job
    let calculateDesireOccupyJob  modifier (j:Job) (s:State)= 
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
        int <| (((float newValue) * personalValueMod) - (float oldJobValue))    +     (-(distanceToJob * DISTANCE_TO_OCCUPY_JOB_MOD))    +    modifier


    //Try to find any repair jobs put up by the agent itself.
    let rec tryFindRepairJob (inputState:State) (knownJobs:Job list) =
            match knownJobs with
            | (_ , rdata) :: tail -> if rdata = RepairJob(inputState.Self.Node,inputState.Self.Name) then Some knownJobs.Head else tryFindRepairJob inputState tail
            | [] -> None

            
    let nodeIsUnexplored (state:State) node =
        let n = state.World.[node] 
        not <| Set.exists (fun (e:DirectedEdge) -> (fst e).IsSome) n.Edges


    let nodeHasMinValue (state:State) node =
        let n = state.World.[node] 
        if (n.Value.IsSome) then
            n.Value.Value >= MINIMUM_VALUE_VALUE
        else
            false

    ////////////////////////////////////////Logic////////////////////////////////////////////

    //An agent always wants to have exactly one goal
    let onlyOneJob (inputState:State) = 
        Some(
                "have at most 1 job"
                , Communication
                , [Plan(fun state -> 
                                        match state.MyJobs with
                                        | [] -> []
                                        | _::tail -> 
                                            List.map (fun (id,_) -> Communicate (UnapplyJob id)) tail                                       
                        )
                  ]
            )

    //Try to make it so the agent has explored one more node
    let exploreMap (inputState:State) = 
        findAndDo inputState.Self.Node nodeIsUnexplored "mark as explored" inputState
//        if inputState.ExploredCount < inputState.TotalNodeCount
//        then
//            let count = inputState.MyExploredCount
//            Some("explore one more node."
//                ,Activity
//                ,[Requirement(
//                    ((fun state -> state.MyExploredCount > count),)
//                    )]
//                )
//        else
//            None

    //When disabled, post a repair job, then recharge while waiting for a repairer. Temporary version to be updated later.
    //Works by creating a plan to recharge one turn each turn.
    let getRepaired (inputState:State) = 
        if inputState.Self.Status = Disabled 
        then
            let j = tryFindRepairJob inputState inputState.Jobs
            let myName = inputState.Self.Name
            match j with
            //I already created a job:
            | Some(_,RepairJob(_,myName)) -> 
                Some("wait for a repairer.",Activity,[Plan(fun s -> [Perform(Recharge)])])
            //Otherwise, create the job, then start waiting
            | _ -> 
                let here = inputState.Self.Node
                Some("get repaired.",Activity,[Plan(fun state -> [
                                                                 Communicate( CreateJob( (None,5,JobType.RepairJob,1),RepairJob(state.Self.Node,state.Self.Name) ) )
                                                                 ]);Requirement(((fun state -> state.LastAction = Recharge),None))])
        else
            None
            
    //Find a node of at leas value 8 to stand on.
    let generateMinimumValue (inputState:State) = 
        //findAndDo inputState.Self.Node nodeHasMinValue "generate value" inputState
        let targetOpt = findTargetNode inputState.Self.Node nodeHasMinValue inputState
        match targetOpt with
        | None -> None
        | Some target ->
                Some
                        (   "get minimum value at " + target
                        ,   Activity
                        ,   [
                                Requirement((fun state -> (state.Self.Node = target)), Some (distanceBetweenAgentAndNode target));
                                Plan(fun s -> [Perform(Recharge)])
                            ]
                        )

    let shareKnowledge (s:State) : Option<Intention> =
         Some ("share my knowledge", Communication, [Plan (fun state -> [(Communicate <| ShareKnowledge ( state.NewKnowledge))] )])
    
    
    let applyToOccupyJob  modifier (inputState:State) = 
        let applicationList = createApplicationList inputState JobType.OccupyJob (calculateDesireOccupyJob modifier)
        Some(
                "apply to all occupy jobs"
                , Communication
                , [Plan(fun state -> applicationList)]
            )
    

    let workOnOccupyJob (inputState:State) =
        let myJobs = List.map (fun (id,_) -> getJobFromJobID inputState id) inputState.MyJobs
        let myOccupyJobs = getJobsByType JobType.OccupyJob myJobs
        match myOccupyJobs with
        | ((id,_,_,_),_)::_ -> 
            let (_,node) = List.find (fun (jid,_) -> id.Value = jid) inputState.MyJobs
            Some
                (   "occupy node " + node
                ,   Activity
                ,   [
                        Requirement <| ((fun state -> state.Self.Node = node), Some (fun state -> (distanceBetweenNodes state.Self.Node node state)))
                    ;   Plan <| fun _ -> [Perform Recharge]
                    ]
                )
        | [] -> None