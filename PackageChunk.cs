namespace PackagesLexer
{
    /// <summary>
    /// Holds data about a package chunk.
    /// </summary>
    public class PackageChunk
    {
        /// <summary>
        /// The raw string package.
        /// </summary>
        public string RawChunk;
        /// <summary>
        /// The name of the package.
        /// </summary>
        public string Name;
        /// <summary>
        /// Name/Path+Name to the base package.
        /// </summary>
        /// <remarks>
        /// If <see cref="BasePackage"/> is only the name, assume the same header path as this package.
        /// </remarks>
        public string BasePackage;
        /// <summary>
        /// The header path that this package is located in.
        /// </summary>
        public string HeaderPath;
        /// <summary>
        /// A dictionary that holds key-value pairs of the data in this package.
        /// </summary>
        public Package ParsedChunk;
    }
}