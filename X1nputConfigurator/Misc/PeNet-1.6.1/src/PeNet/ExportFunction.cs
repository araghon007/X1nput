namespace PeNet
{
    /// <summary>
    ///     Represents an exported function.
    /// </summary>
    public class ExportFunction
    {
        /// <summary>
        ///     Create a new ExportFunction object.
        /// </summary>
        /// <param name="name">Name of the function.</param>
        /// <param name="address">Address of function.</param>
        /// <param name="ordinal">Ordinal of the function.</param>
        public ExportFunction(string name, uint address)
        {
            Name = name;
            Address = address;
        }

        /// <summary>
        ///     Function name.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        ///     Function RVA.
        /// </summary>
        public uint Address { get; set; }
    }
}