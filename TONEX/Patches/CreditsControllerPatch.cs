using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using static TONEX.Translator;

namespace TONEX;

[HarmonyPatch(typeof(CreditsController))]
public class CreditsControllerPatch
{
    private static List<CreditsController.CreditStruct> GetModCredits()
    {
        var devList = new List<string>()
            {
                //$"<color=#bd262a><size=150%>{GetString("FromChina")}</size></color>",

                $"喜 - {GetString("Creater")}",
                $"Slok7565 - {GetString("Collaborators")}",
                $"玖咪 - {GetString("PullRequester")}",

                $"caaattt - {GetString("Art")}",
                $"小黄 - {GetString("Art")}",

                $"Niko233 - {GetString("Contributor")}",
                $"天寸梦初 - {GetString("Contributor")}",

                $"Commandf1 - {GetString("Contributor")}",
                $"水木年华 - {GetString("Contributor")}",
                $"Lonnie - {GetString("Translator")}",
                $"SolarFlare - {GetString("Contributor")}",
                $"Mousse - {GetString("Contributor")}",
                 $"Moe - {GetString("Contributor")}",
                 $"Lonnie - {GetString("Contributor")}",
            };
        var translatorList = new List<string>()
            {
                $"Gurge44 - {GetString(StringNames.LangEnglish)}",
                $"SolarFlare - {GetString(StringNames.LangEnglish)}",
                $"Filipianosol - {GetString(StringNames.LangEnglish)}",

                $"Tommy-XL - {GetString(StringNames.LangEnglish)}&{GetString(StringNames.LangRussian)}",
                $"MogekoNik - {GetString(StringNames.LangEnglish)}&{GetString(StringNames.LangRussian)}",
                $"Антон (chill_ultimated) - {GetString(StringNames.LangRussian)}",
                $"Лагутин Виталий (lagutin1991) - {GetString(StringNames.LangRussian)}",

                $"阿龍 - {GetString("LangTChinese")}",
                $"法官 - {GetString("LangTChinese")}",

                $"DopzyGamer - {GetString(StringNames.LangBrazPort)}",
            };
        var acList = new List<string>()
            {
                //Mods
                $"{GetString("TownOfHost")}",
                $"{GetString("TownOfHost_Y")}",
                $"{GetString("TownOfHost-TheOtherRoles")}",
                $"{GetString("SuperNewRoles")}",
                $"{GetString("Project-Lotus")}",

                // Sponsor
                $"罗寄",
                $"鬼",
                $"喜",
                $"小叨院长",
                $"波奇酱",
                $"法师",
                $"沐煊",
                $"SolarFlare",
                $"侠客",
                $"林林林",
                $"撒币",
                $"斯卡蒂Skadi",
                $"ltemten",
                $"Night_瓜",
                $"群诱饵",
                $"Slok",
                $"辣鸡",
                $"湛蓝色",
                $"小黄117",
                $"chun",
                $"Z某",
                $"Shark",
                $"清风awa",
                $"1 1 1 1",

                //Discord Server Booster
                $"bunny",
                $"Loonie",
                $"Namra",
                $"KNIGHT",
                $"SolarFlare",
                $"Bluéfôx.",
                $"shiftyrose",
                $"M ™",
                $"yunfi",

                $"...",
            };

        var credits = new List<CreditsController.CreditStruct>();

        AddTitleToCredits(Utils.ColorString(Main.ModColor32, Main.ModName));
        AddPersonToCredits(devList);
        AddSpcaeToCredits();

        AddTitleToCredits(GetString("Translator"));
        AddPersonToCredits(translatorList);
        AddSpcaeToCredits();

        AddTitleToCredits(GetString("Acknowledgement"));
        AddPersonToCredits(acList);
        AddSpcaeToCredits();

        return credits;

        void AddSpcaeToCredits()
        {
            AddTitleToCredits(string.Empty);
        }
        void AddTitleToCredits(string title)
        {
            credits.Add(new()
            {
                format = "title",
                columns = new[] { title },
            });
        }
        void AddPersonToCredits(List<string> list)
        {
            foreach (var line in list)
            {
                var cols = line.Split(" - ").ToList();
                if (cols.Count < 2) cols.Add(string.Empty);
                credits.Add(new()
                {
                    format = "person",
                    columns = cols.ToArray(),
                });
            }
        }
    }

    [HarmonyPatch(nameof(CreditsController.AddCredit)), HarmonyPrefix]
    public static void AddCreditPrefix(CreditsController __instance, [HarmonyArgument(0)] CreditsController.CreditStruct originalCredit)
    {
        if (originalCredit.columns[0] != "logoImage") return;

        foreach (var credit in GetModCredits())
        {
            __instance.AddCredit(credit);
            __instance.AddFormat(credit.format);
        }
    }
}