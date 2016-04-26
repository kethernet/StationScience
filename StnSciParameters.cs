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
using KSP;
using Contracts;
using Contracts.Parameters;

namespace StationScience.Contracts.Parameters
{
    /*
    public class UnlockPartsParameter : ContractParameter
    {
        List<AvailablePart> parts;

        public UnlockPartsParameter()
        {
            this.Enabled = true;
            this.DisableOnStateChange = true;
            this.parts = new List<AvailablePart>();
        }

        public UnlockPartsParameter(IEnumerable<AvailablePart> p)
        {
            this.Enabled = true;
            this.DisableOnStateChange = true;
            SetParts(p);
            doUpdate();
        }

        public UnlockPartsParameter(string p)
        {
            this.Enabled = true;
            this.DisableOnStateChange = true;
            SetParts(p);
            doUpdate();
        }

        public void SetParts(IEnumerable<AvailablePart> p)
        {
            this.parts = new List<AvailablePart>(p);
        }

        public void SetParts(string p)
        {
            this.parts = new List<AvailablePart>();
            foreach (string s in p.Split(','))
            {
                AvailablePart a = PartLoader.getPartInfoByName(s);
                if (a != null)
                    this.parts.Add(a);
                else
                    Debug.LogError("Part not found: " + s);
            }
        }

        public List<AvailablePart> GetParts()
        {
            return new List<AvailablePart>(this.parts);
        }

        protected override string GetHashString()
        {
            return "test";
        }

        public string GetReadableList()
        {
            StringBuilder ret = new StringBuilder("");
            for (int i = 0; i < this.parts.Count; i++)
            {
                if (i > 0)
                {
                    if (i == this.parts.Count - 1)
                        ret.Append(" and ");
                    else
                        ret.Append(", ");
                }
                ret.Append(this.parts[i].title);
            }
            return ret.ToString();
        }

        public string GetNameList()
        {
            return string.Join(",", parts.Select(part => part.name).ToArray());
        }

        protected override string GetTitle()
        {
            return "Research " + GetReadableList();
        }

        protected override void OnRegister()
        {
            GameEvents.OnPartPurchased.Add(OnPartPurchased);
            GameEvents.OnTechnologyResearched.Add(OnTechnologyResearched);
        }
        protected override void OnUnregister()
        {
            GameEvents.OnPartPurchased.Remove(OnPartPurchased);
            GameEvents.OnTechnologyResearched.Remove(OnTechnologyResearched);
        }

        private void OnPartPurchased(AvailablePart a)
        {
            doUpdate();
        }

        private void OnTechnologyResearched(GameEvents.HostTargetAction<RDTech, RDTech.OperationResult> a)
        {
            doUpdate();
        }

        private void doUpdate()
        {
            if (parts.All(part => ResearchAndDevelopment.PartTechAvailable(part) && ResearchAndDevelopment.PartModelPurchased(part)))
                SetComplete();
            else
                SetIncomplete();
        }

        protected override void OnSave(ConfigNode node)
        {
            base.OnSave(node);
            node.AddValue("parts", GetNameList());
        }
        protected override void OnLoad(ConfigNode node)
        {
            base.OnLoad(node);
            string parts = node.GetValue("parts");
            SetParts(parts);
            doUpdate();
        }
    }*/

    public interface PartRelated
    {
        AvailablePart GetPartType();
    }

    public interface BodyRelated
    {
        CelestialBody GetBody();
    }

    public class StnSciParameter : ContractParameter, PartRelated, BodyRelated
    {
        AvailablePart experimentType;
        CelestialBody targetBody;

        public AvailablePart GetPartType()
        {
            return experimentType;
        }

        public CelestialBody GetBody()
        {
            return targetBody;
        }

        public StnSciParameter()
        {
            //SetExperiment("StnSciExperiment1");
            this.Enabled = true;
            this.DisableOnStateChange = false;
        }

