﻿namespace NabfAgentLogic
    open FsPlanning.Agent
    open System
    open AgentTypes
    open JSLibrary.Data.GenericEvents;

    type MasterCommunicator() =
        class
            
            let mutable awaitingPercepts = []


            let perceptLock = new Object()
            let actionLock = new Object()

            let NewPerceptsEvent = new Event<EventHandler, EventArgs>()
            let ActuatorReadyEvent = new Event<EventHandler, EventArgs>()
            let NewActionEvent = new Event<UnaryValueHandler<int*CommunicationAction>, UnaryValueEvent<int*CommunicationAction>>()

            [<CLIEvent>]
            member this.NewAction = NewActionEvent.Publish

            member this.SetMessage (msg:AgentServerMessage) =
                match msg with
                | JobMessage jobpercept -> 
                        lock perceptLock (fun () -> awaitingPercepts <- jobpercept::awaitingPercepts)
                | SharedPercepts percepts ->
                        ()
                | _ -> ()
                 
                NewPerceptsEvent.Trigger(this, new EventArgs())

            interface Actuator<AgentAction> with
                member this.CanPerformAction action =
                    match action with
                    | Perform _ -> false
                    | Communicate _ -> true

                member this.PerformAction action =
                    match action with
                    | Communicate act ->
                        //lock actionLock (fun () -> NewActionEvent.Trigger(this, new UnaryValueEvent<_>((id,act))))
                        ()
                    | _ -> ()
                member this.IsReady =  true

                [<CLIEvent>]
                member this.ActuatorReady = ActuatorReadyEvent.Publish

            interface Sensor<Percept> with
                member this.ReadPercepts() = 
//                    lock perceptLock (fun () ->
//                        let percepts = awaitingPercepts
//                        awaitingPercepts <- []
//                        percepts)
                        []
                [<CLIEvent>]
                member this.NewPercepts = NewPerceptsEvent.Publish

        end 