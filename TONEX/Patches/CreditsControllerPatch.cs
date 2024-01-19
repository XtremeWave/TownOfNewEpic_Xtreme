using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using static Il2CppSystem.Net.Http.Headers.Parser;
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
                //XtremeWave
                "Team-XtremeWave",
                $"喜 - {GetString("Collaborators")}",
                $"Slok7565 - {GetString("Collaborators")}",
                $"Zwyan - {GetString("Collaborators")}",
                $"玖咪 - {GetString("PullRequester")}",
                $"杰慕斯 - {GetString("PullRequester")}",
                $"caaattt - {GetString("Art")}",
                $"小黄117 - {GetString("Art")}",
                $"QingFeng - {GetString("PullRequester")}",
                $"中立小黑 - {GetString("PullRequester")}",

                //Others
                "Others",
                $"KARPED1EM - {GetString("Creater")}",
                $"Niko233 - {GetString("Contributor")}",
                $"Moe - {GetString("Contributor")}",
                $"ryuk - {GetString("Contributor")}",
                $"Gurge44 - {GetString("Contributor")}",
                $"水木年华 - {GetString("Contributor")}",
                $"Lonnie - {GetString("Contributor")}",
                $"Night_瓜 - {GetString("Contributor")}",
                $"天寸梦初(in TONX) - {GetString("Contributor")}",
                $"Commandf1(in TONX) - {GetString("Contributor")}",
                $"SolarFlare(in TONX) - {GetString("Contributor")}",
                $"Mousse(in TONX) - {GetString("Contributor")}",
            };
        var translatorList = new List<string>()
            {
                $"Gurge44(in TONX) - {GetString(StringNames.LangEnglish)}",
                $"SolarFlare(in TONX) - {GetString(StringNames.LangEnglish)}",
                $"Filipianosol(in TONX) - {GetString(StringNames.LangEnglish)}",

                $"Tommy-XL(in TONX) - {GetString(StringNames.LangEnglish)}&{GetString(StringNames.LangRussian)}",
                $"MogekoNik(in TONX) - {GetString(StringNames.LangEnglish)}&{GetString(StringNames.LangRussian)}",
                $"Антон (chill_ultimated)(in TONX) - {GetString(StringNames.LangRussian)}",
                $"Лагутин Виталий (lagutin1991)(in TONX) - {GetString(StringNames.LangRussian)}",

                $"阿龍(in TONX) - {GetString("LangTChinese")}",
                $"法官(in TONX) - {GetString("LangTChinese")}",

                $"DopzyGamer(in TONX) - {GetString(StringNames.LangBrazPort)}",
            };
        var acList = new List<string>()
            {
                //Mods
                $"{GetString("TownOfHost")}",
                $"{GetString("TownOfNext")}",
                $"{GetString("TownOfHost_Y")}",
                $"{GetString("TownOfHost-TheOtherRoles")}",
                $"{GetString("SuperNewRoles")}",
                $"{GetString("TownOfHostRe-Edited")}",
                $"{GetString("TownOfHostEnhanced")}",
                $"{GetString("TownOfHosEdited_PLUS")}",
                $"{GetString("TownOfHosEdited_Niko")}",
                $"{GetString("To_Hope")}",
                $"{GetString("Project-Lotus")}",

                // Sponsor

                //Discord Server Booster
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