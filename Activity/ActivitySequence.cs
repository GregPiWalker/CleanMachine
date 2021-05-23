using CleanMachine;
using CleanMachine.Behavioral;
using CleanMachine.Interfaces;
using log4net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Concurrency;
using System.Threading;
using Unity;

namespace Activity
{
    //public class ActivityChainBuilder
    //{
    //    public ActivityChainBuilder(string name, IScheduler behaviorScheduler)
    //    {
    //        Name = name;
    //    }

    //    protected ActivityChain UnderConstruction { get; set; }
    //}

    public class ActivitySequence : IDisposable
    {
        protected const string InitialNodeName = "Initial";
        protected const string NoopNodeName = "NoOp";
        protected const string FinalNodeName = "Final";
        protected readonly object _runLock = new object();
        protected readonly IScheduler _signalScheduler;
        protected readonly IScheduler _invocationScheduler;
        protected readonly CancellationTokenSource _abortTokenSource = new CancellationTokenSource();
        protected readonly Func<bool> _abortCondition;
        protected ActionNode _lastAttachedNode;
        protected ActionNode _detachedNode;
        protected ILog _logger;
        private bool _isDisposed;

        public ActivitySequence(string name, IScheduler signalScheduler, IScheduler invocationScheduler)
        {
            Name = name;
            _signalScheduler = signalScheduler;
            _invocationScheduler = invocationScheduler;

            BuildPhase = Phase.Mutable;
            _abortCondition = () => _abortTokenSource.Token.IsCancellationRequested;
        }

        public Phase BuildPhase { get; protected set; }

        public string Name { get; protected set; }

        public IUnityContainer RuntimeContainer { get; } = new UnityContainer();

        public ActionNode InitialNode { get; protected set; }

        public ActionNode CurrentNode { get; protected set; }

        public ActionNode FinalNode { get; protected set; }

        public SequenceState State { get; protected set; }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        public void Run()
        {
            //todo: locking
            if (State != SequenceState.Assembling)
            {
                //TODO: log or throw exception
                return;
            }

            if (BuildPhase != Phase.Mutable)
            {
                //TODO: log or throw exception
                return;
            }

            CompleteAssembly();

            CurrentNode = InitialNode;
            State = SequenceState.Running;

        }

        public void Pause()
        {
            //todo: locking
            if (State != SequenceState.Running)
            {
                return;
            }

            State = SequenceState.Paused;
        }

        public void Resume()
        {
            //todo: locking
            if (State != SequenceState.Paused)
            {
                return;
            }

            Proceed();
        }

        protected void Proceed()
        {
            lock (_runLock)
            {
                if (State != SequenceState.Running)
                {
                    return;
                }

                if (TryAbort())
                {

                }
                else if (TryExit())
                {

                }
                else if (TryContinue())
                {

                }
                else
                {
                    // CurrentNode is end of the sequence.  Try to finish.
                }
            }
        }

        protected bool TryContinue()
        {
            foreach (var continuation in CurrentNode.ContinueLinks)
            {
                if (continuation.AttemptTransition())
                {
                    return true;
                }
            }

            //TODO LOG IT
            return false;
        }

        protected bool TryAbort()
        {
            return CurrentNode.AbortLink.AttemptTransition();
        }

        protected bool TryExit()
        {
            return CurrentNode.ExitLink.AttemptTransition();
        }

        protected void OnFinished(IUnityContainer container)
        {
            if (CurrentNode != FinalNode)
            {
                //log
                return;
            }

            //TODO: get transition 
            Link finalLink = null;
            Stereotypes finalReason = finalLink.Stereotype.ToEnum<Stereotypes>();
            switch (finalReason)
            {
                case Stereotypes.Exit:
                    State = SequenceState.Exited;
                    break;

                case Stereotypes.Abort:
                    State = SequenceState.Aborted;
                    break;

                case Stereotypes.Continue:
                case Stereotypes.Finish:
                    State = SequenceState.Finished;
                    break;
            }
        }

        internal protected ActivitySequence StartWithBehavior(IBehavior behavior)
        {
            Initialize();

            AddAction(behavior);

            return this;
        }

        internal protected ActivitySequence StartWithBehavior(string actionName, Action<IUnityContainer> action)
        {
            Initialize();

            AddAction(actionName, action);

            return this;
        }

        internal protected ActivitySequence StartWithConstraint(IConstraint constraint, IEnumerable<TriggerBase> triggers = null)
        {
            Initialize();

            // Create a detached node that will consume the continue links.
            EditWithConstraint(constraint, triggers);

            return this;
        }

        internal protected ActivitySequence StartWithConstraint(string conditionName, Func<bool> condition, IEnumerable<TriggerBase> triggers = null)
        {
            return StartWithConstraint(new Constraint(conditionName, condition, _logger), triggers);
        }

        public ActivitySequence EditWithConstraint(IConstraint constraint, IEnumerable<TriggerBase> triggers = null)
        {
            if (BuildPhase != Phase.Mutable)
            {
                throw new InvalidOperationException("Sequential edit phase is already complete.");
            }

            if (constraint == null)
            {
                throw new ArgumentNullException(nameof(constraint));
            }

            if (InitialNode == null)
            {
                throw new InvalidOperationException("No initial action exists for this activity.");
            }

            PopulateDetachedNode(string.Empty);
            _detachedNode.Stereotype = $"[{constraint.Name}]{_detachedNode.Stereotype}";
            _detachedNode.AddEntryLink(constraint, triggers)
                .TraversalRequested += HandleLinkTransitionRequested;

            return this;
        }

        internal protected ActivitySequence EditWithConstraint(string conditionName, Func<bool> condition, IEnumerable<TriggerBase> triggers = null)
        {
            return EditWithConstraint(new Constraint(conditionName, condition, _logger), triggers);
        }