        public StnSciParameter(AvailablePart type, CelestialBody body)
        {
            this.Enabled = true;
            this.DisableOnStateChange = false;
            this.experimentType = type;
            this.targetBody = body;
            this.AddParameter(new Parameters.NewPodParameter(), null);
            this.AddParameter(new Parameters.DoExperimentParameter(), null);
            this.AddParameter(new Parameters.ReturnExperimentParameter(), null);
        }

        protected override string GetHashString()
        {
            return experimentType.name;
        }

        protected override string GetTitle()
        {
            return "Complete " + experimentType.title + " in orbit around " + targetBody.theName;
        }

        protected override string GetNotes()
        {
            return "Launch a new experiment part (" + experimentType.title +
                "), bring it into orbit around " + targetBody.theName +
                ", complete the experiment, return it (with results inside) to Kerbin and recover it";
        }

        private bool SetExperiment(string exp)
        {
            experimentType = PartLoader.getPartInfoByName(exp);
            if (experimentType == null)
            {
                Debug.LogError("Couldn't find experiment part: " + exp);
                return false;
            }
            return true;
        }

        public void Complete()
        {
            SetComplete();
        }

        private bool SetTarget(string planet)
        {
            targetBody = FlightGlobals.Bodies.FirstOrDefault(body => body.bodyName.ToLower() == planet.ToLower());
            if (targetBody == null)
            {
                Debug.LogError("Couldn't find planet: " + planet);
                return false;
            }
            return true;
        }

        protected override void OnSave(ConfigNode node)
        {
            base.OnSave(node);
            node.AddValue("targetBody", targetBody.name);
            node.AddValue("experimentType", experimentType.name);
        }
        protected override void OnLoad(ConfigNode node)
        {
            base.OnLoad(node);
            this.Enabled = true;
            string expID = node.GetValue("experimentType");
            SetExperiment(expID);
            string bodyID = node.GetValue("targetBody");
            SetTarget(bodyID);
        }

        static public AvailablePart getExperimentType(ContractParameter o)
        {
            object par = o.Parent;
            if (par == null)
                par = o.Root;
            PartRelated parent = par as PartRelated;
            if (parent != null)
                return parent.GetPartType();
            else
                return null;
        }

        static public CelestialBody getTargetBody(ContractParameter o)
        {
            BodyRelated parent = o.Parent as BodyRelated;
            if (parent != null)
                return parent.GetBody();
            else
                return null;
        }
    }

    public class NewPodParameter : ContractParameter
    {
        public NewPodParameter()
        {
            //SetExperiment("StnSciExperiment1");
            this.Enabled = true;
            this.DisableOnStateChange = false;
        }

        protected override string GetHashString()
        {
            return "new pod parameter " + this.GetHashCode();
        }
        protected override string GetTitle()
        {
            AvailablePart experimentType = StnSciParameter.getExperimentType(this);
            if (experimentType == null)
                return "Launch new experiment pod";
            return "Launch new " + experimentType.title;
        }

        protected override void OnRegister()
        {
            GameEvents.onLaunch.Add(OnLaunch);
            GameEvents.onVesselSituationChange.Add(OnVesselSituationChange);
        }
        protected override void OnUnregister()
        {
            GameEvents.onLaunch.Remove(OnLaunch);
            GameEvents.onVesselSituationChange.Remove(OnVesselSituationChange);
        }

        private void OnVesselCreate(Vessel vessel)
        {
            Debug.Log("OnVesselCreate");
            AvailablePart experimentType = StnSciParameter.getExperimentType(this);
            if (experimentType == null)
                return;
            foreach (Part part in vessel.Parts)
            {
                if (part.name == experimentType.name)
                {
                    StationExperiment e = part.FindModuleImplementing<StationExperiment>();
                    if (e != null)
                    {
                        e.launched = (float)Planetarium.GetUniversalTime();
                    }
                }
            }
        }

