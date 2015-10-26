using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EloBuddy;
using EloBuddy.SDK;

namespace _Auc__Orbwalker.Utility
{
	class MinionManager
	{
		public enum Type
		{
			Attackable,
			Enemy,
			Neutral,
			Friendly,
			All
		}
		internal static IEnumerable<Obj_AI_Minion> GetCreeps(float range, Type type = Type.All)
		{
			switch (type)
			{
				case Type.All:
					return EntityManager.MinionsAndMonsters.Combined.Where(
							o => ObjectManager.Player.Distance(o) <= range && o.IsValid && !o.IsDead);
				case Type.Attackable:
					return EntityManager.MinionsAndMonsters.Combined.Where(
							o => ObjectManager.Player.Distance(o) <= range && o.IsValid && !o.IsDead && !o.IsAlly);
				case Type.Enemy:
					return EntityManager.MinionsAndMonsters.Combined.Where(
							o => ObjectManager.Player.Distance(o) <= range && o.IsValid && !o.IsDead && o.IsEnemy);
				case Type.Neutral:
					return EntityManager.MinionsAndMonsters.Combined.Where(
							o => ObjectManager.Player.Distance(o) <= range && o.IsValid && !o.IsDead && o.IsMonster);
				case Type.Friendly:
					return EntityManager.MinionsAndMonsters.Combined.Where(
							o => ObjectManager.Player.Distance(o) <= range && o.IsValid && !o.IsDead && o.IsAlly);

			}
			return EntityManager.MinionsAndMonsters.Combined.Where(
							o => ObjectManager.Player.Distance(o) <= range && o.IsValid && !o.IsDead);
		}
	}
}
