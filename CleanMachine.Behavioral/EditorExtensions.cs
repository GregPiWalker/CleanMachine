using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reactive.Concurrency;
using CleanMachine.Behavioral.Behaviors;
using CleanMachine.Behavioral.Generic;
using CleanMachine.Generic;
using CleanMachine.Interfaces;
using Unity;

namespace CleanMachine.Behavioral
{
    /// <summary>
    /// TODO: REFACTOR
    /// </summary>
    public static class EditorExtensions
    {
        public static TransitionEditor TransitionTo<TState>(this StateEditor<TState> editor, TState toState) where TState : struct
        {
            var transition = editor.Machine.CreateTransition(editor.State, toState);
            transition.Edit();
            return new TransitionEditor(transition, editor.Machine.TriggerScheduler, editor.Machine.Logger);
        }

        public static TransitionEditor HaveEffect(this TransitionEditor editor, string effectName, Action<IUnityContainer> effect)
        {
            var transition = editor.Transition;
            //if (transition == null)
            //{
            //    throw new InvalidOperationException($"An Effect can only be added to a Transition.");
            //}

            var fx = new Behavior(effectName, effect);
            transition.Effect = fx;
            return editor;
        }

        public static TransitionEditor HaveEffect(this TransitionEditor editor, IBehavior effect)
        {
            editor.Transition.Effect = effect;
            return editor;
        }

        public static TransitionEditor TriggerWithStateChange<TState>(this TransitionEditor editor, StateMachine<TState> machine, TState toState) where TState : struct
        {
            var trigger = new StateChangedTrigger<TState>(machine, toState, machine.TriggerScheduler, editor.Logger);
            editor.Transition.AddTrigger(trigger);
            return editor;
        }

        public static TransitionEditor TriggerWithStateChange<TState>(this TransitionEditor editor, IScheduler triggerScheduler, List<StateMachine<TState>> machines, TState toState) where TState : struct
        {
            var trigger = new StateChangedTrigger<TState>(machines, toState, triggerScheduler, editor.Logger);
            editor.Transition.AddTrigger(trigger);
            return editor;
        }

        public static TransitionEditor TriggerWithProperties<TSender>(this TransitionEditor editor, IScheduler triggerScheduler, IEnumerable<TSender> sender, string propertyName = "") where TSender : INotifyPropertyChanged
        {
            var trigger = new GroupPropertyChangedTrigger<TSender>(sender.ToList(), propertyName, triggerScheduler, editor.Logger);
            editor.Transition.AddTrigger(trigger);
            return editor;
        }

        public static void AddDoBehavior<TState>(this MachineEditor<TState> editor, TState state, Action<IUnityContainer> behavior) where TState : struct
        {
            if (!editor.Machine.Editable)
            {
                throw new InvalidOperationException($"StateMachine {editor.Machine.Name} must be in editable in order to modify a state.");
            }

            var behavioralState = editor.Machine[state] as BehavioralState;
            if (behavioralState == null)
            {
                throw new InvalidOperationException($"A Do behavior can only be added to a BehavioralState.");
            }

            behavioralState.AddDoBehavior(behavior);
        }
    }
}
