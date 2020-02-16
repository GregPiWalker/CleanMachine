using System;

namespace CleanMachine.Interfaces
{
    /// <summary>
    /// 
    /// </summary>
    public interface IConstraint
    {
        /// <summary>
        /// 
        /// </summary>
        string Name { get; }

        bool VerboseLogging { get; set; }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        bool IsTrue();
    }
}
