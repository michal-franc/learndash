﻿namespace LearnDash.Controllers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Web;
    using System.Web.Mvc;
    using System.Web.Routing;
    using Castle.Core.Logging;
    using LearnDash.Dal;
    using LearnDash.Dal.Models;
    using LearnDash.Dal.NHibernate;
    using LearnDash.Services;

    [Authorize]
    public class LearningFlowController : Controller
    {
        public ILearningFlowService LearningFlowService { get; set; }

        public IRepository<LearningTask> LearningTaskRepository { get; set; }
        public IRepository<UserProfile> UserRepository { get; set; } 


        public static NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        //todo : should open default learing flow
        public ActionResult Index()
        {
            return this.View();
        }

        [HttpGet]
        public ActionResult Edit(int id)
        {
            var flow = LearningFlowService.Get(id);
            if (flow != null)
            {
                return this.View(flow);
            }
            else
            {
                return this.View("Error", ErrorType.NotFound);
            }
        }

        [HttpPost]
        public ActionResult Edit(LearningFlow flow)
        {
            if (ModelState.IsValid)
            {
                var state = LearningFlowService.Update(flow);
                if (state)
                {
                    Notification.Add(new Notification(NotificationType.SuccesfullyEdited));
                }
                else
                {
                    Notification.Add(new Notification(NotificationType.FailEdited));
                }
                return View(flow);
            }

            Logger.Warn("Flow model not valid!. Propably client validation didn't worked out.");
            return View();
        }


        [HttpGet]
        public ActionResult Add()
        {
            return View();
        }

        [HttpPost]
        public ActionResult Add(LearningFlow newFlow)
        {
            if (ModelState.IsValid)
            {
                newFlow.Tasks = new List<LearningTask>();
                var id = LearningFlowService.Add(newFlow);
                if (id != null)
                {
                    Notification.Add(new Notification(NotificationType.SuccesfullyAdd));
                }
                else
                {
                    Notification.Add(new Notification(NotificationType.FailAdd));
                }
                return RedirectToAction("Edit", new { id });
            }

            Logger.Warn("Flow model not valid!. Propably client validation didn't worked out.");
            return View();
        }

        [HttpGet]
        public ActionResult Remove(int id)
        {
            var flow = LearningFlowService.Get(id);
            if (flow != null)
            {
                return View(flow);
            }
            else
            {
                Logger.Warn("Unable to remove flow beacuse it wasn't found in DB. Possible error. Beacuse flow is visible in view but not visible in DB.");
                return View("Error", ErrorType.NotFound);
            }

        }

        [HttpPost]
        public ActionResult Remove(LearningFlow flow)
        {
            LearningFlowService.Remove(flow.ID);
            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        public ActionResult List()
        {
            // refactor : here we are using GetByParameteres euqls which return IList unnecesary implement nhibernate Linq :X
            var user = this.UserRepository.GetByParameterEqualsFilter("UserId", User.Identity.Name).SingleOrDefault();

            if (user != null)
            {
                var flows = user.Dashboards.First().Flows.ToList();
                return View(flows);
            }
            else
            {
                Logger.Warn("User '{0}' doesn't exist \r\nPropably session should be recycled and register procedure performed again.", User.Identity.Name);
                return RedirectToAction("Logout", "Account");
            }
        }

        [HttpPost]
        public ActionResult CompleteTask(int lastCompleteTaskId, int newCompleteTaskId)
        {
            if (lastCompleteTaskId != 0 && newCompleteTaskId != 0)
            {
                Notification.Add(new Notification(NotificationType.SuccesfullyAdd));                
            }
            else
            {
                Notification.Add(new Notification(NotificationType.FailAdd));
            }

            return this.MakeNext(lastCompleteTaskId, newCompleteTaskId);
        }

        [HttpPost]
        public ActionResult MakeNext(int lastCompleteTaskId, int newCompleteTaskId)
        {
            if (lastCompleteTaskId >= 0 && newCompleteTaskId >= 0)
            {
                var lastTask = this.LearningTaskRepository.GetById(lastCompleteTaskId);
                var newTask = this.LearningTaskRepository.GetById(newCompleteTaskId);

                lastTask.IsNext = false;
                newTask.IsNext = true;

                this.LearningTaskRepository.Update(lastTask);
                this.LearningTaskRepository.Update(newTask);
                return this.Json(Is.Success);
            }

            Logger.Warn("Wrong data sent to the action \r\nparams: lTaskId - {0}\r\n nTaskId - {1} ", lastCompleteTaskId, newCompleteTaskId);
            return this.Json(Is.Fail.Message("Wrong data sent"));
        }

        public ActionResult View(int id)
        {
            var flow = this.LearningFlowService.Get(id);
            if (flow != null)
            {
                return this.View(flow);
            }
            else
            {
                Logger.Warn("Requested flow with id - {0} that doesn't exist", id);
                return this.View("Error", ErrorType.NotFound);
            }
        }

        [HttpPost]
        public ActionResult Save(LearningFlow flow)
        {
            if (ModelState.IsValid)
            {
                var state = LearningFlowService.Update(flow);
                if (state)
                {
                    Notification.Add(new Notification(NotificationType.SuccesfullyEdited));
                }
                else
                {
                    Notification.Add(new Notification(NotificationType.FailEdited));
                }
                return Json(Is.Success);
            }

            Logger.Warn("Flow model not valid!. Propably client validation didn't worked out.");
            return Json(Is.Fail.Message("Save Failed"));
        }
    }

    public static class Is
    {
        public static object Success
        {
            get
            {
                return new
                           {
                               isSuccess = true
                           };
            }
        }

        public class Fail
        {
            public static object Message(string message)
            {
                return new
                           {
                               isSuccsess = false,
                               message
                           };
            }
        }
    }
}
