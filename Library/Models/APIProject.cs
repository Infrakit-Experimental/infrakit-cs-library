using System.Diagnostics.Metrics;

namespace Library.Models
{
    /// <summary>
    /// Represents a api project which is used to create a new Infrakit project.
    /// </summary>
    public class APIProject
    {
        /// <summary>
        /// The name of the project
        /// </summary>
        public string name { get; set; }

        //public CoordinateSystem coordinateSystem { get; set; }

        /// <summary>
        /// Initializes a new instance of the APIProject class with the specified name.
        /// </summary>
        /// <param name="name">The name of the project.</param>
        public APIProject(string name)
        {
            this.name = name;
        }
    }
}
