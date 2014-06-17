﻿namespace NabfAgentLogic
module SentinelTree =

    open FsPlanning.Agent.Planning
    open Sentinel
    open AgentTypes
    open Common
    open Constants

    let getSentinelDesires : DesireTree<State,Intention> = 
            ManyDesires 
                [
                    Desire(unapplyFromJobsWhenDisabled)

                    Desire(applyToOccupyJob SENTINEL_OCCUPYJOB_MOD)

                    Desire(selfDefence)

                    //Desire(workOnOccupyJobThenParryIfEnemiesClose)

                    Desire(workOnOccupyJob)

                    Desire(applyToDisruptJob)

                    Desire(workOnDisruptJobThenParryIfEnemiesClose)
                ]