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

        public APIProject.CoordinateSystem coordinateSystem { get; set; }

        public APIProject.HeightSystem heightSystem { get; set; }

        /// <summary>
        /// Initializes a new instance of the APIProject class with the specified name.
        /// </summary>
        /// <param name="name">The name of the project.</param>
        public APIProject(string name)
        {
            this.name = name;
        }

        public class CoordinateSystem
        {
            /// <summary>
            /// The name of the coordinate system.
            /// </summary>
            public string name { get; set; }

            /// <summary>
            /// The identifier of the coordinate system, such as an EPSG code.
            /// </summary>
            public string identifier { get; set; }

            /// <summary>
            /// The PROJ4 string for the coordinate system.
            /// </summary>
            public string? projString { get; set; }

            /// <summary>
            /// The WGS84 parameters for the coordinate system.
            /// </summary>
            public string? wgs84Parameters { get; set; }

            /// <summary>
            /// The north offset of the coordinate system in meters.
            /// </summary>
            public double offsetN { get; set; }

            /// <summary>
            /// The east offset of the coordinate system in meters.
            /// </summary>
            public double offsetE { get; set; }

            public string? country { get; set; }
            public bool? visible { get; set; }

            /// <summary>
            /// Initializes a new instance of the <see cref="CoordinateSystem"/> class.
            /// </summary>
            /// <param name="name">The name of the coordinate system.</param>
            /// <param name="identifier">The identifier of the coordinate system, such as an EPSG code.</param>
            /// <param name="projString">The PROJ4 string for the coordinate system.</param>
            /// <param name="wgs84Parameters">The WGS84 parameters for the coordinate system.</param>
            /// <param name="offsetN">The north offset of the coordinate system in meters.</param>
            /// <param name="offsetE">The east offset of the coordinate system in meters.</param>
            public CoordinateSystem(string name, string identifier, string? projString, string? wgs84Parameters, double offsetN, double offsetE, string? country, bool? visible)
            {
                this.name = name;
                this.identifier = identifier;
                this.projString = projString;
                this.wgs84Parameters = wgs84Parameters;
                this.offsetN = offsetN;
                this.offsetE = offsetE;
                this.country = country;
                this.visible = visible;
            }
        }

        public class HeightSystem
        {
            /// <summary>
            /// The name of the height system.
            /// </summary>
            public string name { get; set; }

            /// <summary>
            /// The identifier of the height system.
            /// </summary>
            public string identifier { get; set; }

            public string? country { get; set; }
            public bool? visible { get; set; }

            public HeightSystem(string name, string identifier, string? country, bool? visible)
            {
                this.name = name;
                this.identifier = identifier;
                this.country = country;
                this.visible = visible;
            }
        }

    }
}
