using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataTemplates
{
    public class VoiceTemplate
    {
        public VoiceCommandType type { get; set; }
        public VoiceCommandDay day { get; set; }
    }
    public enum VoiceCommandType
    {
        umbrella,
        jacket,
        conditions
    }
    public enum VoiceCommandDay
    {
        today,
        tomorrow
    }
}
