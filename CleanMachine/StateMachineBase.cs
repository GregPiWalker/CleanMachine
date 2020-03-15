using log4net;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.ComponentModel;
using CleanMachine.Interfaces;

namespace CleanMachine
{
    public abstract class StateMachineBase : IStateMachine, INotifyPropertyChanged
    {
        protected readonly List<Transition> _transitions = new List<Transition>();
        protected readonly List<State> _states = new List<State>();
        protected State _currentState;
        protected State _initialState;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="name"></param>
        /// <param name="logger"></param>
        protected StateMachineBase(string name, ILog logger)
        {
            Name = name;
            Logger = logger;
        }

        public virtual event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// 
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// 
        /// </summary>
        public ILog Logger { get; }

        /// <summary>
        /// 
        /// </summary>
        public ReadOnlyCollection<IState> States
        {
            get { return _states.Cast<IState>().ToList().AsReadOnly(); }
        }

        //public State FinalState { get; private set; }

        /// <summary>
        /// 
        /// </summary>
        internal bool Editable { get; private set; }

        /// <summary>
        /// 
        /// </summary>
        internal bool IsAssembled { get; private set; }

        internal State CurrentState => _currentState;

        /// <summary>
        /// Set the machine's desired initial state.  This is enforced
        /// as a step in machine assembly so that initial state is defined in the same
        /// location as the rest of the machine structure.
        /// </summary>
        public void SetInitialState(string initialState)
        {
            if (!Editable)
            {
                throw new InvalidOperationException($"{Name} must be editable in order to set initial state.");
            }

            var state = FindState(initialState);
            if (state == null)
            {
                throw new ArgumentException($"{Name} does not contain a state named {initialState}.");
            }

            _initialState = state;
        }

        /// <summary>
        /// Take this machine out of edit mode, mark it as fully assembled and then
        /// enter the initial state.
        /// </summary>
        public void CompleteEdit()
        {
            if (!Editable || IsAssembled)
            {
                return;
            }

            IsAssembled = true;
            Editable = false;

            foreach (var state in _states)
            {
                state.CompleteEdit();
            }

            Logger.Debug($"{Name}:  editing disabled.");
            EnterInitialState();
        }

