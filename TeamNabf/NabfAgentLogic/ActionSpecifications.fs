namespace NabfAgentLogic
module ActionSpecifications =

    open NabfAgentLogic.AgentTypes
    open Graphing.Graph
    open Constants
    open Logging
    open GeneralLib



    type ConditionResult = 
        | Success
        | Failure of string

    type Condition = (State -> ConditionResult)

    let condToBool condres =
        match condres with
        | Success -> true
        | Failure _ -> false

    let turnCost (state : State) =
        state.Self.MaxEnergy.Value / 2

    [<CustomEquality>]
    [<CustomComparison>]
    type ActionSpecification =
        { ActionType    : AgentAction
        ; Preconditions : Condition list 
        ; Effect        : State -> State
        ; Cost          : State -> int
        }
        override x.GetHashCode() = 0
        override self.Equals (other) = 
            match other with
            | :? ActionSpecification as spec -> spec.ActionType = self.ActionType
            | _ -> false

        interface System.IComparable with
            member self.CompareTo yobj =
                match yobj with
                | :? ActionSpecification as spec -> compare spec.ActionType self.ActionType
                | _ -> failwith "fsharp sucks"
    
    let isNotDisabled state = 
        match state.Self.Status = Normal with
        | true -> Success
        | false -> Failure "Agent is disabled"

    let enoughEnergy cost state = 
        match state.Self.Energy.Value >= cost with
        | true -> Success
        | false -> Failure <| sprintf "Not enough energy (cost: %A, have: %A)" cost state.Self.Energy.Value

    let deductEnergy cost state =
        { state.Self with Energy = Some <| state.Self.Energy.Value - cost }

    let definiteOptimisticCost cost = 
        match cost with 
        | Some c -> c
        | None -> Constants.MINIMUM_EDGE_COST
    
    let definitePessimisticCost cost =
        match cost with
        | Some c -> c
        | None -> Constants.MAXIMUM_EDGE_COST

    let decide defaultValue opt =
        match opt with
        | Some ret -> ret
        | None -> defaultValue

    let findAgentPosition (agent : AgentName) agentList =
        let findAgent = List.find (fun a -> a.Name = agent) agentList
        findAgent.Node

    let inRangeOfAgent state agent fromList = 
        match state.Self.Node = findAgentPosition agent fromList with
        | true -> Success
        | false -> Failure <| sprintf "Not in range of agent %A" agent

    let inAttackRangeOfAgent state agentName fromList = 
        let agent = List.find (fun a -> a.Name = agentName) fromList
        match agent.Role with
        | Some Sentinel when agent.RoleCertainty >= MINIMUM_ROLE_CERTAINTY ->
            let neighbours = getNeighbourIds state.Self.Node state.World
            match Option.isSome (List.tryFind ((=) agent.Node) neighbours) with
            | true -> Success
            | false -> Failure <| sprintf "Not in range of agent %A" agent
        | _ -> inRangeOfAgent state agentName fromList

    let rec tryRemoveFromList selector list =
        match list with
        | head :: tail when selector head -> Some (head, tail)  
        | head :: tail -> match (tryRemoveFromList selector tail) with
                          | Some (h, l) -> Some (h, head :: tail)
                          | None -> None
        | [] -> None

    let removeFromList selector list =
        match tryRemoveFromList selector list with
        | Some result -> result
        | None -> failwith "removeFromList: Element not found"

    let communicationAction action =
        { ActionType = Communicate action
        ; Preconditions = []
        ; Effect = fun state -> state
        ; Cost = fun _ -> 0
        }

    let moveAction (destination : VertexName) = 
        let edgeCost state = 