        private void OnVesselSituationChange(GameEvents.HostedFromToAction<Vessel,Vessel.Situations> arg)
        {
            Debug.Log("OnVesselSituationChanged");
            if(!((arg.from == Vessel.Situations.LANDED || arg.from == Vessel.Situations.PRELAUNCH) &&
                  (arg.to == Vessel.Situations.FLYING || arg.to == Vessel.Situations.SUB_ORBITAL)))
                return;
            if (arg.host.mainBody.theName != "Kerbin")
                return;
            AvailablePart experimentType = StnSciParameter.getExperimentType(this);
            if (experimentType == null)
                return;
            foreach (Part part in arg.host.Parts)
            {
                if (part.name == experimentType.name)
                {
                    StationExperiment e = part.FindModuleImplementing<StationExperiment>();
                    if (e != null && e.launched == 0)
                    {
                        e.launched = (float)Planetarium.GetUniversalTime();
                    }
                }
            }
        }

        private void OnLaunch(EventReport report)
        {
            AvailablePart experimentType = StnSciParameter.getExperimentType(this);
            if (experimentType == null)
                return;
            Vessel vessel = FlightGlobals.ActiveVessel;
            foreach (Part part in vessel.Parts)
            {
                if (part.name == experimentType.name)
                {
                    StationExperiment e = part.FindModuleImplementing<StationExperiment>();
                    if (e != null && e.launched == 0)
                    {
                        e.launched = (float)Planetarium.GetUniversalTime();
                    }
                }
            }
        }

        private float lastUpdate = 0;

        protected override void OnUpdate()
        {
            base.OnUpdate();
            if (lastUpdate > UnityEngine.Time.realtimeSinceStartup + .1)
                return;
            lastUpdate = UnityEngine.Time.realtimeSinceStartup;
            Vessel vessel = FlightGlobals.ActiveVessel;
            AvailablePart experimentType = StnSciParameter.getExperimentType(this);
            if (experimentType == null)
                return;
            if (vessel != null)
                foreach (Part part in vessel.Parts)
                {
                    if (part.name == experimentType.name)
                    {
                        StationExperiment e = part.FindModuleImplementing<StationExperiment>();
                        if (e != null)
                        {
                            if (e.launched >= this.Root.DateAccepted)
                            {
                                SetComplete();
                                return;
                            }
                        }
                    }
                }
            SetIncomplete();
        }

        protected override void OnSave(ConfigNode node)
        {
            base.OnSave(node);
        }
        protected override void OnLoad(ConfigNode node)
        {
            base.OnLoad(node);
            this.Enabled = true;
        }
    }

    public class DoExperimentParameter : ContractParameter
    {
        public DoExperimentParameter()
        {
            this.Enabled = true;
            this.DisableOnStateChange = false;
        }

        protected override string GetHashString()
        {
            return "do experiment " + this.GetHashCode();
        }
        protected override string GetTitle()
        {
            CelestialBody targetBody = StnSciParameter.getTargetBody(this);
            if (targetBody == null)
                return "Complete in orbit";
            else
                return "Complete in orbit around " + targetBody.theName;
        }

        private float lastUpdate = 0;

        protected override void OnUpdate()
        {
            base.OnUpdate();
            if (lastUpdate > UnityEngine.Time.realtimeSinceStartup + .1)
                return;
            CelestialBody targetBody = StnSciParameter.getTargetBody(this);
            AvailablePart experimentType = StnSciParameter.getExperimentType(this);
            if (targetBody == null || experimentType == null)
            if (targetBody == null || experimentType == null)
            {
                Debug.Log("targetBody or experimentType is null");
                return;
            }
            lastUpdate = UnityEngine.Time.realtimeSinceStartup;
            Vessel vessel = FlightGlobals.ActiveVessel;
            if (vessel != null)
                foreach (Part part in vessel.Parts)
                {
                    if (part.name == experimentType.name)
                    {
                        StationExperiment e = part.FindModuleImplementing<StationExperiment>();
                        if (e != null)
                        {
                            if (e.completed >= this.Root.DateAccepted && e.completed > e.launched)
                            {
                                ScienceData[] data = e.GetData();
                                foreach (ScienceData datum in data)
                                {
                                    if (datum.subjectID.ToLower().Contains("@" + targetBody.name.ToLower() + "inspace"))
                                    {
                                        SetComplete();
                                        return;
                                    }
                                }
                            }
                        }
                    }
                }
            SetIncomplete();
        }

