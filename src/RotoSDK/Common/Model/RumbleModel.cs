namespace com.rotovr.sdk
{
    [Serializable]
    public class RumbleModel
    {
        public RumbleModel(float duration, int power)
        {
            Duration = duration;
            Power = power;
        }
        
        public RumbleModel(string json)
        {
            var dict = JsonSerializer.Deserialize<Dictionary<string, object>>(json);
            
            Duration = Convert.ToSingle(dict["Duration"]);
            Power = Convert.ToInt32(dict["Power"]);
        }

        public float Duration { get; }
        public int Power { get; }
        
        
        public string ToJson()
        {
            var dict = new Dictionary<string, object>
            {
                { "Duration", Duration },
                { "Power", Power }
            };
            
            return JsonSerializer.Serialize(dict);
        }
    }
}