//            logImportant <| sprintf "At: %A, neighbours: %A, destination: %A" state.Self.Node state.World.[state.Self.Node].Edges destination
            let neighbour = 
                state.World.[state.Self.Node].Edges 
                |> Set.toList 
                |> List.tryFind (fun (_, name) -> name = destination)

            match neighbour with
            | Some (cost, _) -> Some cost
            | None -> None
        

        let cost state = decide Constants.ACTION_COST_CHEAP (edgeCost state).Value

        let updateState state = 
            let self = { state.Self with Node = destination }
            let newSelf = deductEnergy (cost state) { state with Self = self}
            let edge = 
                match List.find (snd >> ((=) destination)) <| Set.toList state.World.[state.Self.Node].Edges with
                | (Some cost, vn) -> (Some cost, state.Self.Node, vn)
                | (None, vn) -> (Some (MINIMUM_EDGE_COST), state.Self.Node, vn)

            { state with 
                Self = newSelf; 
                LastAction = Action.Goto destination;
                World = addEdge edge state.World
                ExploredNodes = Set.add destination state.ExploredNodes
                NodesInVisionRange = Set.add destination state.NodesInVisionRange
            }


        let canMoveTo state = 
            match edgeCost state with
            | Some cost ->
                if (state.Self.Energy.Value - definitePessimisticCost cost) >= 0 then
                    Success
                else 
                    Failure "Not enough energy"
            | None -> Failure "Not a neighbour"

        { ActionType    = Perform <| Goto destination
        ; Preconditions = [ canMoveTo ]
        ; Effect        = updateState
        ; Cost          = cost
        }

    let attackAction (enemyAgent : AgentName) =
        let canAttack state = inAttackRangeOfAgent state enemyAgent state.EnemyData

        let updateState state =
            let attacked, rest = List.partition (fun e -> e.Name = enemyAgent) state.EnemyData 
            let updateAtt = { List.head attacked with Status = Disabled }
            { state with EnemyData = updateAtt::rest; 
                         Self = deductEnergy Constants.ACTION_COST_EXPENSIVE state
                         LastAction = Action.Attack enemyAgent
                         PlannerDisabledEnemies = Set.add enemyAgent state.PlannerDisabledEnemies}
        
        { ActionType    = Perform <| Attack enemyAgent
        ; Preconditions = [ canAttack; enoughEnergy Constants.ACTION_COST_EXPENSIVE; isNotDisabled ]
        ; Effect        = updateState
        ; Cost          = fun state -> turnCost state + Constants.ACTION_COST_EXPENSIVE
        }

    let rechargeAction =
        let updateState state = 
//            logImportant "updating state rechargeAction"
            let newEnergy = state.Self.Energy.Value + (int ((float state.Self.MaxEnergy.Value) * RECHARGE_FACTOR)) 
            { state with Self = { state.Self with Energy = Some newEnergy}; LastAction = Recharge }
        { ActionType    = Perform <| Recharge
        ; Preconditions = [  ]
        ; Effect        = updateState
        ; Cost = fun state -> turnCost state
        }       

    let repairAction (damagedAgent : AgentName) =
//        let canRepair state = inRangeOfAgent state damagedAgent state.FriendlyData

        let isNotSelf (state:State) = 
            match state.Self.Name <> damagedAgent with
            | true -> Success
            | false -> Failure "Unable to repair self"

        let isInRange state =
            if isAgentHere damagedAgent state then
                Success
            else
                Failure <| sprintf "Agent %A is not here" damagedAgent

        let repairCost state = 
            match state.Self.Status with
            | Normal   -> Constants.ACTION_COST_EXPENSIVE
            | Disabled -> Constants.ACTION_COST_DISABLED

        let updateState state =
            let repairedAgent, rest = removeFromList (fun a -> a.Name = damagedAgent) state.FriendlyData 
            let updateAgent = { repairedAgent with Health = repairedAgent.MaxHealth; Status = Normal }
            { state with FriendlyData = updateAgent :: rest;
                         Self = deductEnergy (repairCost state) state 
                         LastAction = Action.Repair damagedAgent
                         PlannerRepairedAgents = Set.add damagedAgent state.PlannerRepairedAgents
            }

        { ActionType    = Perform <| Repair damagedAgent
        ; Preconditions = [ isInRange; isNotSelf; fun state -> enoughEnergy (repairCost state) state ]
        ; Effect        = updateState
        ; Cost          = fun state -> turnCost state + repairCost state
        }

    let probeAction vertexOption =
        let realVertex state = 
            match vertexOption with
            | Some vertex -> vertex
            | None -> state.Self.Node
        
        let vertexUnProbed state = 
            match state.World.[realVertex state].Value with
            | None -> Success
            | Some _ -> Failure <| sprintf "Vertex %A is already probed" (realVertex state)

        let inRange state =
            let trueness = 
                match vertexOption with
                | Some vertex -> 
                    Set.contains vertex (Set.ofList <| getNeighbourIds state.Self.Node state.World)
                | None -> 
                    realVertex state = state.Self.Node

            if trueness then
                Success
            else
                Failure <| sprintf "not at or adjacent to %A" (realVertex state)

        let updateState state = 
