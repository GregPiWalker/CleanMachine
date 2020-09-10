using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using CleanMachine.Behavioral.Generic;
using CleanMachine.Generic;
using CleanMachine.Interfaces;

namespace CleanMachine.Behavioral
{
    public static class EditorExtensions
    {
        public static TransitionEditor TransitionTo<TState>(this StateEditor<TState> editor, TState toState) where TState : struct
        {
            var transition = editor.Machine.CreateTransition(editor.State, toState);
            transition.Edit();
            return new TransitionEditor(transition, editor.Machine.Logger);
        }

        public static TransitionEditor HaveEffect(this TransitionEditor editor, Action effect)
        {
            var transition = editor.Transition as BehavioralTransition;
            if (transition == null)
            {
                throw new InvalidOperationException($"An Effect can only be added to a BehavioralTransition.");
            }

            transition.Effect = effect;
            return editor;
        }

        public static TransitionEditor TriggerWithStateChange<TState>(this TransitionEditor editor, BehavioralStateMachine<TState> machine, TState toState) where TState : struct
        {
            var trigger = new StateChangedTrigger<TState>(machine, toState, editor.Logger);
            editor.Transition.AddTrigger(trigger);
            return editor;
        }

        public static TransitionEditor TriggerWithStateChange<TState>(this TransitionEditor editor, List<BehavioralStateMachine<TState>> machines, TState toState) where TState : struct
        {
            var trigger = new StateChangedTrigger<TState>(machines, toState, editor.Logger);
            editor.Transition.AddTrigger(trigger);
            return editor;
        }

        public static TransitionEditor TriggerWithProperties<TSender>(this TransitionEditor editor, IEnumerable<TSender> sender, string propertyName = "") where TSender : INotifyPropertyChanged
        {
            var trigger = new GroupPropertyChangedTrigger<TSender>(sender.ToList(), propertyName, editor.Logger);
            editor.Transition.AddTrigger(trigger);
            return editor;
        }

        public static void AddDoBehavior<TState>(this MachineEditor<TState> editor, TState state, Action<IState> behavior) where TState : struct
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
