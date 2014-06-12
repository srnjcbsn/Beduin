﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using NabfProject.NewNoticeBoardModel;
using NabfProject.AI;
using System.Reflection;
using JSLibrary.Data;
using NabfProject.KnowledgeManagerModel;
using NabfProject.Events;

namespace NabfTest.NewNoticeBoardModelTest
{
    /*
     ****Notices:
     * Create
     * Update
     * Delete
     * 
     ****Jobs(internal consistency):
     * Apply
     * Update application
     * Unapply
     * Fire
     * AssignJobs
     * 
     ****Messaging to/from agents:
     * All of the above + SendOutAllNoticesToAgent
     */

    [TestFixture]
	public class NoticeBoardTest
	{
        NewNoticeBoard nb;
        NabfAgent agent1, agent2, agent3, agent4;
        OccupyJob OccupyJob1, OccupyJob2, OccupyJob3;

        int DontCareInt = 1;
        string DontCareString = "";
        List<NodeKnowledge> DontCareNodes = new List<NodeKnowledge>() { new NodeKnowledge("uniquename") };


        //called before each test
        [SetUp]
        public void Initialization()
        {
            nb = new NewNoticeBoard();
            agent1 = new NabfAgent("a1"); agent2 = new NabfAgent("a2"); agent3 = new NabfAgent("a3"); agent4 = new NabfAgent("a4");
        }

        #region CRUD for notices
        [Test]
		public void CreateNotice_NoDuplicateExists_Success()
		{
            #region init
            int agentsNeeded = 0;
            int jobValue = 0;
            string notNeededForOccupyJob = "";
            NewNoticeBoard.JobType jobType = NewNoticeBoard.JobType.Occupy;
            List<NodeKnowledge> whichNodesIsInvolvedInJob = new List<NodeKnowledge>() { };
            List<NodeKnowledge> whichNodesToStandOn = new List<NodeKnowledge>() { };
            #endregion

            Assert.AreEqual(0, nb.GetAllNotices().Count);

            bool createSuccess = nb.CreateNotice(jobType, agentsNeeded, whichNodesIsInvolvedInJob, whichNodesToStandOn, notNeededForOccupyJob, jobValue);

            Assert.True(createSuccess);
            Assert.AreEqual(1,nb.GetAllNotices().Count);
		}

        [Test]
        public void CreateNotice_ContentDuplicateExists_Failure()
        {
            #region init
            int agentsNeeded = 0;
            int jobValue = 0;
            string notNeededForOccupyJob = "";
            NewNoticeBoard.JobType jobType = NewNoticeBoard.JobType.Occupy;
            List<NodeKnowledge> whichNodesIsInvolvedInJob = new List<NodeKnowledge>() { };
            List<NodeKnowledge> whichNodesToStandOn = new List<NodeKnowledge>() { };
            #endregion

            Assert.AreEqual(0, nb.GetAllNotices().Count);

            nb.CreateNotice(jobType, agentsNeeded, whichNodesIsInvolvedInJob, whichNodesToStandOn, notNeededForOccupyJob, jobValue);
            Assert.AreEqual(1, nb.GetAllNotices().Count);

            bool createSuccessDuplicate = nb.CreateNotice(jobType, agentsNeeded, whichNodesIsInvolvedInJob, whichNodesToStandOn, notNeededForOccupyJob, jobValue);

            Assert.False(createSuccessDuplicate);
            Assert.AreEqual(1, nb.GetAllNotices().Count);
        }

        [Test]
        public void UpdateNotice_NoticeExists_Success()
        {
            #region init
            int agentsNeeded = 0;
            int jobValue = 0;
            int agentsNeededUpdated = 1;
            int jobValueUpdated = 1;
            string notNeededForOccupyJob = "";
            NewNoticeBoard.JobType jobType = NewNoticeBoard.JobType.Occupy;
            List<NodeKnowledge> whichNodesIsInvolvedInJob = new List<NodeKnowledge>() { };
            List<NodeKnowledge> whichNodesToStandOn = new List<NodeKnowledge>() { };
            #endregion

            nb.CreateNotice(jobType, agentsNeeded, whichNodesIsInvolvedInJob, whichNodesToStandOn, notNeededForOccupyJob, jobValue);

            Assert.AreEqual(agentsNeeded, nb.GetAllNotices().ToList()[0].AgentsNeeded);
            Assert.AreEqual(jobValue, nb.GetAllNotices().ToList()[0].Value);

            bool updateSuccess = nb.UpdateNotice(0, whichNodesIsInvolvedInJob, whichNodesToStandOn, agentsNeededUpdated, jobValueUpdated, notNeededForOccupyJob);

            Assert.True(updateSuccess);
            Assert.AreEqual(agentsNeededUpdated, nb.GetAllNotices().ToList()[0].AgentsNeeded);
            Assert.AreEqual(jobValueUpdated, nb.GetAllNotices().ToList()[0].Value);
        }
            
