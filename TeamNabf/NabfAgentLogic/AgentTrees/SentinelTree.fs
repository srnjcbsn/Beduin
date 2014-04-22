﻿namespace NabfAgentLogic
module SentinelTree =

    open FsPlanning.Agent.Planning
    open Sentinel
    open AgentTypes

    let getSentinelDesires : DesireTree<State,Intention> = 
            ManyDesires 
                [
                    Desire(applyToOccupyJob)

                    Desire(doOccupyJobThenParryIfEnemiesClose)

                    Desire(applyToDisruptJob)

                    Desire(doDisruptJobThenParryIfEnemiesClose)
                ]