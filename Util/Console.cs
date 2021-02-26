using RoR2;

namespace R2DSEssentials.Util
{
    public static class Console
    {
        public static string MergeArgs(ConCommandArgs args, int fromIndex)
        {
            if (fromIndex > args.Count)
                return "";
            if (fromIndex < 0)
                fromIndex = 0;
            string[] strArgs = new string[args.Count - fromIndex];
            for (int i = fromIndex; i < args.Count; i++)
            {
                strArgs[i - fromIndex] = args[i];
            }
            return string.Join(" ", strArgs);
        }
    }
}