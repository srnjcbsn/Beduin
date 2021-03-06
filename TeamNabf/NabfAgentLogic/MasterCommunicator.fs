﻿namespace NabfAgentLogic
    open FsPlanning.Agent
    open System
    open AgentTypes
    open JSLibrary.Data.GenericEvents;
    open Constants

    type MasterCommunicator() =
        class
            
            let mutable awaitingPercepts = []
            let mutable timerStarted = false
            

            let timerLock = new Object()
            let perceptLock = new Object()
            let actionLock = new Object()

            let NewPerceptsEvent = new Event<EventHandler, EventArgs>()
            let ActuatorReadyEvent = new Event<EventHandler, EventArgs>()
            let NewActionEvent = new Event<UnaryValueHandler<CommunicationAction>, UnaryValueEvent<CommunicationAction>>()

            [<CLIEvent>]
            member this.NewAction = NewActionEvent.Publish

            member this.StartTriggerTimer () =
                lock timerLock 
                    (fun () ->
                        if not timerStarted then
                            timerStarted <- true
                            let timer = new Timers.Timer(PERCEPT_TIME_BUFFER)
                            timer.AutoReset <- false
                            timer.Elapsed.Add(fun _ -> 
                                NewPerceptsEvent.Trigger(this, new EventArgs())
                                lock timerLock (fun () -> timerStarted <- false)
                                )
                            timer.Start()
                    )

            member this.SetMessage (msg:AgentServerMessage) =
                match msg with
                | JobMessage jobpercept -> 
                        lock perceptLock (fun () -> awaitingPercepts <- awaitingPercepts@[JobPercept jobpercept])
                | SharedPercepts percepts ->
                        lock perceptLock (fun () -> awaitingPercepts <- percepts @ awaitingPercepts)
                | _ -> ()

                this.StartTriggerTimer()


            interface Actuator<AgentAction> with
                member this.CanPerformAction action =
                    match action with
                    | Perform _ -> false
                    | Communicate _ -> true

                member this.PerformAction action =
                    match action with
                    | Communicate act ->
                        lock perceptLock (fun () -> awaitingPercepts <- (CommucationSent act)::awaitingPercepts)
                        match act with
                        | ShareKnowledge pl -> NewPerceptsEvent.Trigger(this, new EventArgs())
                        | _ -> ()
                        lock actionLock (fun () -> NewActionEvent.Trigger(this, new UnaryValueEvent<_>((act))))
                        ()
                    | _ -> ()

                member this.PerformActionBlockUntilFinished action = (this :> Actuator<AgentAction>).PerformAction action

                member this.IsReady =  true

                [<CLIEvent>]
                member this.ActuatorReady = ActuatorReadyEvent.Publish

            interface Sensor<Percept> with
                member this.ReadPercepts() = 
                    lock perceptLock (fun () ->
                        let percepts = awaitingPercepts
                        awaitingPercepts <- []
                        percepts)
                
                [<CLIEvent>]
                member this.NewPercepts = NewPerceptsEvent.Publish

        end 