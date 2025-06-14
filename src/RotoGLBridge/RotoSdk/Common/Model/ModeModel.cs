using System;
using System.Collections.Generic;

namespace com.rotovr.sdk
{
    [Serializable]
    public class ModeModel
    {
        public ModeModel(string mode, ModeParametersModel parametersModel)
        {
            Mode = mode;
            ModeParametersModel = parametersModel;
        }

        public ModeModel(string json)
        {
            var dict = JsonSerializer.Deserialize<Dictionary<string, object>>(json);
            
            Mode = dict["Mode"].ToString();
            
            var modeParamsDict = dict["ModeParametersModel"] as Dictionary<string, object>;

            var targetCockpit = Convert.ToInt32(modeParamsDict["TargetCockpit"]);
            var maxPower = Convert.ToInt32(modeParamsDict["MaxPower"]);
            var movementMode = modeParamsDict["MovementMode"].ToString();
            ModeParametersModel = new ModeParametersModel(targetCockpit, maxPower, movementMode);
        }

        public string Mode { get; set; }
        public ModeParametersModel ModeParametersModel { get; set; }

        public string ToJson()
        {
            
            var dict = new Dictionary<string, object>();
            dict.Add("Mode", Mode);
            var modeParametersModel = new Dictionary<string, object>
            {
                { "TargetCockpit", ModeParametersModel.TargetCockpit },
                { "MaxPower", ModeParametersModel.MaxPower },
                { "MovementMode", ModeParametersModel.MovementMode }
            };
            dict.Add("ModeParametersModel", modeParametersModel);

            return JsonSerializer.Serialize(dict);
        }
    }
}