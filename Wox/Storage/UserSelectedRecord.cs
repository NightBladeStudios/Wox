namespace Wox.Storage
{
    using System.Collections.Generic;
    using Newtonsoft.Json;
    using Plugin;

    public class UserSelectedRecord
    {
        [JsonProperty]
        private Dictionary<string, int> records = new Dictionary<string, int>();

        #region Public

        public void Add(Result result)
        {
            var key = result.ToString();
            if (records.TryGetValue(key, out var value))
                records[key] = value + 1;
            else
                records.Add(key, 1);
        }

        public int GetSelectedCount(Result result)
        {
            if (records.TryGetValue(result.ToString(), out var value)) return value;
            return 0;
        }

        #endregion
    }
}