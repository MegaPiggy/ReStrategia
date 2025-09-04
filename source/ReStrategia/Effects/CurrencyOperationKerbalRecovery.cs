using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;
using KSP;
using Strategies;
using Strategies.Effects;
using ContractConfigurator;

namespace ReStrategia
{
    /// <summary>
    /// Special CurrencyOperation that multiplies by the amount of XP gained by Kerbals.
    /// </summary>
    public class CurrencyOperationKerbalRecovery : StrategyEffect
    {
        Currency currency;
        float multiplier;

        public CurrencyOperationKerbalRecovery(Strategy parent)
            : base(parent)
        {
        }

        protected override string GetDescription()
        {
            return (multiplier > 0.0 ? "+" : "") + multiplier.ToString("F1") + " " + currency + " per experience point gained by astronauts (awarded on recovery).";
        }

        protected override void OnLoadFromConfig(ConfigNode node)
        {
            currency = ConfigNodeUtil.ParseValue<Currency>(node, "currency");
            multiplier = ConfigNodeUtil.ParseValue<float>(node, "multiplier");
        }

        protected override void OnRegister()
        {
            if (Parent.IsActive)
            {
                GameEvents.onKerbalStatusChange.Add(new EventData<ProtoCrewMember, ProtoCrewMember.RosterStatus, ProtoCrewMember.RosterStatus>.OnEvent(OnKerbalStatusChange));
            }
        }

        protected override void OnUnregister()
        {
            GameEvents.onKerbalStatusChange.Remove(new EventData<ProtoCrewMember, ProtoCrewMember.RosterStatus, ProtoCrewMember.RosterStatus>.OnEvent(OnKerbalStatusChange));
        }

        private void OnKerbalStatusChange(ProtoCrewMember pcm, ProtoCrewMember.RosterStatus oldStatus, ProtoCrewMember.RosterStatus newStatus)
        {
            if (oldStatus == ProtoCrewMember.RosterStatus.Assigned && newStatus == ProtoCrewMember.RosterStatus.Available)
            {
                FlightLog tmpLog = new FlightLog();
                foreach (FlightLog.Entry entry in pcm.careerLog.Entries.Union(pcm.flightLog.Entries))
                {
                    tmpLog.AddEntry(entry);
                }

                float xp = KerbalRoster.CalculateExperience(pcm.careerLog);
                float xp2 = KerbalRoster.CalculateExperience(tmpLog);

                float amount = (xp2 - xp) * multiplier;

                if (currency == Currency.Funds)
                {
                    Funding.Instance.AddFunds(amount, TransactionReasons.Strategies);
                }
                else if (currency == Currency.Reputation)
                {
                    Reputation.Instance.AddReputation(amount, TransactionReasons.Strategies);
                }
                else if (currency == Currency.Science)
                {
                    ResearchAndDevelopment.Instance.AddScience(amount, TransactionReasons.Strategies);
                }

                CurrencyPopup.Instance.AddPopup(currency, amount, TransactionReasons.Strategies, Parent.Config.Title, false);
            }
        }
    }
}
