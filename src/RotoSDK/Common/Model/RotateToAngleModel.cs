namespace com.rotovr.sdk
{
    [Serializable]
    public class RotateToAngleModel
    {
        public RotateToAngleModel(int angle, int power, Direction direction): this(angle, power, direction.ToString())
        {
        }
        
        public RotateToAngleModel(int angle, int power, string direction)
        {
            Angle = angle;
            Power = power;
            Direction = direction;
        }

        public RotateToAngleModel(string json)
        {
            var dict = JsonSerializer.Deserialize<Dictionary<string, object>>(json);
            
            Angle = Convert.ToInt32(dict["Angle"]);
            Power = Convert.ToInt32(dict["Power"]);
            Direction = dict["Direction"].ToString();
        }
        
        public int Angle { get; }
        public int Power { get; }
        public string Direction { get; }

        public string ToJson()
        {
            var dict = new Dictionary<string, object>();
            dict.Add("Angle", Angle);
            dict.Add("Power", Power);
            dict.Add("Direction", Direction);
            
            return JsonSerializer.Serialize(dict);

        }
    }
}
