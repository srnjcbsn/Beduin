﻿namespace NabfAgentLogic
module AgentClientLib =
    open System
    open Graphing.Graph
    open JSLibrary.IiLang
    open JSLibrary.IiLang.DataContainers
    open AgentTypes
    open IiLang.IiLangDefinitions
    open IiLang.IilTranslator
    open Logging
    open Constants

    let parseIilPercepts (perceptCollection:IilPerceptCollection) : ServerMessage =
            let percepts = parsePerceptCollection perceptCollection
            parseIilServerMessage percepts

    let buildIilSendMessage ((id,act):SendMessage) =
        IiLang.IiLangDefinitions.buildIilAction (IiLang.IilTranslator.buildIilMetaAction act id)

    let buildInitState (name, simData:SimStartData) =
            {   World = Map.empty
            ;   Self =  {   Energy = Some 0                        
                        ;   Health = Some 0
                        ;   MaxEnergy = Some 0
                        ;   MaxEnergyDisabled = Some 0
                        ;   MaxHealth = Some 0
                        ;   Name = name
                        ;   Node = ""
                        ;   Role = Some (simData.SimRole)
                        ;   Strength = Some 0
                        ;   Team = OUR_TEAM
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
            ;   Money = 0
            ;   Score = 0
            ;   ThisZoneScore = 0
            ;   LastActionResult = Successful
            ;   LastAction = Skip
            ;   TeamZoneScore = 0
            ;   Jobs = []
            ;   TotalNodeCount = 0
            ;   ExploredCount = 0
            ;   MyExploredCount = 0
            ;   MyProbedCount = 0
            ;   ProbedCount = 0            
            } : State