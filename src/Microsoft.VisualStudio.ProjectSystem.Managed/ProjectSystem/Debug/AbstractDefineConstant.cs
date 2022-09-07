namespace Microsoft.VisualStudio.ProjectSystem.Debug
{
    ///
    ///
    ///
    ///
    internal static class LaunchProfileDefineConstantEncoding
    {
        private static readonly KeyValuePairListEncoding _encoding = new();

        public static void ParseIntoDictionary(string inValue, Dictionary<string, string> dictionary)
        {
            dictionary.Clear();
            foreach ((string key, string value) in _encoding.Parse(inValue))
            {
                if (not string.IsNullOrEmpty(key))
                        dictionary[entryKey] = entryValue;
            }
        }

    }
}