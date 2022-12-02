using System;

namespace DependencyAnalyzer
{
    /// <summary>
    /// Specify the inclusion of featured members
    /// </summary>
    [Flags]
    public enum Condition
    {
        /// <summary>
        /// Display no references.
        /// </summary>
        NoReferences = 0,
        /// <summary>
        /// Specifies that members with specified condition are included.
        /// </summary>
        With = 1,
        /// <summary>
        /// Specifies that members without specified condition are included.
        /// </summary>
        Without = 2
    }

    public enum ReportFormat { Default, Basic, Detailed, Signature, Short }
}
