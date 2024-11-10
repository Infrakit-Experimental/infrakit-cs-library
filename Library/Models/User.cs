using System;

namespace Library.Models
{
    /// <summary>
    /// Represents a user.
    /// </summary>
    public class User
    {
        /// <summary>
        /// The unique identifier of the user.
        /// </summary>
        public Guid uuid { get; set; }

        /// <summary>
        /// The username of the user.
        /// </summary>
        public string username { get; set; }

        /// <summary>
        /// The firstName of the user.
        /// </summary>
        public string firstName { get; set; }

        /// <summary>
        /// The lastName of the user.
        /// </summary>
        public string lastName { get; set; }

        /// <summary>
        /// The email of the user.
        /// </summary>
        public string email { get; set; }

        /// <summary>
        /// The phonenumber of the user.
        /// </summary>
        public string phonenumber { get; set; }

        /// <summary>
        /// Timestamp when the user was created.
        /// </summary>
        public DateTime created { get; set; }

        /// <summary>
        /// Tag if the user is currently active.
        /// </summary>
        public bool active { get; set; }

        /// <summary>
        /// The organization identifier of the user.
        /// </summary>
        public Guid organizationUuid { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="User"/> class.
        /// </summary>
        /// <param name="uuid">The unique identifier of the user</param>
        /// <param name="username">The username of the user</param>
        /// <param name="email">The email of the user</param>
        public User(Guid uuid, string username, string email)
        {
            this.uuid = uuid;
            this.username = username;
            this.email = email;
        }
    }
}