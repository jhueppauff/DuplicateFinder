using System;

namespace DuplicateFinder.Models
{
    public class File
    {
        public string Name { get; set; }

        public string Hash { get; set; }

        public long Size { get; set; }

        public DateTime CreateDate { get; set; }

        public string Path { get; set; }

        public DateTime ModfiedDate { get; set; }

        public string Creator { get; set; }

        public string ErrorMessage { get; set; }
    }
}
