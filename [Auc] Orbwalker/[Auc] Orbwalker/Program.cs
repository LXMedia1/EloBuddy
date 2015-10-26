using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EloBuddy;
using EloBuddy.SDK;
using EloBuddy.SDK.Events;
using EloBuddy.SDK.Menu;
using EloBuddy.SDK.Menu.Values;

namespace _Auc__Orbwalker
{
	class Program
	{
		public static Menu Menu_Auc;
		static void Main(string[] args)
		{
			Loading.OnLoadingComplete += Load_Auc;
		}

		private static void Load_Auc(EventArgs args)
		{
			Menu_Auc = MainMenu.AddMenu("A--U--C", "AUC_Orbwalk_" + Player.Instance.ChampionName);
			Menu_Auc.AddLabel("AUC Orbwalker by Lekks",30);
			Menu_Auc.AddLabel("If you enable AUC Orbwalker  the Common Orbwalker will get Disabled.");
			Addons.Orbwalker.Initiate();
		}
	}
}