//            logImportant "updating state probeAction"
            let vertex = (realVertex state)
            let newWorld = addVertexValue vertex 0 state.World
            { state with 
                    World = newWorld
                    Self = deductEnergy Constants.ACTION_COST_CHEAP state
                    PlannerProbed = Set.add vertex state.PlannerProbed
                    LastAction = Action.Probe vertexOption
            }

        let cost state = 
            match vertexOption with
            | Some vertex -> turnCost state + Constants.ACTION_COST_CHEAP + 1
            | None -> turnCost state + Constants.ACTION_COST_CHEAP

        { ActionType    = Perform <| Probe vertexOption
        ; Preconditions = [ inRange; vertexUnProbed; enoughEnergy Constants.ACTION_COST_CHEAP; isNotDisabled ]
        ; Effect        = updateState
        ; Cost          = fun state -> turnCost state + Constants.ACTION_COST_CHEAP
        }

    let surveyAction =
//        let edgesAreUnsurveyed state = 
//            let rangeOneEdges = state.World.[state.Self.Node].Edges          
//            
//            match 0 = (Set.count <| Set.filter (fun (value,_) -> Option.isNone value) rangeOneEdges) with
//            | false -> Success
//            | true -> Failure <| sprintf "Edges around me is already surveyed"

        let updateState state = 
//            logImportant "updating state surveyAction"
            let rangeOneEdges = state.World.[state.Self.Node].Edges 
            let undirectedEdges = Set.map (fun (cost, toV) -> (cost, state.Self.Node, toV)) rangeOneEdges
            let newWorld = Set.fold (fun graph edge -> addEdge edge graph) state.World undirectedEdges            
            { state with 
                    World = newWorld
                    Self = deductEnergy Constants.ACTION_COST_CHEAP state
                    LastAction = Action.Survey
            }

        { ActionType    = Perform <| Survey
        ; Preconditions = [ enoughEnergy Constants.ACTION_COST_CHEAP; isNotDisabled ]
        ; Effect        = updateState
        ; Cost          = fun state -> turnCost state + Constants.ACTION_COST_CHEAP
        }

    let inspectAction vertexNameOption =
        let whichVertex state = 
            match vertexNameOption with
            | Some vertex -> vertex
            | None -> state.Self.Node
        let agentNames (state : State) = 
            match vertexNameOption with
            | Some vertexName -> [ vertexName ]
            | None -> List.filter (fun enemy -> enemy.Node = state.Self.Node) state.EnemyData |> List.map (fun enemy -> enemy.Name)

        let enemiesNotInspected state = 
            let res = not <| List.forall (fun enemy -> Option.isSome enemy.Role) (enemiesHere state state.Self.Node)
//            logImportant <| sprintf "result: %A %A" res (enemiesHere state state.Self.Node)
//            let res = not <| List.forall (fun enemy -> Set.contains enemy state.InspectedEnemies) (agentNames state)
            match res with
            | true -> Success
            | false -> Failure <| sprintf "No uninspected agents present"

        let newEnemyData state =
            let vertex = whichVertex state

            let updatedEnemies = 
                List.map (fun enemy -> {enemy with Role = Some Explorer}) (enemiesHere state vertex)
            
            let enemiesNotHere = 
                Set.toList <| Set.difference (Set.ofList state.EnemyData) (Set.ofList <| enemiesHere state vertex)
            
            enemiesNotHere @ updatedEnemies


        let updateState (state : State) = 
            let inspectedEnemyNames = 
                [for enemy in enemiesHere state <| whichVertex state -> enemy.Name] 
                |> Set.ofList
            { state with 
                EnemyData = newEnemyData state
                Self = deductEnergy Constants.ACTION_COST_EXPENSIVE state
                LastAction = Action.Inspect vertexNameOption
                PlannerInspectedEnemies = inspectedEnemyNames + state.PlannerInspectedEnemies
            }

        { ActionType    = Perform <| Inspect vertexNameOption
        ; Preconditions = [ enemiesNotInspected; enoughEnergy Constants.ACTION_COST_EXPENSIVE; isNotDisabled ]
        ; Effect        = updateState
        ; Cost          = fun state -> turnCost state + Constants.ACTION_COST_EXPENSIVE
        }

    let parryAction =
        
        let updateState state =
            { state with Self = deductEnergy Constants.ACTION_COST_EXPENSIVE state }

