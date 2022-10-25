using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SkydrmLocal.rmc.ui.utils
{
    class NameConvertHelper
    {
        public static string ConvertNameToAvatarText(string name, string rule)
        {
            name = name.Trim();
            if (string.IsNullOrEmpty(name))
            {
                return "";
            }
            string letter = "";
            if (string.Equals(" ", rule))
            {
                rule = "\\s+";
            }
            String[] split = System.Text.RegularExpressions.Regex.Split(name, rule);
            if (split.Length > 1)
            {

                letter = string.Concat(letter, split[0].Substring(0, 1).ToUpper());
                //fix bug 51464
                //letter = string.Concat(letter," ");
                letter = string.Concat(letter, split[split.Length - 1].Substring(0, 1).ToUpper());
            }
            else
            {
                letter = name.Substring(0, 1).ToUpper();
            }
            return letter;
        }

        public static string SelectionBackgroundColor(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return "#9D9FA2";
            }
            switch (name.Substring(0, 1).ToUpper())
            {
                case "A":
                    return "#DD212B";

                case "B":
                    return "#FDCB8A";

                case "C":
                    return "#98C44A";

                case "D":
                    return "#1A5279";

                case "E":
                    return "#EF6645";

                case "F":
                    return "#72CAC1";

                case "G":
                    return "#B7DCAF";

                case "H":
                    return "#705A9E";

                case "I":
                    return "#FCDA04";

                case "J":
                    return "#ED1D7C";

                case "K":
                    return "#F7AAA5";

                case "L":
                    return "#4AB9E6";

                case "M":
                    return "#603A18";

                case "N":
                    return "#88B8BC";

                case "O":
                    return "#ECA81E";

                case "P":
                    return "#DAACD0";

                case "Q":
                    return "#6D6E73";

                case "R":
                    return "#9D9FA2";

                case "S":
                    return "#B5E3EE";

                case "T":
                    return "#90633D";

                case "U":
                    return "#BDAE9E";

                case "V":
                    return "#C8B58E";

                case "W":
                    return "#F8BDD2";

                case "X":
                    return "#FED968";

                case "Y":
                    return "#F69679";

                case "Z":
                    return "#EE6769";

                case "0":
                    return "#D3E050";

                case "1":
                    return "#D8EBD5";

                case "2":
                    return "#F27EA9";

                case "3":
                    return "#1782C0";

                case "4":
                    return "#CDECF9";

                case "5":
                    return "#FDE9E6";

                case "6":
                    return "#FCED95";

                case "7":
                    return "#F99D21";

                case "8":
                    return "#F9A85D";

                case "9":
                    return "#BCE2D7";

                default:
                    return "#333333";
            }
        }

        public static string SelectionTextColor(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return "#ffffff";
            }
            switch (name.Substring(0, 1).ToUpper())
            {
                case "A":
                    return "#ffffff";

                case "B":
                    return "#8F9394";

                case "C":
                    return "#ffffff";

                case "D":
                    return "#ffffff";

                case "E":
                    return "#ffffff";

                case "F":
                    return "#ffffff";

                case "G":
                    return "#8F9394";

                case "H":
                    return "#ffffff";

                case "I":
                    return "#8F9394";

                case "J":
                    return "#ffffff";

                case "K":
                    return "#ffffff";

                case "L":
                    return "#ffffff";

                case "M":
                    return "#ffffff";

                case "N":
                    return "#ffffff";

                case "O":
                    return "#ffffff";

                case "P":
                    return "#ffffff";

                case "Q":
                    return "#ffffff";

                case "R":
                    return "#ffffff";

                case "S":
                    return "#ffffff";


                case "T":
                    return "#ffffff";


                case "U":
                    return "ffffff";


                case "V":
                    return "#ffffff";


                case "W":
                    return "#ffffff";


                case "X":
                    return "#8F9394";


                case "Y":
                    return "#ffffff";


                case "Z":
                    return "#ffffff";


                case "0":
                    return "#8F9394";


                case "1":
                    return "#8F9394";


                case "2":
                    return "#ffffff";


                case "3":
                    return "#ffffff";

                case "4":
                    return "#8F9394";


                case "5":
                    return "#8F9394";


                case "6":
                    return "#8F9394";

                case "7":
                    return "#ffffff";

                case "8":
                    return "#ffffff";

                case "9":
                    return "#8F9394";

                default:
                    return "#ffffff";

            }
        }

    }
}