        /// <summary>
        /// Put this machine in edit mode, which allows you to assemble the structure.
        /// </summary>
        internal void Edit()
        {
            if (Editable || IsAssembled)
            {
                return;
            }

            foreach (var state in _states)
            {
                state.Edit();
            }

            Editable = true;
            Logger.Debug($"{Name}:  editing enabled.");
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="stateNames"></param>
        protected abstract void CreateStates(IEnumerable<string> stateNames);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="supplierState"></param>
        /// <param name="consumerState"></param>
        /// <returns></returns>
        internal Transition CreateTransition(string supplierState, string consumerState)
        {
            if (!Editable)
            {
                throw new InvalidOperationException($"{Name} must be in editable in order to create a new transition.");
            }

            var supplier = FindState(supplierState);
            if (supplier == null)
            {
                throw new InvalidOperationException($"{Name} does not contain state {supplierState}");
            }

            var consumer = FindState(consumerState);
            if (consumer == null)
            {
                throw new InvalidOperationException($"{Name} does not contain state {consumerState}");
            }

            var transition = supplier.CreateTransitionTo(Name, consumer);
            transition.Requested += HandleTransitionRequest;
            return transition;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="stateName"></param>
        /// <returns></returns>
        protected State FindState(string stateName)
        {
            return _states.FirstOrDefault(s => s.Name == stateName);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="stateName"></param>
        /// <returns></returns>
        protected bool ContainsState(string stateName)
        {
            return _states.Any(s => s.Name == stateName);
        }

        /// <summary>
        /// Perform state-changed work.
        /// Implementations of this method should be synchronous.  That is, they should avoid
        /// calling methods or raising events asynchronously.
        /// </summary>
        /// <param name="transition"></param>
        /// <param name="args"></param>
        protected abstract void OnStateChanged(Transition transition, TriggerEventArgs args);

        /// <summary>
        /// Perform state-entered work.
        /// Implementations of this method should be synchronous.  That is, they should avoid
        /// calling methods or raising events asynchronously.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        protected abstract void OnStateEntered(object sender, StateEnteredEventArgs args);

        /// <summary>
        /// Perform state-exited work.
        /// Implementations of this method should be synchronous.  That is, they should avoid
        /// calling methods or raising events asynchronously.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        protected abstract void OnStateExited(object sender, StateExitedEventArgs args);

        /// <summary>
        /// Enter the Initial state and mark it as the current state.  Also, try
        /// to run to completion from the InitialNode.
        /// </summary>
        internal void EnterInitialState()
        {
            if (_initialState == null)
            {
                throw new InvalidOperationException($"{Name}:  initial state was not configured.");
            }

            if (!IsAssembled || Editable)
            {
                throw new InvalidOperationException($"{Name} must be fully assembled before it can enter the inital state.");
            }

            Logger.Info($"{Name}:  entering initial state {_initialState.Name}.");
            JumpToState(_initialState);
        }
        
        internal virtual void AttemptTransition(TransitionEventArgs args)
        {
            AttemptTransitionUnsafe(args);
        }

        internal virtual void JumpToState(State jumpTo)
        {
            if (jumpTo == null)
            {
                throw new ArgumentNullException("jumpTo");
            }

            JumpToStateUnsafe(jumpTo);
        }

        /// <summary>
        /// The first successful transition will dispose of all other trigger handlers that were competing for action
        /// by disposing of the state selection context associated to the triggers.
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        protected virtual bool AttemptTransitionUnsafe(TransitionEventArgs args)
        {
            // Provide escape route in case the trigger became irrelevant while the handler for it was waiting.
            //TODO: it's possible this condition can move into BehavioralStateMachine.
            if (args.TriggerArgs.TriggerContext != _currentState.SelectionContext)
            {
                Logger.Debug($"{Name}.{nameof(AttemptTransitionUnsafe)}:  transition rejected for trigger '{args.TriggerArgs.Trigger.ToString()}'.  The trigger occurred in a different context of selection of state {_currentState.Name}.");
                return false;
            }

            // Provide escape route in case the trigger was deactivated while the handler for it was waiting.
            if (!args.TriggerArgs.Trigger.IsActive)
            {
                Logger.Debug($"{Name}.{nameof(AttemptTransitionUnsafe)}:  transition rejected for trigger '{args.TriggerArgs.Trigger.ToString()}'. Trigger is currently inactive.");
                return false;
            }

            if (args.Transition.AttemptTransition(args.TriggerArgs))
            {
                _currentState = args.Transition.To;
                OnStateChanged(args.Transition, args.TriggerArgs);
            }
            else
            {
                //OnTransitionFailed(transition, args);
            }

            // A transition attempt was made.
            return true;
        }

        protected void JumpToStateUnsafe(State jumpTo)
        {
            if (!jumpTo.CanEnter(null))
            {
                throw new InvalidOperationException($"{Name}:  state {jumpTo.Name} could not be entered.");
            }

            Logger.Debug($"{Name}:  jumping to state {jumpTo.Name}.");
            jumpTo.Enter(null);
            _currentState = jumpTo;

            OnStateChanged(null, new TriggerEventArgs() { Cause = this });
        }

        protected virtual void HandleTransitionRequest(object sender, TriggerEventArgs args)
        {
            if (args.TriggerContext == null)
            {
                return;
            }

            var currentState = _currentState;
            if (currentState == null || !currentState.IsEnabled)
            {
                return;
            }

            var transition = sender as BehavioralTransition;
            var transitionArgs = transition.ToTransitionArgs(args);

            AttemptTransition(transitionArgs);
        }
    }
}
