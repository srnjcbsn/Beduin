﻿namespace NabfAgentLogic
module LogicLib =
    
    open FsPlanning.Agent.Planning
    open AgentTypes
    open Graphing.Graph

    let nodeListContains n (nl:string list) =
        (List.tryFind (fun s -> s = n) nl).IsSome

    let listContains element elementList =
        (List.tryFind (fun s -> s = element) elementList).IsSome

    let agentHasFulfilledRequirement aName func state =
        (List.tryFind (fun ag -> (func ag) && ag.Name = aName) state.EnemyData).IsSome
        
    let neighbourNodes state (self:Agent) = 
        List.append (getNeighbourIds self.Node state.World) [self.Node]

    let nearbyEnemies state source = 
        List.filter (fun a -> nodeListContains a.Node (neighbourNodes state source)) state.EnemyData 
                
    let enemiesOnSource state source = 
        List.filter (fun a -> nodeListContains a.Node (neighbourNodes state source)) state.EnemyData 
        
    let nearbyAllies state = 
        List.filter (fun a -> nodeListContains a.Node (neighbourNodes state state.Self)) state.FriendlyData 

    let getJobsByType state (jobtype:JobType) : Job list = List.filter 
                                                            (
                                                                fun j -> 
                                                                    match j with
                                                                    | ((_, _, jt, _), _) when jt = jobtype -> true
                                                                    | _ -> false
                                                            ) state.Jobs

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
                 (excludeLesserJobs state calculateDesire (getJobsByType state jobtype))

    let getJobValueFromJoblist (list:(JobID*_) list) (s:State) : int =
        let (id,_) = list.Head
        let ((_,value,_,_),_) = (getJobFromJobID s id)
        value

    let getDistanceToJob (targetNode:VertexName) (s:State) : float =
        1.0//make my functionality


    //let isPartOfOccupyJob n (s:State) = List.exists (fun (j:Job) -> j ) s.Jobs