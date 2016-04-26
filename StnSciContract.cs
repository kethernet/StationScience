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
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Contracts;
using Contracts.Parameters;
using KSP;
using KSPAchievements;

// Thanks to MrHappyFace for the intial code I used to figure out Contracts

namespace StationScience.Contracts
{
    public class StnSciContract : Contract, Parameters.PartRelated, Parameters.BodyRelated
    {
        CelestialBody targetBody = null;
        AvailablePart experimentType = null;

        public AvailablePart GetPartType()
        {
            return experimentType;
        }

        public CelestialBody GetBody()
        {
            return targetBody;
        }

        double value = 0;

        //private static System.Random random = new System.Random();

        static double GetUniform()
        {
            //return random.NextDouble();
            return UnityEngine.Random.value;
        }

        // Thanks to John D. Cook (johndcook.com) for this public domain random variate code

        // Get normal (Gaussian) random sample with specified mean and standard deviation
        static double GetNormal(double mean = 0.0, double standardDeviation = 1.0)
        {
            if (standardDeviation <= 0.0)
            {
                Debug.LogWarning("Invalid standard deviation: " + standardDeviation);
                return 0;
            }
              // Use Box-Muller algorithm
            double u1 = GetUniform();
            double u2 = GetUniform();
            double r = Math.Sqrt( -2.0*Math.Log(u1) );
            double theta = 2.0*Math.PI*u2;
            return mean + standardDeviation*r*Math.Sin(theta);
        }

        static double GetGamma(double shape, double scale)
        {
            // Implementation based on "A Simple Method for Generating Gamma Variables"
            // by George Marsaglia and Wai Wan Tsang.  ACM Transactions on Mathematical Software
            // Vol 26, No 3, September 2000, pages 363-372.

            double d, c, x, xsquared, v, u;

            if (shape >= 1.0)
            {
                d = shape - 1.0/3.0;
                c = 1.0/Math.Sqrt(9.0*d);
                for (;;)
                {
                    do
                    {
                        x = GetNormal();
                        v = 1.0 + c*x;
                    }
                    while (v <= 0.0);
                    v = v*v*v;
                    u = GetUniform();
                    xsquared = x*x;
                    if (u < 1.0 -.0331*xsquared*xsquared || Math.Log(u) < 0.5*xsquared + d*(1.0 - v + Math.Log(v)))
                        return scale*d*v;
                }
            }
            else if (shape <= 0.0)
            {
                Debug.LogWarning("Invalid Gamma shape: " + shape);
                return 0;
            }
            else
            {
                double g = GetGamma(shape+1.0, 1.0);
                double w = GetUniform();
                return scale*g*Math.Pow(w, 1.0/shape);
            }
        }

        bool AllUnlocked(HashSet<string> set)
        {
            foreach (string entry in set)
            {
                AvailablePart part = PartLoader.getPartInfoByName(entry);
                if (!(ResearchAndDevelopment.PartTechAvailable(part) && ResearchAndDevelopment.PartModelPurchased(part)))
                    return false; 
            }
            return true;
        }

        List<string> GetUnlockedExperiments()
        {
            List<string> ret = new List<string>();
            foreach (var exp in StnSciScenario.Instance.settings.experimentPrereqs)
            {
                if (AllUnlocked(exp.Value))
                    ret.Add(exp.Key);
            }
            return ret;
        }

        private class ContractCandidate
        {
            public string experiment;
            public CelestialBody body;
            public double value;
            public double weight;
        }

