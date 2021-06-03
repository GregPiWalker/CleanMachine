using System;
using System.Linq;
using System.Threading;
using System.Collections.Generic;
using System.Reactive.Concurrency;
using CleanMachine;
using CleanMachine.Generic;
using CleanMachine.Behavioral.Behaviors;
using CleanMachine.Interfaces;
using Unity;
using log4net;

namespace Sequentials.Instructions
{
    public abstract class InstructionBuilderBase
    {
        protected ILog _logger;
        protected List<Binder> _linkBinders;
        protected Dictionary<Guid, ActionNode> _nodes;
        protected Dictionary<string, ActionNode> _namedNodes;
        protected string _finishName;
        protected Func<bool> _finishCondition;
        protected string[] _finishReflexKeys;

        /// <summary>
        /// Gets the available Stimuli, which are templates for creating Triggers.
        /// This is virtual so that derived types can have a static set of constructors scoped to them specifically.
        /// </summary>
        protected virtual Dictionary<string, Func<TriggerBase>> Stimuli { get; }

        protected Sequence Sequence { get; set; }

        protected ActionNode PreviousSupplier { get; set; }

        protected ActionNode Supplier { get; set; }

        protected ActionNode Consumer { get; set; }

        //public virtual ActivitySequence CreateSequence(string name, IUnityContainer runtimeContainer)
        //{
        //    Sequence = new ActivitySequence(name, runtimeContainer, null);
        //    return Sequence;
        //}

        protected virtual void ConfigureStimuli()
        {
            // Intentionally blank
        }

        protected void BeginBuilding(Sequence sequence)
        {
            Sequence = sequence;
            _logger = sequence.Logger;

            if (_linkBinders == null)
            {
                _linkBinders = new List<Binder>();
            }
            else
            {
                _linkBinders.Clear();
            }

            if (_nodes == null)
            {
                _nodes = new Dictionary<Guid, ActionNode>();
            }
            else
            {
                _nodes.Clear();
            }

            if (_namedNodes == null)
            {
                _namedNodes = new Dictionary<string, ActionNode>();
            }
            else
            {
                _namedNodes.Clear();
            }
        }

        protected void CompleteBuild()
        {
            //TODO: do all the link-up here.
        }

        protected void TakeFrom(InstructionBuilderBase other)
        {
            Sequence = other.Sequence;
            _logger = other._logger;
            _linkBinders = other._linkBinders;
            _nodes = other._nodes;
            _namedNodes = other._namedNodes;
            _finishName = other._finishName;
            _finishCondition = other._finishCondition;
            _finishReflexKeys = other._finishReflexKeys;

            PreviousSupplier = other.PreviousSupplier;
            Supplier = other.Supplier;
            Consumer = other.Consumer;
        }

        protected InstructionBuilderBase AddStart(string actionName, Action<IUnityContainer> action)
        {
            Consumer = Sequence.InitialNode;
            AppendActionNode(actionName, action);

            return this;
        }

        protected InstructionBuilderBase AddStartWhen(string whenName, Func<bool> whenCondition, params string[] reflexKeys)
        {
            Consumer = Sequence.InitialNode;
            AppendNoOpNode("When", whenName, whenCondition, reflexKeys);

            return this;
        }

        protected InstructionBuilderBase AddDo(string actionName, Action<IUnityContainer> action)
        {
            AppendActionNode(actionName, action);

            return this;
        }

        protected InstructionBuilderBase AddWhen(string whenName, Func<bool> whenCondition, params string[] reflexKeys)
        {
            // Add the consumer no-op node and a link to it from the previous node.
            AppendNoOpNode("When", whenName, whenCondition, reflexKeys);

            return this;
        }

        protected InstructionBuilderBase AddOrWhen(string conditionName, Func<bool> condition, params string[] reflexKeys)
        {
            var previous = PreviousSupplier;
            // Add the consumer no-op node and a link to it from the previous node.
            InsertLink(Supplier, Consumer, conditionName, condition, reflexKeys);

            return this;
        }

        protected InstructionBuilderBase AddBranchIf(string branchDestName, string ifName, Func<bool> ifCondition, params string[] reflexKeys)
        {
            // Add the branch node and a link to it from the previous node.
            AppendNoOpNode("Branch");

            // Add the branching link to a branch target.
            var destination = _namedNodes[branchDestName];
            InsertLink(Supplier, destination, ifName, ifCondition, reflexKeys);

            // Add a no-op node to consume the conditional continuation link.
            // The opposite condition needs the same triggers.
            AppendNoOpNode("NoOp", "Not " + ifName, () => !ifCondition(), reflexKeys);

            return this;
        }

        protected InstructionBuilderBase AddIfThen(string ifName, Func<bool> ifCondition, string thenName, Action<IUnityContainer> thenBehavior, params string[] reflexKeys)
        {
            AppendConditionalActionNode(thenName, thenBehavior, ifName, ifCondition, reflexKeys);

            // Add the no-op node to tie up both links.
            AppendNoOpNode("NoOp");

            // Add the by-pass link to skip over the THEN action.
            InsertLink(PreviousSupplier, Consumer, "Not " + ifName, () => !ifCondition(), reflexKeys);

            return this;
        }

