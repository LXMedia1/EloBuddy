using System.Collections.Generic;
using System.Linq;
using EloBuddy;

namespace _Auc__Orbwalker.Utility
{
	class Interrupt
	{
		public enum State
		{
			None,
			CanMove,
			CanNothing
		}
		public enum DangerLevel
		{
			Low,
			Medium,
			High
		}

		public static List<InterruptableSpells> InterruptableSpellList = GetList();

		private static List<InterruptableSpells> GetList()
		{
			if (InterruptableSpellList != null) return InterruptableSpellList;
			var list = new List<InterruptableSpells>();
			list.Add(new InterruptableSpells("Caitlyn", SpellSlot.R, DangerLevel.High));
			list.Add(new InterruptableSpells("FiddleSticks", SpellSlot.W, DangerLevel.Medium));
			list.Add(new InterruptableSpells("FiddleSticks", SpellSlot.R, DangerLevel.High));
			list.Add(new InterruptableSpells("Galio", SpellSlot.R, DangerLevel.High));
			list.Add(new InterruptableSpells("Janna", SpellSlot.R, DangerLevel.Low));
			list.Add(new InterruptableSpells("Karthus", SpellSlot.R, DangerLevel.High));
			list.Add(new InterruptableSpells("Katarina", SpellSlot.R, DangerLevel.High));
			list.Add(new InterruptableSpells("Lucian", SpellSlot.R, DangerLevel.High, true));
			list.Add(new InterruptableSpells("Malzahar", SpellSlot.R, DangerLevel.High));
			list.Add(new InterruptableSpells("MasterYi",SpellSlot.W, DangerLevel.Low));
			list.Add(new InterruptableSpells("MissFortune", SpellSlot.R, DangerLevel.High));
			list.Add(new InterruptableSpells("Nunu", SpellSlot.R, DangerLevel.High));
			list.Add(new InterruptableSpells("Pantheon", SpellSlot.E, DangerLevel.Low));
			list.Add(new InterruptableSpells("Pantheon", SpellSlot.R, DangerLevel.High));
			list.Add(new InterruptableSpells("RekSai", SpellSlot.R, DangerLevel.High));
			list.Add(new InterruptableSpells("Sion", SpellSlot.R, DangerLevel.Low));
			list.Add(new InterruptableSpells("Shen", SpellSlot.R, DangerLevel.Low));
			list.Add(new InterruptableSpells("TwistedFate", SpellSlot.R, DangerLevel.Medium));
			list.Add(new InterruptableSpells("Urgot", SpellSlot.R, DangerLevel.High));
			list.Add(new InterruptableSpells("Varus", SpellSlot.Q, DangerLevel.Low, true));
			list.Add(new InterruptableSpells("Velkoz", SpellSlot.R, DangerLevel.High));
			list.Add(new InterruptableSpells("Warwick", SpellSlot.R, DangerLevel.High));
			list.Add(new InterruptableSpells("Xerath", SpellSlot.R, DangerLevel.High));
			InterruptableSpellList = list;
			return list;
		}

		internal static State IsCastingInterruptableSpell(AIHeroClient hero)
		{
			return (from interruptableSpell in InterruptableSpellList 
					where interruptableSpell.ChampionName == hero.ChampionName 
					where hero.Spellbook.IsCastingSpell || hero.Spellbook.IsChanneling || hero.Spellbook.IsCharging 
					where hero.Spellbook.ActiveSpellSlot == interruptableSpell.Spellslot 
					where hero.Spellbook.CastEndTime < Game.Time 
					select interruptableSpell).Select(interruptableSpell => interruptableSpell.CanMove ? State.CanMove : State.CanNothing).FirstOrDefault();
		}
	}

	class InterruptableSpells
	{
		public string ChampionName;
		public SpellSlot Spellslot;
		public Interrupt.DangerLevel DangerLevel;
		public bool CanMove;
		public InterruptableSpells(string champname, SpellSlot spellSlot, Interrupt.DangerLevel dangerlevel, bool canmove = false)
		{
			ChampionName = champname;
			Spellslot = spellSlot;
			DangerLevel = dangerlevel;
			CanMove = canmove;
		}

	}
}
