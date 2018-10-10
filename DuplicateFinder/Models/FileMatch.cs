using System.Collections.Generic;

namespace DuplicateFinder.Models
{
    public class FileMatch
    {
        public File SourceFile { get; set; }

        public File DestinationFile { get; set; }

        public bool Match { get; set; }

        public List<string> TypesMatched { get; set; }
    }
}
