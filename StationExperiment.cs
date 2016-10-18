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
namespace StationScience
{
    public class StationExperiment : ModuleScienceExperiment
    {
        [KSPField(isPersistant = false)]
        public int eurekasRequired;

        [KSPField(isPersistant = false)]
        public int kuarqsRequired;

        [KSPField(isPersistant = false)]
        public float kuarqHalflife;

        [KSPField(isPersistant = false, guiName = "Decay rate", guiUnits = " kuarqs/s", guiActive = false, guiFormat = "F2")]
        public float kuarqDecay;

        [KSPField(isPersistant = false)]
        public int bioproductsRequired;

        [KSPField(isPersistant = true)]
        public float launched = 0;

        [KSPField(isPersistant = true)]
        public float completed = 0;

        [KSPField(isPersistant = true)]
        public string last_subjectId = "";

        public static bool checkBoring(Vessel vessel, bool msg = false)
        {
            //print(vessel.Landed + ", " + vessel.landedAt + ", " + vessel.launchTime + ", " + vessel.situation + ", " + vessel.orbit.referenceBody.name);
            if ((vessel.orbit.referenceBody.name == "Kerbin") && (vessel.situation == Vessel.Situations.LANDED || vessel.situation == Vessel.Situations.PRELAUNCH || vessel.situation == Vessel.Situations.SPLASHED || vessel.altitude <= vessel.orbit.referenceBody.atmosphereDepth))
            {
                if (msg)
                    ScreenMessages.PostScreenMessage("Too boring here. Go to space!", 6, ScreenMessageStyle.UPPER_CENTER);
                return true;
            }
            return false;
        }

        public PartResource getResource(string name)
        {
            return ResourceHelper.getResource(part, name);
        }

        public double getResourceAmount(string name)
        {
            return ResourceHelper.getResourceAmount(part, name);
        }

        public double getResourceMaxAmount(string name)
        {
            return ResourceHelper.getResourceMaxAmount(part, name);
        }

        public PartResource setResourceMaxAmount(string name, double max)
        {
            return ResourceHelper.setResourceMaxAmount(part, name, max);
        }

        public bool finished()
        {
            double numEurekas = getResourceAmount("Eurekas");
            double numKuarqs = getResourceAmount("Kuarqs");
            double numBioproducts = getResourceAmount("Bioproducts");
            //print(part.partInfo.title + " Eurekas: " + numEurekas + "/" + eurekasRequired);
            //print(part.partInfo.title + " Kuarqs: " + numKuarqs + "/" + kuarqsRequired);
            //print(part.partInfo.title + " Bioproducts: " + numBioproducts + "/" + bioproductsRequired);
            return Math.Round(numEurekas, 2) >= eurekasRequired && Math.Round(numKuarqs,2) >= kuarqsRequired && Math.Round(numBioproducts, 2) >= bioproductsRequired - 0.001;
        }

        public override void OnStart(StartState state)
        {
            base.OnStart(state);
            if (state == StartState.Editor) { return; }
            Fields["kuarqDecay"].guiActive = (kuarqsRequired > 0 && kuarqHalflife > 0);
            Events["DeployExperiment"].active = finished();
            this.part.force_activate();
            StartCoroutine(updateStatus());
            //Actions["DeployAction"].active = false;
        }

        [KSPEvent(guiActive = true, guiName = "Start Experiment", active = true)]
        public void StartExperiment()
        {
            if (GetScienceCount() > 0)
            {
                ScreenMessages.PostScreenMessage("Experiment already finalized.", 6, ScreenMessageStyle.UPPER_CENTER);
                return;
            }
            if (checkBoring(vessel, true)) return;
            PartResource eurekas = setResourceMaxAmount("Eurekas", eurekasRequired);
            PartResource kuarqs = setResourceMaxAmount("Kuarqs", kuarqsRequired);
            PartResource bioproducts = setResourceMaxAmount("Bioproducts", bioproductsRequired);
            if (eurekas.amount == 0 && bioproducts != null) bioproducts.amount = 0;
            Events["StartExperiment"].active = false;
            ScreenMessages.PostScreenMessage("Started experiment!", 6, ScreenMessageStyle.UPPER_CENTER);
        }

        [KSPAction("Start Experiment")]
        public void StartExpAction(KSPActionParam p)
        {
            StartExperiment();
        }


        public bool deployChecks()
        {
            if (checkBoring(vessel, true)) return false;
            if (finished())
            {
                Events["DeployExperiment"].active = false;
                Events["StartExperiment"].active = false;
                return true;
            }
            else
            {
                ScreenMessages.PostScreenMessage("Experiment not finished yet!", 6, ScreenMessageStyle.UPPER_CENTER);
            }
            return false;
        }

        new public void DeployExperiment()
        {
            if (deployChecks())
                base.DeployExperiment();
        }

        new public void DeployAction(KSPActionParam p)
        {
            if (deployChecks())
                base.DeployAction(p);
        }

        new public void ResetExperiment()
        {
            base.ResetExperiment();
            stopResearch("Bioproducts");
            Events["StartExperiment"].active = true;
        }

        new public void ResetExperimentExternal()
        {
            base.ResetExperimentExternal();
            stopResearch("Bioproducts");
            Events["StartExperiment"].active = true;
        }

        new public void ResetAction(KSPActionParam p)
        {
            base.ResetAction(p);
            stopResearch("Bioproducts");
            Events["StartExperiment"].active = true;
        }

