using Verse;

namespace RimCoach
{
    public class PatchDef : Def
    {
        public string targetClass;
        public string targetMethod;

        public string label;
        public string category;
        public string severityHint;
        public string modHint;
    }
}