        protected override void OnSave(ConfigNode node)
        {
            base.OnSave(node);
        }
        protected override void OnLoad(ConfigNode node)
        {
            base.OnLoad(node);
            this.Enabled = true;
        }
    }

    public class ReturnExperimentParameter : ContractParameter
    {
        public ReturnExperimentParameter()
        {
            this.Enabled = true;
            this.DisableOnStateChange = false;
        }

        public void OnAccept(Contract contract)
        {
        }

        protected override string GetHashString()
        {
            return "recover experiment " + this.GetHashCode();
        }
        protected override string GetTitle()
        {
            return "Recover at Kerbin";
        }

        protected override void OnRegister()
        {
            //GameEvents.OnVesselRecoveryRequested.Add(OnRecovery);
            GameEvents.onVesselRecovered.Add(OnRecovered);
        }
        protected override void OnUnregister()
        {
            //GameEvents.OnVesselRecoveryRequested.Remove(OnRecovery);
            GameEvents.onVesselRecovered.Remove(OnRecovered);
        }

        private void OnRecovered(ProtoVessel pv, bool dummy)
        {
            Debug.Log("Recovered " + pv.vesselName);
            CelestialBody targetBody = StnSciParameter.getTargetBody(this);
            AvailablePart experimentType = StnSciParameter.getExperimentType(this);
            if (targetBody == null || experimentType == null)
            {
                Debug.Log("targetBody or experimentType is null");
                return;
            }
            foreach (ProtoPartSnapshot part in pv.protoPartSnapshots)
            {
                if (part.partName == experimentType.name)
                {
                    foreach(ProtoPartModuleSnapshot module in part.modules)
                    {
                        if (module.moduleName == "StationExperiment")
                        {
                            ConfigNode cn = module.moduleValues;
                            if (!cn.HasValue("launched") || !cn.HasValue("completed"))
                                continue;
                            float launched, completed;
                            try
                            {
                                launched = float.Parse(cn.GetValue("launched"));
                                completed = float.Parse(cn.GetValue("completed"));
                            }
                            catch(Exception e)
                            {
                                Debug.LogError(e.ToString());
                                continue;
                            }
                            if (launched >= this.Root.DateAccepted && completed >= launched)
                            {
                                foreach (ConfigNode datum in cn.GetNodes("ScienceData"))
                                {
                                    if (!datum.HasValue("subjectID"))
                                        continue;
                                    string subjectID = datum.GetValue("subjectID");
                                    if (subjectID.ToLower().Contains("@" + targetBody.name.ToLower() + "inspace"))
                                    {
                                        StnSciParameter parent = this.Parent as StnSciParameter;
                                        SetComplete();
                                        if (parent != null)
                                            parent.Complete();
                                        return;
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        private void OnRecovery(Vessel vessel)
        {
            Debug.Log("Recovering " + vessel.vesselName);
            CelestialBody targetBody = StnSciParameter.getTargetBody(this);
            AvailablePart experimentType = StnSciParameter.getExperimentType(this);
            if (targetBody == null || experimentType == null)
            {
                Debug.Log("targetBody or experimentType is null");
                return;
            }
            foreach (Part part in vessel.Parts)
            {
                if (part.name == experimentType.name)
                {
                    StationExperiment e = part.FindModuleImplementing<StationExperiment>();
                    if (e != null)
                    {
                        if (e.launched >= this.Root.DateAccepted && e.completed >= e.launched)
                        {
                            ScienceData[] data = e.GetData();
                            foreach (ScienceData datum in data)
                            {
                                if (datum.subjectID.ToLower().Contains("@" + targetBody.name.ToLower() + "inspace"))
                                {
                                    StnSciParameter parent = this.Parent as StnSciParameter;
                                    SetComplete();
                                    if (parent != null)
                                        parent.Complete();
                                    return;
                                }
                            }
                        }
                    }
                }
            }
            SetIncomplete();
        }

        protected override void OnSave(ConfigNode node)
        {
            base.OnSave(node);
        }
        protected override void OnLoad(ConfigNode node)
        {
            base.OnLoad(node);
            this.Enabled = true;
        }
    }
}