//        let saboteurPresent state = 
//            let res = List.exists (fun enemy -> enemy.Node = state.Self.Node && (enemy.Role = None || enemy.Role = Some Saboteur)) state.EnemyData
//            match res with 
//            | true -> Success
//            | false -> Failure "No saboteur present"

        { ActionType    = Perform <| Parry
        ; Preconditions = [ enoughEnergy Constants.ACTION_COST_EXPENSIVE; isNotDisabled ]
        ; Effect        = updateState
        ; Cost          = fun state -> turnCost state + Constants.ACTION_COST_EXPENSIVE
        }

    let skipAction =
        { ActionType    = Perform Skip
        ; Preconditions = []
        ; Effect        = fun state -> state
        ; Cost          = fun _ -> 0
        }

    let buyAction upgrade =
        { ActionType = Perform <| Buy upgrade
        ; Preconditions = [ enoughEnergy Constants.ACTION_COST_EXPENSIVE; isNotDisabled ]
        ; Effect = fun state -> state
        ; Cost = fun _ -> 0
        }

    let unSatisfiedPreconditions state actionSpec =
        let failmsg = function
        | Failure msg -> Some msg
        | Success -> None

        List.choose failmsg <| List.map (fun prec -> prec state) actionSpec.Preconditions

    let isApplicable state actionSpec = 
        List.isEmpty <| unSatisfiedPreconditions state actionSpec

    let agentsAt node agentList =
        List.filter (fun agent -> agent.Node = node) agentList
        |> List.map (fun agent -> agent.Name)

    let gotoActions (state : State) = 
        List.map moveAction <| getNeighbourIds state.Self.Node state.World
    
    let isAdjacentSentinel agentName state =
        let neighbours = getNeighbourIds state.Self.Node state.World
        let agent = List.find (fun a -> a.Name = agentName) state.EnemyData
        Option.isSome <| List.tryFind ((=) agent.Node) neighbours

    let attackActions state agent = 
        if (isAgentHere agent state) || (isAdjacentSentinel agent state) then
            [attackAction agent]
        else
            []

    let rechargeActions state = [rechargeAction]

    let repairActions  (state : State) agent = 
        if isAgentHere agent state then
            [repairAction agent]
        else
            []

    let probeActions state  vertex= 
        let rangedActions = 
            List.map (Some >> probeAction) (List.filter (isUnprobed state) <| adjacentDeadEnds state)
            
        if state.Self.Node = vertex then
            probeAction None :: rangedActions
        else 
            rangedActions

    let inspectActions state vertex = 
        let someUninspectedEnemy = 
            not <| List.forall (fun enemy -> Option.isSome enemy.Role) (enemiesHere state vertex)
        if vertex = state.Self.Node && someUninspectedEnemy then
            [inspectAction None]
        else 
            []

    let parryActions state = [parryAction]

    let commonActions state = gotoActions state @ rechargeActions state

    let actionSpecification (action : AgentAction) =
        match action with
        | Communicate comm -> communicationAction comm
        | Perform act -> 
            match act with
            | Attack agent -> attackAction agent
            | Goto vn -> moveAction vn
            | Inspect opt -> inspectAction opt
            | Parry -> parryAction
            | Probe opt -> probeAction opt
            | Recharge -> rechargeAction
            | Repair agent -> repairAction agent
            | Skip -> rechargeAction
            | Survey -> surveyAction//failwith "survey does not have an actionspecification"
            | Buy upgrade -> buyAction upgrade

