/*
    This file is part of Station Science.

    Station Science is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    Station Science is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with Station Science.  If not, see <http://www.gnu.org/licenses/>.
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace StationScience
{
    public class StnSciContractReward : IConfigNode
    {
        [Persistent] public float y_intercept;
        [Persistent] public float slope;

        [Persistent] public float advance_multiplier;
        [Persistent] public float failure_multiplier;
        [Persistent] public float first_time_multiplier;

        public StnSciContractReward(float y_intercept = 0, float slope = 0,
                float advance_multiplier = 0, float failure_multiplier = 0, float first_time_multiplier = 1)
        {
            this.y_intercept = y_intercept;
            this.slope = slope;
            this.advance_multiplier = advance_multiplier;
            this.failure_multiplier = failure_multiplier;
            this.first_time_multiplier = first_time_multiplier;
        }

        public float calcReward(float value, bool first_time = false)
        {
            Debug.Log("calcReward: " + y_intercept + " + " + value + " * " + slope + " * ( " + first_time + " ? " + first_time_multiplier + ")");
            return (y_intercept + value * slope) * (first_time ? first_time_multiplier : 1);
        }

        public float calcAdvance(float value, bool first_time = false)
        {
            return calcReward(value, first_time) * advance_multiplier;
        }

        public float calcFailure(float value, bool first_time = false)
        {
            return calcReward(value, first_time) * failure_multiplier;
        }

        public void Load(ConfigNode node)
        {
        }

        public void Save(ConfigNode node)
        {
        }
    }

    public class CNMap<TValue> : Dictionary<string, TValue>, IConfigNode where TValue : IConvertible
    {
        public void Load(ConfigNode node)
        {
            Debug.Log("CNMap: Load called");
            foreach (ConfigNode.Value val in node.values)
            {
                try
                {
                    this[val.name] = (TValue)System.Convert.ChangeType(val.value, typeof(TValue));
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                    this[val.name] = default(TValue);
                }
            }
        }

        public void Save(ConfigNode node)
        {
            Debug.LogError("CNMap.Save called; not implemented");
        }
    }

    public class CNMapSet<TValue> : Dictionary<string, HashSet<TValue> > where TValue : IConvertible
    {
        private static readonly char[] sep = { ',', ' ', '\t', '|' };

        public static HashSet<TValue> parse(string s)
        {
            HashSet<TValue> ret = new HashSet<TValue>();
            foreach(string cur in s.Split(sep))
            {
                ret.Add((TValue)System.Convert.ChangeType(cur, typeof(TValue)));
            }
            return ret;
        }

        public void Load(ConfigNode node)
        {
            Debug.Log("CNMapList: Load called");
            foreach (ConfigNode.Value val in node.values)
            {
                if(!this.ContainsKey(val.name))
                    this[val.name] = new HashSet<TValue>();
                try
                {
                    this[val.name].UnionWith(parse(val.value));
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
            }
        }

        public void Save(ConfigNode node)
        {
            Debug.LogError("CNMapList.Save called; not implemented");
        }
    }

    public class StnSciSettings : IConfigNode
    {
        [Persistent] public int maxContracts = 4;
        [Persistent] public float progressionFactor = 0.5f;
        [Persistent] public float reputationFactor = 0.01f;
        [Persistent] public float trivialMultiplier = 0.25f;
        [Persistent] public float significantMultiplier = 1;
        [Persistent] public float exceptionalMultiplier = 1.5f;

        [Persistent] public StnSciContractReward contractScience = new StnSciContractReward(50, 10);
        [Persistent] public StnSciContractReward contractFunds = new StnSciContractReward(10000, 2000, 1.0f/3.0f, 0.5f, 30);
        [Persistent] public StnSciContractReward contractReputation = new StnSciContractReward(10, 1, failure_multiplier: 1.5f);
        [Persistent] public StnSciContractReward contractDeadline = new StnSciContractReward(2, 0.1f, first_time_multiplier: 2);

        [Persistent] public CNMap<double> experimentChallenge = new CNMap<double>() {
              { "StnSciExperiment1", 1 },
              { "StnSciExperiment2", 2 },
              { "StnSciExperiment3", 2.5 },
              { "StnSciExperiment4", 3 },
              { "StnSciExperiment5", 2.5 },
              { "StnSciExperiment6", 3.5 },
          };
        [Persistent] public CNMap<double> planetChallenge = new CNMap<double>() {
              { "Kerbin", 1 },
              { "Mun", 3 },
              { "Minmus", 3.25 },
              { "Duna", 6 },
              { "Ike", 6.5 },
              { "Eve", 6 },
              { "Gilly", 6.5 },
              { "Dres", 8 },
              { "Jool", 10 },
              { "Laythe", 11 },
              { "Vall", 11.5 },
              { "Tylo", 12 },
              { "Pol", 11 },
              { "Bop", 11 },
              { "Eeloo", 13 },
          };
        [Persistent]
        public CNMapSet<string> experimentPrereqs = new CNMapSet<string>() {
              { "StnSciExperiment1", CNMapSet<string>.parse("StnSciExperiment1,StnSciLab") },
              { "StnSciExperiment2", CNMapSet<string>.parse("StnSciExperiment2,StnSciLab,StnSciCyclo") },
              { "StnSciExperiment3", CNMapSet<string>.parse("StnSciExperiment3,StnSciLab,StnSciCyclo") },
              { "StnSciExperiment4", CNMapSet<string>.parse("StnSciExperiment4,StnSciLab,StnSciCyclo") },
              { "StnSciExperiment5", CNMapSet<string>.parse("StnSciExperiment5,StnSciLab,StnSciZoo") },
              { "StnSciExperiment6", CNMapSet<string>.parse("StnSciExperiment6,StnSciLab,StnSciZoo,StnSciCyclo") },
          };

        public StnSciSettings()
        { }

        public void Load(ConfigNode node)
        {
            Debug.Log("Setings: load called");
            foreach (ConfigNode n in node.GetNodes("experimentChallenge"))
                experimentChallenge.Load(n);
            foreach (ConfigNode n in node.GetNodes("planetChallenge"))
                planetChallenge.Load(n);
            foreach (ConfigNode n in node.GetNodes("experimentPrereqs"))
                experimentPrereqs.Load(n);
        }

        public void Save(ConfigNode node)
        {
        }
    }

    [KSPAddon(KSPAddon.Startup.Instantly, false)]
    public class StnSciContractsUpdater : MonoBehaviour
    {
        static bool started = false;

        internal void Awake()
        {
            if (started)
            {
                print("StnSciContractsUpdater already started");
                Destroy(gameObject);
            }
            print("StnSciContractsUpdater started");
            GameEvents.Contract.onContractsLoaded.Add(OnContractsLoaded);
            started = true;
        }

        public void OnContractsLoaded()
        {
            print("Contracts Loaded");
            foreach (var contract in global::Contracts.ContractSystem.Instance.Contracts)
            {
                if (contract is FinePrint.Contracts.BaseContract || contract is FinePrint.Contracts.StationContract)
                {
                    foreach (var param in contract.AllParameters)
                    {
                        var pr = param as FinePrint.Contracts.Parameters.PartRequestParameter;
                        if (pr != null)
                        {
                            ConfigNode node = new ConfigNode("PARAM");
                            pr.Save(node);
                            print(node.ToString());
                            FixParam(node);
                            pr.Load(node);
                        }
                    }
                }
            }
        }


        public void FixParam(ConfigNode param)
        {
            print("PARAM " + param.GetValue("partNames"));
            if (param.HasValue("partNames"))
            {
                string partNames = param.GetValue("partNames");
                if(partNames.Contains("Large_Crewed_Lab"))
                {
                    bool doUpdate = false;
                    if(!partNames.Contains("StnSciLab"))
                    {
                        partNames += ",StnSciLab";
                        doUpdate = true;
                    }
                    if(!partNames.Contains("StnSciZoo"))
                    {
                        partNames += ",StnSciZoo";
                        doUpdate = true;
                    }
                    if (doUpdate)
                    {
                        print("Updating to: partNames = " + partNames);
                        param.SetValue("partNames", partNames);
                    }
                }
            }
        }
    }

    [KSPScenario(ScenarioCreationOptions.AddToAllGames, GameScenes.SPACECENTER,
                    GameScenes.FLIGHT, GameScenes.EDITOR, GameScenes.TRACKSTATION)]
    public class StnSciScenario : ScenarioModule
    {
        public static StnSciScenario Instance { get; private set; }

        public StnSciSettings settings { get; private set; }

        [KSPField(isPersistant = true)]
        public float xp = 0;

        public StnSciScenario()
        {
            Instance = this;
            if(settings == null)
            {
                settings = new StnSciSettings();
                foreach(ConfigNode node in GameDatabase.Instance.GetConfigNodes("STN_SCI_SETTINGS"))
                {
                    if (!ConfigNode.LoadObjectFromConfig(settings, node))
                    {
                        Debug.Log("Station Science: failed to load settings");
                    }
                    settings.Load(node);
                }
            }
        }

        void Start()
        {
            print("StnSciScenario started");
        }
    }
}