        protected InstructionBuilderBase AddIfThenElse(string ifName, Func<bool> ifCondition, string thenName, Action<IUnityContainer> thenBehavior, string elseName, Action<IUnityContainer> elseBehavior, params string[] reflexKeys)
        {
            AppendConditionalActionNode(thenName, thenBehavior, ifName, ifCondition, reflexKeys);

            // Add the no-op node to tie up both links.
            var noOp = AppendNoOpNode("NoOp");
            
            // Restore references in order to add another linked node from the same supplier.
            Consumer = PreviousSupplier;
            AppendConditionalActionNode(elseName, elseBehavior, "Not " + ifName, () => !ifCondition(), reflexKeys);

            // Now add a link from the ELSE node to the no-op terminal node.
            InsertLink(Consumer, noOp);

            // Finally, fix the reference.
            Consumer = noOp;

            return this;
        }

        /// <summary>
        /// Finish at any time when the supplied finish condition is met.  If no finish condition is given,
        /// the sequence will only finish at its end.
        /// </summary>
        /// <param name="finishName"></param>
        /// <param name="finishCondition"></param>
        /// <param name="reflexKeys"></param>
        /// <returns></returns>
        protected Sequence AddFinish(string finishName = null, Func<bool> finishCondition = null, params string[] reflexKeys)
        {
            _finishName = finishName;
            _finishCondition = finishCondition;
            _finishReflexKeys = reflexKeys;
            return Sequence;
        }

        internal ActionNode CreateNode(string nodeName, Action<IUnityContainer> doBehavior = null)
        {
            //todo: real cancellation token
            var node = new ActionNode(nodeName, Sequence.Name, _logger, Sequence.RuntimeContainer, new CancellationTokenSource().Token);
            if (doBehavior != null)
            {
                node.AddDoBehavior(new Behavior(nodeName, doBehavior));
            }

            //node.Stereotype = $"[{constraint.Name}]{_detachedNode.Stereotype}";
            return node;
        }

        internal Link CreateLink(IConstraint constraint = null, IEnumerable<TriggerBase> triggers = null)
        {
            //todo: real cancellation token
            var link = new Link(Sequence.Name, Stereotypes.Continue.ToString(), _logger, new CancellationTokenSource().Token);
            link.Guard = constraint;
            if (triggers != null)
            {
                foreach (var t in triggers)
                {
                    link.AddTrigger(t);
                }
            }

            return link;
        }

        internal ActionNode AppendActionNode(string actionName, Action<IUnityContainer> action)
        {
            PreviousSupplier = Supplier;
            Supplier = Consumer;
            Consumer = CreateNode(actionName, action);
            _nodes.Add(Consumer.Uid, Consumer);
            _namedNodes.Add(Consumer.Name, Consumer);

            InsertLink(Supplier, Consumer);

            return Consumer;
        }

        internal ActionNode AppendConditionalActionNode(string actionName, Action<IUnityContainer> action, string conditionName, Func<bool> condition, params string[] reflexKeys)
        {
            PreviousSupplier = Supplier;
            Supplier = Consumer;
            Consumer = CreateNode(actionName, action);
            _nodes.Add(Consumer.Uid, Consumer);
            _namedNodes.Add(Consumer.Name, Consumer);

            InsertLink(Supplier, Consumer, conditionName, condition, reflexKeys);

            return Consumer;
        }


        internal ActionNode AppendNoOpNode(string nodeName, string conditionName = null, Func<bool> condition = null, params string[] reflexKeys)
        {
            PreviousSupplier = Supplier;
            Supplier = Consumer;
            Consumer = CreateNode(nodeName);
            _nodes.Add(Consumer.Uid, Consumer);

            InsertLink(Supplier, Consumer, conditionName, condition, reflexKeys);

            return Consumer;
        }

        internal void InsertLink(State fromState, State toState, string conditionName = null, Func<bool> condition = null, params string[] reflexKeys)
        {
            Link link = null;
            if (condition == null)
            {
                link = CreateLink();
            }
            else
            {
                //TODO: do the triggers later
                var triggers = from key in reflexKeys
                               where Stimuli.ContainsKey(key)
                               select Stimuli[key].Invoke();

                link = CreateLink(new Constraint(conditionName, condition, _logger), triggers);
            }

            var binder = new Binder(link) { FromState = fromState, ToState = toState, ReflexKeys = reflexKeys };
            _linkBinders.Add(binder);
        }

        protected static void AddStimulus<TSource, TEventArgs>(Dictionary<string, Func<TriggerBase>> creators, string key, TSource evSource, string evName, ILog logger, IScheduler scheduler = null, Func<TEventArgs, bool> filter = null, string filterName = null) //where TEventArgs : EventArgs
        {
            if (creators.ContainsKey(key))
            {
                return;
            }

            if (filter == null)
            {
                creators[key] = () => new Trigger<TSource, TEventArgs>(evSource, evName, null, scheduler, logger);
            }
            else
            {
                creators[key] = () => new Trigger<TSource, TEventArgs>(evSource, evName, new Constraint<TEventArgs>(filterName, filter, logger), scheduler, logger);
            }
        }

        protected static void AddDelegateStimulus<TSource, TDelegate, TEventArgs>(Dictionary<string, Func<TriggerBase>> creators, string key, TSource evSource, string evName, Func<TEventArgs, bool> filter = null, string filterName = null) //where TEventArgs : EventArgs
        {
            if (creators.ContainsKey(key))
            {
                return;
            }

            if (filter == null)
            {
                creators[key] = () => new DelegateTrigger<TSource, TDelegate, TEventArgs>(evSource, evName, null);
            }
            else
            {
                creators[key] = () => new DelegateTrigger<TSource, TDelegate, TEventArgs>(evSource, evName, new Constraint<TEventArgs>(filterName, filter, null), null);
            }
        }
    }
}
