using HarmonyLib;
using System.Collections.Generic;
using UnityEngine;
using static TONEX.NameTagManager;
using static TONEX.Translator;

namespace TONEX;

public static class InternalNameTags
{
    public static IReadOnlyDictionary<string, NameTag> Get()
    {
        var tags = GetAll();
        tags.Do(t => t.Value.Isinternal = true);
        return tags;
    }
    private static Dictionary<string, NameTag> GetAll() => new()
    {
        {
            "actorour#0029", //咔哥
            new()
            {
                UpperText = new()
                {
                    Text = $"∞ {GetString("Creater")} ∞",
                    Gradient = new(new Color32(198, 255, 221, 255), new Color32(251, 215, 134, 255), new Color32(247, 121, 125, 255)),
                    SizePercentage = 80
                },
                Prefix = new()
                {
                    Text = "✿",
                    TextColor = new Color32(246, 79, 89, 255)
                },
                Suffix = new()
                {
                    Text = "✿",
                    TextColor = new Color32(18, 194, 233, 255)
                },
                Name = new()
                {
                    Gradient = new(new Color32(18, 194, 233, 255), new Color32(196, 113, 237, 255), new Color32(246, 79, 89, 255)),
                    SizePercentage = 90
                }
            }
        },
        {
            "pinklaze#1776", //NCM
            new()
            {
                UpperText = new()
                {
                    Text = GetString("PullRequester"),
                    TextColor = new Color32(255, 192, 203, 255),
                    SizePercentage = 80
                }
            }
        },
        {
            "keepchirpy#6354", //Tommy-XL
            new()
            {
                UpperText = new()
                {
                    Text = $"Переводчик",
                    TextColor = new(31, 243, 198, 255),
                    SizePercentage = 80
                }
            }
        },
        {
            "taskunsold#2701", //Tem
            new()
            {
                UpperText = new()
                {
                    Text = $"Temmie",
                    Gradient = new(new Color32(66, 103, 152, 255), new Color32(246, 229, 9, 255)),
                    SizePercentage = 80
                }
            }
        },
        {
            "timedapper#9496", //阿龍
            new()
            {
                UpperText = new()
                {
                    Text = $"阿龍",
                    TextColor = new Color32(72, 255, 255, 255),
                    SizePercentage = 80
                }
            }
        },
        {
            "keyscreech#2151", //Endrmen40409
            new()
            {
                UpperText = new()
                {
                    Text = $"美術NotKomi",
                    Gradient = new(new Color32(211, 164, 255, 255), new Color32(90, 90, 173, 255)),
                    SizePercentage = 80
                }
            }
        },
        {
            "neatnet#5851", //Gurge44
            new()
            {
                UpperText = new()
                {
                    Text = $"The 200IQ guy",
                    TextColor = new Color32(255, 255, 0, 255),
                    SizePercentage = 80
                }
            }
        },
        {
            "contenthue#0404", //Gurge44
            new()
            {
                UpperText = new()
                {
                    Text = $"The 200IQ guy",
                    TextColor = new Color32(255, 255, 0, 255),
                    SizePercentage = 80
                }
            }
        },
        {
            "heavyclod#2286", //小叨
            new()
            {
                UpperText = new()
                {
                    Text = $"小叨.exe已停止运行",
                    TextColor = new Color32(255, 255, 0, 255),
                    SizePercentage = 80
                }
            }
        },
        {
            "storeroan#0331", //Night_瓜
            new()
            {
                UpperText = new()
                {
                    Text = $"Night_瓜",
                    TextColor = new Color32(255, 0, 102, 255),
                    SizePercentage = 80
                }
            }
        },
        {
            "teamelder#5856", //Slok7565
            new()
            {
                UpperText = new()
                {
                    Text = $"XtremeWave_Dev_Slok",
                    TextColor = new Color32(205, 255, 253, 255),
                    SizePercentage = 80
                },
                Prefix = new()
                {
                    Text = "★",
                    TextColor = new Color32(230, 173, 10, 255)
                },
                Suffix = new()
                {
                    Text = "★",
                    TextColor = new Color32(230, 173, 10, 255)
                },
                Name = new()
                {
                    Gradient = new(new Color32(0, 255, 255, 255), new Color32(205, 255, 253, 255)),
                    SizePercentage = 90
                }
            }
        },
        {
            "recentduct#6068", //法师
            new()
            {
                UpperText = new()
                {
                    Text = $"高冷男模法师",
                    TextColor = new Color32(255, 0, 255, 255),
                    SizePercentage = 80
                }
            }
        },
        {
            "canneddrum#2370", //喜
            new()
            {
                UpperText = new()
                {
                    Text = $"我是喜唉awa",
                    TextColor = new Color32(255, 252, 190, 255),
                    SizePercentage = 80
                }
            }
        },
        {
            "dovefitted#5329", //
            new()
            {
                UpperText = new()
                {
                    Text = $"不要首刀我",
                    TextColor = new Color32(19, 121, 191, 255),
                    SizePercentage = 80
                }
            }
        },
        {
            "luckylogo#7352", //林@林
            new()
            {
                UpperText = new()
                {
                    Text = $"林@林",
                    TextColor = new Color32(243, 0, 0, 255),
                    SizePercentage = 80
                }
            }
        },
        {
            "axefitful#8788", //罗寄
            new()
            {
                UpperText = new()
                {
                    Text = $"寄才是真理",
                    TextColor = new Color32(142, 129, 113, 255),
                    SizePercentage = 80
                }
            }
        },
        {
            "raftzonal#8893", //罗寄
            new()
            {
                UpperText = new()
                {
                    Text = $"寄才是真理",
                    TextColor = new Color32(142, 129, 113, 255),
                    SizePercentage = 80
                }
            }
        },
        {
            "twainrobin#8089", //
            new()
            {
                UpperText = new()
                {
                    Text = $"啊哈修maker",
                    TextColor = new Color32(0, 0, 255, 255),
                    SizePercentage = 80
                }
            }
        },
        {
            "mallcasual#6075", //波奇
            new()
            {
                UpperText = new()
                {
                    Text = $"波奇酱",
                    TextColor = new Color32(248, 156, 203, 255),
                    SizePercentage = 80
                }
            }
        },
        {
            "beamelfin#9478", // 1 1 1 1
            new()
            {
                UpperText = new()
                {
                    Text = $"Amaster-1111",
                    TextColor = new Color32(100, 149, 237, 255),
                    SizePercentage = 80
                }
            }
        },
        {
            "lordcosy#8966", //K
            new()
            {
                UpperText = new()
                {
                    Text = $"HostTONX",
                    TextColor = new Color32(255, 214, 236, 255),
                    SizePercentage = 80
                }
            }
        },
        {
            "honestsofa#2870", //SolarFlare
            new()
            {
                UpperText = new()
                {
                    Text = $"Sylveon",
                    TextColor = new Color32(211, 129, 217, 255),
                    SizePercentage = 80
                }
            }
        },
        {
            "caseeast#7194", //laikrai
            new()
            {
                UpperText = new()
                {
                    Text = $"disc.gg/maul",
                    TextColor = new Color32(28, 36, 81, 255),
                    SizePercentage = 80
                }
            }
        },
        //TONEX&TOHEX
        {
            "squishyhod#5187", //野生的猴子，TONEX
            new()
            {
                UpperText = new()
                {
                    Text = $"白色游戏",
                    Gradient = new(new Color32(255, 215, 0, 255), new Color32(211, 211, 211, 255)),
                    SizePercentage = 80
                }
            }
        },
        {
            "actorcoy#7049", //橄哥，TONEX
            new()
            {
                UpperText = new()
                {
                    Text = $"橄哥",
                    Gradient = new(new Color32(23, 26, 236, 255), new Color32(224, 23, 236, 255), new Color32(236, 135, 23, 255)),
                    SizePercentage = 80
                }
            }
        },
        {
            "annualday#0075", //橄哥，TONEX
            new()
            {
                UpperText = new()
                {
                    Text = $"橄哥",
                    Gradient = new(new Color32(23, 26, 236, 255), new Color32(224, 23, 236, 255), new Color32(236, 135, 23, 255)),
                    SizePercentage = 80
                }
            }
        },
        {
            "closegrub#6217", //警长不会玩，TONEX
            new()
            {
                UpperText = new()
                {
                    Text = $"赞助者",
                    Gradient = new(new Color32(127, 201, 172, 255), new Color32(219, 229, 198, 255)),
                    SizePercentage = 80
                }
            }
        },
        {
            "muckleeach#4046", //清风，TONEX
            new()
            {
                UpperText = new()
                {
                    Text = $"FengQing",
                    Gradient = new(new Color32(129,169,233, 255), new Color32(215,244,240, 255), new Color32(43,143,137, 255)),
                    SizePercentage = 80
                }
            }
        },
        {
            "smartlatex#9383", //NotOrange，TONEX
            new()
            {
                UpperText = new()
                {
                    Text = $"不是橙色",
                   TextColor = new Color32(239,127,70, 255),
                    SizePercentage = 80
                }
            }
        },
        {
            "lacesome#2271", //株式会社，TONEX
            new()
            {
                UpperText = new()
                {
                    Text = $"九天弘教普济生灵掌阴阳功过大道思仁紫极仙翁一阳真人元虚玄应开化伏魔忠孝帝君太上大罗天仙紫极长生圣智昭灵统三元证应玉虚总掌五雷大真人玄都境万寿帝君",
                    Gradient = new(new Color32(255,215,0, 255), new Color32(26,244,137, 255), new Color32(109,223,198, 255)),
                    SizePercentage = 80
                }
            }
        },
        {
            "veryscarf#5368", //小武，TONEX
            new()
            {
                UpperText = new()
                {
                    Text = $"跪求三连",
                    Gradient = new(new Color32(255,128,0, 255), new Color32(255,0,0, 255)),
                    SizePercentage = 80
                }
            }
        },
        {
            "hoppypuree#2528", //水³，TONEX
            new()
            {
                UpperText = new()
                {
                    Text = $"水³",
                    Gradient = new(new Color32(204, 255, 255, 255), new Color32(0, 153, 204, 255)),
                    SizePercentage = 80
                }
            }
        },
        {
            "caretholy#1519", //一只小黑懋，TONEX
            new()
            {
                UpperText = new()
                {
                    Text = $"可爱 色欲 技术 绘画于一身的小猫",
                    Gradient = new(new Color32(171,253,255,255), new Color32(88, 252, 255, 255), new Color32(1, 250, 255, 255)),
                    SizePercentage = 80
                }
            }
        },
        {
            "elfalpha#5174", //心语，TONEX
            new()
            {
                UpperText = new()
                {
                    Text = $"xindyu",
                    TextColor = new Color32(255, 192, 203, 255),
                    SizePercentage = 80
                }
            }
        },
        {
            "shystripe#2541", //心语，TONEX
            new()
            {
                UpperText = new()
                {
                    Text = $"xindyu",
                    TextColor = new Color32(255, 192, 203, 255),
                    SizePercentage = 80
                }
            }
        },
    };
}