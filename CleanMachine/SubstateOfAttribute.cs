using System;

namespace CleanMachine
{
    /// <summary>
    /// This attribute merely allows semantic linking of one state to another state in a
    /// Superstate-Substate relationship.  It is a stepping stone to better sub-state support
    /// in the framework.
    /// </summary>
    public class SubstateOfAttribute : Attribute
    {
        public SubstateOfAttribute(string superstate)
        {
            SuperstateName = superstate;
        }

        /// <summary>
        /// Gets the name of the superstate in a superstate-substate relationship.
        /// </summary>
        public string SuperstateName { get; }
    }
}