        protected override bool Generate()
        {
            if (ActiveCount() >= StnSciScenario.Instance.settings.maxContracts)
            {
                Debug.Log("StationScience contracts cap hit (" +
                    StnSciScenario.Instance.settings.maxContracts + ").");
                return false;
            }
            double xp = StnSciScenario.Instance.xp + Reputation.Instance.reputation * StnSciScenario.Instance.settings.reputationFactor;
            if (this.Prestige == ContractPrestige.Trivial)
                xp *= StnSciScenario.Instance.settings.trivialMultiplier;
            if (this.Prestige == ContractPrestige.Significant)
                xp *= StnSciScenario.Instance.settings.significantMultiplier;
            if (this.Prestige == ContractPrestige.Exceptional)
                xp *= StnSciScenario.Instance.settings.exceptionalMultiplier;
            if (xp <= 0.5)
                xp = 0.5;

            List<string> experiments = GetUnlockedExperiments();
            List<CelestialBody> bodies = GetBodies_Reached(true, false);

            List<ContractCandidate> candidates = new List<ContractCandidate>();
            double totalWeight = 0.0;

            //Get most difficult combination of planet and experiment that doesn't exceed random difficulty target
            foreach (var exp in experiments)
            {
                Debug.Log("Experiment: " + exp);
                double expValue;
                try
                {
                    expValue = StnSciScenario.Instance.settings.experimentChallenge[exp];
                }
                catch (KeyNotFoundException)
                {
                    continue;
                }
                foreach (var body in bodies)
                {
                    Debug.Log("Body: " + body.name);
                    int acount = ActiveCount(exp, body);
                    if (acount > 0)
                    {
                        Debug.Log("Contract already active!");
                        continue;
                    }
                    double plaValue;
                    try
                    {
                        plaValue = StnSciScenario.Instance.settings.planetChallenge[body.name];
                    }
                    catch (KeyNotFoundException)
                    {
                        continue;
                    }
                    ContractCandidate candidate = new ContractCandidate();
                    candidate.body = body;
                    candidate.experiment = exp;
                    candidate.value = expValue * plaValue;
                    /* log-gaussian function;
                     * when val equals xp, weight is 1
                     * when val is half of xp, weight is .5 */
                    candidate.weight = Math.Exp(-Math.Pow(Math.Log(candidate.value/xp,2),2)/
                                                         (2*Math.Pow(2/2.355,2)));
                    candidates.Add(candidate);
                    totalWeight += candidate.weight;
                }
            }
            Debug.Log("Candidate List: " + candidates.Count);
            double rand = GetUniform() * totalWeight;
            ContractCandidate chosen = null;
            foreach (var cand in candidates)
            {
                if (rand <= cand.weight)
                {
                    chosen = cand;
                    break;
                }
                rand -= cand.weight;
                    
            }

            if (chosen == null)
            {
                Debug.LogError("Couldn't find appropriate planet/experiment!");
                return false;
            }

            if (!SetExperiment(chosen.experiment))
                return false;
            targetBody = chosen.body;

            this.value = chosen.value;

            this.AddParameter(new Parameters.StnSciParameter(experimentType, targetBody), null);

            int ccount = CompletedCount(experimentType.name, targetBody);
            bool first_time = (ccount == 0);
            float v = (float)this.value;

            base.SetExpiry();

            float sciReward = StnSciScenario.Instance.settings.contractScience.calcReward(v, first_time);
            Debug.Log("SciReward: " + sciReward);
            base.SetScience(sciReward, targetBody);

            base.SetDeadlineYears(StnSciScenario.Instance.settings.contractDeadline.calcReward(v, first_time), targetBody);

            base.SetReputation(StnSciScenario.Instance.settings.contractReputation.calcReward(v, first_time),
                               StnSciScenario.Instance.settings.contractReputation.calcFailure(v, first_time), targetBody);

            base.SetFunds(StnSciScenario.Instance.settings.contractFunds.calcAdvance(v, first_time),
                          StnSciScenario.Instance.settings.contractFunds.calcReward(v, first_time),
                          StnSciScenario.Instance.settings.contractFunds.calcFailure(v, first_time), targetBody);
            return true;
        }

        private int ActiveCount(String exp = null, CelestialBody body = null)
        {
            int ret = 0;
            if (ContractSystem.Instance == null)
            {
                Debug.Log("ContractSystem Instance is null");
                return 0;
            }
            if (ContractSystem.Instance.Contracts == null)
            {
                Debug.Log("ContractSystem ContratsFinished is null");
                return 0;
            }
            foreach(Contract con in ContractSystem.Instance.Contracts)
            {
                StnSciContract sscon = con as StnSciContract;
                if (sscon != null && (sscon.ContractState == Contract.State.Active ||
                    sscon.ContractState == Contract.State.Offered) &&
                  (exp == null || sscon.experimentType != null) &&
                  (body == null || sscon.targetBody != null) &&
                  ((exp == null || exp == sscon.experimentType.name) &&
                   (body == null || body.theName == sscon.targetBody.theName)))
                    ret += 1;
            }
            return ret;
        }

