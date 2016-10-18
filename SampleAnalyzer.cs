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
    class SampleAnalyzer : ModuleScienceContainer
    {
        //[KSPEvent(active=true, guiActive=true, guiName="Update")]
        List<BaseEvent> sampleEvents = new List<BaseEvent>();

        [KSPField(isPersistant = false)]
        public int kuarqsRequired = 0;

        [KSPField(isPersistant = false)]
        public float kuarqHalflife = 0;

        [KSPField(isPersistant = false, guiName = "Decay rate", guiUnits = " kuarqs/s", guiActive = false, guiFormat = "F2")]
        public float kuarqDecay = 0;

        [KSPField(isPersistant = false)]
        public float txValue = .8F;

        [KSPField(isPersistant = true)]
        public int lightsMode = 1;
        // 0: force off; 1: auto; 2: force on

        public void updateLightsMode()
        {
            switch (lightsMode)
            {
                case 0:
                    Events["LightsMode"].guiName = "Lights: Off";
                    break;
                case 1:
                    Events["LightsMode"].guiName = "Lights: Auto";
                    break;
                case 2:
                    Events["LightsMode"].guiName = "Lights: On";
                    break;
            }
            updateLights();
        }

        [KSPEvent(guiActive = true, guiName = "Lights: Auto", active = true)]
        public void LightsMode()
        {
            lightsMode += 1;
            if (lightsMode > 2)
                lightsMode = 0;
            updateLightsMode();
        }

        public PartResource getResource(string name)
        {
            return ResourceHelper.getResource(part, name);
        }

        public double getResourceAmount(string name)
        {
            return ResourceHelper.getResourceAmount(part, name);
        }

        public PartResource setResourceMaxAmount(string name, double max)
        {
            return ResourceHelper.setResourceMaxAmount(part, name, max);
        }

        public void analyze(IScienceDataContainer cont, ScienceData sd)
        {
            //print(sd.title);
            if (GetScienceCount() > 0)
            {
                ScreenMessages.PostScreenMessage("Analyzer already full. Transmit the data!", 6, ScreenMessageStyle.UPPER_CENTER);
                return;
            }
            cont.DumpData(sd);
            this.AddData(sd);
            if (kuarqsRequired > 0)
            {
                setResourceMaxAmount("Kuarqs", kuarqsRequired);
                Events["ReviewDataEvent"].guiActive = false;
            }
            else
            {
                sd.baseTransmitValue = txValue;
                this.ReviewData();
            }
            this.updateList();
        }

        public new void ReviewDataEvent()
        {
            var kuarqs = getResourceAmount("Kuarqs");
            if (kuarqs > 0 && kuarqs < kuarqsRequired)
            {
                ScreenMessages.PostScreenMessage("Analysis still in progress.", 6, ScreenMessageStyle.UPPER_CENTER);
                return;
            }
            base.ReviewDataEvent();
        }

        public new void ReviewData()
        {
            var kuarqs = getResourceAmount("Kuarqs");
            if (kuarqs > 0 && kuarqs < kuarqsRequired)
            {
                ScreenMessages.PostScreenMessage("Analysis still in progress.", 6, ScreenMessageStyle.UPPER_CENTER);
                return;
            }
            base.ReviewData();
        }

        [KSPField(guiActive = true, guiName = "Status", isPersistant = false)]
        public string status;

        public void addDataButton(ScienceData sd, IScienceDataContainer cont)
        {
            var subject = ResearchAndDevelopment.GetSubjectByID(sd.subjectID);
            var parts = sd.subjectID.Split('@');
            var experiment = ResearchAndDevelopment.GetExperiment(parts[0]);
            if (experiment != null && sd.baseTransmitValue < txValue)
            {
                KSPEvent kspevent = new KSPEvent();
                kspevent.active = true;
                kspevent.name = sd.subjectID;
                kspevent.guiActive = true;
                kspevent.guiName = sd.title;
                IScienceDataContainer my_cont = cont;
                ScienceData my_sd = sd;
                BaseEvent ev = new BaseEvent(Events, sd.subjectID, delegate() { analyze(my_cont, my_sd); }, kspevent);
                sampleEvents.Add(ev);
                Events.Add(ev);
            }
        }

        public void updateLights()
        {
            bool animActive = false;
            if (animator != null)
            {
                if (lightsMode == 1)
                {
                    var kuarqs = getResource("Kuarqs");
                    animActive = (kuarqs!=null && kuarqs.maxAmount > 0 && kuarqs.amount < kuarqsRequired && kuarqs.amount > 0);
                }
                else if (lightsMode == 2) animActive = true;
                else if (lightsMode == 0) animActive = false;
                if (animActive && animator.Progress == 0 && animator.status.StartsWith("Locked", true, null))
                {
                    animator.allowManualControl = true;
                    animator.Toggle();
                    animator.allowManualControl = false;
                }
                else if (!animActive && animator.Progress == 1 && animator.status.StartsWith("Locked", true, null))
                {
                    animator.allowManualControl = true;
                    animator.Toggle();
                    animator.allowManualControl = false;
                }
            }
        }

        public void updateList()
        {
            /*
            var events = UnityEngine.Object.FindObjectsOfType<UIPartActionEventItem>();
            foreach(var evit in events)
            {
                BaseEvent ev = evit.Evt;
                if (sampleEvents.Contains(ev))
                {
                    return;
                }
            }*/
            updateLights();
            foreach (BaseEvent ev in sampleEvents)
            {
                ev.guiActive = false;
                Events.Remove(ev);
            }
            sampleEvents.Clear();
            if (GetScienceCount() > 0)
            {
                if (kuarqsRequired > 0)
                {
                    var kuarqs = getResource("Kuarqs");
                    if (kuarqs != null && kuarqs.maxAmount > 0 && kuarqs.amount < kuarqsRequired)
                    {
                        if (kuarqs.amount == 0)
                            status = "Ready to analyze.";
                        else
                            status = "Analyzing...";
                    }
                    else
                        status = "Ready to transmit.";
                }
                else
                    status = "Ready to transmit.";
            }
            else
            {
                foreach (var cont in vessel.FindPartModulesImplementing<IScienceDataContainer>())
                {
                    if (cont is SampleAnalyzer)
                        continue;
                    foreach (ScienceData sd in cont.GetData())
                    {
                        addDataButton(sd, cont);
                    }
                }
                if (sampleEvents.Count == 0)
                    status = "Nothing to analyze.";
                else
                    status = "Ready to analyze.";
            }
            updateLights();
        }

        private double lastUpdate = 0;

        public override void OnFixedUpdate()
        {
            base.OnFixedUpdate();
            double curTime = UnityEngine.Time.realtimeSinceStartup;
            if (lastUpdate + 2 < curTime)
            {
                updateList();
                lastUpdate = curTime;
            }
            if (kuarqsRequired > 0)
            {
                var numKuarqs = getResourceAmount("Kuarqs");
                if (numKuarqs > 0)
                {

                    if (GetScienceCount() == 0)
                    {
                        ScreenMessages.PostScreenMessage("Sample under analysis was transmitted away before completion.", 6, ScreenMessageStyle.UPPER_CENTER);
                        setResourceMaxAmount("Kuarqs", 0);
                    }
                    if (numKuarqs >= kuarqsRequired && GetScienceCount() > 0)
                    {
                        var sdata = this.GetData();
                        if (sdata != null && sdata.Length == 1)
                        {
                            var sd = sdata[0];
                            sd.baseTransmitValue = txValue;
                            ScreenMessages.PostScreenMessage("Analysis complete, and ready to transmit.", 6, ScreenMessageStyle.UPPER_CENTER);
                            setResourceMaxAmount("Kuarqs", 0);
                            Events["ReviewDataEvent"].guiActive = true;
                        }
                    }
                    if (kuarqHalflife > 0)
                    {
                        var kuarqs = getResource("Kuarqs");
                        if (kuarqs != null && kuarqs.amount < (.99 * kuarqsRequired))
                        {
                            double delta = TimeWarp.fixedDeltaTime;
                            double decay = Math.Pow(.5, delta / kuarqHalflife);
                            kuarqDecay = (float)((kuarqs.amount * (1 - decay)) / delta);
                            kuarqs.amount = kuarqs.amount * decay;
                        }
                        else
                            kuarqDecay = 0;
                    }
                }
            }
        }

        private ModuleAnimateGeneric animator = null;

        public override void OnStart(StartState state)
        {
            base.OnStart(state);
            var animators = this.part.FindModulesImplementing<ModuleAnimateGeneric>();
            if (animators != null && animators.Count >= 1)
            {
                this.animator = animators[0];
                for(int i = 0; i < animator.Fields.Count; i++) {
                    if(animator.Fields[i] != null)
                        animator.Fields[i].guiActive = false;
                }
                /*for(int i = 0; i < animator.Actions.Count; i++) {
                    if(animator.Actions[i] != null)
                        animator.Actions[i].active = false;
                }
                for(int i = 0; i < animator.Events.Count; i++) {
                    if (animator.Events[i] != null)
                    {
                        animator.Events[i].guiActive = false;
                        animator.Events[i].active = false;
                    }
                }*/
            }
            this.capacity = 1;
            if (state == StartState.Editor) { return; }
            if (kuarqHalflife > 0)
                Fields["kuarqDecay"].guiActive = true;
            this.part.force_activate();
            updateLightsMode();
        }

        public override string GetInfo()
        {
            string ret = "";
            string reqCyclo = "";
            ret += "Improved Transmit Quality: <color=#22DD44>" + Math.Round(txValue * 100) + "%</color>";
            if (kuarqsRequired > 0)
            {
                ret += "\n\nKuarqs required: " + kuarqsRequired;
                double productionRequired = 0.01;
                if (kuarqHalflife > 0)
                {
                    ret += "\nKuarq decay halflife: " + kuarqHalflife + " seconds" + "\n";
                    productionRequired = kuarqsRequired * (1 - Math.Pow(.5, 1.0 / kuarqHalflife));
                    ret += String.Format("Production required: {0:F2} kuarq/s", productionRequired);
                }
                if (productionRequired > 1)
                    reqCyclo = "\n<color=#DD8800>Requires " + (Math.Ceiling(productionRequired)) + " D-ZZY Cyclotrons</color>";
                else
                    reqCyclo = "\n<color=#DD8800>Requires a D-ZZY Cyclotron</color>";
            }
            return ret + reqCyclo;
        }
    }
}