        internal protected ActivitySequence FinishEditWithBehavior(IBehavior behavior)
        {
            if (BuildPhase != Phase.Mutable)
            {
                throw new InvalidOperationException("Sequential edit phase is already complete.");
            }

            if (InitialNode == null)
            {
                throw new InvalidOperationException("No initial action exists for this activity.");
            }

            AddAction(behavior);

            // Only add an unguarded Continue Link if no other Continue Links exist.
            if (!_detachedNode.ContinueLinks.Any())
            {
                // No transition request handler required here, since the Link does not have a Guard.
                _detachedNode.AddEntryLink(null, null);
            }

            AttachNode(_detachedNode);
            _detachedNode = null;

            return this;
        }

        internal protected ActivitySequence FinishEditWithBehavior(string actionName, Action<IUnityContainer> action)
        {
            return FinishEditWithBehavior(new Behavior(actionName, action));
        }

        //TODO: CALL THIS FROM SOMEWHERE???
        /// <summary>
        /// Complete all sequence editing and set immutable phase.
        /// </summary>
        /// <returns></returns>
        internal protected ActivitySequence CompleteAssembly()
        {
            if (BuildPhase != Phase.Mutable)
            {
                throw new InvalidOperationException("Sequential edit phase is already complete.");
            }

            if (InitialNode == null)
            {
                throw new InvalidOperationException("No initial action exists for this sequential.");
            }

            if (FinalNode != null)
            {
                throw new InvalidOperationException("This sequential is already immutable.");
            }

            if (_detachedNode == null)
            {
                throw new InvalidOperationException("Cannot finish a sequential when no detached node exists.");
            }

            AttachNode(_detachedNode);
            _detachedNode = null;

            BuildPhase = Phase.Immutable;
            State = SequenceState.Ready;

            return this;
        }

        protected void AttachNode(ActionNode node)
        {
            if (node == null)
            {
                return;
            }

            // Attach all the incoming Continue links from the existing detached node to the last attached node.
            foreach (var inbound in node.InboundLinks)
            {
                inbound.AttachSupplier(_lastAttachedNode);
                _lastAttachedNode.AddTransition(inbound);
            }

            // Link the detached node to the final node with Finish and Abort links.
            SetFinalLinks(node);

            _lastAttachedNode = node;
        }

        /// <summary>
        /// Ensure that the <see cref="_detachedNode"/> field has a value
        /// with the supplied name.
        /// </summary>
        /// <param name="nodeName"></param>
        protected void PopulateDetachedNode(string nodeName)
        {
            if (_detachedNode == null)
            {
                _detachedNode = new ActionNode(nodeName, Name, _logger, RuntimeContainer, _invocationScheduler, _abortTokenSource.Token);
            }
            else if (string.IsNullOrEmpty(_detachedNode.Name))
            {
                _detachedNode.Name = nodeName;
            }
        }

        /// <summary>
        /// Ensures that a detached node exists and adds the supplied behavior to it.
        /// </summary>
        /// <param name="behavior"></param>
        /// <returns></returns>
        protected ActivitySequence AddAction(IBehavior behavior)
        {
            PopulateDetachedNode(behavior.Name);
            _detachedNode.AddDoBehavior(behavior);

            return this;
        }

        /// <summary>
        /// Ensures that a detached node exists and adds the supplied behavior to it.
        /// </summary>
        /// <param name="actionName"></param>
        /// <param name="action"></param>
        /// <returns></returns>
        protected ActivitySequence AddAction(string actionName, Action<IUnityContainer> action)
        {
            var behavior = new Behavior(actionName, action);
            AddAction(behavior);
            return this;
        }

        /// <summary>
        /// Create the Initial and Final nodes.
        /// </summary>
        protected void Initialize()
        {
            if (BuildPhase != Phase.Mutable)
            {
                throw new InvalidOperationException("Sequential edit phase is already complete.");
            }

            if (InitialNode != null)
            {
                throw new InvalidOperationException("An initial node already exists for this activity.");
            }

            if (FinalNode != null)
            {
                throw new InvalidOperationException("A final node already exists for this activity.");
            }

            // Add an initial node that has no action to perform. NOTE: no inbound links here.
            InitialNode = new ActionNode(InitialNodeName, Name, _logger, RuntimeContainer, _invocationScheduler, _abortTokenSource.Token);
            FinalNode = new ActionNode(FinalNodeName, Name, _logger, RuntimeContainer, _invocationScheduler, _abortTokenSource.Token);
            FinalNode.SetEntryBehavior(new Behavior(nameof(OnFinished), (c) => OnFinished(c)));
            SetFinalLinks(InitialNode);
        }

        protected void SetFinalLinks(ActionNode node)
        {
            var abort = node.CreateLinkTo(Name, Stereotypes.Abort.ToString(), FinalNode);
            abort.Guard = new Constraint(Stereotypes.Abort.ToString(), _abortCondition, _logger);

            if (node != InitialNode)
            {
                //var exit = node.CreateLinkTo(Name, Stereotypes.Exit.ToString(), FinalNode);
                //TODO: what is Exit condition?
                //exit.Guard = new Constraint(Stereotypes.Exit.ToString(), _exitCondition, _logger);
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_isDisposed)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects)
                }

                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                // TODO: set large fields to null
                _isDisposed = true;
            }
        }

        // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        // ~ActivitySequence()
        // {
        //     // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        //     Dispose(disposing: false);
        // }

        private void HandleLinkTransitionRequested(object sender, CleanMachine.SignalEventArgs e)
        {
            throw new NotImplementedException("Lookie here!");
            Proceed();
        }
    }
}
