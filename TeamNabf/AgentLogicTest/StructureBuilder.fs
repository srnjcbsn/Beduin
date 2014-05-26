﻿namespace AgentLogicTest
module StructureBuilder =
    open NabfAgentLogic.AgentTypes
    


    let buildStateWithEnergy node role world energy = 
        {   World = world
            ;   Self =  {   Energy = Some energy                        
                        ;   Health = Some 0
                        ;   MaxEnergy = Some 30
                        ;   MaxEnergyDisabled = Some 30
                        ;   MaxHealth = Some 0
                        ;   Name = "gunner"
                        ;   Node = node
                        ;   Role = Some role
                        ;   Strength = Some 0
                        ;   Team = "Team Love Unit testing"
                        ;   Status = Normal
                        ;   VisionRange = Some 0
                        }
            ;   FriendlyData = []
            ;   EnemyData = List.Empty
            ;   InspectedEnemies = Set.empty
            ;   SimulationStep = 0
            ;   LastPosition = ""
            ;   NewVertices = []
            ;   NewEdges = []
            ;   LastStepScore = 0
            ;   Score = 0
            ;   ThisZoneScore = 0
            ;   LastActionResult = Successful
            ;   LastAction = Skip
            ;   TeamZoneScore = 0
            ;   Jobs = []
            ;   MyJobs = []
            ;   TotalNodeCount = List.length <| Map.toList world
            ;   MyExploredCount = 0
            ;   ProbedCount = 0  
            ;   NewKnowledge = []
            ;   Probed = Set.empty
            } : State

    let buildState node role world = buildStateWithEnergy node role world 30 