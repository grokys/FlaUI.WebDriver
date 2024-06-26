﻿using FlaUI.WebDriver.Models;
using FlaUI.WebDriver.Services;
using Microsoft.AspNetCore.Mvc;

namespace FlaUI.WebDriver.Controllers
{
    [Route("session/{sessionId}/[controller]")]
    [ApiController]
    public class ActionsController : ControllerBase
    {
        private readonly ILogger<ActionsController> _logger;
        private readonly ISessionRepository _sessionRepository;
        private readonly IActionsDispatcher _actionsDispatcher;

        public ActionsController(ILogger<ActionsController> logger, ISessionRepository sessionRepository, IActionsDispatcher actionsDispatcher)
        {
            _logger = logger;
            _sessionRepository = sessionRepository;
            _actionsDispatcher = actionsDispatcher;
        }

        [HttpPost]
        public async Task<ActionResult> PerformActions([FromRoute] string sessionId, [FromBody] ActionsRequest actionsRequest)
        {
            _logger.LogDebug("Performing actions for session {SessionId}", sessionId);

            var session = GetSession(sessionId);
            var actionsByTick = ExtractActionSequence(session, actionsRequest);
            foreach (var tickActions in actionsByTick)
            {
                var tickDuration = tickActions.Max(tickAction => tickAction.Duration) ?? 0;
                var dispatchTickActionTasks = tickActions.Select(tickAction => _actionsDispatcher.DispatchAction(session, tickAction));
                if (tickDuration > 0)
                {
                    dispatchTickActionTasks = dispatchTickActionTasks.Concat(new[] { Task.Delay(tickDuration) });
                }
                await Task.WhenAll(dispatchTickActionTasks);
            }

            return WebDriverResult.Success();
        }

        [HttpDelete]
        public async Task<ActionResult> ReleaseActions([FromRoute] string sessionId)
        {
            _logger.LogDebug("Releasing actions for session {SessionId}", sessionId);

            var session = GetSession(sessionId);

            foreach (var cancelAction in session.InputState.InputCancelList)
            {
                await _actionsDispatcher.DispatchAction(session, cancelAction);
            }
            session.InputState.Reset();

            return WebDriverResult.Success();
        }

        /// <summary>
        /// See https://www.w3.org/TR/webdriver2/#dfn-extract-an-action-sequence.
        /// Returns all sequence actions synchronized by index.
        /// </summary>
        /// <param name="session">The session</param>
        /// <param name="actionsRequest">The request</param>
        /// <returns></returns>
        private static List<List<Action>> ExtractActionSequence(Session session, ActionsRequest actionsRequest)
        {
            var actionsByTick = new List<List<Action>>();
            foreach (var actionSequence in actionsRequest.Actions)
            {
                // TODO: Implement other input source types.
                if (actionSequence.Type == "key")
                {
                    session.InputState.GetOrCreateInputSource(actionSequence.Type, actionSequence.Id);
                }

                for (var tickIndex = 0; tickIndex < actionSequence.Actions.Count; tickIndex++)
                {
                    var actionItem = actionSequence.Actions[tickIndex];
                    var action = new Action(actionSequence, actionItem);
                    if (actionsByTick.Count < tickIndex + 1)
                    {
                        actionsByTick.Add(new List<Action>());
                    }
                    actionsByTick[tickIndex].Add(action);
                }
            }
            return actionsByTick;
        }

        private Session GetSession(string sessionId)
        {
            var session = _sessionRepository.FindById(sessionId);
            if (session == null)
            {
                throw WebDriverResponseException.SessionNotFound(sessionId);
            }
            session.SetLastCommandTimeToNow();
            return session;
        }
    }
}
