﻿using System;
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
    public class ReducedBuilder2 : InstructionBuilderBase
    {
        internal ReducedBuilder2()
        {
        }

        internal ReducedBuilder2(InstructionBuilderBase other)
        {
            TakeFrom(other);
        }

        public ReducedBuilder2 BranchIf(string branchDestName, string ifName, Func<bool> ifCondition, params string[] reflexKeys)
        {
            AddBranchIf(branchDestName, ifName, ifCondition, reflexKeys);
            return this;
        }

        public ReducedBuilder2 Do(string actionName, Action<IUnityContainer> action)
        {
            AddDo(actionName, action);
            return this;
        }

        public ReducedBuilder2 IfThen(string ifName, Func<bool> ifCondition, string thenName, Action<IUnityContainer> thenBehavior, params string[] reflexKeys)
        {
            AddIfThen(ifName, ifCondition, thenName, thenBehavior, reflexKeys);
            return this;
        }

        public ReducedBuilder2 IfThenElse(string ifName, Func<bool> ifCondition, string thenName, Action<IUnityContainer> thenBehavior, string elseName, Action<IUnityContainer> elseBehavior, params string[] reflexKeys)
        {
            AddIfThenElse(ifName, ifCondition, thenName, thenBehavior, elseName, elseBehavior, reflexKeys);
            return this;
        }

        public ReducedBuilder1 When(string whenName, Func<bool> whenCondition, params string[] reflexKeys)
        {
            AddWhen(whenName, whenCondition, reflexKeys);
            return new ReducedBuilder1(this);
        }

        public ActivitySequence Finish()
        {
            return AddFinish();
        }

        public ActivitySequence Finish(string finishName, Func<bool> finishCondition, params string[] reflexKeys)
        {
            return AddFinish(finishName, finishCondition, reflexKeys);
        }
    }
}