        [Test]
        public void UpdateNotice_NoticeDontExists_Failure()
        {
            #region init
            int agentsNeededUpdated = 1;
            int jobValueUpdated = 1;
            string notNeededForOccupyJob = "";
            List<NodeKnowledge> whichNodesIsInvolvedInJob = new List<NodeKnowledge>() { };
            List<NodeKnowledge> whichNodesToStandOn = new List<NodeKnowledge>() { };
            #endregion

            Assert.AreEqual(0, nb.GetAllNotices().Count);

            bool updateSuccess = nb.UpdateNotice(0, whichNodesIsInvolvedInJob, whichNodesToStandOn, agentsNeededUpdated, jobValueUpdated, notNeededForOccupyJob);

            Assert.False(updateSuccess);
        }

        [Test]
        public void DeleteNotice_NoticeExists_Success()
        {
            #region init
            int agentsNeeded = 0;
            int jobValue = 0;
            string notNeededForOccupyJob = "";
            NewNoticeBoard.JobType jobType = NewNoticeBoard.JobType.Occupy;
            List<NodeKnowledge> whichNodesIsInvolvedInJob = new List<NodeKnowledge>() { };
            List<NodeKnowledge> whichNodesToStandOn = new List<NodeKnowledge>() { };
            #endregion

            nb.CreateNotice(jobType, agentsNeeded, whichNodesIsInvolvedInJob, whichNodesToStandOn, notNeededForOccupyJob, jobValue);

            Assert.AreEqual(1, nb.GetAllNotices().Count);

            bool deleteSuccess = nb.DeleteNotice(0);

            Assert.True(deleteSuccess);
            Assert.AreEqual(0, nb.GetAllNotices().Count);
        }

        [Test]
        public void DeleteNotice_NoticeDontExists_Failure()
        {
            #region init
            int agentsNeeded = 0;
            int jobValue = 0;
            string notNeededForOccupyJob = "";
            NewNoticeBoard.JobType jobType = NewNoticeBoard.JobType.Occupy;
            List<NodeKnowledge> whichNodesIsInvolvedInJob = new List<NodeKnowledge>() { };
            List<NodeKnowledge> whichNodesToStandOn = new List<NodeKnowledge>() { };
            #endregion

            nb.CreateNotice(jobType, agentsNeeded, whichNodesIsInvolvedInJob, whichNodesToStandOn, notNeededForOccupyJob, jobValue);
           
            Assert.AreEqual(1, nb.GetAllNotices().Count);

            bool deleteSuccess = nb.DeleteNotice(1);//none-existing ID

            Assert.False(deleteSuccess);
            Assert.AreEqual(1, nb.GetAllNotices().Count);
        }
        #endregion

        #region Internal consistency (Jobs)
        [Test]
        public void ApplyToNotice_Simple_AddApplication()
        {
        }

        [Test]
        public void ApplyToNotice_AgentHasAlreadyApplied_overrideApplication()
        {
        }

        [Test]
        public void UnapplyToNotice_DontHaveTheJob_AgentRemovedFromApplyList()
        {
        }

        [Test]
        public void UnapplyToNotice_NoticeDontExists_failure()
        {
        }

        [Test]
        public void UnapplyToNotice_HasTheJob_StatusSetToAvailable()
        {
        }
        #endregion

        #region Messaging
        [Test]
        public void CreateNotice_NoDuplicateExists_MsgArrived()
        {
            #region init
            int agentsNeeded = 0;
            int jobValue = 0;
            string notNeededForOccupyJob = "";
            NewNoticeBoard.JobType jobType = NewNoticeBoard.JobType.Occupy;
            List<NodeKnowledge> whichNodesIsInvolvedInJob = new List<NodeKnowledge>() { };
            List<NodeKnowledge> whichNodesToStandOn = new List<NodeKnowledge>() { };
            #endregion

            int eventFiredCounter = 0;
            agent1.Register(new XmasEngineModel.Management.Trigger<NewNoticeEvent>(evt => eventFiredCounter++));

            nb.CreateNotice(jobType, agentsNeeded, whichNodesIsInvolvedInJob, whichNodesToStandOn, notNeededForOccupyJob, jobValue);
            Assert.AreEqual(0,eventFiredCounter);

            nb.Subscribe(agent1);
            nb.CreateNotice(jobType, agentsNeeded, DontCareNodes, DontCareNodes, notNeededForOccupyJob, jobValue);
            Assert.AreEqual(1, eventFiredCounter);
        }
        #endregion


        private object getField(object instance, bool useBase, String name)
        {
            Type t;
            if (useBase)
                t = instance.GetType().BaseType;
            else
                t = instance.GetType();

            FieldInfo f = t.GetField(name, BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.IgnoreCase);
            
            return f.GetValue(instance);
        }
        
	}
}
