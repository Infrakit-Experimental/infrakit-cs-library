using System;

namespace Library.Models
{
    public class Time
    {
        // TODO: comment
        public DateTime utc;

        // TODO: comment
        public DateTime server;

        // TODO: comment
        public DateTime? user;

        // TODO: comment
        public Time(DateTime utc, DateTime server, DateTime? user = null)
        {
            this.utc = utc;
            this.server = server;
            this.user = user;
        }
    }
}
