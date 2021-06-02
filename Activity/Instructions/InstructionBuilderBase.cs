using System;
using System.Linq;
using System.Threading;
using System.Collections.Generic;
using CleanMachine;
using CleanMachine.Behavioral.Behaviors;
using CleanMachine.Interfaces;
using Activity;
using Unity;
using log4net;

namespace Sequentials.Instructions
{
    public class InstructionBuilderBase
    {
        /// <summary>
        /// Using a static field so that all instances of <see cref="TestActivity"/> can share
        /// </summary>
        protected static readonly Dictionary<string, Func<TriggerBase>> _stimuli = new Dictionary<string, Func<TriggerBase>>();

        protected ILog _logger;
        protected List<Binder> _linkBinders;
        protected Dictionary<Guid, ActionNode> _nodes;
        protected Dictionary<string, ActionNode> _namedNodes;
        protected string _finishName;
        protected Func<bool> _finishCondition;
        protected string[] _finishReflexKeys;

        protected Dictionary<string, Func<TriggerBase>> Stimuli => _stimuli;

        protected ActivitySequence Sequence { get; set; }

        protected ActionNode PreviousSupplier { get; set; }

        protected ActionNode Supplier { get; set; }

        protected ActionNode Consumer { get; set; }

        protected void Initialize(ActivitySequence sequence, ILog logger)
        {
            Sequence = sequence;
            _logger = logger;
            _linkBinders = new List<Binder>();
            _nodes = new Dictionary<Guid, ActionNode>();
            _namedNodes = new Dictionary<string, ActionNode>();
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
            Link(Supplier, Consumer, conditionName, condition, reflexKeys);

            return this;
        }

        protected InstructionBuilderBase AddBranchIf(string branchDestName, string ifName, Func<bool> ifCondition, params string[] reflexKeys)
        {
            // Add the branch node and a link to it from the previous node.
            AppendNoOpNode("Branch");

            // Add the branching link to a branch target.
            var destination = _namedNodes[branchDestName];
            Link(Supplier, destination, ifName, ifCondition, reflexKeys);

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
            Link(PreviousSupplier, Consumer, "Not " + ifName, () => !ifCondition(), reflexKeys);

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
            Link(Consumer, noOp);

            // Finally, fix the reference.
            Consumer = noOp;

            return this;
        }

        protected ActivitySequence AddFinish()
        {
            return Sequence;
        }

        protected ActivitySequence AddFinish(string finishName, Func<bool> finishCondition, params string[] reflexKeys)
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

            Link(Supplier, Consumer);

            return Consumer;
        }

        internal ActionNode AppendConditionalActionNode(string actionName, Action<IUnityContainer> action, string conditionName, Func<bool> condition, params string[] reflexKeys)
        {
            PreviousSupplier = Supplier;
            Supplier = Consumer;
            Consumer = CreateNode(actionName, action);
            _nodes.Add(Consumer.Uid, Consumer);
            _namedNodes.Add(Consumer.Name, Consumer);

            Link(Supplier, Consumer, conditionName, condition, reflexKeys);

            return Consumer;
        }


        internal ActionNode AppendNoOpNode(string nodeName, string conditionName = null, Func<bool> condition = null, params string[] reflexKeys)
        {
            PreviousSupplier = Supplier;
            Supplier = Consumer;
            Consumer = CreateNode(nodeName);
            _nodes.Add(Consumer.Uid, Consumer);

            Link(Supplier, Consumer, conditionName, condition, reflexKeys);

            return Consumer;
        }

        internal void Link(State fromState, State toState, string conditionName = null, Func<bool> condition = null, params string[] reflexKeys)
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
    }
}