        public override void OnFixedUpdate()
        {
            base.OnFixedUpdate();
            if (kuarqHalflife > 0 && kuarqsRequired > 0)
            {
                var kuarqs = getResource("Kuarqs");
                if (kuarqs != null && kuarqs.amount < (.99 * kuarqsRequired))
                {
                    double decay = Math.Pow(.5, TimeWarp.fixedDeltaTime / kuarqHalflife);
                    kuarqDecay = (float)((kuarqs.amount * (1 - decay)) / TimeWarp.fixedDeltaTime);
                    kuarqs.amount = kuarqs.amount * decay;
                }
                else
                    kuarqDecay = 0;
            }
        }

        public void stopResearch(string resName)
        {
            setResourceMaxAmount(resName, 0);
        }

        public void stopResearch()
        {
            stopResearch("Eurekas");
            stopResearch("Kuarqs");
        }

        public System.Collections.IEnumerator updateStatus()
        {
            while (true)
            {
                //print(part.partInfo.title + "updateStatus");
                double numEurekas = getResourceAmount("Eurekas");
                double numEurekasMax = getResourceMaxAmount("Eurekas");
                double numKuarqs = getResourceAmount("Kuarqs");
                double numKuarqsMax = getResourceMaxAmount("Kuarqs");
                double numBioproducts = getResourceAmount("Bioproducts");
                int sciCount = GetScienceCount();
                //print(part.partInfo.title + " finished: " + finished());
                if (!finished())
                {
                    Events["DeployExperiment"].active = false;
                    Events["StartExperiment"].active = (!Inoperable && sciCount == 0 && numEurekasMax == 0 && numKuarqsMax == 0);
                }
                else
                {
                    Events["DeployExperiment"].active = true;
                    Events["StartExperiment"].active = false;
                }
                var subject = ScienceHelper.getScienceSubject(experimentID, vessel);
                string subjectId = ((subject == null) ? "" : subject.id);
                if(subjectId != "" && last_subjectId != "" && last_subjectId != subjectId &&
                    (numEurekas > 0 || numKuarqs > 0 || (numBioproducts > 0 && sciCount == 0))) {
                    ScreenMessages.PostScreenMessage("Location changed mid-experiment! " + part.partInfo.title + " ruined.", 6, ScreenMessageStyle.UPPER_CENTER);
                    stopResearch();
                    stopResearch("Bioproducts");
                }
                last_subjectId = subjectId;
                if (sciCount > 0)
                {
                    stopResearch();
                    if (completed == 0)
                        completed = (float) Planetarium.GetUniversalTime();
                }
                if (numEurekas > 0)
                {
                    var eurekasModules = vessel.FindPartModulesImplementing<StationScienceModule>();
                    if (eurekasModules == null || eurekasModules.Count() < 1)
                    {
                        ScreenMessages.PostScreenMessage("Warning: " + part.partInfo.title + " has detached from the station without being finalized.", 2, ScreenMessageStyle.UPPER_CENTER);
                    }
                }
                /*
                if (numKuarqs > 0)
                {
                    var kuarqModules = vessel.FindPartModulesImplementing<KuarqGenerator>();
                    if (kuarqModules == null || kuarqModules.Count() < 1)
                    {
                        stopResearch("Kuarqs");
                    }
                }
                */
                if (numBioproducts > 0 && Inoperable)
                {
                    stopResearch("Bioproducts");
                }
                if (bioproductsRequired > 0 && GetScienceCount() > 0 && numBioproducts < bioproductsRequired)
                {
                    ResetExperiment();
                }
                yield return new UnityEngine.WaitForSeconds(1f);
            }
        }

        public override string GetInfo()
        {
            string ret = "";
            string reqLab = "", reqCyclo = "", reqZoo = "";
            if (eurekasRequired > 0)
            {
                ret += "Eurekas required: " + eurekasRequired;
                reqLab = "\n<color=#DD8800>Requires a TH-NKR Research Lab</color>";
            }
            if (kuarqsRequired > 0)
            {
                if (ret != "") ret += "\n";
                ret += "Kuarqs required: " + kuarqsRequired;
                double productionRequired = 0.01;
                if (kuarqHalflife > 0)
                {
                    if (ret != "") ret += "\n";
                    ret += "Kuarq decay halflife: " + kuarqHalflife + " seconds" + "\n";
                    productionRequired = kuarqsRequired * (1 - Math.Pow(.5, 1.0 / kuarqHalflife));
                    ret += String.Format("Production required: {0:F2} kuarq/s", productionRequired);
                }
                if (productionRequired > 1)
                    reqCyclo = "\n<color=#DD8800>Requires " + (Math.Ceiling(productionRequired)) + " D-ZZY Cyclotrons</color>";
                else
                    reqCyclo = "\n<color=#DD8800>Requires a D-ZZY Cyclotron</color>";
            }
            if (bioproductsRequired > 0)
            {
                if (ret != "") ret += "\n";
                ret += "Bioproducts required: " + bioproductsRequired;
                double bioproductDensity = ResourceHelper.getResourceDensity("Bioproducts");
                if (bioproductDensity > 0)
                    ret += String.Format("\nMass when complete: {0:G} t", Math.Round(bioproductsRequired * bioproductDensity + part.mass,2));
                reqZoo = "\n<color=#DD8800>Requires a F-RRY Zoology Bay</color>";
            }
            return ret + reqLab + reqCyclo + reqZoo + "\n\n" + base.GetInfo();
        }
    }
}