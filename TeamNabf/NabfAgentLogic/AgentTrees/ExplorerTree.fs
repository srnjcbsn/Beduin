﻿namespace NabfAgentLogic
module ExplorerTree =

    open FsPlanning.Agent.Planning
    open Explorer
    open AgentTypes
    open Common
    open Constants

    let getExplorerDesires : DesireTree<State,Intention> = 
            ManyDesires 
                [
                    Desire(unapplyFromJobsWhenDisabled)

                    Desire(workOnOccupyJob)

                    Desire(findNewIslandZone)
                    Desire(findNewZone)
                    
                    Conditional
                        (   lightProbingDone,
                            Desire(applyToOccupyJob EXPLORER_OCCUPYJOB_MOD)
                        )

                    Desire(probeThisAndAdjacentDeadEnds)
                    Desire(findNodeToProbe)
                ]