        private int CompletedCount(String exp = null, CelestialBody body = null)
        {
            int ret = 0;
            if (ContractSystem.Instance == null)
            {
                Debug.Log("ContractSystem Instance is null");
                return 0;
            }
            if (ContractSystem.Instance.ContractsFinished == null)
            {
                Debug.Log("ContractSystem ContratsFinished is null");
                return 0;
            }
            foreach(Contract con in ContractSystem.Instance.ContractsFinished)
            {
                StnSciContract sscon = con as StnSciContract;
                if (sscon != null && sscon.ContractState == Contract.State.Completed &&
                  sscon.experimentType != null && sscon.targetBody != null &&
                  (exp == null || sscon.experimentType != null) &&
                  (body == null || sscon.targetBody != null) &&
                  ((exp == null || exp == sscon.experimentType.name) &&
                   (body == null || body.theName == sscon.targetBody.theName)))
                    ret += 1;
            }
            return ret;
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

        public override bool CanBeCancelled()
        {
            return true;
        }
        public override bool CanBeDeclined()
        {
            return true;
        }

        protected override string GetHashString()
        {
            return targetBody.bodyName + ":" + experimentType.name;
        }
        protected override string GetTitle()
        {
            return "Perform " + experimentType.title + " in orbit around " + targetBody.theName;
        }
        protected override string GetDescription()
        {
            //those 3 strings appear to do nothing
            return TextGen.GenerateBackStories(Agent.Name, Agent.GetMindsetString(), "experiment", "station", "kill all humans", new System.Random().Next());
        }
        protected override string GetSynopsys()
        {
            return "We need you to complete " + experimentType.title + " in orbit around " + targetBody.theName + ", and return it to Kerbin for recovery";
        }
        protected override string MessageCompleted()
        {
            return "You have successfully performed " + experimentType.title + " in orbit around " + targetBody.theName;
        }

        protected override void OnCompleted()
        {
            base.OnCompleted();
            StnSciScenario.Instance.xp += (float) this.value * StnSciScenario.Instance.settings.progressionFactor;
        }

        protected override void OnLoad(ConfigNode node)
        {
            string expID = node.GetValue("experimentType");
            SetExperiment(expID);
            string bodyID = node.GetValue("targetBody");
            SetTarget(bodyID);
            this.value = float.Parse(node.GetValue("value"));
        }
        protected override void OnSave(ConfigNode node)
        {
            string bodyID = targetBody.bodyName;
            node.AddValue("targetBody", bodyID);
            string expID = experimentType.name;
            node.AddValue("experimentType", expID);
            node.AddValue("value", (float)value);
        }

        bool IsPartUnlocked(string name)
        {
            AvailablePart part = PartLoader.getPartInfoByName(name);
            if (part != null && ResearchAndDevelopment.PartTechAvailable(part))
                return true;
            return false;
        }

        public override bool MeetRequirements()
        {

            CelestialBodySubtree progress = null;
            foreach (var node in ProgressTracking.Instance.celestialBodyNodes)
            {
                if (node.Body == Planetarium.fetch.Home)
                    progress = node;
            }
            if (progress == null)
            {
                Debug.LogError("ProgressNode for Kerbin not found, terminating");
                return false;
            }
            if(progress.orbit.IsComplete && 
                  ( IsPartUnlocked("dockingPort1") ||
                   IsPartUnlocked("dockingPort2") ||
                   IsPartUnlocked("dockingPort3") ||
                   IsPartUnlocked("dockingPortLarge") ||
                   IsPartUnlocked("dockingPortLateral"))
                  && (IsPartUnlocked("StnSciLab") || IsPartUnlocked("StnSciCyclo")))
                return true;
            return false;
        }
    }
}